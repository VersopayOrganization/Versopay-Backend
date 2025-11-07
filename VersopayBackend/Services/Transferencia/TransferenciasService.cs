using VersopayBackend.Common;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Services
{
    public sealed class TransferenciasService : ITransferenciasService
    {
        private readonly ITransferenciaRepository _transferenciaRepository;
        private readonly IUsuarioRepository _usuarioRepository;
        private readonly IClock _clock;

        public TransferenciasService(ITransferenciaRepository transferenciaRepository, IUsuarioRepository usuarioRepository, IClock clock)
        {
            _transferenciaRepository = transferenciaRepository;
            _usuarioRepository = usuarioRepository;
            _clock = clock;
        }

        public async Task<TransferenciaResponseDto> CreateAsync(TransferenciaCreateDto body, CancellationToken ct)
        {
            var usuario = await _usuarioRepository.GetByIdNoTrackingAsync(body.SolicitanteId, ct)
                ?? throw new ArgumentException("SolicitanteId inválido.");

            var e = new Transferencia
            {
                SolicitanteId = usuario.Id,
                Status = StatusTransferencia.PendenteAnalise,
                DataSolicitacao = _clock.UtcNow,
                ValorSolicitado = body.ValorSolicitado,
                Nome = usuario.Nome,
                Empresa = (usuario.TipoCadastro.HasValue && usuario.TipoCadastro.Value == TipoCadastro.PJ) ? usuario.Nome : null,
                ChavePix = string.IsNullOrWhiteSpace(body.ChavePix) ? null : body.ChavePix.Trim(),
                Aprovacao = AprovacaoManual.Pendente,
                TipoEnvio = null,
                Taxa = null,
                ValorFinal = null,
                DataCadastro = _clock.UtcNow
            };

            await _transferenciaRepository.AddAsync(e, ct);
            await _transferenciaRepository.SaveChangesAsync(ct);

            return Map(e);
        }

        public async Task<IEnumerable<TransferenciaResponseDto>> GetAllAsync(
            int? solicitanteId, StatusTransferencia? status, DateTime? inicio, DateTime? fim, int page, int pageSize, CancellationToken ct)
        {
            var list = await _transferenciaRepository.GetAllAsync(solicitanteId, status, inicio, fim, page, pageSize, ct);
            return list.Select(Map);
        }

        public async Task<TransferenciaResponseDto?> GetByIdAsync(int id, CancellationToken ct)
        {
            var e = await _transferenciaRepository.GetByIdNoTrackingAsync(id, ct);
            return e is null ? null : Map(e);
        }

        public async Task<TransferenciaResponseDto?> AdminUpdateAsync(int id, TransferenciaAdminUpdateDto body, CancellationToken ct)
        {
            var e = await _transferenciaRepository.FindByIdAsync(id, ct);
            if (e is null) return null;

            e.Status = body.Status;
            e.Aprovacao = body.Aprovacao;
            e.TipoEnvio = body.TipoEnvio;
            e.Taxa = body.Taxa;

            if (body.ValorFinal.HasValue) e.ValorFinal = body.ValorFinal.Value;
            else if (e.Taxa.HasValue) e.ValorFinal = e.ValorSolicitado - e.Taxa.Value;

            if (body.Aprovacao == AprovacaoManual.Aprovado && e.DataAprovacao is null)
                e.DataAprovacao = _clock.UtcNow;
            if (body.Aprovacao != AprovacaoManual.Aprovado)
                e.DataAprovacao = null;

            await _transferenciaRepository.SaveChangesAsync(ct);
            return Map(e);
        }

        public async Task<bool> CancelarAsync(int id, CancellationToken ct)
        {
            var e = await _transferenciaRepository.FindByIdAsync(id, ct);
            if (e is null) return false;
            e.Status = StatusTransferencia.Cancelado;
            e.Aprovacao = AprovacaoManual.Reprovado;
            await _transferenciaRepository.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> ConcluirAsync(int id, decimal? taxa, decimal? valorFinal, CancellationToken ct)
        {
            var e = await _transferenciaRepository.FindByIdAsync(id, ct);
            if (e is null) return false;

            e.Status = StatusTransferencia.Concluido;
            e.Aprovacao = AprovacaoManual.Aprovado;
            e.DataAprovacao = e.DataAprovacao ?? _clock.UtcNow;

            e.Taxa = taxa;
            e.ValorFinal = valorFinal ?? (taxa.HasValue ? e.ValorSolicitado - taxa.Value : e.ValorSolicitado);

            await _transferenciaRepository.SaveChangesAsync(ct);
            return true;
        }

        private static TransferenciaResponseDto Map(Transferencia e) => new()
        {
            Id = e.Id,
            SolicitanteId = e.SolicitanteId,
            Status = e.Status,
            DataSolicitacao = e.DataSolicitacao,
            ValorSolicitado = e.ValorSolicitado,
            Nome = e.Nome,
            Empresa = e.Empresa,
            ChavePix = e.ChavePix,
            Aprovacao = e.Aprovacao,
            TipoEnvio = e.TipoEnvio,
            Taxa = e.Taxa,
            ValorFinal = e.ValorFinal,
            DataCadastro = e.DataCadastro,
            DataAprovacao = e.DataAprovacao,
            MetodoPagamento = e.MetodoPagamento
        };
    }
}