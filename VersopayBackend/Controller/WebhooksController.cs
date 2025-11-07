// VersopayBackend/Controllers/WebhooksController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using VersopayBackend.Dtos;
using VersopayBackend.Dtos.VexyBank;
using VersopayBackend.Repositories;
using VersopayBackend.Repositories.VexyClient.PixIn;
using VersopayBackend.Services;
using VersopayBackend.Services.Vexy;
using VersopayBackend.Utils;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/webhooks")]
    public class WebhooksController : ControllerBase
    {
        private readonly IWebhooksService _webhooksService;
        private readonly IInboundWebhookService _inboundWebhookService;
        private readonly IProviderCredentialRepository _providerCredRepo;
        private readonly ILogger<WebhooksController> _logger;
        private readonly IInboundWebhookLogRepository _inboundLogRepo;
        private readonly IVexyBankPixInRepository _pixInRepo;
        private readonly IPedidoRepository _pedidoRepo;
        private readonly IPedidoMatchRepository _pedidoMatchRepo;
        private readonly ITransferenciaMatchRepository _transferMatchRepo;
        private readonly ITransferenciaRepository _transferRepo;

        public WebhooksController(
        ILogger<WebhooksController> logger,
        IInboundWebhookLogRepository inboundLogRepo,
        IVexyBankPixInRepository pixInRepo,
        IPedidoRepository pedidoRepo,
        IPedidoMatchRepository pedidoMatchRepo,
        ITransferenciaMatchRepository transferMatchRepo,
        ITransferenciaRepository transferRepo)
        {
            _logger = logger;
            _inboundLogRepo = inboundLogRepo;
            _pixInRepo = pixInRepo;
            _pedidoRepo = pedidoRepo;
            _pedidoMatchRepo = pedidoMatchRepo;
            _transferMatchRepo = transferMatchRepo;
            _transferRepo = transferRepo;
        }

        // ============================
        // OUTBOUND CRUD (exige JWT)
        // ============================

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<WebhookResponseDto>> Create([FromBody] WebhookCreateDto body, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var res = await _webhooksService.CreateAsync(body, ct);
            return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<WebhookResponseDto>>> GetAll([FromQuery] bool? ativo, CancellationToken ct)
        {
            var res = await _webhooksService.GetAllAsync(ativo, ct);
            return Ok(res);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<WebhookResponseDto>> GetById(int id, CancellationToken ct)
        {
            var res = await _webhooksService.GetByIdAsync(id, ct);
            return res is null ? NotFound() : Ok(res);
        }

        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<ActionResult<WebhookResponseDto>> Update(int id, [FromBody] WebhookUpdateDto body, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var res = await _webhooksService.UpdateAsync(id, body, ct);
            return res is null ? NotFound() : Ok(res);
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Delete(int id, CancellationToken ct)
        {
            var ok = await _webhooksService.DeleteAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/test")]
        [Authorize]
        public async Task<IActionResult> SendTest(int id, [FromBody] WebhookTestPayloadDto payload, CancellationToken ct)
        {
            var delivered = await _webhooksService.SendTestAsync(id, payload, ct);
            return delivered ? Ok(new { delivered = true }) : BadRequest(new { delivered = false });
        }

        // ==========================================
        // INBOUND (PROVIDERS) - público (webhooks)
        // ==========================================

        /// <summary>Versell Pay</summary>
        [HttpPost("providers/versell")]
        [AllowAnonymous]
        [Consumes("application/json")]
        public async Task<IActionResult> InboundVersell([FromBody] VersellWebhookDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            var status = await _inboundWebhookService.HandleVersellAsync(dto, ip, headers, ct);

            return status switch
            {
                ProcessingStatus.Success => Ok(new { ok = true }),
                ProcessingStatus.Duplicate => Ok(new { ok = true, duplicate = true }),
                ProcessingStatus.InvalidAuth => Unauthorized(),
                _ => StatusCode(500, new { ok = false })
            };
        }

        /// <summary>VexyPayments – rota antiga /providers/vexy</summary>
        [HttpPost("providers/vexy")]
        [AllowAnonymous]
        [Consumes("application/json")]
        public async Task<IActionResult> InboundVexy(CancellationToken ct)
        {
            // 1) raw body (necessário p/ HMAC)
            HttpContext.Request.EnableBuffering();
            string rawBody;
            using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                rawBody = await reader.ReadToEndAsync();
                Request.Body.Position = 0;
            }

            // 2) headers/ip
            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";

            // 3) tentar HMAC somente se houver secrets no banco
            var allSecrets = await _providerCredRepo.GetAllVexyWebhookSecretsAsync(ct);
            var mustRequireHmac = allSecrets.Any(s => !string.IsNullOrWhiteSpace(s));

            bool signatureOk = !mustRequireHmac; // em DEV, se não há secrets, não exige HMAC
            if (mustRequireHmac)
            {
                var signatureHeader =
                    Request.Headers["Vexy-Signature"].FirstOrDefault()
                    ?? Request.Headers["vexy-signature"].FirstOrDefault();

                foreach (var secret in allSecrets)
                {
                    if (string.IsNullOrWhiteSpace(secret)) continue;
                    if (VexySignatureVerifier.Verify(signatureHeader, rawBody, secret, timestampToleranceSeconds: 300))
                    {
                        signatureOk = true;
                        break;
                    }
                }
                if (!signatureOk)
                {
                    _logger.LogWarning("Assinatura Vexy-Signature ausente/invalidada e há secrets cadastrados. Rejeitando.");
                    return Unauthorized(); // em PROD: rejeita quando há secret e assinatura falhou
                }
            }

            // 4) desserializa DTO
            VexyWebhookDto? dto;
            try
            {
                dto = JsonSerializer.Deserialize<VexyWebhookDto>(
                    rawBody,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Payload Vexy inválido (providers).");
                return BadRequest("JSON inválido.");
            }
            if (dto is null) return BadRequest("Payload ausente.");

            // 5) processa via serviço (ele ainda pode verificar fallback de headers e retornar InvalidAuth)
            var status = await _inboundWebhookService.HandleVexyAsync(dto, ip, headers, ct);

            // 6) resposta
            return status switch
            {
                ProcessingStatus.Success => Ok(new { ok = true, verified = signatureOk }),
                ProcessingStatus.Duplicate => Ok(new { ok = true, duplicate = true, verified = signatureOk }),
                ProcessingStatus.InvalidAuth =>
                    // Em DEV você pode optar por 200 para inspecionar; em PROD mantenha 401:
                    Unauthorized(),
                _ => StatusCode(500, new { ok = false })
            };
        }


        /// <summary>VexyBank – webhook por owner (pix-in | pix-out)</summary>
        /// Ex.: POST /api/webhooks/v1/vexy/{ownerUserId}/{channel}
        /// channel: "pix-in" ou "pix-out"
        [HttpPost("v1/vexy/{ownerUserId:int}/{channel}")]
        [AllowAnonymous]
        [Consumes("application/json")]
        public async Task<IActionResult> InboundVexyV1(
            [FromRoute] int ownerUserId,
            [FromRoute] string channel,
            [FromBody] VexyWebhookEnvelope payload,
            CancellationToken ct)
        {
            if (payload is null) return BadRequest("Payload obrigatório.");

            var sourceIp = HttpContext.Connection?.RemoteIpAddress?.ToString() ?? "";
            var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());

            // === Idempotência (dedupe) ===
            var eventKey = $"vexy:{ownerUserId}:{payload.Type}:{payload.Event}:{payload.Id}".ToLowerInvariant();
            if (await _inboundLogRepo.ExistsByEventKeyAsync(eventKey, ct))
            {
                _logger.LogInformation("Webhook Vexy duplicado ignorado: {EventKey}", eventKey);
                return Ok(new { ok = true, duplicated = true });
            }

            // === Log base ===
            var baseLog = new InboundWebhookLog
            {
                Provedor = ProvedorWebhook.VexyPayments,
                Evento = payload.Type?.Equals("transaction", StringComparison.OrdinalIgnoreCase) == true
                            ? WebhookEvento.PagamentoPIX
                            : WebhookEvento.TransferenciaPIX,
                EventKey = eventKey,
                SourceIp = sourceIp,
                HeadersJson = JsonSerializer.Serialize(headers),
                PayloadJson = JsonSerializer.Serialize(payload),
                ReceivedAtUtc = DateTime.UtcNow,
                ProcessedAtUtc = DateTime.UtcNow,
                ProcessingStatus = ProcessingStatus.Success,
                TransactionId = payload.Transaction?.Id ?? payload.Transfer?.Id,
                Status = payload.Transaction?.Status ?? payload.Transfer?.Status,
                DataEventoUtc = DateTime.UtcNow
            };
            await _inboundLogRepo.AddAsync(baseLog, ct);
            await _inboundLogRepo.SaveChangesAsync(ct);

            // ======================================================================================
            // PIX-IN (transaction) -> Atualiza VexyBankPixIn e Pedido; Auto-cria se não existir
            // ======================================================================================
            if (payload.Type?.Equals("transaction", StringComparison.OrdinalIgnoreCase) == true &&
                payload.Transaction is not null &&
                !string.IsNullOrWhiteSpace(payload.Transaction.Id))
            {
                var extId = payload.Transaction.Id!;
                var status = payload.Transaction.Status?.ToLowerInvariant();

                // 🔧 Seus DTOs têm 'Amount' (int). Assumo que é em centavos.
                var amountCents = (long)payload.Transaction.Amount;
                var amount = amountCents / 100m;

                // 🔧 Seus DTOs não têm 'Description'; uso um fallback simples.
                var descricao = $"PIX {extId}";

                var payerDoc = payload.Transaction.Pix?.PayerInfo?.Document
                               ?? payload.Transaction.PayerDocument;

                // 1) Atualiza/Cria o registro local do PIX-IN
                var localPix = await _pixInRepo.FindByExternalIdAsync(ownerUserId, extId, ct);
                if (localPix is null)
                {
                    localPix = new VexyBankPixIn
                    {
                        OwnerUserId = ownerUserId,
                        ExternalId = extId,
                        Status = status,
                        AmountCents = amountCents,
                        PayerDocument = payerDoc,
                        PixEmv = null,
                        QrPngBase64 = null,
                        UpdatedAtUtc = DateTime.UtcNow
                    };
                    await _pixInRepo.AddAsync(localPix, ct);
                    await _pixInRepo.SaveChangesAsync(ct);
                }
                else
                {
                    localPix.Status = status;
                    if (!string.IsNullOrWhiteSpace(payerDoc))
                        localPix.PayerDocument = payerDoc;

                    if (status is "paid" or "completed")
                        localPix.PaidAtUtc = DateTime.UtcNow;

                    localPix.UpdatedAtUtc = DateTime.UtcNow;
                    await _pixInRepo.SaveChangesAsync(ct);
                }

                // 2) Atualiza ou cria o Pedido
                bool pedidoAtualizadoOuCriado = false;

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
                            ped.DataPagamento ??= DateTime.UtcNow;
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
                        pedidoAtualizadoOuCriado = true;
                    }
                }

                if (!pedidoAtualizadoOuCriado)
                {
                    var ped = await _pedidoMatchRepo.GetByGatewayIdAsync(extId, ct);
                    if (ped is not null)
                    {
                        ped.Provider = PaymentProvider.Vexy;

                        if (status is "paid" or "completed")
                        {
                            ped.Status = StatusPedido.Aprovado;
                            ped.DataPagamento ??= DateTime.UtcNow;
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
                        pedidoAtualizadoOuCriado = true;

                        if (!localPix.PedidoId.HasValue || localPix.PedidoId.Value <= 0)
                        {
                            localPix.PedidoId = ped.Id;
                            localPix.UpdatedAtUtc = DateTime.UtcNow;
                            await _pixInRepo.SaveChangesAsync(ct);
                        }
                    }
                }

                if (!pedidoAtualizadoOuCriado)
                {
                    var novo = new Pedido
                    {
                        VendedorId = ownerUserId,
                        Valor = amount,
                        MetodoPagamento = MetodoPagamento.Pix,
                        Produto = descricao,
                        Status = (status is "paid" or "completed") ? StatusPedido.Aprovado : StatusPedido.Pendente,
                        DataPagamento = (status is "paid" or "completed") ? DateTime.UtcNow : null,
                        Provider = PaymentProvider.Vexy,
                        GatewayTransactionId = extId,
                        Criacao = DateTime.UtcNow
                    };

                    await _pedidoRepo.AddAsync(novo, ct);
                    await _pedidoRepo.SaveChangesAsync(ct);

                    localPix.PedidoId = novo.Id;
                    localPix.UpdatedAtUtc = DateTime.UtcNow;
                    await _pixInRepo.SaveChangesAsync(ct);
                }
            }

            // ======================================================================================
            // PIX-OUT (transfer) -> Atualiza Transferencia; Auto-cria se não existir
            // ======================================================================================
            if (payload.Type?.Equals("transfer", StringComparison.OrdinalIgnoreCase) == true &&
                payload.Transfer is not null &&
                !string.IsNullOrWhiteSpace(payload.Transfer.Id))
            {
                var gwId = payload.Transfer.Id!;
                var s = payload.Transfer.Status?.ToLowerInvariant();

                // 🔧 Seus DTOs têm 'Amount' (int). Assumo centavos também.
                var amountCents = payload.Transfer.AmountInCents;
                var amount = amountCents / 100m;

                var t = await _transferMatchRepo.GetByGatewayIdAsync(gwId, ct);
                if (t is not null)
                {
                    t.Provider = PaymentProvider.Vexy;

                    if (s is "completed" or "paid")
                    {
                        t.Status = StatusTransferencia.Concluido;
                        t.Aprovacao = AprovacaoManual.Aprovado;
                        t.DataAprovacao ??= DateTime.UtcNow;
                    }
                    else if (s is "med" or "held")
                    {
                        t.Status = StatusTransferencia.RetidoMed;
                    }
                    else if (s is "canceled" or "failed")
                    {
                        t.Status = StatusTransferencia.Cancelado;
                        t.Aprovacao = AprovacaoManual.Reprovado;
                    }

                    await _transferRepo.SaveChangesAsync(ct);
                }
                else
                {
                    // Ingestão por webhook: cria Transferência se não existir
                    var nova = new Transferencia
                    {
                        SolicitanteId = ownerUserId,
                        Status = s is "completed" or "paid" ? StatusTransferencia.Concluido :
                                               s is "canceled" or "failed" ? StatusTransferencia.Cancelado :
                                               StatusTransferencia.PendenteAnalise,
                        Aprovacao = s is "completed" or "paid" ? AprovacaoManual.Aprovado :
                                               s is "canceled" or "failed" ? AprovacaoManual.Reprovado :
                                               AprovacaoManual.Pendente,
                        DataSolicitacao = DateTime.UtcNow,
                        DataAprovacao = s is "completed" or "paid" ? DateTime.UtcNow : null,
                        ValorSolicitado = amount,
                        // 🔧 Seus DTOs não têm RecipientName nem PixKey
                        Nome = null,
                        Empresa = null,
                        ChavePix = null,
                        Provider = PaymentProvider.Vexy,
                        GatewayTransactionId = gwId,
                        DataCadastro = DateTime.UtcNow
                    };

                    await _transferRepo.AddAsync(nova, ct);
                    await _transferRepo.SaveChangesAsync(ct);
                }
            }

            return Ok(new { ok = true });
        }

    }
}
