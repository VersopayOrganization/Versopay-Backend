using VersopayLibrary.Models;

namespace VersopayBackend.Utils
{
    public static class CpfCnpjUtils
    {
        public static string ValidarQtdDigitos(string? s) =>
            string.IsNullOrWhiteSpace(s) ? "" : new string(s.Where(char.IsDigit).ToArray());

        public static bool IsValidoTipo(string digits, TipoCadastro tipo) =>
            (tipo == TipoCadastro.PF && digits.Length == 11) ||
            (tipo == TipoCadastro.PJ && digits.Length == 14);

        public static string? Mascara(string? d)
        {
            if (string.IsNullOrWhiteSpace(d)) return null;
            var x = ValidarQtdDigitos(d);
            if (x.Length == 11) return Convert.ToUInt64(x).ToString(@"000\.000\.000\-00");
            if (x.Length == 14) return Convert.ToUInt64(x).ToString(@"00\.000\.000\/0000\-00");
            return x;
        }
    }
}
