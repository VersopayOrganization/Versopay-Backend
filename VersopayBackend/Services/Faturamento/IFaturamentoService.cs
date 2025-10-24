using VersopayBackend.Dtos;

namespace VersopayBackend.Services
{
    public interface IFaturamentoService
    {
        Task<FaturamentoDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<List<FaturamentoDto>> ListarAsync(string cpfCnpj, DateTime? dataInicio, DateTime? dataFim, CancellationToken cancellationToken);
        Task<FaturamentoDto> RecalcularAsync(FaturamentoRecalcularRequest faturamentoRecalcularRequest, CancellationToken cancellationToken);
    }
}
