using HallApp.Application.DTOs.Champer.Hall;

namespace HallApp.Application.DTOs.Champer.Contact;

public class ContactDto
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Phone { get; set; }
    public int HallID { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
}
