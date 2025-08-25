using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Utils;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;
using VersopayBackend.Common; // IClock

namespace VersopayBackend.Services
{
    public sealed class AntecipacoesService(IAntecipacaoRepository repo, IClock clock) : IAntecipacoesService
    {
        public async Task<AntecipacaoResponseDto> CreateAsync(AntecipacaoCreateDto dto, CancellationToken ct)
        {
            if (!await repo.UsuarioExistsAsync(dto.EmpresaId, ct))
                throw new ArgumentException("EmpresaId inválido.");

            if (dto.Valor <= 0m)
                throw new ArgumentException("Valor deve ser maior que zero.");

            var a = new Antecipacao
            {
                EmpresaId = dto.EmpresaId,
                Status = StatusAntecipacao.PendenteFila,
                DataSolicitacao = clock.UtcNow, // UTC
                Valor = dto.Valor
            };

            await repo.AddAsync(a, ct);
            await repo.SaveChangesAsync(ct);

            // carregar Empresa para nome (opcional)
            var full = await repo.GetByIdNoTrackingAsync(a.Id, ct) ?? a;
            return full.ToResponseDto();
        }

        public async Task<IEnumerable<AntecipacaoResponseDto>> GetAllAsync(
            int? empresaId, string? status, DateTime? deUtc, DateTime? ateUtc, int page, int pageSize, CancellationToken ct)
        {
            StatusAntecipacao? st = null;
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse(status, true, out StatusAntecipacao parsed))
                st = parsed;

            var list = await repo.GetAllAsync(empresaId, st, deUtc, ateUtc, page, pageSize, ct);
            return list.Select(x => x.ToResponseDto());
        }

        public async Task<AntecipacaoResponseDto?> GetByIdAsync(int id, CancellationToken ct)
        {
            var item = await repo.GetByIdNoTrackingAsync(id, ct);
            return item is null ? null : item.ToResponseDto();
        }

        public async Task<bool> UpdateStatusAsync(int id, AntecipacaoStatusUpdateDto dto, CancellationToken ct)
        {
            var item = await repo.FindByIdAsync(id, ct);
            if (item is null) return false;

            // Regras simples de transição
            if (item.Status is StatusAntecipacao.Concluido or StatusAntecipacao.Cancelado)
                throw new InvalidOperationException("Não é possível alterar o status de uma antecipação já concluída/cancelada.");

            if (dto.Status == item.Status) return true;

            item.Status = dto.Status;
            await repo.SaveChangesAsync(ct);
            return true;
        }
    }
}
