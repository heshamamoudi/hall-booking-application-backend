using HallApp.Core.Entities;

namespace HallApp.Core.Interfaces.IRepositories;

public interface IIdempotencyKeyRepository
{
    Task<IdempotencyKey?> GetByKeyAsync(string key);
    Task AddAsync(IdempotencyKey idempotencyKey);
    Task<int> DeleteExpiredKeysAsync();
}
