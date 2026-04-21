using FACEBOOK_INTEGRATION.Models;

namespace FACEBOOK_INTEGRATION.Interface;

public interface IClientInterface
{
    Task<List<Client>> GetAllAsync(CancellationToken ct = default);
}
