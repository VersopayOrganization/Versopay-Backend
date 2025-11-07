// Dtos/Common/VexyVendorWebhookHandler.cs
using System.Text.Json;
using VersopayBackend.Dtos.VexyBank;
using VersopayBackend.Repositories;
using VersopayBackend.Repositories.VexyClient.PixIn;
using VersopayBackend.Utils;
using VersopayBackend.Services;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Dtos.Common
{
    public sealed class VexyVendorWebhookHandler : IVendorWebhookHandler
    {
        private readonly IProviderCredentialRepository _credRepo;
        private readonly IVexyBankPixInRepository _pixInRepo;
        private readonly IPedidoRepository _pedidoRepo;
        private readonly IPedidoMatchRepository _pedidoMatchRepo;
        private readonly ITransferenciaRepository _transferRepo;
        private readonly ITransferenciaMatchRepository _transferMatchRepo;
        private readonly IPedidosService _pedidosService;
        private readonly ITransferenciasService _transferService;
        private readonly ILogger<VexyVendorWebhookHandler> _logger;

        public VexyVendorWebhookHandler(
            IProviderCredentialRepository credRepo,
            IVexyBankPixInRepository pixInRepo,
            IPedidoRepository pedidoRepo,
            IPedidoMatchRepository pedidoMatchRepo,
            ITransferenciaRepository transferRepo,
            ITransferenciaMatchRepository transferMatchRepo,
            IPedidosService pedidosService,
            ITransferenciasService transferService,
            ILogger<VexyVendorWebhookHandler> logger)
        {
            _credRepo = credRepo;
            _pixInRepo = pixInRepo;
            _pedidoRepo = pedidoRepo;
            _pedidoMatchRepo = pedidoMatchRepo;
            _transferRepo = transferRepo;
            _transferMatchRepo = transferMatchRepo;
            _pedidosService = pedidosService;
            _transferService = transferService;
            _logger = logger;
        }

        public async Task<bool> VerifySignatureAsync(int ownerUserId, string rawBody, IDictionary<string, string> headers, CancellationToken ct)
        {
            // Busca o segredo do webhook da Vexy para ESTE owner.
            var secret = await _credRepo.GetWebhookSecretAsync(ownerUserId, PaymentProvider.Vexy, ct);

            // Se não houver secret, em DEV você pode aceitar (retorne true). Em PROD: exija.
            if (string.IsNullOrWhiteSpace(secret))
            {
                _logger.LogWarning("Vexy webhook sem secret cadastrado (OwnerUserId={Owner}). Aceitando por configuração.", ownerUserId);
                return true;
            }

            var sig = headers.TryGetValue("Vexy-Signature", out var v1) ? v1
                    : headers.TryGetValue("vexy-signature", out var v2) ? v2
                    : null;

            return VexySignatureVerifier.Verify(sig, rawBody, secret, timestampToleranceSeconds: 300);
        }

        public string BuildEventKey(string rawBody, IDictionary<string, string> headers)
        {
            // Usa o envelope da Vexy para montar chave determinística
            var env = JsonSerializer.Deserialize<VexyWebhookEnvelope>(rawBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            if (env is null) return $"vexy:invalid:{CryptoUtils.Sha256Base64(rawBody)}";
            return $"vexy:{env.Type}:{env.Event}:{env.Id}".ToLowerInvariant();
        }

        public async Task<InboundWebhookLog> ProcessAsync(
            int ownerUserId,
            string channel,
            string rawBody,
            IDictionary<string, string> headers,
            string sourceIp,
            CancellationToken ct)
        {
            var env = JsonSerializer.Deserialize<VexyWebhookEnvelope>(rawBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                      ?? throw new InvalidOperationException("Payload Vexy inválido.");

            var now = DateTime.UtcNow;
            var log = new InboundWebhookLog
            {
                Provedor = ProvedorWebhook.VexyPayments,
                Evento = env.Type?.Equals("transaction", StringComparison.OrdinalIgnoreCase) == true
                            ? WebhookEvento.PagamentoPIX
                            : WebhookEvento.TransferenciaPIX,
                EventKey = $"vexy:{ownerUserId}:{env.Type}:{env.Event}:{env.Id}".ToLowerInvariant(),
                SourceIp = sourceIp,
                HeadersJson = JsonSerializer.Serialize(headers),
                PayloadJson = rawBody,
                ReceivedAtUtc = now,
                ProcessedAtUtc = now,
                ProcessingStatus = ProcessingStatus.Success,
                TransactionId = env.Transaction?.Id ?? env.Transfer?.Id,
                Status = env.Transaction?.Status ?? env.Transfer?.Status,
                DataEventoUtc = now
            };

            // ---------------- PIX-IN (transaction) ----------------
            if (env.Type?.Equals("transaction", StringComparison.OrdinalIgnoreCase) == true &&
                env.Transaction is not null &&
                !string.IsNullOrWhiteSpace(env.Transaction.Id))
            {
                var tx = env.Transaction;
                var extId = tx.Id!;
                var status = (tx.Status ?? "").ToLowerInvariant();
                var amount = (decimal)tx.Amount / 100m; // teu DTO tem Amount (int) em centavos
                var descricao = $"PIX {extId}";
                var payerDoc = tx.Pix?.PayerInfo?.Document ?? tx.PayerDocument;

                // 1) Upsert VexyBankPixIn
                var localPix = await _pixInRepo.FindByExternalIdAsync(ownerUserId, extId, ct);
                if (localPix is null)
                {
                    localPix = new VexyBankPixIn
                    {
                        OwnerUserId = ownerUserId,
                        ExternalId = extId,
                        Status = status,
                        AmountCents = tx.Amount,
                        PayerDocument = payerDoc,
                        UpdatedAtUtc = now
                    };
                    await _pixInRepo.AddAsync(localPix, ct);
                    await _pixInRepo.SaveChangesAsync(ct);
                }
                else
                {
                    localPix.Status = status;
                    if (!string.IsNullOrWhiteSpace(payerDoc)) localPix.PayerDocument = payerDoc;
                    if (status is "paid" or "completed") localPix.PaidAtUtc = now;
                    localPix.UpdatedAtUtc = now;
                    await _pixInRepo.SaveChangesAsync(ct);
                }

                // 2) Atualiza/Cria Pedido
                var pedidoResolvido = false;

                // 2.1) Se o PixIn já aponta para um Pedido
                if (localPix.PedidoId.HasValue && localPix.PedidoId.Value > 0)
                {
                    var ped = await _pedidoRepo.FindByIdAsync(localPix.PedidoId.Value, ct);
                    if (ped is not null)
                    {
                        ped.Provider = PaymentProvider.Vexy;
                        ped.GatewayTransactionId ??= extId;

                        if (status is "paid" or "completed")
                        {
                            ped.Status = StatusPedido.Aprovado;
                            ped.DataPagamento ??= now;
                        }
                        else if (status is "expired")
                        {
                            ped.Status = StatusPedido.Expirado;
                        }
                        else if (status is "canceled")
                        {
                            ped.Status = StatusPedido.Cancelado;
                        }

                        await _pedidoRepo.SaveChangesAsync(ct);
                        pedidoResolvido = true;
                    }
                }

                // 2.2) Fallback: procura por GatewayTransactionId
                if (!pedidoResolvido)
                {
                    var ped = await _pedidoMatchRepo.GetByGatewayIdAsync(extId, ct);
                    if (ped is not null)
                    {
                        ped.Provider = PaymentProvider.Vexy;

                        if (status is "paid" or "completed")
                        {
                            ped.Status = StatusPedido.Aprovado;
                            ped.DataPagamento ??= now;
                        }
                        else if (status is "expired")
                        {
                            ped.Status = StatusPedido.Expirado;
                        }
                        else if (status is "canceled")
                        {
                            ped.Status = StatusPedido.Cancelado;
                        }

                        await _pedidoRepo.SaveChangesAsync(ct);
                        pedidoResolvido = true;

                        if (!localPix.PedidoId.HasValue || localPix.PedidoId.Value <= 0)
                        {
                            localPix.PedidoId = ped.Id;
                            localPix.UpdatedAtUtc = now;
                            await _pixInRepo.SaveChangesAsync(ct);
                        }
                    }
                }

                // 2.3) Ainda não existe? Cria via serviço (boa prática)
                if (!pedidoResolvido)
                {
                    var novoDto = await _pedidosService.CreateAsync(new PedidoCreateDto
                    {
                        VendedorId = ownerUserId,
                        Valor = amount,
                        MetodoPagamento = "Pix",
                        Produto = descricao
                    }, ct);

                    var novo = await _pedidoRepo.FindByIdAsync(novoDto.Id, ct);
                    if (novo is not null)
                    {
                        novo.Provider = PaymentProvider.Vexy;
                        novo.GatewayTransactionId = extId;

                        if (status is "paid" or "completed")
                        {
                            novo.Status = StatusPedido.Aprovado;
                            novo.DataPagamento ??= now;
                        }
                        else if (status is "expired")
                        {
                            novo.Status = StatusPedido.Expirado;
                        }
                        else if (status is "canceled")
                        {
                            novo.Status = StatusPedido.Cancelado;
                        }

                        await _pedidoRepo.SaveChangesAsync(ct);
                    }

                    localPix.PedidoId = novoDto.Id;
                    localPix.UpdatedAtUtc = now;
                    await _pixInRepo.SaveChangesAsync(ct);
                }
            }

            // ---------------- PIX-OUT (transfer) ----------------
            if (env.Type?.Equals("transfer", StringComparison.OrdinalIgnoreCase) == true &&
                env.Transfer is not null &&
                !string.IsNullOrWhiteSpace(env.Transfer.Id))
            {
                var tr = env.Transfer;
                var gwId = tr.Id!;
                var status = (tr.Status ?? "").ToLowerInvariant();
                var amount = (decimal)tr.AmountInCents / 100m;

                var t = await _transferMatchRepo.GetByGatewayIdAsync(gwId, ct);
                if (t is not null)
                {
                    t.Provider = PaymentProvider.Vexy;

                    if (status is "completed" or "paid")
                    {
                        t.Status = StatusTransferencia.Concluido;
                        t.Aprovacao = AprovacaoManual.Aprovado;
                        t.DataAprovacao ??= now;
                    }
                    else if (status is "med" or "held")
                    {
                        t.Status = StatusTransferencia.RetidoMed;
                    }
                    else if (status is "canceled" or "failed")
                    {
                        t.Status = StatusTransferencia.Cancelado;
                        t.Aprovacao = AprovacaoManual.Reprovado;
                    }

                    await _transferRepo.SaveChangesAsync(ct);
                }
                else
                {
                    // Se preferir criar sempre via service:
                    var criado = await _transferService.CreateAsync(new TransferenciaCreateDto
                    {
                        SolicitanteId = ownerUserId,
                        ValorSolicitado = amount,
                        ChavePix = null
                    }, ct);

                    var nova = await _transferRepo.FindByIdAsync(criado.Id, ct);
                    if (nova is not null)
                    {
                        nova.Provider = PaymentProvider.Vexy;
                        nova.GatewayTransactionId = gwId;

                        if (status is "completed" or "paid")
                        {
                            nova.Status = StatusTransferencia.Concluido;
                            nova.Aprovacao = AprovacaoManual.Aprovado;
                            nova.DataAprovacao ??= now;
                        }
                        else if (status is "med" or "held")
                        {
                            nova.Status = StatusTransferencia.RetidoMed;
                        }
                        else if (status is "canceled" or "failed")
                        {
                            nova.Status = StatusTransferencia.Cancelado;
                            nova.Aprovacao = AprovacaoManual.Reprovado;
                        }

                        await _transferRepo.SaveChangesAsync(ct);
                    }
                }
            }

            return log;
        }
    }
}
