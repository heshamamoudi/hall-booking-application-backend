using HallApp.Core.Entities.CustomerEntities;
using HallApp.Core.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

public class FavoriteRepository : GenericRepository<Favorite>, IFavoriteRepository
{
    public FavoriteRepository(DataContext context) : base(context)
    {
    }

    public async Task<bool> FavoriteExistsAsync(int customerId, int hallId)
    {
        return await _context.Favorites
            .AnyAsync(f => f.CustomerId == customerId && f.HallId == hallId);
    }

    public async Task AddFavoriteAsync(int customerId, int hallId)
    {
        var favorite = new Favorite
        {
            CustomerId = customerId,
            HallId = hallId,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Favorites.AddAsync(favorite);
    }

    public async Task<bool> RemoveFavoriteAsync(int customerId, int hallId)
    {
        var favorite = await _context.Favorites
            .FirstOrDefaultAsync(f => f.CustomerId == customerId && f.HallId == hallId);

        if (favorite == null)
            return false;

        _context.Favorites.Remove(favorite);
        return true;
    }

    public async Task<List<Favorite>> GetFavoriteHallsAsync(int customerId)
    {
        var favorites = await _context.Favorites
            .Where(f => f.CustomerId == customerId)
            .Include(f => f.Hall)
                .ThenInclude(h => h.Location)
            .Include(f => f.Hall)
                .ThenInclude(h => h.MediaFiles)
            .Include(f => f.Hall)
                .ThenInclude(h => h.Reviews)
            .ToListAsync();

        return favorites;
    }

    public async Task<IEnumerable<Favorite>> GetFavoritesByCustomerIdAsync(int customerId)
    {
        return await _context.Favorites
            .Where(f => f.CustomerId == customerId)
            .Include(f => f.Hall)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> IsFavoriteAsync(int customerId, int hallId)
    {
        return await FavoriteExistsAsync(customerId, hallId);
    }

    public async Task<Favorite> GetFavoriteAsync(int customerId, int hallId)
    {
        return await _context.Favorites
            .FirstOrDefaultAsync(f => f.CustomerId == customerId && f.HallId == hallId);
    }
}
