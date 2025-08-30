namespace HallApp.Core.Entities;

public class BaseEntity
{
    public int Id { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime? Updated { get; set; }
    public bool IsActive { get; set; } = true;
}
