using System.Text.Json;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Services
{
    public sealed class InboundWebhookService(
        IInboundWebhookLogRepository logRepository,
        VersopayBackend.Repositories.IPedidoMatchRepository pedidoRepository,
        VersopayBackend.Repositories.ITransferenciaMatchRepository transferenciaRepository,
        IProviderCredentialRepository providerCredRepo
    ) : IInboundWebhookService
    {
        public async Task<ProcessingStatus> HandleVersellAsync(
            VersellWebhookDto dto, string sourceIp, IDictionary<string, string> headers, CancellationToken ct)
        {
            // Autentica por usuário: headers vspi/vsps
            if (!headers.TryGetValue("vspi", out var vspi) || !headers.TryGetValue("vsps", out var vsps))
                return ProcessingStatus.InvalidAuth;

            var cred = await providerCredRepo.FindByClientAsync(PaymentProvider.Versell, vspi, vsps, ct);
            if (cred is null) return ProcessingStatus.InvalidAuth;

            var eventKey = $"versell:{dto.idTransaction}:{dto.statusTransaction}".ToLowerInvariant();
            if (await logRepository.ExistsByEventKeyAsync(eventKey, ct))
                return ProcessingStatus.Duplicate;

            var log = BuildLog(
                ProvedorWebhook.VersellPay,
                MapVersellEvento(dto.statusTransaction),
                eventKey,
                sourceIp, headers, dto);

            log.TransactionId = dto.idTransaction;
            log.RequestNumber = dto.requestNumber;
            log.Status = dto.statusTransaction;
            log.TipoTransacao = dto.typeTransaction;
            log.Valor = dto.value;
            log.DebtorName = dto.debtorName;
            log.DebtorDocument = dto.debtorDocument;
            log.DataEventoUtc = DateTime.SpecifyKind(dto.date, DateTimeKind.Utc);

            try
            {
                if (dto.statusTransaction.Equals("PAID_OUT", StringComparison.OrdinalIgnoreCase))
                {
                    var pedido = await pedidoRepository.GetByGatewayIdAsync(dto.idTransaction, ct)
                                 ?? await pedidoRepository.GetByExternalIdAsync(dto.requestNumber ?? "", ct);

                    if (pedido is not null)
                    {
                        pedido.Status = StatusPedido.Aprovado;
                        pedido.DataPagamento = log.DataEventoUtc ?? DateTime.UtcNow;
                        await pedidoRepository.SaveChangesAsync(ct);
                        log.PedidoId = pedido.Id;
                    }
                }
                else if (dto.statusTransaction.Equals("CHARGEBACK", StringComparison.OrdinalIgnoreCase))
                {
                    var pedido = await pedidoRepository.GetByGatewayIdAsync(dto.idTransaction, ct);
                    if (pedido is not null)
                    {
                        pedido.Status = StatusPedido.Estornado;
                        await pedidoRepository.SaveChangesAsync(ct);
                        log.PedidoId = pedido.Id;
                    }
                }

                await logRepository.AddAsync(log, ct);
                await logRepository.SaveChangesAsync(ct);
                return ProcessingStatus.Success;
            }
            catch (Exception ex)
            {
                log.ProcessingStatus = ProcessingStatus.Error;
                log.ProcessingError = ex.Message;
                await logRepository.AddAsync(log, ct);
                await logRepository.SaveChangesAsync(ct);
                return ProcessingStatus.Error;
            }
        }

        public async Task<ProcessingStatus> HandleVexyAsync(
            VexyWebhookDto dto, string sourceIp, IDictionary<string, string> headers, CancellationToken ct)
        {
            // Autentica por usuário: tente obter client_id/secret dos headers (ajuste se sua Vexy manda de outro modo)
            if (!headers.TryGetValue("client_id", out var clientId) || !headers.TryGetValue("client_secret", out var clientSecret))
                return ProcessingStatus.InvalidAuth;

            var cred = await providerCredRepo.FindByClientAsync(PaymentProvider.Vexy, clientId, clientSecret, ct);
            if (cred is null) return ProcessingStatus.InvalidAuth;

            var eventKey = $"vexy:{dto.transaction_id}:{dto.status}".ToLowerInvariant();
            if (await logRepository.ExistsByEventKeyAsync(eventKey, ct))
                return ProcessingStatus.Duplicate;

            var evento = MapVexyEvento(dto.status);
            var log = BuildLog(ProvedorWebhook.VexyPayments, evento, eventKey, sourceIp, headers, dto);
            log.TransactionId = dto.transaction_id;
            log.Status = dto.status;
            log.Valor = dto.amount;
            log.Fee = dto.fee;
            log.NetAmount = dto.net_amount;
            log.Ispb = dto.ispb;
            log.NomeRecebedor = dto.nome_recebedor;
            log.CpfRecebedor = dto.cpf_recebedor;
            log.DataEventoUtc = DateTime.UtcNow;

            try
            {
                if (dto.status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase))
                {
                    var pedido = await pedidoRepository.GetByGatewayIdAsync(dto.transaction_id, ct);
                    if (pedido is not null)
                    {
                        pedido.Status = StatusPedido.Aprovado;
                        pedido.DataPagamento = DateTime.UtcNow;
                        await pedidoRepository.SaveChangesAsync(ct);
                        log.PedidoId = pedido.Id;
                    }
                    else
                    {
                        var transf = await transferenciaRepository.GetByGatewayIdAsync(dto.transaction_id, ct);
                        if (transf is not null)
                        {
                            transf.Status = StatusTransferencia.Concluido;
                            transf.DataAprovacao = DateTime.UtcNow;
                            await transferenciaRepository.SaveChangesAsync(ct);
                            log.TransferenciaId = transf.Id;
                        }
                    }
                }
                else if (dto.status.Equals("RETIDO", StringComparison.OrdinalIgnoreCase))
                {
                    var transf = await transferenciaRepository.GetByGatewayIdAsync(dto.transaction_id, ct);
                    if (transf is not null)
                    {
                        transf.Status = StatusTransferencia.RetidoMed; // certifique-se de ter esse enum
                        await transferenciaRepository.SaveChangesAsync(ct);
                        log.TransferenciaId = transf.Id;
                    }
                }

                await logRepository.AddAsync(log, ct);
                await logRepository.SaveChangesAsync(ct);
                return ProcessingStatus.Success;
            }
            catch (Exception ex)
            {
                log.ProcessingStatus = ProcessingStatus.Error;
                log.ProcessingError = ex.Message;
                await logRepository.AddAsync(log, ct);
                await logRepository.SaveChangesAsync(ct);
                return ProcessingStatus.Error;
            }
        }

        // helpers
        private static InboundWebhookLog BuildLog(
            ProvedorWebhook provedor, WebhookEvento evento, string key,
            string ip, IDictionary<string, string> headers, object payload)
        {
            return new InboundWebhookLog
            {
                Provedor = provedor,
                Evento = evento,
                EventKey = key,
                SourceIp = ip ?? "",
                HeadersJson = JsonSerializer.Serialize(headers),
                PayloadJson = JsonSerializer.Serialize(payload),
                ReceivedAtUtc = DateTime.UtcNow,
                ProcessedAtUtc = DateTime.UtcNow,
                ProcessingStatus = ProcessingStatus.Success
            };
        }

        private static WebhookEvento MapVersellEvento(string status) =>
            status?.ToUpperInvariant() switch
            {
                "PAID_OUT" => WebhookEvento.DepositoPago,
                "CHARGEBACK" => WebhookEvento.Chargeback,
                _ => WebhookEvento.Desconhecido
            };

        private static WebhookEvento MapVexyEvento(string status) =>
            status?.ToUpperInvariant() switch
            {
                "COMPLETED" => WebhookEvento.DepositoPago,    // ou SaqueConcluido—você decide conforme match
                "RETIDO" => WebhookEvento.SaqueRetidoMED,
                _ => WebhookEvento.Desconhecido
            };
    }
}
