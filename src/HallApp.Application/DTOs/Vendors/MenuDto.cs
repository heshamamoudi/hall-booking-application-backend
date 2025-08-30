namespace HallApp.Application.DTOs.Vendors;

public class MenuDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<MenuItemDto> MenuItems { get; set; } = new();
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
}

public class CreateMenuDto
{
    public string Name { get; set; } = string.Empty;
    public List<CreateMenuItemDto> MenuItems { get; set; } = new();
}

public class UpdateMenuDto
{
    public string Name { get; set; } = string.Empty;
    public List<UpdateMenuItemDto> MenuItems { get; set; } = new();
}

public class DeleteMenuDto
{
    public int Id { get; set; }
}
