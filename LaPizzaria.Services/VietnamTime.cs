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
				return TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");
			}
		}

		public static DateTime UtcToVietnamLocal(DateTime utc)
		{
			var u = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
			return TimeZoneInfo.ConvertTimeFromUtc(u, GetTimeZone());
		}
	}
}
