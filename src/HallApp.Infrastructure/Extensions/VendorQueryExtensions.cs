using Microsoft.EntityFrameworkCore;
using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Infrastructure.Extensions;

public static class VendorQueryExtensions
{
    public static IQueryable<Vendor> IncludeAllRelations(this IQueryable<Vendor> query)
    {
        return query
            .Include(v => v.VendorType)
            .Include(v => v.Location)
            .Include(v => v.ServiceItems)
            .Include(v => v.Managers)
                .ThenInclude(m => m.AppUser)
            .Include(v => v.BusinessHours)
            .Include(v => v.BlockedDates);
    }

    public static IQueryable<Vendor> IncludeBasicRelations(this IQueryable<Vendor> query)
    {
        return query
            .Include(v => v.VendorType)
            .Include(v => v.Location);
    }

    public static IQueryable<Vendor> IncludeForDetails(this IQueryable<Vendor> query)
    {
        return query
            .Include(v => v.VendorType)
            .Include(v => v.Location)
            .Include(v => v.ServiceItems)
            .Include(v => v.BusinessHours);
    }
}
