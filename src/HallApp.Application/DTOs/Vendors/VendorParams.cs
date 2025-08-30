namespace HallApp.Application.DTOs.Vendors;

public class VendorParams
{
    private const int MaxPageSize = 50;
    private int _pageSize = 10;
    private string _searchTerm;

    public int PageNumber { get; set; } = 1;
    
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
    }

    public string SearchTerm
    {
        get => _searchTerm;
        set => _searchTerm = value?.Trim().ToLower();
    }

    public int? VendorTypeId { get; set; }
    public bool? IsActive { get; set; }
    public string OrderBy { get; set; } = "name";
}
