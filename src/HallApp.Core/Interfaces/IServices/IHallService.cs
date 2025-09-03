using HallApp.Core.Entities.ChamperEntities;

namespace HallApp.Core.Interfaces.IServices
{
    public interface IHallService
    {
        Task<Hall> GetHallByIdAsync(int hallId);
        Task<List<Hall>> GetAllHallsAsync();
        Task<List<Hall>> GetHallsByManagerAsync(string managerId);
        Task<Hall> CreateHallAsync(Hall hall);
        Task<Hall> UpdateHallAsync(Hall hall);
        Task<Hall> ManagerUpdateHallAsync(Hall hall, string managerId);
        Task<bool> DeleteHallAsync(int hallId);
        Task<List<Hall>> GetHallsByVendorAsync(int vendorId);
        Task<bool> IsHallAvailableAsync(int hallId, DateTime startDate, DateTime endDate);
        Task<List<Hall>> SearchHallsAsync(string searchTerm);
        Task<List<Hall>> GetHallsByLocationAsync(string location);
        Task<List<Hall>> GetHallsByCapacityAsync(int minCapacity, int maxCapacity);
        Task<List<Hall>> GetHallsByPriceRangeAsync(double minPrice, double maxPrice);
        Task<List<Hall>> GetPopularHallsAsync();
        Task<List<Hall>> GetRecommendedHallsAsync(string customerId);
        Task<Hall> ToggleHallStatusAsync(int hallId);
        Task<bool> UpdateHallImagesAsync(int hallId, List<string> imageUrls);
        Task<List<Hall>> GetHallsByFeaturesAsync(List<string> features);
        Task<List<Hall>> GetNearbyHallsAsync(double latitude, double longitude, double radiusKm);
        Task<List<Hall>> GetHallsRequiringApprovalAsync();
        Task<bool> ApproveHallAsync(int hallId);
        Task<bool> RejectHallAsync(int hallId, string reason);
        Task<bool> UpdateHallAvailabilityAsync(int hallId, List<DateTime> availableDates);
        Task<List<DateTime>> GetHallAvailableDatesAsync(int hallId);
        Task<decimal> CalculateHallRevenueAsync(int hallId, DateTime startDate, DateTime endDate);
        Task<bool> SetHallMaintenanceModeAsync(int hallId, bool isMaintenanceMode);
        Task<List<Hall>> GetHallsInMaintenanceAsync();
        Task<bool> UpdateHallRatingAsync(int hallId);
        Task<double> GetHallAverageRatingAsync(int hallId);
        Task<int> GetHallReviewCountAsync(int hallId);
        Task<List<Hall>> GetTopRatedHallsAsync(int count);
        Task<List<Hall>> GetAvailableHallsAsync(DateTime eventDate, int? excludeHallId = null);
    }
}
