using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Utils;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;
using VersopayBackend.Common; // IClock

namespace VersopayBackend.Services
{
    public sealed class AntecipacoesService(IAntecipacaoRepository iAntecipacaoRepository, IClock clock) : IAntecipacoesService
    {
        public async Task<AntecipacaoResponseDto> CreateAsync(AntecipacaoCreateDto antecipacaoCreateDto, CancellationToken cancellationToken)
        {
            if (!await iAntecipacaoRepository.UsuarioExistsAsync(antecipacaoCreateDto.EmpresaId, cancellationToken))
                throw new ArgumentException("EmpresaId inválido.");

            if (antecipacaoCreateDto.Valor <= 0m)
                throw new ArgumentException("Valor deve ser maior que zero.");

            var a = new Antecipacao
            {
                EmpresaId = antecipacaoCreateDto.EmpresaId,
                Status = StatusAntecipacao.PendenteFila,
                DataSolicitacao = clock.UtcNow, // UTC
                Valor = antecipacaoCreateDto.Valor
            };

            await iAntecipacaoRepository.AddAsync(a, cancellationToken);
            await iAntecipacaoRepository.SaveChangesAsync(cancellationToken);

            // carregar Empresa para nome (opcional)
            var full = await iAntecipacaoRepository.GetByIdNoTrackingAsync(a.Id, cancellationToken) ?? a;
            return full.ToResponseDto();
        }

        public async Task<IEnumerable<AntecipacaoResponseDto>> GetAllAsync(
            int? empresaId, string? status, DateTime? deUtc, DateTime? ateUtc, int page, int pageSize, CancellationToken cancellationToken)
        {
            StatusAntecipacao? statusAntecipacao = null;
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse(status, true, out StatusAntecipacao parsed))
                statusAntecipacao = parsed;

            var list = await iAntecipacaoRepository.GetAllAsync(empresaId, statusAntecipacao, deUtc, ateUtc, page, pageSize, cancellationToken);
            return list.Select(x => x.ToResponseDto());
        }

        public async Task<AntecipacaoResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
        {
            var item = await iAntecipacaoRepository.GetByIdNoTrackingAsync(id, cancellationToken);
            return item is null ? null : item.ToResponseDto();
        }

        public async Task<bool> UpdateStatusAsync(int id, AntecipacaoStatusUpdateDto antecipacaoStatusUpdateDto, CancellationToken cancellationToken)
        {
            var item = await iAntecipacaoRepository.FindByIdAsync(id, cancellationToken);
            if (item is null) return false;

            // Regras simples de transição
            if (item.Status is StatusAntecipacao.Concluido or StatusAntecipacao.Cancelado)
                throw new InvalidOperationException("Não é possível alterar o status de uma antecipação já concluída/cancelada.");

            if (antecipacaoStatusUpdateDto.Status == item.Status) return true;

            item.Status = antecipacaoStatusUpdateDto.Status;
            await iAntecipacaoRepository.SaveChangesAsync(cancellationToken);
            return true;
        }
    }
}
