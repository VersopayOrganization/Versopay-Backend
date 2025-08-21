using System.Linq;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Utils;
using VersopayLibrary.Enums;
using KycKybModel = VersopayLibrary.Models.KycKyb; // alias claro p/ a ENTIDADE

namespace VersopayBackend.Services.KycKybFeature
{
    // implementa a interface usando o nome qualificado do namespace da interface
    public sealed class KycKybService : Services.KycKyb.IKycKybService
    {
        private readonly IKycKybRepository _kycRepo;
        private readonly IUsuarioRepository _usuarioRepo;

        public KycKybService(IKycKybRepository kycRepo, IUsuarioRepository usuarioRepo)
        {
            _kycRepo = kycRepo;
            _usuarioRepo = usuarioRepo;
        }

        public async Task<KycKybResponseDto> CreateAsync(KycKybCreateDto dto, CancellationToken ct)
        {
            var u = await _usuarioRepo.GetByIdNoTrackingAsync(dto.UsuarioId, ct)
                     ?? throw new ArgumentException("UsuarioId inválido.");

            var item = new KycKybModel
            {
                UsuarioId = u.Id,
                Status = dto.Status,
                CpfCnpj = u.CpfCnpj, // snapshot
                Nome = u.Nome,       // snapshot
                NumeroDocumento = dto.NumeroDocumento,
                DataAprovacao = dto.Status == StatusKycKyb.Aprovado ? DateTime.UtcNow : null
            };

            await _kycRepo.AddAsync(item, ct);
            await _kycRepo.SaveChangesAsync(ct);

            return Map(item);
        }

        public async Task<IEnumerable<KycKybResponseDto>> GetAllAsync(int? usuarioId, string? status, int page, int pageSize, CancellationToken ct)
        {
            StatusKycKyb? st = null;
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse(status, true, out StatusKycKyb parsed))
                st = parsed;

            var list = await _kycRepo.GetAllAsync(usuarioId, st, page, pageSize, ct);
            return list.Select(MapWithMask);
        }

        public async Task<KycKybResponseDto?> GetByIdAsync(int id, CancellationToken ct)
        {
            var item = await _kycRepo.GetByIdNoTrackingAsync(id, ct);
            return item is null ? null : MapWithMask(item);
        }

        public async Task<bool> UpdateStatusAsync(int id, KycKybStatusUpdateDto dto, CancellationToken ct)
        {
            var item = await _kycRepo.FindByIdAsync(id, ct);
            if (item is null) return false;

            item.Status = dto.Status;
            item.DataAprovacao = dto.Status == StatusKycKyb.Aprovado
                ? (item.DataAprovacao ?? DateTime.UtcNow)
                : null;

            await _kycRepo.SaveChangesAsync(ct);
            return true;
        }

        public Task<bool> AprovarAsync(int id, CancellationToken ct) =>
            UpdateStatusAsync(id, new KycKybStatusUpdateDto { Status = StatusKycKyb.Aprovado }, ct);

        public Task<bool> ReprovarAsync(int id, CancellationToken ct) =>
            UpdateStatusAsync(id, new KycKybStatusUpdateDto { Status = StatusKycKyb.Reprovado }, ct);

        private static KycKybResponseDto Map(KycKybModel x) => new()
        {
            Id = x.Id,
            UsuarioId = x.UsuarioId,
            Status = x.Status,
            CpfCnpj = x.CpfCnpj,
            Nome = x.Nome,
            NumeroDocumento = x.NumeroDocumento,
            DataAprovacao = x.DataAprovacao
        };

        private static KycKybResponseDto MapWithMask(KycKybModel x)
        {
            var dto = Map(x);
            dto.CpfCnpjFormatado = CpfCnpjUtils.Mask(dto.CpfCnpj);
            return dto;
        }
    }
}
