namespace VersopayBackend.Common
{
    public static class DocumentoFormatter
    {
        public static string? Mask(string? d)
        {
            if (string.IsNullOrWhiteSpace(d)) return null;
            var x = new string(d.Where(char.IsDigit).ToArray());
            return x.Length switch
            {
                11 => Convert.ToUInt64(x).ToString(@"000\.000\.000\-00"),
                14 => Convert.ToUInt64(x).ToString(@"00\.000\.000\/0000\-00"),
                _ => x
            };
        }
    }
}
