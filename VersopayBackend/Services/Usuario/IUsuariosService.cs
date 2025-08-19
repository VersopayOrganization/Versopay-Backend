using VersopayBackend.Dtos;

namespace VersopayBackend.Services
{
    public interface IUsuariosService
    {
        Task<UsuarioResponseDto> CreateAsync(UsuarioCreateDto dto, CancellationToken ct);
        Task<IEnumerable<UsuarioResponseDto>> GetAllAsync(CancellationToken ct);
        Task<UsuarioResponseDto?> GetByIdAsync(int id, CancellationToken ct);
        Task<UsuarioResponseDto?> UpdateAsync(int id, UsuarioUpdateDto dto, CancellationToken ct);
    }
}
