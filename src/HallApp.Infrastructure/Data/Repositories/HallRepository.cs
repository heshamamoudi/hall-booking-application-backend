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
            .AsNoTracking()
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
            .AsNoTracking()
            .Include(h => h.Location)
            .Include(h => h.MediaFiles)
            .Include(h => h.Reviews)
            .OrderBy(h => h.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Hall>> GetHallsByLocationAsync(string city, string state)
    {
        var query = _context.Halls.Where(h => h.IsActive).AsNoTracking();

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
            .AsNoTracking()
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
            .AsNoTracking()
            .Include(h => h.Location)
            .Include(h => h.MediaFiles)
            .Include(h => h.Reviews)
            .OrderBy(h => h.BothWeekDays)
            .ToListAsync();
    }

    // PERF-001: Repository-level filtered queries

    public async Task<IEnumerable<Hall>> GetActiveHallsByFlagAsync(string flagName, int limit)
    {
        var query = _context.Halls
            .Where(h => h.IsActive)
            .AsNoTracking();

        query = flagName.ToLower() switch
        {
            "featured" => query.Where(h => h.IsFeatured),
            "premium" => query.Where(h => h.IsPremium),
            "specialoffer" => query.Where(h => h.HasSpecialOffer),
            _ => query
        };

        return await query
            .IncludeBasicRelations()
            .Include(h => h.Reviews)
            .OrderByDescending(h => h.AverageRating)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Hall>> GetNewlyAddedHallsAsync(int limit)
    {
        return await _context.Halls
            .Where(h => h.IsActive)
            .AsNoTracking()
            .IncludeBasicRelations()
            .Include(h => h.Reviews)
            .OrderByDescending(h => h.Created)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Hall>> GetPopularHallsWithReviewsAsync(int minReviewCount, double minRating, int limit)
    {
        return await _context.Halls
            .Where(h => h.IsActive &&
                        h.Reviews != null &&
                        h.Reviews.Count >= minReviewCount &&
                        h.AverageRating >= minRating)
            .AsNoTracking()
            .IncludeBasicRelations()
            .Include(h => h.Reviews)
            .OrderByDescending(h => h.Reviews.Count)
            .ThenByDescending(h => h.AverageRating)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<IEnumerable<Hall>> GetAvailableHallsForDateAsync(
        DateTime eventDate, DateTime startDateTime, DateTime endDateTime,
        int? genderPreference = null)
    {
        // PERF-002 FIX: Single query instead of N+1 per-hall availability check
        var query = _context.Halls
            .Where(h => h.IsActive)
            .AsNoTracking();

        // Apply gender filter at database level
        if (genderPreference.HasValue && genderPreference.Value != 2)
        {
            query = query.Where(h => h.Gender == genderPreference.Value || h.Gender == 2);
        }

        // Exclude halls that have conflicting bookings on the event date
        var confirmedStatuses = new[] { "Confirmed", "HallApproved", "VendorApprovalInProgress", "Paid" };
        query = query.Where(h => !_context.Bookings
            .Any(b => b.HallId == h.ID &&
                      b.EventDate.Date == eventDate.Date &&
                      (b.IsBookingConfirmed || confirmedStatuses.Contains(b.Status))));

        return await query
            .IncludeBasicRelations()
            .Include(h => h.Reviews)
            .OrderBy(h => h.Name)
            .ToListAsync();
    }
}
