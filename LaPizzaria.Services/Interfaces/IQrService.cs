using System.Threading.Tasks;

namespace LaPizzaria.Services
{
    public interface IQrService
    {
        string GenerateTableQrPayload(string tableCode);
        Task<bool> SyncOrderFromQrAsync(string payload);
    }

    public class QrOrderItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class QrOrderRequest
    {
        public string TableCode { get; set; } = string.Empty;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public List<QrOrderItem> Items { get; set; } = new();
    }
}


