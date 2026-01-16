using HallApp.Application.DTOs.Auth;

namespace HallApp.Application.DTOs.Customer;

public class UserCustomerDto
{
    public UserDto User { get; set; }
    public CustomerDto Customer { get; set; } 
}
