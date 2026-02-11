using HallApp.Core.Entities.ChamperEntities;

namespace HallApp.Core.Interfaces.IRepositories;

public interface IHallRepository : IGenericRepository<Hall>
{
    Task<IEnumerable<Hall>> GetHallsByManagerIdAsync(int managerId);
    Task<IEnumerable<Hall>> GetActiveHallsAsync();
    Task<Hall> GetHallWithDetailsAsync(int hallId);
    Task<IEnumerable<Hall>> SearchHallsAsync(string searchTerm);
    Task<IEnumerable<Hall>> GetHallsByLocationAsync(string city, string state);
    Task<bool> IsHallNameUniqueAsync(string name, int excludeId = 0);
    Task<IEnumerable<Hall>> GetHallsByGenderAsync(int gender);
    Task<IEnumerable<Hall>> GetHallsByPriceRangeAsync(double minPrice, double maxPrice);

    // PERF-001: Repository-level filtered queries to eliminate in-memory filtering
    /// <summary>
    /// Gets active halls with a specific boolean flag set (featured, premium, special offer).
    /// </summary>
    Task<IEnumerable<Hall>> GetActiveHallsByFlagAsync(string flagName, int limit);

    /// <summary>
    /// Gets the most recently created active halls.
    /// </summary>
    Task<IEnumerable<Hall>> GetNewlyAddedHallsAsync(int limit);

    /// <summary>
    /// Gets popular active halls (high review count and rating).
    /// </summary>
    Task<IEnumerable<Hall>> GetPopularHallsWithReviewsAsync(int minReviewCount, double minRating, int limit);

    /// <summary>
    /// Gets available halls for a given date, optionally filtered by gender.
    /// PERF-002: Replaces N+1 availability check pattern.
    /// </summary>
    Task<IEnumerable<Hall>> GetAvailableHallsForDateAsync(
        DateTime eventDate, DateTime startDateTime, DateTime endDateTime,
        int? genderPreference = null);
}
