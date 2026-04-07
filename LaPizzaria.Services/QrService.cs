using System.Text.Json;
using System.Threading.Tasks;

namespace LaPizzaria.Services
{
    public class QrService : IQrService
    {
        public string GenerateTableQrPayload(string tableCode)
        {
            var payload = new { type = "table", code = tableCode };
            return JsonSerializer.Serialize(payload);
        }

        public Task<bool> SyncOrderFromQrAsync(string payload)
        {
            // Placeholder: parse payload and sync with backend (e.g., open order, attach table)
            return Task.FromResult(true);
        }
    }
}


