using Microsoft.EntityFrameworkCore;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayLibrary.Models;
using static VersopayLibrary.Enums.FinanceiroEnums;

namespace VersopayBackend.Services
{
    public sealed class ExtratoService : IExtratoService
    {
        private readonly IExtratoRepository _extratoRepository;
        private readonly IMovimentacaoRepository _movimentacaoRepository;
        private readonly IUsuarioRepository _usuarioRepository;

        public ExtratoService(
            IExtratoRepository extratoRepository,
            IMovimentacaoRepository movimentacaoRepository,
            IUsuarioRepository usuarioRepository)
        {
            _extratoRepository = extratoRepository;
            _movimentacaoRepository = movimentacaoRepository;
            _usuarioRepository = usuarioRepository;
        }

        public async Task<ExtratoResponseDto> GetByClienteAsync(int clienteId, CancellationToken ct)
        {
            var extrato = await EnsureExtratoAsync(clienteId, ct);
            return Map(extrato);
        }

        public async Task<MovimentacaoResponseDto> LancarAsync(MovimentacaoCreateDto body, CancellationToken ct)
        {
            var user = await _usuarioRepository.GetByIdAsync(body.ClienteId, ct)
                ?? throw new ArgumentException("ClienteId inválido.");

            var extrato = await EnsureExtratoAsync(body.ClienteId, ct);

            var mov = new MovimentacaoFinanceira
            {
                ClienteId = body.ClienteId,
                Tipo = body.Tipo,
                Status = body.EfetivarAgora ? StatusMovimentacao.Efetivada : StatusMovimentacao.Pendente,
                Valor = body.Valor,
                Descricao = body.Descricao,
                Referencia = body.Referencia,
                EfetivadoEmUtc = body.EfetivarAgora ? DateTime.UtcNow : null
            };

            ApplyBusinessRule(extrato, mov, isNew: true);

            await _movimentacaoRepository.AddAsync(mov, ct);
            await _extratoRepository.SaveChangesAsync(ct);

            return Map(mov);
        }

        public async Task<MovimentacaoResponseDto?> ConfirmarAsync(Guid id, CancellationToken ct)
        {
            var mov = await _movimentacaoRepository.FindByIdAsync(id, ct);
            if (mov is null) return null;
            if (mov.Status != StatusMovimentacao.Pendente) return Map(mov);

            var extrato = await EnsureExtratoAsync(mov.ClienteId, ct);

            mov.Status = StatusMovimentacao.Efetivada;
            mov.EfetivadoEmUtc = DateTime.UtcNow;

            ApplyBusinessRule(extrato, mov, isNew: false);

            await _movimentacaoRepository.SaveChangesAsync(ct);
            return Map(mov);
        }

        public async Task<bool> CancelarAsync(Guid id, CancellationToken ct)
        {
            var mov = await _movimentacaoRepository.FindByIdAsync(id, ct);
            if (mov is null) return false;

            if (mov.Status != StatusMovimentacao.Pendente)
                throw new InvalidOperationException("Apenas movimentações pendentes podem ser canceladas.");

            var extrato = await EnsureExtratoAsync(mov.ClienteId, ct);

            if (mov.Tipo == TipoMovimentacao.Credito)
            {
                extrato.SaldoPendente -= mov.Valor;
            }
            else
            {
                extrato.ReservaFinanceira -= mov.Valor;
                extrato.SaldoDisponivel += mov.Valor;
            }

            extrato.AtualizadoEmUtc = DateTime.UtcNow;

            mov.Status = StatusMovimentacao.Cancelada;
            mov.CanceladoEmUtc = DateTime.UtcNow;

            await _movimentacaoRepository.SaveChangesAsync(ct);
            return true;
        }

        public async Task<IEnumerable<MovimentacaoResponseDto>> ListarMovimentacoesAsync(
            int clienteId, MovimentacaoFiltroDto filtro, CancellationToken ct)
        {
            if (filtro.Page < 1) filtro.Page = 1;
            if (filtro.PageSize < 1 || filtro.PageSize > 200) filtro.PageSize = 20;

            var q = _movimentacaoRepository.QueryNoTracking().Where(x => x.ClienteId == clienteId);
            if (filtro.Status.HasValue) q = q.Where(x => x.Status == filtro.Status.Value);

            var list = await q.OrderByDescending(x => x.CriadoEmUtc)
                              .Skip((filtro.Page - 1) * filtro.PageSize)
                              .Take(filtro.PageSize)
                              .ToListAsync(ct);

            return list.Select(Map);
        }

        private static void ApplyBusinessRule(Extrato e, MovimentacaoFinanceira m, bool isNew)
        {
            if (m.Tipo == TipoMovimentacao.Credito)
            {
                if (isNew)
                {
                    if (m.Status == StatusMovimentacao.Pendente)
                        e.SaldoPendente += m.Valor;
                    else
                        e.SaldoDisponivel += m.Valor;
                }
                else
                {
                    e.SaldoPendente -= m.Valor;
                    e.SaldoDisponivel += m.Valor;
                }
            }
            else
            {
                if (isNew)
                {
                    if (m.Status == StatusMovimentacao.Pendente)
                    {
                        if (e.SaldoDisponivel < m.Valor)
                            throw new InvalidOperationException("Saldo disponível insuficiente para reservar.");

                        e.SaldoDisponivel -= m.Valor;
                        e.ReservaFinanceira += m.Valor;
                    }
                    else
                    {
                        if (e.SaldoDisponivel < m.Valor)
                            throw new InvalidOperationException("Saldo disponível insuficiente.");

                        e.SaldoDisponivel -= m.Valor;
                    }
                }
                else
                {
                    e.ReservaFinanceira -= m.Valor;
                }
            }

            e.AtualizadoEmUtc = DateTime.UtcNow;
        }

        private async Task<Extrato> EnsureExtratoAsync(int clienteId, CancellationToken ct)
        {
            var e = await _extratoRepository.GetByClienteIdAsync(clienteId, ct);
            if (e is not null) return e;

            var novo = new Extrato
            {
                ClienteId = clienteId,
                SaldoDisponivel = 0m,
                SaldoPendente = 0m,
                ReservaFinanceira = 0m,
                AtualizadoEmUtc = DateTime.UtcNow
            };
            await _extratoRepository.AddAsync(novo, ct);
            await _extratoRepository.SaveChangesAsync(ct);
            return novo;
        }

        private static ExtratoResponseDto Map(Extrato e) => new()
        {
            Id = e.Id,
            ClienteId = e.ClienteId,
            SaldoDisponivel = e.SaldoDisponivel,
            SaldoPendente = e.SaldoPendente,
            ReservaFinanceira = e.ReservaFinanceira,
            AtualizadoEmUtc = e.AtualizadoEmUtc
        };

        private static MovimentacaoResponseDto Map(MovimentacaoFinanceira m) => new()
        {
            Id = m.Id,
            ClienteId = m.ClienteId,
            Tipo = m.Tipo,
            Status = m.Status,
            Valor = m.Valor,
            Descricao = m.Descricao,
            Referencia = m.Referencia,
            CriadoEmUtc = m.CriadoEmUtc,
            EfetivadoEmUtc = m.EfetivadoEmUtc,
            CanceladoEmUtc = m.CanceladoEmUtc
        };
    }
}
