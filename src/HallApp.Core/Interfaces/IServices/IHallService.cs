using HallApp.Core.Entities.ChamperEntities;

namespace HallApp.Core.Interfaces.IServices;

/// <summary>
/// ISP FIX (Fix #8): IHallService now extends both IHallQueryService and IHallCommandService.
/// Existing consumers continue to work with IHallService. New consumers should depend on
/// IHallQueryService or IHallCommandService based on their actual needs.
/// </summary>
public interface IHallService : IHallQueryService, IHallCommandService
{
    // All members are inherited from IHallQueryService and IHallCommandService.
    // This interface exists for backward compatibility with existing DI registrations
    // and consumers that need both read and write access.
}
