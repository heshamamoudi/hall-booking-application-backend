using HallApp.Core.Entities.CustomerEntities;

namespace HallApp.Core.Interfaces.IRepositories;

public interface IFavoriteRepository : IGenericRepository<Favorite>
{
    Task<bool> FavoriteExistsAsync(int customerId, int hallId);
    Task AddFavoriteAsync(int customerId, int hallId);
    Task<bool> RemoveFavoriteAsync(int customerId, int hallId);
    Task<List<Favorite>> GetFavoriteHallsAsync(int customerId);
    Task<IEnumerable<Favorite>> GetFavoritesByCustomerIdAsync(int customerId);
    Task<bool> IsFavoriteAsync(int customerId, int hallId);
    Task<Favorite> GetFavoriteAsync(int customerId, int hallId);
}
