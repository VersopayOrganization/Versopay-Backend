using System.Text.Json;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories.Webhook;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Services.Webhook
{
    public sealed class InboundWebhookService(
        IInboundWebhookLogRepository logRepository,
        IPedidoMatchRepository pedidoRepository,
        ITransferenciaMatchRepository transferenciaRepository
    ) : IInboundWebhookService
    {
        public async Task<ProcessingStatus> HandleVersellAsync(
            VersellWebhookDto versellWbhookDto, string sourceIp, IDictionary<string, string> headers, CancellationToken cancellationToken)
        {
            // EventKey p/ idempotência (id + status)
            var eventKey = $"versell:{versellWbhookDto.idTransaction}:{versellWbhookDto.statusTransaction}".ToLowerInvariant();
            if (await logRepository.ExistsByEventKeyAsync(eventKey, cancellationToken))
                return ProcessingStatus.Duplicate;

            var log = BuildLog(
                ProvedorWebhook.VersellPay,
                MapVersellEvento(versellWbhookDto.statusTransaction),
                eventKey,
                sourceIp, headers, versellWbhookDto);

            log.TransactionId = versellWbhookDto.idTransaction;
            log.RequestNumber = versellWbhookDto.requestNumber;
            log.Status = versellWbhookDto.statusTransaction;
            log.TipoTransacao = versellWbhookDto.typeTransaction;
            log.Valor = versellWbhookDto.value;
            log.DebtorName = versellWbhookDto.debtorName;
            log.DebtorDocument = versellWbhookDto.debtorDocument;
            log.DataEventoUtc = DateTime.SpecifyKind(versellWbhookDto.date, DateTimeKind.Utc);

            try
            {
                // DEPÓSITO PAGO -> marca Pedido como Aprovado
                if (versellWbhookDto.statusTransaction.Equals("PAID_OUT", StringComparison.OrdinalIgnoreCase))
                {
                    var pedido = await pedidoRepository.GetByGatewayIdAsync(versellWbhookDto.idTransaction, cancellationToken)
                                 ?? await pedidoRepository.GetByExternalIdAsync(versellWbhookDto.requestNumber ?? "", cancellationToken);

                    if (pedido is not null)
                    {
                        pedido.Status = StatusPedido.Aprovado;
                        pedido.DataPagamento = log.DataEventoUtc ?? DateTime.UtcNow;
                        await pedidoRepository.SaveChangesAsync(cancellationToken);
                        log.PedidoId = pedido.Id;
                    }
                }
                // CHARGEBACK -> marca pedido como Estornado/Chargeback
                else if (versellWbhookDto.statusTransaction.Equals("CHARGEBACK", StringComparison.OrdinalIgnoreCase))
                {
                    var pedido = await pedidoRepository.GetByGatewayIdAsync(versellWbhookDto.idTransaction, cancellationToken);
                    if (pedido is not null)
                    {
                        pedido.Status = StatusPedido.Estornado;
                        await pedidoRepository.SaveChangesAsync(cancellationToken);
                        log.PedidoId = pedido.Id;
                    }
                }

                await logRepository.AddAsync(log, cancellationToken);
                await logRepository.SaveChangesAsync(cancellationToken);
                return ProcessingStatus.Success;
            }
            catch (Exception exception)
            {
                log.ProcessingStatus = ProcessingStatus.Error;
                log.ProcessingError = exception.Message;
                await logRepository.AddAsync(log, cancellationToken);
                await logRepository.SaveChangesAsync(cancellationToken);
                return ProcessingStatus.Error;
            }
        }

        public async Task<ProcessingStatus> HandleVexyAsync(
            VexyWebhookDto vexyWebhookDto, string sourceIp, IDictionary<string, string> headers, CancellationToken cancellationToken)
        {
            var eventKey = $"vexy:{vexyWebhookDto.transaction_id}:{vexyWebhookDto.status}".ToLowerInvariant();
            if (await logRepository.ExistsByEventKeyAsync(eventKey, cancellationToken))
                return ProcessingStatus.Duplicate;

            var evento = MapVexyEvento(vexyWebhookDto.status);
            var log = BuildLog(ProvedorWebhook.VexyPayments, evento, eventKey, sourceIp, headers, vexyWebhookDto);
            log.TransactionId = vexyWebhookDto.transaction_id;
            log.Status = vexyWebhookDto.status;
            log.Valor = vexyWebhookDto.amount;
            log.Fee = vexyWebhookDto.fee;
            log.NetAmount = vexyWebhookDto.net_amount;
            log.Ispb = vexyWebhookDto.ispb;
            log.NomeRecebedor = vexyWebhookDto.nome_recebedor;
            log.CpfRecebedor = vexyWebhookDto.cpf_recebedor;
            log.DataEventoUtc = DateTime.UtcNow;

            try
            {
                // COMPLETED pode ser depósito (Pedido) ou saque (Transferência)
                if (vexyWebhookDto.status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase))
                {
                    // 1) tenta casar com Pedido
                    var pedido = await pedidoRepository.GetByGatewayIdAsync(vexyWebhookDto.transaction_id, cancellationToken);
                    if (pedido is not null)
                    {
                        pedido.Status = StatusPedido.Aprovado;
                        pedido.DataPagamento = DateTime.UtcNow;
                        await pedidoRepository.SaveChangesAsync(cancellationToken);
                        log.PedidoId = pedido.Id;
                    }
                    else
                    {
                        // 2) tenta Transferência
                        var transferencia = await transferenciaRepository.GetByGatewayIdAsync(vexyWebhookDto.transaction_id, cancellationToken);
                        if (transferencia is not null)
                        {
                            transferencia.Status = StatusTransferencia.Concluido;
                            transferencia.DataAprovacao = DateTime.UtcNow;
                            await transferenciaRepository.SaveChangesAsync(cancellationToken);
                            log.TransferenciaId = transferencia.Id;
                        }
                    }
                }
                // RETIDO (MED)
                else if (vexyWebhookDto.status.Equals("RETIDO", StringComparison.OrdinalIgnoreCase))
                {
                    var transferencia = await transferenciaRepository.GetByGatewayIdAsync(vexyWebhookDto.transaction_id, cancellationToken);
                    if (transferencia is not null)
                    {
                        // se você adicionou RetidoMed no enum
                        transferencia.Status = StatusTransferencia.RetidoMed;
                        await transferenciaRepository.SaveChangesAsync(cancellationToken);
                        log.TransferenciaId = transferencia.Id;
                    }
                }

                await logRepository.AddAsync(log, cancellationToken);
                await logRepository.SaveChangesAsync(cancellationToken);
                return ProcessingStatus.Success;
            }
            catch (Exception ex)
            {
                log.ProcessingStatus = ProcessingStatus.Error;
                log.ProcessingError = ex.Message;
                await logRepository.AddAsync(log, cancellationToken);
                await logRepository.SaveChangesAsync(cancellationToken);
                return ProcessingStatus.Error;
            }
        }

        // helpers
        private static InboundWebhookLog BuildLog(
            ProvedorWebhook provedorWebhook, WebhookEvento webhookEvento, string key,
            string ip, IDictionary<string, string> headers, object payload)
        {
            return new InboundWebhookLog
            {
                Provedor = provedorWebhook,
                Evento = webhookEvento,
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
                "COMPLETED" => WebhookEvento.DepositoPago, // ou SaqueConcluido dependendo do match
                "RETIDO" => WebhookEvento.SaqueRetidoMED,
                _ => WebhookEvento.Desconhecido
            };
    }
}
