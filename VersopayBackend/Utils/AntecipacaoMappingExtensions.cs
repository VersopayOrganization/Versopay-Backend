using VersopayBackend.Dtos;
using VersopayLibrary.Models;

namespace VersopayBackend.Utils
{
    public static class AntecipacaoMappingExtensions
    {
        public static AntecipacaoResponseDto ToResponseDto(this Antecipacao x) => new()
        {
            Id = x.Id,
            EmpresaId = x.EmpresaId,
            EmpresaNome = x.Empresa?.Nome, // pode vir nulo se não incluído
            Status = x.Status,
            DataSolicitacao = x.DataSolicitacao,
            Valor = x.Valor
        };
    }
}
