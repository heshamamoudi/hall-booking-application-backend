using Microsoft.EntityFrameworkCore;
using HallApp.Core.Entities.ChamperEntities;

namespace HallApp.Infrastructure.Extensions;

public static class HallQueryExtensions
{
    public static IQueryable<Hall> IncludeAllRelations(this IQueryable<Hall> query)
    {
        return query
            .Include(h => h.Location)
            .Include(h => h.MediaFiles)
            .Include(h => h.Packages)
            .Include(h => h.Services)
            .Include(h => h.Contacts)
            .Include(h => h.Managers)
                .ThenInclude(m => m.AppUser)
            .Include(h => h.Reviews);
    }

    public static IQueryable<Hall> IncludeBasicRelations(this IQueryable<Hall> query)
    {
        return query
            .Include(h => h.Location)
            .Include(h => h.MediaFiles);
    }

    public static IQueryable<Hall> IncludeForDetails(this IQueryable<Hall> query)
    {
        return query
            .Include(h => h.Location)
            .Include(h => h.MediaFiles)
            .Include(h => h.Packages)
            .Include(h => h.Services)
            .Include(h => h.Contacts)
            .Include(h => h.Reviews);
    }
}
