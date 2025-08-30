namespace HallApp.Application.DTOs.Customer;

public class AddressDto
{
    public int Id { get; set; } 
    public string AddressName { get; set; }
    public string City { get; set; } // Added City field
    public string Street1 { get; set; } // Added Street1 field
    public string Street2 { get; set; } // Added Street2 field
    public int PostalCode { get; set; } // Added PostalCode field
    public int ZipCode { get; set; } // Added ZipCode field
    public bool IsMain { get; set; }
    public int CustomerId { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
}
