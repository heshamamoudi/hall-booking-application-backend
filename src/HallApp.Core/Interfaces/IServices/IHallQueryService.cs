using HallApp.Core.Entities.ChamperEntities;

namespace HallApp.Core.Interfaces.IServices;

/// <summary>
/// ISP FIX (Fix #8): Query-only operations for halls.
/// Read-only consumers should depend on this interface, not the full IHallService.
/// </summary>
public interface IHallQueryService
{
    Task<Hall> GetHallByIdAsync(int hallId);
    Task<List<Hall>> GetAllHallsAsync();
    Task<List<Hall>> GetHallsByManagerAsync(string managerId);
    Task<List<Hall>> GetHallsByVendorAsync(int vendorId);
    Task<bool> IsHallAvailableAsync(int hallId, DateTime startDate, DateTime endDate);
    Task<List<Hall>> SearchHallsAsync(string searchTerm);
    Task<List<Hall>> GetHallsByLocationAsync(string location);
    Task<List<Hall>> GetHallsByCapacityAsync(int minCapacity, int maxCapacity);
    Task<List<Hall>> GetHallsByPriceRangeAsync(double minPrice, double maxPrice);
    Task<List<Hall>> GetPopularHallsAsync();
    Task<List<Hall>> GetRecommendedHallsAsync(string customerId);
    Task<List<Hall>> GetHallsByFeaturesAsync(List<string> features);
    Task<List<Hall>> GetNearbyHallsAsync(double latitude, double longitude, double radiusKm);
    Task<List<Hall>> GetHallsRequiringApprovalAsync();
    Task<List<DateTime>> GetHallAvailableDatesAsync(int hallId);
    Task<decimal> CalculateHallRevenueAsync(int hallId, DateTime startDate, DateTime endDate);
    Task<List<Hall>> GetHallsInMaintenanceAsync();
    Task<double> GetHallAverageRatingAsync(int hallId);
    Task<int> GetHallReviewCountAsync(int hallId);
    Task<List<Hall>> GetTopRatedHallsAsync(int count);
    Task<List<Hall>> GetAvailableHallsAsync(DateTime eventDate, int? excludeHallId = null);
    Task<List<Hall>> GetFeaturedHallsAsync(int limit);
    Task<List<Hall>> GetPremiumHallsAsync(int limit);
    Task<List<Hall>> GetSpecialOfferHallsAsync(int limit);
    Task<List<Hall>> GetNewlyAddedHallsAsync(int limit);
    Task<List<Hall>> GetPopularHallsFilteredAsync(int minReviewCount, double minRating, int limit);
    Task<List<Hall>> GetAvailableHallsForDateAsync(DateTime eventDate, DateTime start, DateTime end, int? genderPreference = null);
    Task<List<Hall>> GetOrganizationHallsAsync(int orgId);
    Task<List<Hall>> GetManagerAssignedHallsAsync(int managerId);

    /// <summary>
    /// Builds a mapping from AppUserId to HallManager.Id for a batch of user IDs.
    /// HIGH-6 FIX: Enables efficient in-memory filtering without per-member database calls.
    /// </summary>
    Task<Dictionary<int, int>> GetAppUserToHallManagerIdMapAsync(IEnumerable<int> appUserIds);
}
