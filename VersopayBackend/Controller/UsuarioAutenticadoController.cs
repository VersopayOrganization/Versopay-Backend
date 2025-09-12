using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Common;
using VersopayBackend.Services;
using VersopayBackend.Utils;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/usuario-autenticado")]
    public sealed class UsuarioAutenticadoController(IUsuarioAutenticadoService svc, IClock clock) : ControllerBase
    {
        [HttpGet("perfil")]
        public async Task<IActionResult> Perfil(CancellationToken ct)
        {
            var userId = HttpUser.GetUserId(User);
            var dto = await svc.GetPerfilAsync(userId, ct);
            return Ok(dto);
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard([FromQuery] DateTime? de, [FromQuery] DateTime? ate, CancellationToken ct)
        {
            // Default: mês atual em UTC
            if (de is null || ate is null)
            {
                var now = clock.UtcNow;
                var first = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
                de ??= first;
                ate ??= first.AddMonths(1);
            }

            var userId = HttpUser.GetUserId(User);
            var dto = await svc.GetDashboardAsync(userId, de!.Value, ate!.Value, ct);
            return Ok(dto);
        }
    }
}
