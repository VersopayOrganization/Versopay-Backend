using VersopayBackend.Dtos;

namespace VersopayBackend.Services
{
    public interface IFaturamentoService
    {
        Task<FaturamentoDto?> GetByIdAsync(int id, CancellationToken ct);
        Task<List<FaturamentoDto>> ListarAsync(string cpfCnpj, DateTime? inicioUtc, DateTime? fimUtc, CancellationToken ct);
        Task<FaturamentoDto> RecalcularAsync(FaturamentoRecalcularRequest req, CancellationToken ct);
    }
}
