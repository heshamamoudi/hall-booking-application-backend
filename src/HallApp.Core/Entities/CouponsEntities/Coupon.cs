namespace HallApp.Core.Entities.CouponsEntities;

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
}
