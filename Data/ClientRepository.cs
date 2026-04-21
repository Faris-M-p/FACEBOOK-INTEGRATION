using FACEBOOK_INTEGRATION.Interface;
using FACEBOOK_INTEGRATION.Models;
using Microsoft.EntityFrameworkCore;

namespace FACEBOOK_INTEGRATION.Data;

public sealed class ClientRepository : IClientInterface
{
    private readonly AppDbContext _dbContext;

    public ClientRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Client>> GetAllAsync(CancellationToken ct = default)
    {
        return await _dbContext.Clients
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new Client(c.ClientId, c.Name))
            .ToListAsync(ct);
    }
}
