using VersopayBackend.Dtos;
using static VersopayBackend.Dtos.PasswordResetDtos;

namespace VersopayBackend.Services
{
    public interface IUsuariosService
    {
        Task<IEnumerable<UsuarioResponseDto>> GetAllAsync(CancellationToken cancellationToken);
        Task<UsuarioResponseDto?> GetByIdAsync(int id, CancellationToken cancellationToken);
        Task<UsuarioResponseDto?> UpdateAsync(int id, UsuarioUpdateDto usuarioUpdatedto, CancellationToken cancellationToken);
        Task<UsuarioResponseDto> CadastroInicialAsync(UsuarioCreateDto usuarioCreateDto, CancellationToken cancellationToken);
        Task<UsuarioResponseDto?> CompletarCadastroAsync(UsuarioCompletarCadastroDto usuarioCompletarCadastroDto, CancellationToken ctcancellationToken);
        Task<string> ResetSenhaRequestAsync(SenhaEsquecidaRequest senhaEsquecidaResquest, string baseResetUrl, string? ip, string? userAgent, CancellationToken cancellationToken);
        Task<bool> ValidarTokenResetSenhaAsync(string rawToken, CancellationToken cancellationToken);
        Task<bool> ResetSenhaAsync(RedefinirSenhaRequest redefinirSenhaRequest, CancellationToken cancellationToken);
    }
}
