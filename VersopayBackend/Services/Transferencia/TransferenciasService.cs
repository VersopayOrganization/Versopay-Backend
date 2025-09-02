using VersopayBackend.Common;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Services
{
    public sealed class TransferenciasService(
       ITransferenciaRepository transferenciaRepository,
       IUsuarioRepository usuarioRepository,
       IClock clock
   ) : ITransferenciasService
    {
        public async Task<TransferenciaResponseDto> CreateAsync(TransferenciaCreateDto transferenciaCreateDto, CancellationToken cancellationToken)
        {
            var usuario = await usuarioRepository.GetByIdNoTrackingAsync(transferenciaCreateDto.SolicitanteId, cancellationToken)
                    ?? throw new ArgumentException("SolicitanteId inválido.");

            var transferencia = new Transferencia
            {
                SolicitanteId = usuario.Id,
                Status = StatusTransferencia.PendenteAnalise,
                DataSolicitacao = clock.UtcNow,
                ValorSolicitado = transferenciaCreateDto.ValorSolicitado,
                Nome = usuario.Nome,
                Empresa = (usuario.TipoCadastro.HasValue && usuario.TipoCadastro.Value == TipoCadastro.PJ) ? usuario.Nome : null,
                ChavePix = string.IsNullOrWhiteSpace(transferenciaCreateDto.ChavePix) ? null : transferenciaCreateDto.ChavePix.Trim(),
                Aprovacao = AprovacaoManual.Pendente,
                TipoEnvio = null,
                Taxa = null,
                ValorFinal = null,
                DataCadastro = clock.UtcNow
            };

            await transferenciaRepository.AddAsync(transferencia, cancellationToken);
            await transferenciaRepository.SaveChangesAsync(cancellationToken);

            return Map(transferencia);
        }

        public async Task<IEnumerable<TransferenciaResponseDto>> GetAllAsync(
            int? solicitanteId, StatusTransferencia? status, DateTime? dataInicio, DateTime? dataFim,
            int page, int pageSize, CancellationToken cancellationToken)
        {
            var list = await transferenciaRepository.GetAllAsync(solicitanteId, status, dataInicio, dataFim, page, pageSize, cancellationToken);
            return list.Select(Map);
        }

        public async Task<TransferenciaResponseDto?> GetByIdAsync(int id, CancellationToken ct)
        {
            var e = await transferenciaRepository.GetByIdNoTrackingAsync(id, ct);
            return e is null ? null : Map(e);
        }

        public async Task<TransferenciaResponseDto?> AdminUpdateAsync(int id, TransferenciaAdminUpdateDto transferenciaAdminUpdateDto, CancellationToken cancellationToken)
        {
            var transferencia = await transferenciaRepository.FindByIdAsync(id, cancellationToken);
            if (transferencia is null) return null;

            transferencia.Status = transferenciaAdminUpdateDto.Status;
            transferencia.Aprovacao = transferenciaAdminUpdateDto.Aprovacao;
            transferencia.TipoEnvio = transferenciaAdminUpdateDto.TipoEnvio;
            transferencia.Taxa = transferenciaAdminUpdateDto.Taxa;

            // se não veio ValorFinal, calcula (solicitado - taxa)
            if (transferenciaAdminUpdateDto.ValorFinal.HasValue)
                transferencia.ValorFinal = transferenciaAdminUpdateDto.ValorFinal.Value;
            else if (transferencia.Taxa.HasValue)
                transferencia.ValorFinal = transferencia.ValorSolicitado - transferencia.Taxa.Value;

            // grava DataAprovacao quando aprovar
            if (transferenciaAdminUpdateDto.Aprovacao == AprovacaoManual.Aprovado && transferencia.DataAprovacao is null)
                transferencia.DataAprovacao = clock.UtcNow;
            if (transferenciaAdminUpdateDto.Aprovacao != AprovacaoManual.Aprovado)
                transferencia.DataAprovacao = null;

            await transferenciaRepository.SaveChangesAsync(cancellationToken);
            return Map(transferencia);
        }

        public async Task<bool> CancelarAsync(int id, CancellationToken cancellationToken)
        {
            var transferencia = await transferenciaRepository.FindByIdAsync(id, cancellationToken);
            if (transferencia is null) return false;
            transferencia.Status = StatusTransferencia.Cancelado;
            transferencia.Aprovacao = AprovacaoManual.Reprovado;
            await transferenciaRepository.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<bool> ConcluirAsync(int id, decimal? taxa, decimal? valorFinal, CancellationToken cancellationToken)
        {
            var transferencia = await transferenciaRepository.FindByIdAsync(id, cancellationToken);
            if (transferencia is null) return false;

            transferencia.Status = StatusTransferencia.Concluido;
            transferencia.Aprovacao = AprovacaoManual.Aprovado;
            transferencia.DataAprovacao = transferencia.DataAprovacao ?? clock.UtcNow;

            transferencia.Taxa = taxa;
            transferencia.ValorFinal = valorFinal ?? (taxa.HasValue ? transferencia.ValorSolicitado - taxa.Value : transferencia.ValorSolicitado);

            await transferenciaRepository.SaveChangesAsync(cancellationToken);
            return true;
        }

        private static TransferenciaResponseDto Map(Transferencia transferencia) => new()
        {
            Id = transferencia.Id,
            SolicitanteId = transferencia.SolicitanteId,
            Status = transferencia.Status,
            DataSolicitacao = transferencia.DataSolicitacao,
            ValorSolicitado = transferencia.ValorSolicitado,
            Nome = transferencia.Nome,
            Empresa = transferencia.Empresa,
            ChavePix = transferencia.ChavePix,
            Aprovacao = transferencia.Aprovacao,
            TipoEnvio = transferencia.TipoEnvio,
            Taxa = transferencia.Taxa,
            ValorFinal = transferencia.ValorFinal,
            DataCadastro = transferencia.DataCadastro,
            DataAprovacao = transferencia.DataAprovacao
        };
    }
}
