using VersopayBackend.Dtos;

namespace VersopayBackend.Services
{
    public interface IUsuariosService
    {
        Task<UsuarioResponseDto> CreateAsync(UsuarioCreateDto usuarioCreatedto, CancellationToken cancellationToken);
        Task<IEnumerable<UsuarioResponseDto>> GetAllAsync(CancellationToken cancellationToken);
        Task<UsuarioResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<UsuarioResponseDto?> UpdateAsync(int id, UsuarioUpdateDto usuarioUpdatedto, CancellationToken cancellationToken);
    }
}
