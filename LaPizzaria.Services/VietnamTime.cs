using System;

namespace LaPizzaria.Services
{
	public static class VietnamTime
	{
		public static TimeZoneInfo GetTimeZone()
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

		public static DateTime UtcToVietnamLocal(DateTime utc)
		{
			var normalized = utc.Kind switch
			{
				DateTimeKind.Utc => utc,
				DateTimeKind.Local => utc.ToUniversalTime(),
				_ => DateTime.SpecifyKind(utc, DateTimeKind.Utc)
			};
			return TimeZoneInfo.ConvertTimeFromUtc(normalized, GetTimeZone());
		}

		public static DateTime NormalizeToVietnamLocal(DateTime value)
		{
			return value.Kind == DateTimeKind.Utc ? UtcToVietnamLocal(value) : value;
		}

		public static int ToMinuteOfDay(DateTime localTime)
		{
			return localTime.Hour * 60 + localTime.Minute;
		}
	}
}
