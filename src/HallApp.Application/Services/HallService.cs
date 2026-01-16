using AutoMapper;
using HallApp.Core.Entities.ChamperEntities;
using HallApp.Core.Entities.ChamperEntities.MediaEntities;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IServices;

namespace HallApp.Application.Services;

public class HallService : IHallService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public HallService(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Hall> GetHallByIdAsync(int hallId)
    {
        return await _unitOfWork.HallRepository.GetByIdAsync(hallId);
    }

    public async Task<List<Hall>> GetAllHallsAsync()
    {
        var halls = await _unitOfWork.HallRepository.GetAllAsync();
        return halls.ToList();
    }

    public async Task<List<Hall>> GetHallsByManagerAsync(string managerId)
    {
        if (int.TryParse(managerId, out int managerIdInt))
        {
            var halls = await _unitOfWork.HallRepository.GetHallsByManagerIdAsync(managerIdInt);
            return halls.ToList();
        }
        return new List<Hall>();
    }

    public async Task<Hall> CreateHallAsync(Hall hall)
    {
        await _unitOfWork.HallRepository.AddAsync(hall);
        await _unitOfWork.Complete();
        return hall;
    }

    public async Task<Hall> UpdateHallAsync(Hall hall)
    {
        // First, fetch existing hall to update all properties
        var existingHall = await _unitOfWork.HallRepository.GetByIdAsync(hall.ID);
        if (existingHall != null)
        {
            // Update all scalar properties
            existingHall.Name = hall.Name;
            existingHall.CommercialRegistration = hall.CommercialRegistration;
            existingHall.Vat = hall.Vat;
            existingHall.BothWeekDays = hall.BothWeekDays;
            existingHall.BothWeekEnds = hall.BothWeekEnds;
            existingHall.MaleWeekDays = hall.MaleWeekDays;
            existingHall.MaleWeekEnds = hall.MaleWeekEnds;
            existingHall.MaleMin = hall.MaleMin;
            existingHall.MaleMax = hall.MaleMax;
            existingHall.MaleActive = hall.MaleActive;
            existingHall.FemaleWeekDays = hall.FemaleWeekDays;
            existingHall.FemaleWeekEnds = hall.FemaleWeekEnds;
            existingHall.FemaleMin = hall.FemaleMin;
            existingHall.FemaleMax = hall.FemaleMax;
            existingHall.FemaleActive = hall.FemaleActive;
            existingHall.Gender = hall.Gender;
            existingHall.Description = hall.Description;
            
            // Update direct contact information
            existingHall.Email = hall.Email ?? existingHall.Email;
            existingHall.Phone = hall.Phone ?? existingHall.Phone;
            existingHall.WhatsApp = hall.WhatsApp ?? existingHall.WhatsApp;
            existingHall.Logo = hall.Logo ?? existingHall.Logo;
            
            // Update flags
            existingHall.IsActive = hall.IsActive;
            existingHall.HasSpecialOffer = hall.HasSpecialOffer;
            existingHall.IsFeatured = hall.IsFeatured;
            existingHall.IsPremium = hall.IsPremium;
            
            existingHall.Updated = DateTime.UtcNow;
            
            // Update MediaFiles collection
            if (hall.MediaFiles != null)
            {
                existingHall.MediaFiles.Clear();
                foreach (var mediaFile in hall.MediaFiles)
                {
                    existingHall.MediaFiles.Add(mediaFile);
                }
            }
            
            // Update Location
            if (hall.Location != null)
            {
                if (existingHall.Location == null)
                {
                    existingHall.Location = hall.Location;
                }
                else
                {
                    existingHall.Location.Latitude = hall.Location.Latitude;
                    existingHall.Location.Longitude = hall.Location.Longitude;
                    existingHall.Location.City = hall.Location.City;
                    existingHall.Location.Address = hall.Location.Address;
                }
            }
            
            // Update Managers collection
            if (hall.Managers != null)
            {
                existingHall.Managers?.Clear();
                existingHall.Managers = hall.Managers;
            }
            
            // Update Contacts collection
            if (hall.Contacts != null)
            {
                existingHall.Contacts?.Clear();
                existingHall.Contacts = hall.Contacts;
            }
            
            // Update Packages collection
            if (hall.Packages != null)
            {
                existingHall.Packages?.Clear();
                existingHall.Packages = hall.Packages;
            }
            
            // Update Services collection
            if (hall.Services != null)
            {
                existingHall.Services?.Clear();
                existingHall.Services = hall.Services;
            }
            
            await _unitOfWork.Complete();
            return existingHall;
        }
        
        // If hall not found, try normal update
        _unitOfWork.HallRepository.Update(hall);
        await _unitOfWork.Complete();
        return hall;
    }

    public async Task<Hall> ManagerUpdateHallAsync(Hall hall, string managerId)
    {
        // Verify manager has permission to update this hall
        if (int.TryParse(managerId, out int managerIdInt))
        {
            var existingHall = await _unitOfWork.HallRepository.GetByIdAsync(hall.ID);
            if (existingHall != null && existingHall.Managers.Any(m => m.Id == managerIdInt))
            {
                _unitOfWork.HallRepository.Update(hall);
                await _unitOfWork.Complete();
                return hall;
            }
        }
        return new Hall();
    }

    public async Task<bool> DeleteHallAsync(int hallId)
    {
        var hall = await _unitOfWork.HallRepository.GetByIdAsync(hallId);
        if (hall == null) return false;
        
        _unitOfWork.HallRepository.Delete(hall);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<List<Hall>> GetHallsByVendorAsync(int vendorId)
    {
        // Implementation depends on how halls are associated with vendors
        // For now, return all halls as this relationship may need clarification
        var halls = await _unitOfWork.HallRepository.GetAllAsync();
        return halls.ToList();
    }

    public async Task<bool> IsHallAvailableAsync(int hallId, DateTime startDate, DateTime endDate)
    {
        // Check if hall exists and is active
        var hall = await _unitOfWork.HallRepository.GetByIdAsync(hallId);
        if (hall == null || !hall.IsActive) return false;
        
        var existingBookings = await _unitOfWork.BookingRepository.GetBookingsByHallIdAsync(hallId);
        return !existingBookings.Any(b => b.EventDate.Date >= startDate.Date && b.EventDate.Date <= endDate.Date);
    }

    public async Task<List<Hall>> SearchHallsAsync(string searchTerm)
    {
        var halls = await _unitOfWork.HallRepository.SearchHallsAsync(searchTerm);
        return halls.ToList();
    }

    public async Task<List<Hall>> GetHallsByLocationAsync(string location)
    {
        var halls = await _unitOfWork.HallRepository.GetHallsByLocationAsync(location, "");
        return halls.ToList();
    }

    public async Task<List<Hall>> GetHallsByCapacityAsync(int minCapacity, int maxCapacity)
    {
        var halls = await _unitOfWork.HallRepository.GetAllAsync();
        return halls.Where(h => h.MaleMax >= minCapacity && h.MaleMax <= maxCapacity || 
                               h.FemaleMax >= minCapacity && h.FemaleMax <= maxCapacity).ToList();
    }

    public async Task<List<Hall>> GetHallsByPriceRangeAsync(double minPrice, double maxPrice)
    {
        var halls = await _unitOfWork.HallRepository.GetHallsByPriceRangeAsync(minPrice, maxPrice);
        return halls.ToList();
    }

    public async Task<List<Hall>> GetPopularHallsAsync()
    {
        var allHalls = await _unitOfWork.HallRepository.GetActiveHallsAsync();
        // Sort by review count and average rating
        return allHalls.OrderByDescending(h => h.Reviews.Count)
                      .ThenByDescending(h => h.Reviews.Any() ? h.Reviews.Average(r => r.Rating) : 0)
                      .Take(10)
                      .ToList();
    }

    public async Task<List<Hall>> GetRecommendedHallsAsync(string customerId)
    {
        // Implementation would depend on recommendation algorithm
        return await GetPopularHallsAsync();
    }

    public async Task<Hall> ToggleHallStatusAsync(int hallId)
    {
        var hall = await _unitOfWork.HallRepository.GetByIdAsync(hallId);
        if (hall == null) return new Hall();
        
        hall.IsActive = !hall.IsActive;
        _unitOfWork.HallRepository.Update(hall);
        await _unitOfWork.Complete();
        return hall;
    }

    public async Task<bool> UpdateHallImagesAsync(int hallId, List<string> imageUrls)
    {
        var hall = await _unitOfWork.HallRepository.GetByIdAsync(hallId);
        if (hall == null) return false;
        
        // Clear existing images and add new ones
        hall.MediaFiles.Clear();
        foreach (var url in imageUrls)
        {
            hall.MediaFiles.Add(new HallMedia { URL = url, HallID = hallId, MediaType = "image", Gender = 3 });
        }
        
        _unitOfWork.HallRepository.Update(hall);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<List<Hall>> GetHallsByFeaturesAsync(List<string> features)
    {
        var allHalls = await _unitOfWork.HallRepository.GetActiveHallsAsync();
        // Filter halls that have all requested features
        // This assumes features are stored as a property or related entity
        return allHalls.ToList(); // Simplified implementation
    }

    public async Task<List<Hall>> GetNearbyHallsAsync(double latitude, double longitude, double radiusKm)
    {
        var allHalls = await _unitOfWork.HallRepository.GetActiveHallsAsync();
        // Filter by distance (simplified implementation)
        // In a real scenario, you'd use spatial queries or distance calculations
        return allHalls.ToList(); // Simplified implementation
    }

    public async Task<List<Hall>> GetHallsRequiringApprovalAsync()
    {
        var allHalls = await _unitOfWork.HallRepository.GetAllAsync();
        // Filter halls that need approval (assuming there's an approval status)
        return allHalls.Where(h => !h.IsActive).ToList();
    }

    public async Task<bool> ApproveHallAsync(int hallId)
    {
        var hall = await _unitOfWork.HallRepository.GetByIdAsync(hallId);
        if (hall == null) return false;
        
        hall.IsActive = true;
        _unitOfWork.HallRepository.Update(hall);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<bool> RejectHallAsync(int hallId, string reason)
    {
        var hall = await _unitOfWork.HallRepository.GetByIdAsync(hallId);
        if (hall == null) return false;
        
        // Mark hall as rejected (set Active to false)
        hall.IsActive = false;
        _unitOfWork.HallRepository.Update(hall);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<bool> UpdateHallAvailabilityAsync(int hallId, List<DateTime> availableDates)
    {
        // This would typically involve updating a separate availability table
        // For now, we'll just verify the hall exists
        var hall = await _unitOfWork.HallRepository.GetByIdAsync(hallId);
        return hall != null;
    }

    public async Task<List<DateTime>> GetHallAvailableDatesAsync(int hallId)
    {
        // Get dates that don't have confirmed bookings
        var hall = await _unitOfWork.HallRepository.GetByIdAsync(hallId);
        if (hall == null) return new List<DateTime>();
        
        // This is a simplified implementation - in reality you'd check booking conflicts
        var availableDates = new List<DateTime>();
        var currentDate = DateTime.Today;
        for (int i = 0; i < 30; i++) // Next 30 days
        {
            availableDates.Add(currentDate.AddDays(i));
        }
        return availableDates;
    }

    public async Task<decimal> CalculateHallRevenueAsync(int hallId, DateTime startDate, DateTime endDate)
    {
        var bookings = await _unitOfWork.BookingRepository.GetBookingsByHallIdAsync(hallId);
        var filteredBookings = bookings.Where(b => b.EventDate >= startDate && b.EventDate <= endDate);
        return filteredBookings.Sum(b => (decimal)b.TotalAmount);
    }

    public async Task<bool> SetHallMaintenanceModeAsync(int hallId, bool isMaintenanceMode)
    {
        var hall = await _unitOfWork.HallRepository.GetByIdAsync(hallId);
        if (hall == null) return false;
        
        // Set maintenance mode (using Active property as proxy)
        hall.IsActive = !isMaintenanceMode;
        _unitOfWork.HallRepository.Update(hall);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<List<Hall>> GetHallsInMaintenanceAsync()
    {
        var allHalls = await _unitOfWork.HallRepository.GetAllAsync();
        // Return inactive halls as maintenance mode
        return allHalls.Where(h => !h.IsActive).ToList();
    }

    public async Task<bool> UpdateHallRatingAsync(int hallId)
    {
        var reviews = await _unitOfWork.ReviewRepository.GetReviewsByHallIdAsync(hallId);
        if (!reviews.Any()) return true;
        
        var hall = await _unitOfWork.HallRepository.GetByIdAsync(hallId);
        if (hall == null) return false;
        
        hall.AverageRating = reviews.Average(r => r.Rating);
        _unitOfWork.HallRepository.Update(hall);
        await _unitOfWork.Complete();
        return true;
    }

    public async Task<double> GetHallAverageRatingAsync(int hallId)
    {
        var hall = await _unitOfWork.HallRepository.GetByIdAsync(hallId);
        if (hall == null) return 0.0;
        
        return hall.AverageRating;
    }

    public async Task<int> GetHallReviewCountAsync(int hallId)
    {
        var reviews = await _unitOfWork.ReviewRepository.GetReviewsByHallIdAsync(hallId);
        return reviews.Count();
    }

    public async Task<List<Hall>> GetTopRatedHallsAsync(int count)
    {
        var halls = await _unitOfWork.HallRepository.GetAllAsync();
        return halls.OrderByDescending(h => h.AverageRating)
                   .Take(count)
                   .ToList();
    }

    public async Task<List<Hall>> GetAvailableHallsAsync(DateTime eventDate, int? excludeHallId = null)
    {
        // Get all active halls
        var allHalls = await _unitOfWork.HallRepository.GetAllAsync();
        var activeHalls = allHalls.Where(h => h.IsActive).ToList();
        
        // Exclude the specified hall if provided
        if (excludeHallId.HasValue)
        {
            activeHalls = activeHalls.Where(h => h.ID != excludeHallId.Value).ToList();
        }
        
        // Get all bookings for the event date
        var allBookings = await _unitOfWork.BookingRepository.GetAllAsync();
        var bookingsOnDate = allBookings.Where(b => b.EventDate.Date == eventDate.Date).ToList();
        var confirmedBookings = bookingsOnDate.Where(b => 
            b.IsBookingConfirmed || 
            b.Status == "Confirmed" || 
            b.Status == "HallApproved" ||
            b.Status == "VendorApprovalInProgress").ToList();
        
        // Get hall IDs that are already booked
        var bookedHallIds = confirmedBookings.Select(b => b.HallId).ToHashSet();
        
        // Filter out booked halls
        var availableHalls = activeHalls.Where(h => !bookedHallIds.Contains(h.ID)).ToList();
        
        return availableHalls;
    }

    public async Task<bool> UpdateHallManagersAsync(int hallId, List<int> managerIds)
    {
        try
        {
            var hall = await _unitOfWork.HallRepository.GetByIdAsync(hallId);
            if (hall == null) return false;

            // Get the hall managers by their IDs
            var managers = new List<HallManager>();
            foreach (var managerId in managerIds)
            {
                var manager = await _unitOfWork.HallManagerRepository.GetByIdAsync(managerId);
                if (manager != null)
                {
                    managers.Add(manager);
                }
            }

            // Clear existing managers and add new ones
            hall.Managers?.Clear();
            hall.Managers = managers;

            await _unitOfWork.Complete();
            return true;
        }
        catch
        {
            return false;
        }
    }

}
