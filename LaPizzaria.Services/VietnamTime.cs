using System;

namespace LaPizzaria.Services;

internal static class VietnamTime
{
    private static TimeZoneInfo GetTz()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch
        {
            return TimeZoneInfo.CreateCustomTimeZone("VN+7", TimeSpan.FromHours(7), "VN", "VN");
        }
    }

    public static DateTime UtcNowToLocal(DateTime utc)
    {
        var u = utc.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(utc, DateTimeKind.Utc) : utc.ToUniversalTime();
        return TimeZoneInfo.ConvertTimeFromUtc(u, GetTz());
    }

    public static int ToMinuteOfDay(DateTime localUnspecified)
    {
        return localUnspecified.Hour * 60 + localUnspecified.Minute;
    }
}
