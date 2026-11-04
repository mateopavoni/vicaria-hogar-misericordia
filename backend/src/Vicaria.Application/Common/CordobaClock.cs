namespace Vicaria.Application.Common;

// fecha y hora de Córdoba. El hogar trabaja en esa zona: entre las 21 y las 24 h
// la fecha UTC ya es la del día siguiente, así que "hoy" siempre se toma de acá.
public static class CordobaClock
{
    private static readonly TimeZoneInfo CordobaTimeZone = ResolveTimeZone();

    public static DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, CordobaTimeZone);

    public static DateTime Today => Now.Date;

    // el nombre de la zona horaria cambia entre Linux y Windows; si no existe, se usa UTC-3 fijo
    private static TimeZoneInfo ResolveTimeZone()
    {
        if (TimeZoneInfo.TryFindSystemTimeZoneById("America/Argentina/Cordoba", out var ianaTimeZone))
        {
            return ianaTimeZone;
        }

        if (TimeZoneInfo.TryFindSystemTimeZoneById("Argentina Standard Time", out var windowsTimeZone))
        {
            return windowsTimeZone;
        }

        return TimeZoneInfo.CreateCustomTimeZone(
            "America/Argentina/Cordoba",
            TimeSpan.FromHours(-3),
            "Hora de Córdoba",
            "Hora de Córdoba");
    }
}
