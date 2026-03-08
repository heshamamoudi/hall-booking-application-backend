using HallApp.Core.Entities.ChamperEntities;

namespace HallApp.Core.Interfaces.IServices;

/// <summary>
/// ISP FIX (Fix #8): Command (write) operations for halls.
/// Only consumers that modify halls should depend on this interface.
/// </summary>
public interface IHallCommandService
{
    Task<Hall> CreateHallAsync(Hall hall);
    Task<Hall> UpdateHallAsync(Hall hall);
    Task<Hall?> ManagerUpdateHallAsync(Hall hall, string managerId);
    Task<bool> DeleteHallAsync(int hallId);
    Task<Hall?> ToggleHallStatusAsync(int hallId);
    Task<bool> UpdateHallImagesAsync(int hallId, List<string> imageUrls);
    Task<bool> ApproveHallAsync(int hallId);
    Task<bool> RejectHallAsync(int hallId, string reason);
    Task<bool> UpdateHallAvailabilityAsync(int hallId, List<DateTime> availableDates);
    Task<bool> SetHallMaintenanceModeAsync(int hallId, bool isMaintenanceMode);
    Task<bool> UpdateHallRatingAsync(int hallId);
    Task<bool> UpdateHallManagersAsync(int hallId, List<int> managerIds);
    Task<bool> AssignHallToManagerAsync(int hallId, int hallManagerId);
}
