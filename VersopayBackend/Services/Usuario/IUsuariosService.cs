using VersopayBackend.Dtos;

namespace VersopayBackend.Services
{
    public interface IUsuariosService
    {
        Task<IEnumerable<UsuarioResponseDto>> GetAllAsync(CancellationToken cancellationToken);
        Task<UsuarioResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<UsuarioResponseDto?> UpdateAsync(int id, UsuarioUpdateDto usuarioUpdatedto, CancellationToken cancellationToken);
        Task<UsuarioResponseDto> CadastroInicialAsync(UsuarioCreateDto usuarioCreateDto, CancellationToken cancellationToken);
        Task<UsuarioResponseDto?> CompletarCadastroAsync(UsuarioCompletarCadastroDto usuarioCompletarCadastroDto, CancellationToken ctcancellationToken);
    }
}
