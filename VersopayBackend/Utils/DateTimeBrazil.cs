namespace VersopayBackend.Utils
{
    public class DateTimeBrazil
    {
        private static readonly TimeZoneInfo timeZone =
            OperatingSystem.IsWindows()
            ? TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time")
            : TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

        // Agora em São Paulo (equivalente ao “-03:00” atualmente)
        public static DateTime Now() =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

        // Se precisar converter algum UTC para exibir em SP:
        public static DateTime FromUtc(DateTime utc) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), timeZone);
    }
}