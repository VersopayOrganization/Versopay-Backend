using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayBackend.Services;
using VersopayBackend.Services.Vexy;
using VersopayLibrary.Enums;

namespace VersopayBackend.Controllers
{
    [ApiController]
    [Route("api/transferencias")]
    public class TransferenciaController : ControllerBase
    {
        private readonly ITransferenciasService _transferenciaService;
        private readonly IVexyBankService _vexyBank;
        private readonly ITransferenciaRepository _transferenciaRepository;
        public TransferenciaController(
             ITransferenciasService transferenciaService,
             IVexyBankService vexyBank,
             ITransferenciaRepository transferenciaRepository)
        {
            _transferenciaService = transferenciaService;
            _vexyBank = vexyBank;
            _transferenciaRepository = transferenciaRepository;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<TransferenciaResponseDto>>> GetAll(
            [FromQuery] int? solicitanteId,
            [FromQuery] StatusTransferencia? status,
            [FromQuery] DateTime? dataInicio,
            [FromQuery] DateTime? dataFim,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken cancellationToken = default)
        {
            var list = await _transferenciaService.GetAllAsync(solicitanteId, status, dataInicio, dataFim, page, pageSize, cancellationToken);
            return Ok(list);
        }

        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<TransferenciaResponseDto>> GetById(int id, CancellationToken cancellationToken)
        {
            var dto = await _transferenciaService.GetByIdAsync(id, cancellationToken);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<TransferenciaResponseDto>> Create(
                [FromBody] TransferenciaCreateDto body, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // 1) cria localmente (pendente)
            var dto = await _transferenciaService.CreateAsync(body, ct);

            // 2) dispara PIX-OUT na Vexy (idempotency = transf-{id})
            var ownerUserId = dto.SolicitanteId; // saque do próprio usuário
            var idem = $"transf-{dto.Id}";

            var req = new Dtos.VexyBank.PixOutReqDto
            {
                Amount = (int)Math.Round(dto.ValorSolicitado * 100m, MidpointRounding.AwayFromZero),
                PixKey = dto.ChavePix ?? body.ChavePix,
                Description = $"Saque #{dto.Id}",
                PostbackUrl = null // service monta /v1/vexy/{owner}/pix-out
            };

            var resp = await _vexyBank.SendPixOutAsync(ownerUserId, req, idem, ct);

            // 2.1) salvar Provider+GatewayId na transferência
            var e = await _transferenciaRepository.FindByIdAsync(dto.Id, ct);
            if (e is not null)
            {
                e.Provider = PaymentProvider.Vexy;
                e.GatewayTransactionId = resp.Data.Id; // id de transferência na Vexy
                await _transferenciaRepository.SaveChangesAsync(ct);
            }

            return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
        }

        [HttpPut("{id:int}")]
        [Authorize] // role Admin?
        public async Task<ActionResult<TransferenciaResponseDto>> AdminUpdate(int id, [FromBody] TransferenciaAdminUpdateDto body, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);
            var dto = await _transferenciaService.AdminUpdateAsync(id, body, cancellationToken);
            return dto is null ? NotFound() : Ok(dto);
        }

        [HttpPost("{id:int}/cancelar")]
        [Authorize] // role Admin?
        public async Task<IActionResult> Cancelar(int id, CancellationToken cancellationToken)
        {
            var ok = await _transferenciaService.CancelarAsync(id, cancellationToken);
            return ok ? NoContent() : NotFound();
        }

        [HttpPost("{id:int}/concluir")]
        [Authorize] // role Admin?
        public async Task<IActionResult> Concluir(int id, [FromBody] ConcluirBody body, CancellationToken cancellationToken)
        {
            var ok = await _transferenciaService.ConcluirAsync(id, body.Taxa, body.ValorFinal, cancellationToken);
            return ok ? NoContent() : NotFound();
        }

        public sealed class ConcluirBody
        {
            public decimal? Taxa { get; set; }
            public decimal? ValorFinal { get; set; }
        }
    }
}
