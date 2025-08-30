using HallApp.Application.DTOs.Customer;

namespace HallApp.Application.DTOs.Customer;

public class UserCustomerDto
{
    public UserDto User { get; set; }
    public CustomerDto Customer { get; set; } 
}
