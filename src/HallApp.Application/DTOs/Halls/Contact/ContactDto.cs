using HallApp.Application.DTOs.Halls.Hall;

namespace HallApp.Application.DTOs.Halls.Contact;

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
