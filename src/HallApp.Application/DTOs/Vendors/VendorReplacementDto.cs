namespace HallApp.Application.DTOs.Vendors
{
    public class VendorReplacementDto
    {
        public int RejectedVendorId { get; set; }
        public int NewVendorId { get; set; }
        public List<int> NewServiceIds { get; set; } = new();
    }
}
