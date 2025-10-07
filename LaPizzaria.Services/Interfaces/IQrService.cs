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
        public List<QrOrderItem> Items { get; set; } = new();
    }
}


