using System.Linq;
using VersopayBackend.Common;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Utils;
using VersopayLibrary.Enums;
using KycKybModel = VersopayLibrary.Models.KycKyb;

namespace VersopayBackend.Services.KycKybFeature
{
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
            var user = await _usuarioRepo.GetByIdNoTrackingAsync(kycKybCreateDto.UsuarioId, cancellationToken)
                       ?? throw new ArgumentException("UsuarioId inválido.");

            // snapshot separado
            var item = new KycKybModel
            {
                UsuarioId = user.Id,
                Status = kycKybCreateDto.Status,
                Cpf = user.Cpf,    // pode ser null
                Cnpj = user.Cnpj,  // pode ser null
                Nome = user.Nome,
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
            return list.Select(MapComMascara);
        }

        public async Task<KycKybResponseDto?> PegarPeloIdAsync(int id, CancellationToken cancellationToken)
        {
            var item = await _kycRepo.PegarPeloIdNoTrackingAsync(id, cancellationToken);
            return item is null ? null : MapComMascara(item);
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

        private static KycKybResponseDto Map(KycKybModel e) => new()
        {
            Id = e.Id,
            UsuarioId = e.UsuarioId,
            Status = e.Status,
            Nome = e.Nome,
            NumeroDocumento = e.NumeroDocumento,
            DataAprovacao = e.DataAprovacao,

            // DTO de resposta com campos separados
            Cpf = e.Cpf,
            Cnpj = e.Cnpj,
            CpfFormatado = DocumentoFormatter.Mask(e.Cpf),
            CnpjFormatado = DocumentoFormatter.Mask(e.Cnpj)
        };

        private static KycKybResponseDto MapComMascara(KycKybModel m)
        {
            var dto = Map(m);
            dto.CpfFormatado = DocumentoFormatter.Mask(dto.Cpf);
            dto.CnpjFormatado = DocumentoFormatter.Mask(dto.Cnpj);
            return dto;
        }
    }
}
