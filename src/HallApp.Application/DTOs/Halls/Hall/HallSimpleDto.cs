namespace HallApp.Application.DTOs.Halls.Hall;

/// <summary>
/// Simplified Hall DTO without Managers to prevent circular references
/// </summary>
public class HallSimpleDto
{
    public int ID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int Gender { get; set; }
    public bool IsActive { get; set; }
}
