using System;

namespace VersopayBackend.Utils
{
    public static class TimeUtils
    {
        // Tenta IANA (Linux/containers) e cai para Windows Id
        public static TimeZoneInfo GetBrazilTz()
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo"); }
            catch { return TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"); }
        }

        // Recebe UTC e devolve com offset do Brasil naquele instante
        public static DateTimeOffset ToBrazilOffset(DateTime utc)
        {
            if (utc.Kind != DateTimeKind.Utc) utc = DateTime.SpecifyKind(utc, DateTimeKind.Utc);
            var tz = GetBrazilTz();
            var offset = tz.GetUtcOffset(utc);
            return new DateTimeOffset(utc, TimeSpan.Zero).ToOffset(offset);
        }
    }
}
