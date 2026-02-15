using HallApp.Core.Entities.ChamperEntities;

namespace HallApp.Core.Interfaces.IRepositories;

/// <summary>
/// Repository interface for HallBlockedDate entity operations.
/// Provides methods for managing hall availability blocking.
/// </summary>
public interface IHallBlockedDateRepository : IGenericRepository<HallBlockedDate>
{
    /// <summary>
    /// Gets all active (not soft-deleted) blocked dates for a specific hall.
    /// </summary>
    /// <param name="hallId">The hall ID to query</param>
    /// <returns>Collection of active blocked dates for the hall</returns>
    Task<IEnumerable<HallBlockedDate>> GetActiveBlockedDatesByHallIdAsync(int hallId);

    /// <summary>
    /// Gets a specific blocked date by ID, including navigation properties.
    /// </summary>
    /// <param name="id">The blocked date ID</param>
    /// <returns>The blocked date entity or null if not found</returns>
    Task<HallBlockedDate?> GetBlockedDateByIdAsync(int id);

    /// <summary>
    /// Gets all active blocked dates for a hall that overlap with the specified date range.
    /// Used to check for conflicts when blocking new dates or creating bookings.
    /// </summary>
    /// <param name="hallId">The hall ID to query</param>
    /// <param name="startDate">Start of the date range to check</param>
    /// <param name="endDate">End of the date range to check</param>
    /// <returns>Collection of blocked dates that overlap the specified range</returns>
    Task<IEnumerable<HallBlockedDate>> GetBlockedDatesInRangeAsync(int hallId, DateOnly startDate, DateOnly endDate);

    /// <summary>
    /// Gets blocked dates for a specific month (used for calendar view).
    /// </summary>
    /// <param name="hallId">The hall ID to query</param>
    /// <param name="year">Year to query</param>
    /// <param name="month">Month to query (1-12)</param>
    /// <returns>Collection of blocked dates in the specified month</returns>
    Task<IEnumerable<HallBlockedDate>> GetBlockedDatesByMonthAsync(int hallId, int year, int month);
}
