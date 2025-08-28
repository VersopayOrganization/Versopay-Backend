using VersopayBackend.Dtos;
using VersopayLibrary.Models;

namespace VersopayBackend.Utils
{
    public static class AntecipacaoMappingExtensions
    {
        public static AntecipacaoResponseDto ToResponseDto(this Antecipacao antecipacao) => new()
        {
            Id = antecipacao.Id,
            EmpresaId = antecipacao.EmpresaId,
            EmpresaNome = antecipacao.Empresa?.Nome,
            Status = antecipacao.Status,
            DataSolicitacao = antecipacao.DataSolicitacao,
            Valor = antecipacao.Valor
        };
    }
}
