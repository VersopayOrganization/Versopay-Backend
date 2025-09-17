// VersopayBackend/Dtos/UsuarioUpdateDto.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using VersopayLibrary.Enums;
using VersopayLibrary.Models;

namespace VersopayBackend.Dtos
{
    public sealed class UsuarioUpdateDto : IValidatableObject
    {
        [Required, MaxLength(120)] public string Nome { get; set; } = default!;
        [Required, EmailAddress, MaxLength(160)] public string Email { get; set; } = default!;

        [Required] public TipoCadastro TipoCadastro { get; set; }

        // use este campo para CNPJ **ou** CPF (dependendo de TipoCadastro)
        [Required] public string CpfCnpj { get; set; } = default!;

        [MaxLength(80)] public string? Instagram { get; set; }
        [MaxLength(20)] public string? Telefone { get; set; }

        // Perfil
        [MaxLength(160)] public string? NomeFantasia { get; set; }
        [MaxLength(160)] public string? RazaoSocial { get; set; }
        [MaxLength(160)][Url] public string? Site { get; set; }

        // Endereço
        [MaxLength(9)] public string? EnderecoCep { get; set; }
        [MaxLength(120)] public string? EnderecoLogradouro { get; set; }
        [MaxLength(20)] public string? EnderecoNumero { get; set; }
        [MaxLength(80)] public string? EnderecoComplemento { get; set; }
        [MaxLength(80)] public string? EnderecoBairro { get; set; }
        [MaxLength(80)] public string? EnderecoCidade { get; set; }
        [MaxLength(2)] public string? EnderecoUF { get; set; }

        // Financeiro
        [MaxLength(160)] public string? NomeCompletoBanco { get; set; }
        public string? CpfCnpjDadosBancarios { get; set; }
        [MaxLength(120)] public string? ChavePix { get; set; }
        [MaxLength(120)] public string? ChaveCarteiraCripto { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext _)
        {
            // valida o documento principal conforme TipoCadastro
            var digits = new string((CpfCnpj ?? "").Where(char.IsDigit).ToArray());
            if (digits.Length < 13 && digits.Length != 11)
                yield return new ValidationResult("CPF deve ter 11 dígitos.", new[] { nameof(CpfCnpj) });
            if (digits.Length > 13 && digits.Length != 14)
                yield return new ValidationResult("CNPJ deve ter 14 dígitos.", new[] { nameof(CpfCnpj) });

            // valida bancário apenas se informado
            var bank = new string((CpfCnpjDadosBancarios ?? "").Where(char.IsDigit).ToArray());
            if (!string.IsNullOrWhiteSpace(CpfCnpjDadosBancarios) && bank.Length != 11 && bank.Length != 14)
            {
                yield return new ValidationResult(
                    "CpfCnpjDadosBancarios deve ter 11 (CPF) ou 14 (CNPJ) dígitos.",
                    new[] { nameof(CpfCnpjDadosBancarios) });
            }

            if (!string.IsNullOrWhiteSpace(EnderecoUF) && EnderecoUF!.Length != 2)
                yield return new ValidationResult("UF deve ter 2 letras.", new[] { nameof(EnderecoUF) });
        }
    }
}
