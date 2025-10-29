using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using VersopayBackend.Dtos;

namespace VersopayBackend.Services
{
    public interface IVexyService
    {
        /// Valida as credenciais (tenta autenticar na Vexy).
        Task<(bool ok, string? error)> ValidateCredentialsAsync(int ownerUserId, CancellationToken ct);

        /// Cria um depósito (gera QR Code PIX etc.)
        Task<VexyDepositRespDto> CreateDepositAsync(int ownerUserId, VexyDepositReqDto req, CancellationToken ct);

        /// Solicita um saque PIX.
        Task<VexyWithdrawRespDto> RequestWithdrawalAsync(int ownerUserId, VexyWithdrawReqDto req, CancellationToken ct);

        /// Registra log de MED (RETIDO).
        Task LogMedAsync(int ownerUserId, VexyMedLogDto med, string? sourceIp, IDictionary<string, string>? headers, CancellationToken ct);
    }
}
