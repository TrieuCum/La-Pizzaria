using System;

namespace LaPizzaria.Helpers
{
    public static class ShipperLocationSimulationHelper
    {
        // 193 Đỗ Văn Thi, Trấn Biên, Đồng Nai
        public const double OriginLatitude = 10.937570107102053;
        public const double OriginLongitude = 106.8347959491137;

        public static (double Latitude, double Longitude, double Progress)? ComputeSimulatedLocation(
            int orderId,
            DateTime startedAtUtc,
            double? destinationLatitude,
            double? destinationLongitude)
        {
            if (!destinationLatitude.HasValue || !destinationLongitude.HasValue)
                return null;

            var totalMeters = HaversineMeters(OriginLatitude, OriginLongitude, destinationLatitude.Value, destinationLongitude.Value);
            if (totalMeters <= 1d)
                return (destinationLatitude.Value, destinationLongitude.Value, 1d);

            var elapsedSeconds = Math.Max(0, (DateTime.UtcNow - startedAtUtc).TotalSeconds);
            var ticks = (int)Math.Floor(elapsedSeconds / 5d);

            double traveledMeters = 0;
            for (var i = 0; i < ticks; i++)
            {
                // Mỗi 5 giây di chuyển 100-200m (giả lập deterministic)
                traveledMeters += 100d + ((orderId * 37 + i * 53) % 101);
                if (traveledMeters >= totalMeters)
                    break;
            }

            var progress = Math.Clamp(traveledMeters / totalMeters, 0d, 1d);
            var lat = OriginLatitude + (destinationLatitude.Value - OriginLatitude) * progress;
            var lng = OriginLongitude + (destinationLongitude.Value - OriginLongitude) * progress;
            return (lat, lng, progress);
        }

        public static double HaversineMeters(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371000d;
            var dLat = DegreesToRadians(lat2 - lat1);
            var dLon = DegreesToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        private static double DegreesToRadians(double deg) => deg * Math.PI / 180d;
    }
}
