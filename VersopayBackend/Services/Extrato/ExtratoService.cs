using Microsoft.EntityFrameworkCore;
using VersopayBackend.Dtos;
using VersopayBackend.Repositories;
using VersopayLibrary.Models;
using static VersopayLibrary.Enums.FinanceiroEnums;

namespace VersopayBackend.Services
{
    public sealed class ExtratoService(
        IExtratoRepository extratoRepository,
        IMovimentacaoRepository movimentacaoRepository,
        IUsuarioRepository usuarioRepository
    ) : IExtratoService
    {
        public async Task<ExtratoResponseDto> GetByClienteAsync(int clienteId, CancellationToken cancellationToken)
        {
            var extrato = await EnsureExtratoAsync(clienteId, cancellationToken);
            return Map(extrato);
        }

        public async Task<MovimentacaoResponseDto> LancarAsync(MovimentacaoCreateDto movimentacaoCreateDto, CancellationToken cancellationToken)
        {
            // garante cliente existente
            var user = await usuarioRepository.GetByIdAsync(movimentacaoCreateDto.ClienteId, cancellationToken);
            if (user is null) throw new ArgumentException("ClienteId inválido.");

            var extrato = await EnsureExtratoAsync(movimentacaoCreateDto.ClienteId, cancellationToken);

            var movimentacaoFinanceira = new MovimentacaoFinanceira
            {
                ClienteId = movimentacaoCreateDto.ClienteId,
                Tipo = movimentacaoCreateDto.Tipo,
                Status = movimentacaoCreateDto.EfetivarAgora ? StatusMovimentacao.Efetivada : StatusMovimentacao.Pendente,
                Valor = movimentacaoCreateDto.Valor,
                Descricao = movimentacaoCreateDto.Descricao,
                Referencia = movimentacaoCreateDto.Referencia,
                EfetivadoEmUtc = movimentacaoCreateDto.EfetivarAgora ? DateTime.UtcNow : null
            };

            ApplyBusinessRule(extrato, movimentacaoFinanceira, isNew: true);

            await movimentacaoRepository.AddAsync(movimentacaoFinanceira, cancellationToken);
            await extratoRepository.SaveChangesAsync(cancellationToken); // salva pelo mesmo DbContext

            return Map(movimentacaoFinanceira);
        }

        public async Task<MovimentacaoResponseDto?> ConfirmarAsync(Guid id, CancellationToken cancellationToken)
        {
            var movimentacaoFinanceira = await movimentacaoRepository.FindByIdAsync(id, cancellationToken);
            if (movimentacaoFinanceira is null) return null;
            if (movimentacaoFinanceira.Status != StatusMovimentacao.Pendente) return Map(movimentacaoFinanceira); // nada a fazer

            var e = await EnsureExtratoAsync(movimentacaoFinanceira.ClienteId, cancellationToken);

            movimentacaoFinanceira.Status = StatusMovimentacao.Efetivada;
            movimentacaoFinanceira.EfetivadoEmUtc = DateTime.UtcNow;

            ApplyBusinessRule(e, movimentacaoFinanceira, isNew: false);

            await movimentacaoRepository.SaveChangesAsync(cancellationToken);
            return Map(movimentacaoFinanceira);
        }

        public async Task<bool> CancelarAsync(Guid id, CancellationToken cancellationToken)
        {
            var movimentacaoFinanceira = await movimentacaoRepository.FindByIdAsync(id, cancellationToken);
            if (movimentacaoFinanceira is null) return false;

            if (movimentacaoFinanceira.Status != StatusMovimentacao.Pendente)
                throw new InvalidOperationException("Apenas movimentações pendentes podem ser canceladas.");

            var extrato = await EnsureExtratoAsync(movimentacaoFinanceira.ClienteId, cancellationToken);

            // desfaz efeitos de pendência
            if (movimentacaoFinanceira.Tipo == TipoMovimentacao.Credito)
            {
                extrato.SaldoPendente -= movimentacaoFinanceira.Valor;
            }
            else // Débito pendente
            {
                // devolve a reserva
                extrato.ReservaFinanceira -= movimentacaoFinanceira.Valor;
                extrato.SaldoDisponivel += movimentacaoFinanceira.Valor;
            }

            extrato.AtualizadoEmUtc = DateTime.UtcNow;

            movimentacaoFinanceira.Status = StatusMovimentacao.Cancelada;
            movimentacaoFinanceira.CanceladoEmUtc = DateTime.UtcNow;

            await movimentacaoRepository.SaveChangesAsync(cancellationToken);
            return true;
        }

        public async Task<IEnumerable<MovimentacaoResponseDto>> ListarMovimentacoesAsync(
            int clienteId, MovimentacaoFiltroDto filtro, CancellationToken cancellationToken)
        {
            if (filtro.Page < 1) filtro.Page = 1;
            if (filtro.PageSize < 1 || filtro.PageSize > 200) filtro.PageSize = 20;

            var q = movimentacaoRepository.QueryNoTracking().Where(x => x.ClienteId == clienteId);
            if (filtro.Status.HasValue) q = q.Where(x => x.Status == filtro.Status.Value);

            var list = await q.OrderByDescending(x => x.CriadoEmUtc)
                              .Skip((filtro.Page - 1) * filtro.PageSize)
                              .Take(filtro.PageSize)
                              .ToListAsync(cancellationToken);

            return list.Select(Map);
        }

        // ===== regras de negócio básicas =====
        private static void ApplyBusinessRule(Extrato extrato, MovimentacaoFinanceira movimentacaoFinanceira, bool isNew)
        {
            // isNew = lançamento; !isNew = transição de Pendente -> Efetivada
            if (movimentacaoFinanceira.Tipo == TipoMovimentacao.Credito)
            {
                if (isNew)
                {
                    if (movimentacaoFinanceira.Status == StatusMovimentacao.Pendente)
                        extrato.SaldoPendente += movimentacaoFinanceira.Valor;
                    else // Efetivada direta
                        extrato.SaldoDisponivel += movimentacaoFinanceira.Valor;
                }
                else
                {
                    // confirmar crédito pendente
                    extrato.SaldoPendente -= movimentacaoFinanceira.Valor;
                    extrato.SaldoDisponivel += movimentacaoFinanceira.Valor;
                }
            }
            else // Débito
            {
                if (isNew)
                {
                    if (movimentacaoFinanceira.Status == StatusMovimentacao.Pendente)
                    {
                        // trava a quantia (reserva) e remove do disponível
                        if (extrato.SaldoDisponivel < movimentacaoFinanceira.Valor)
                            throw new InvalidOperationException("Saldo disponível insuficiente para reservar.");

                        extrato.SaldoDisponivel -= movimentacaoFinanceira.Valor;
                        extrato.ReservaFinanceira += movimentacaoFinanceira.Valor;
                    }
                    else // Efetivado direto (sem reserva)
                    {
                        if (extrato.SaldoDisponivel < movimentacaoFinanceira.Valor)
                            throw new InvalidOperationException("Saldo disponível insuficiente.");

                        extrato.SaldoDisponivel -= movimentacaoFinanceira.Valor;
                    }
                }
                else
                {
                    // confirmar débito pendente -> consome a reserva
                    extrato.ReservaFinanceira -= movimentacaoFinanceira.Valor;
                }
            }

            extrato.AtualizadoEmUtc = DateTime.UtcNow;
        }

        private async Task<Extrato> EnsureExtratoAsync(int clienteId, CancellationToken cancellationToken)
        {
            var extrato = await extratoRepository.GetByClienteIdAsync(clienteId, cancellationToken);
            if (extrato is not null) return extrato;

            var novo = new Extrato
            {
                ClienteId = clienteId,
                SaldoDisponivel = 0m,
                SaldoPendente = 0m,
                ReservaFinanceira = 0m,
                AtualizadoEmUtc = DateTime.UtcNow
            };
            await extratoRepository.AddAsync(novo, cancellationToken);
            await extratoRepository.SaveChangesAsync(cancellationToken);
            return novo;
        }

        private static ExtratoResponseDto Map(Extrato extrato) => new()
        {
            Id = extrato.Id,
            ClienteId = extrato.ClienteId,
            SaldoDisponivel = extrato.SaldoDisponivel,
            SaldoPendente = extrato.SaldoPendente,
            ReservaFinanceira = extrato.ReservaFinanceira,
            AtualizadoEmUtc = extrato.AtualizadoEmUtc
        };

        private static MovimentacaoResponseDto Map(MovimentacaoFinanceira movimentacaoFinanceira) => new()
        {
            Id = movimentacaoFinanceira.Id,
            ClienteId = movimentacaoFinanceira.ClienteId,
            Tipo = movimentacaoFinanceira.Tipo,
            Status = movimentacaoFinanceira.Status,
            Valor = movimentacaoFinanceira.Valor,
            Descricao = movimentacaoFinanceira.Descricao,
            Referencia = movimentacaoFinanceira.Referencia,
            CriadoEmUtc = movimentacaoFinanceira.CriadoEmUtc,
            EfetivadoEmUtc = movimentacaoFinanceira.EfetivadoEmUtc,
            CanceladoEmUtc = movimentacaoFinanceira.CanceladoEmUtc
        };
    }
}
