namespace HallApp.Application.DTOs.Admin;

/// <summary>
/// The domain records attached to a login. Each is null when the user has no such
/// record - a plain administrator has none of them, a hall manager has one.
/// </summary>
public class LinkedEntitiesDto
{
    public int? CustomerId { get; set; }
    public int? HallManagerId { get; set; }
    public int? VendorManagerId { get; set; }
}
