using System.Threading.Tasks;

namespace LaPizzaria.Services
{
    public interface IQrService
    {
        string GenerateTableQrPayload(string tableCode);
        Task<bool> SyncOrderFromQrAsync(string payload);
    }
}


