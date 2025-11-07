using Microsoft.AspNetCore.Mvc;
// using Microsoft.AspNetCore.Authorization;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Repositories.VexyClient.PixIn;
using VersopayBackend.Services;
using VersopayBackend.Services.Vexy;
using VersopayLibrary.Enums;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/pedidos")]
    // [Authorize]
    public class PedidosController : ControllerBase
    {
        private readonly IPedidosService _svc;
        private readonly IVexyBankService _vexyBank;              // + injete
        private readonly IVexyBankPixInRepository _pixInRepo;     // + injete
        private readonly IPedidoRepository _pedidoRepo;           // + injete
        public PedidosController(
         IPedidosService svc,
         IVexyBankService vexyBank,
         IVexyBankPixInRepository pixInRepo,
         IPedidoRepository pedidoRepo)
        {
            _svc = svc;
            _vexyBank = vexyBank;
            _pixInRepo = pixInRepo;
            _pedidoRepo = pedidoRepo;
        }

        [HttpPost]
        public async Task<ActionResult<object>> Create([FromBody] PedidoCreateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // 1) cria pedido local (pendente)
            var res = await _svc.CreateAsync(dto, ct);

            // 2) se for PIX → cria PIX-IN na Vexy e amarra IDs
            if (string.Equals(res.MetodoPagamento, "Pix", StringComparison.OrdinalIgnoreCase))
            {
                // owner = vendedor do pedido
                var ownerUserId = res.VendedorId;

                var pixReq = new Dtos.VexyBank.PixInCreateReqDto
                {
                    AmountInCents = res.Valor * 100m,
                    Description = $"Pedido #{res.Id}",
                    // o webhook v1 já será montado se PostbackUrl vier nulo
                    Customer = new()
                    {
                        Name = res.VendedorNome ?? "Cliente",
                        Document = null,       // opcional (você pode preencher se tiver)
                        Email = null,
                        Phone = null
                    },
                    PostbackUrl = null
                };

                var pixResp = await _vexyBank.CreatePixInAsync(ownerUserId, pixReq, ct);

                // 2.1) salva o id externo no Pedido e define Provider
                var p = await _pedidoRepo.FindByIdAsync(res.Id, ct);
                if (p is not null)
                {
                    p.Provider = PaymentProvider.Vexy;
                    p.GatewayTransactionId = pixResp.Data.Id; // id da transação Vexy
                    await _pedidoRepo.SaveChangesAsync(ct);
                }

                // 2.2) vincula o VexyBankPixIn ao Pedido
                var localPix = await _pixInRepo.FindByExternalIdAsync(ownerUserId, pixResp.Data.Id, ct);
                if (localPix is not null && localPix.PedidoId == 0)
                {
                    localPix.PedidoId = res.Id;
                    await _pixInRepo.SaveChangesAsync(ct);
                }

                // 2.3) retorna também dados do QR para o front
                return CreatedAtAction(nameof(GetById), new { id = res.Id }, new
                {
                    pedido = res,
                    pix = new
                    {
                        id = pixResp.Data.Id,
                        status = pixResp.Data.Status,
                        emv = pixResp.Data.Pix?.Emv,
                        qrBase64 = pixResp.Data.Pix?.QrCodeBase64
                    }
                });
            }

            // métodos não-Pix permanecem como antes
            return CreatedAtAction(nameof(GetById), new { id = res.Id }, res);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PedidoDto>>> GetAll(
            [FromQuery] string? status,
            [FromQuery] int? vendedorId,
            [FromQuery] string? metodo,
            [FromQuery] DateTime? dataDe,
            [FromQuery] DateTime? dataAte,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            var list = await _svc.GetAllAsync(status, vendedorId, metodo, dataDe, dataAte, page, pageSize, ct);
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        public async Task<ActionResult<PedidoDto>> GetById(int id, CancellationToken ct)
        {
            var res = await _svc.GetByIdAsync(id, ct);
            return res is null ? NotFound() : Ok(res);
        }

        [HttpPut("{id:int}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] PedidoStatusUpdateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var ok = await _svc.UpdateStatusAsync(id, dto, ct);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/pagar")]
        public async Task<IActionResult> MarcarComoPago(int id, CancellationToken ct)
        {
            var ok = await _svc.MarcarComoPagoAsync(id, ct);
            return ok ? NoContent() : NotFound();
        }
    }
}
