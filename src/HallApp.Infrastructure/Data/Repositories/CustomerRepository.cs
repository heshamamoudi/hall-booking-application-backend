using HallApp.Core.Entities.CustomerEntities;
using HallApp.Core.Interfaces.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace HallApp.Infrastructure.Data.Repositories;

public class CustomerRepository : GenericRepository<Customer>, ICustomerRepository
{
    public CustomerRepository(DataContext context) : base(context)
    {
    }

    // Override to include AppUser navigation property
    public override async Task<IEnumerable<Customer>> GetAllAsync()
    {
        return await _context.Customers
            .Include(c => c.AppUser)
            .Include(c => c.Addresses)
            .Include(c => c.Favorites)
            .Include(c => c.Bookings)
            .ToListAsync();
    }

    // Override to include AppUser navigation property
    public override async Task<Customer> GetByIdAsync(int id)
    {
        return await _context.Customers
            .Include(c => c.AppUser)
            .Include(c => c.Addresses)
            .Include(c => c.Favorites)
            .Include(c => c.Bookings)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Customer> GetCustomerByAppUserIdAsync(int appUserId)
    {
        return await _context.Customers
            .Include(c => c.AppUser)
            .Include(c => c.Addresses)
            .Include(c => c.Favorites)
            .Include(c => c.Bookings)
            .FirstOrDefaultAsync(c => c.AppUserId == appUserId);
    }

    public async Task<Customer> GetCustomerWithAddressesAsync(int customerId)
    {
        return await _context.Customers
            .Include(c => c.AppUser)
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == customerId);
    }

    public async Task<Customer> GetCustomerWithBookingsAsync(int customerId)
    {
        return await _context.Customers
            .Include(c => c.AppUser)
            .Include(c => c.Bookings)
                .ThenInclude(b => b.VendorBookings)
            .FirstOrDefaultAsync(c => c.Id == customerId);
    }

    public async Task<IEnumerable<Customer>> GetCustomersWithOrdersAsync()
    {
        return await _context.Customers
            .Include(c => c.AppUser)
            .Include(c => c.Bookings)
            .Where(c => c.Bookings.Any())
            .OrderByDescending(c => c.NumberOfOrders)
            .ToListAsync();
    }
}
