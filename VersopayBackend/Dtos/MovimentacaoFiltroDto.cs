using static VersopayLibrary.Enums.FinanceiroEnums;

namespace VersopayBackend.Dtos
{
    public sealed class MovimentacaoFiltroDto
    {
        public StatusMovimentacao? Status { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
