namespace VersopayBackend.Utils
{
    public class ValidacaoPadraoSenha
    {
        private const int Min = 8;
        private const int Max = 70;
        private static readonly char[] Specials = "!@#$%&".ToCharArray();

        public static bool IsValid(string? pwd)
        {
            if (string.IsNullOrWhiteSpace(pwd)) return false;
            if (pwd.Length < Min || pwd.Length > Max) return false;

            bool hasUpper = pwd.Any(char.IsUpper);
            bool hasLower = pwd.Any(char.IsLower);
            bool hasDigit = pwd.Any(char.IsDigit);
            bool hasSpecial = pwd.Any(c => Specials.Contains(c));

            return hasUpper && hasLower && hasDigit && hasSpecial;
        }
    }
}