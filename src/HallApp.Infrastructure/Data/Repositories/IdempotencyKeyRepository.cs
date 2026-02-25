using HallApp.Core.Entities;
using HallApp.Core.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

public class IdempotencyKeyRepository : IIdempotencyKeyRepository
{
    private readonly DataContext _context;

    public IdempotencyKeyRepository(DataContext context)
    {
        _context = context;
    }

    public async Task<IdempotencyKey?> GetByKeyAsync(string key)
    {
        return await _context.IdempotencyKeys
            .Where(ik => ik.Key == key && ik.ExpiresAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();
    }

    public async Task AddAsync(IdempotencyKey idempotencyKey)
    {
        await _context.IdempotencyKeys.AddAsync(idempotencyKey);
    }

    public async Task<int> DeleteExpiredKeysAsync()
    {
        var expiredKeys = _context.IdempotencyKeys
            .Where(ik => ik.ExpiresAt <= DateTime.UtcNow);

        _context.IdempotencyKeys.RemoveRange(expiredKeys);
        return await _context.SaveChangesAsync();
    }
}
