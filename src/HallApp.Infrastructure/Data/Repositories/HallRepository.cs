using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Interfaces.IRepositories;
using HallApp.Infrastructure.Extensions;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

public class HallRepository : GenericRepository<Hall>, IHallRepository
{
    public HallRepository(DataContext context) : base(context)
    {
    }

    // Override GetByIdAsync to include related entities using extension method
    public override async Task<Hall> GetByIdAsync(int id)
    {
        return await _context.Halls
            .IncludeAllRelations()
            .FirstOrDefaultAsync(h => h.ID == id);
    }

    // Override GetAllAsync to include related entities using extension method
    public override async Task<IEnumerable<Hall>> GetAllAsync()
    {
        return await _context.Halls
            .IncludeAllRelations()
            .ToListAsync();
    }

    public async Task<IEnumerable<Hall>> GetHallsByManagerIdAsync(int appUserId)
    {
        return await _context.Halls
            .Where(h => h.Managers.Any(m => m.AppUserId == appUserId))
            .IncludeForDetails()
            .ToListAsync();
    }

    public async Task<IEnumerable<Hall>> GetActiveHallsAsync()
    {
        return await _context.Halls
            .Where(h => h.IsActive)
            .IncludeBasicRelations()
            .Include(h => h.Reviews)
            .OrderBy(h => h.Name)
            .ToListAsync();
    }

    public async Task<Hall> GetHallWithDetailsAsync(int hallId)
    {
        return await _context.Halls
            .IncludeAllRelations()
            .FirstOrDefaultAsync(h => h.ID == hallId && h.IsActive);
    }

    public async Task<IEnumerable<Hall>> SearchHallsAsync(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
            return await GetActiveHallsAsync();

        var lowerSearchTerm = searchTerm.ToLower();
        
        return await _context.Halls
            .Where(h => h.IsActive && 
                       (h.Name.ToLower().Contains(lowerSearchTerm) ||
                        h.Description.ToLower().Contains(lowerSearchTerm) ||
                        h.Location.Address.ToLower().Contains(lowerSearchTerm) ||
                        h.Location.City.ToLower().Contains(lowerSearchTerm)))
            .Include(h => h.Location)
            .Include(h => h.MediaFiles)
            .Include(h => h.Reviews)
            .OrderBy(h => h.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Hall>> GetHallsByLocationAsync(string city, string state)
    {
        var query = _context.Halls.Where(h => h.IsActive);

        if (!string.IsNullOrEmpty(city))
            query = query.Where(h => h.Location.City.ToLower().Contains(city.ToLower()));

        if (!string.IsNullOrEmpty(state))
            query = query.Where(h => h.Location.State.ToLower().Contains(state.ToLower()));

        return await query
            .Include(h => h.Location)
            .Include(h => h.MediaFiles)
            .Include(h => h.Reviews)
            .OrderBy(h => h.Name)
            .ToListAsync();
    }

    public async Task<bool> IsHallNameUniqueAsync(string name, int excludeId = 0)
    {
        if (string.IsNullOrEmpty(name))
            return false;

        var query = _context.Halls.AsQueryable();

        if (excludeId != 0)
            query = query.Where(h => h.ID != excludeId);

        return !await query.AnyAsync(h => h.Name.ToLower() == name.ToLower());
    }

    public async Task<IEnumerable<Hall>> GetHallsByGenderAsync(int gender)
    {
        return await _context.Halls
            .Where(h => h.IsActive && h.Gender == gender)
            .Include(h => h.Location)
            .Include(h => h.MediaFiles)
            .Include(h => h.Reviews)
            .OrderBy(h => h.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Hall>> GetHallsByPriceRangeAsync(double minPrice, double maxPrice)
    {
        return await _context.Halls
            .Where(h => h.IsActive && 
                       ((h.MaleActive && h.MaleWeekDays >= minPrice && h.MaleWeekDays <= maxPrice) ||
                        (h.FemaleActive && h.FemaleWeekDays >= minPrice && h.FemaleWeekDays <= maxPrice) ||
                        (h.BothWeekDays >= minPrice && h.BothWeekDays <= maxPrice)))
            .Include(h => h.Location)
            .Include(h => h.MediaFiles)
            .Include(h => h.Reviews)
            .OrderBy(h => h.BothWeekDays)
            .ToListAsync();
    }
}
