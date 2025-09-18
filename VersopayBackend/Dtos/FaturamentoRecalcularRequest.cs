using System.ComponentModel.DataAnnotations;

namespace VersopayBackend.Dtos
{
    public sealed class FaturamentoRecalcularRequest
    {
        [Required] public string CpfCnpj { get; set; } = default!;
        [Required] public DateTime DataInicio { get; set; }
        [Required] public DateTime DataFim { get; set; }
        public bool Salvar { get; set; } = true;
    }
}
