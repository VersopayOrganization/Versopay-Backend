// Controllers/Webhooks/VexyBankWebhookController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Services.Vexy;

[ApiController]
[Route("api/webhooks/v1/vexy/{ownerUserId:int}/{channel}")]
[AllowAnonymous] // webhooks são públicos (valide assinatura se a Vexy prover)
public class VexyBankWebhookController(IVexyBankService service, ILogger<VexyBankWebhookController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(
        int ownerUserId, string channel,
        [FromBody] VexyWebhookEnvelope body,
        CancellationToken ct)
    {
        // Captura cabeçalhos para auditoria/validação futura
        var headers = Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString());
        var sourceIp = HttpContext.Connection.RemoteIpAddress?.ToString();

        // (Opcional) validar assinatura/HMAC se a Vexy disponibilizar header próprio
        await service.HandleWebhookAsync(ownerUserId, body, sourceIp, headers, ct);
        return Ok(new { ok = true });
    }
}
