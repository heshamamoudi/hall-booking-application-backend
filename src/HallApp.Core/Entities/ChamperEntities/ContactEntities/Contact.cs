namespace HallApp.Core.Entities.ChamperEntities.ContactEntities;

public class Contact
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public int HallID { get; set; }
    public Hall Hall { get; set; }
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
}
