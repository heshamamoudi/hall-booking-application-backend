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
}
