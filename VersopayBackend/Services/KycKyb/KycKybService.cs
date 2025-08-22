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

        public async Task<KycKybResponseDto> CriarAsync(KycKybCreateDto kycKybCreateDto, CancellationToken cancellationToken)
        {
            var usuarioRepo = await _usuarioRepo.GetByIdNoTrackingAsync(kycKybCreateDto.UsuarioId, cancellationToken)
                     ?? throw new ArgumentException("UsuarioId inválido.");

            var item = new KycKybModel
            {
                UsuarioId = usuarioRepo.Id,
                Status = kycKybCreateDto.Status,
                CpfCnpj = usuarioRepo.CpfCnpj, // snapshot
                Nome = usuarioRepo.Nome,       // snapshot
                NumeroDocumento = kycKybCreateDto.NumeroDocumento,
                DataAprovacao = kycKybCreateDto.Status == StatusKycKyb.Aprovado ? DateTime.UtcNow : null
            };

            await _kycRepo.AdicionarAsync(item, cancellationToken);
            await _kycRepo.SaveChangesAsync(cancellationToken);

            return Map(item);
        }

        public async Task<IEnumerable<KycKybResponseDto>> PegarTodosAsync(int? usuarioId, string? status, int page, int pageSize, CancellationToken cancellationToken)
        {
            StatusKycKyb? statusKycKyb = null;
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse(status, true, out StatusKycKyb parsed))
                statusKycKyb = parsed;

            var list = await _kycRepo.PegarTodosAsync(usuarioId, statusKycKyb, page, pageSize, cancellationToken);
            return list.Select(MapearComMascara);
        }

        public async Task<KycKybResponseDto?> PegarPeloIdAsync(int id, CancellationToken cancellationToken)
        {
            var item = await _kycRepo.PegarPeloIdNoTrackingAsync(id, cancellationToken);
            return item is null ? null : MapearComMascara(item);
        }

        public async Task<bool> AtualizarStatusAsync(int id, KycKybStatusUpdateDto kycKybStatusUpdateDto, CancellationToken cancellationToken)
        {
            var item = await _kycRepo.AcharPeloIdAsync(id, cancellationToken);
            if (item is null) return false;

            item.Status = kycKybStatusUpdateDto.Status;
            item.DataAprovacao = kycKybStatusUpdateDto.Status == StatusKycKyb.Aprovado
                ? (item.DataAprovacao ?? DateTime.UtcNow)
                : null;

            await _kycRepo.SaveChangesAsync(cancellationToken);
            return true;
        }

        public Task<bool> AprovarAsync(int id, CancellationToken cancellationToken) =>
            AtualizarStatusAsync(id, new KycKybStatusUpdateDto { Status = StatusKycKyb.Aprovado }, cancellationToken);

        public Task<bool> ReprovarAsync(int id, CancellationToken cancellationToken) =>
            AtualizarStatusAsync(id, new KycKybStatusUpdateDto { Status = StatusKycKyb.Reprovado }, cancellationToken);

        private static KycKybResponseDto Map(KycKybModel kycKybModel) => new()
        {
            Id = kycKybModel.Id,
            UsuarioId = kycKybModel.UsuarioId,
            Status = kycKybModel.Status,
            CpfCnpj = kycKybModel.CpfCnpj,
            Nome = kycKybModel.Nome,
            NumeroDocumento = kycKybModel.NumeroDocumento,
            DataAprovacao = kycKybModel.DataAprovacao
        };

        private static KycKybResponseDto MapearComMascara(KycKybModel kycKybModel)
        {
            var kycKybResponseDto = Map(kycKybModel);
            kycKybResponseDto.CpfCnpjFormatado = CpfCnpjUtils.Mask(kycKybResponseDto.CpfCnpj);
            return kycKybResponseDto;
        }
    }
}
