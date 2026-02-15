using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HallApp.Core.Entities.CustomerEntities;
using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Entities.ChamperEntities;

namespace HallApp.Core.Entities.ReviewEntities;

public class Review
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime Created { get; set; } = DateTime.UtcNow;
    public DateTime Updated { get; set; } = DateTime.UtcNow;
    public bool IsApproved { get; set; } = true;
    public bool IsFlagged { get; set; } = false;
    public string RejectionReason { get; set; } = string.Empty;

    // Manager response fields (one response per review)
    public string? ManagerResponse { get; set; }
    public DateTime? ManagerResponseDate { get; set; }
    public int? ManagerResponderId { get; set; }
    public AppUser? ManagerResponder { get; set; }

    // Flag reason (when hall manager flags inappropriate content)
    public string? FlagReason { get; set; }
    public DateTime? FlaggedDate { get; set; }
    public int? FlaggedByUserId { get; set; }
    public AppUser? FlaggedByUser { get; set; }

    // Relationships
    public int? CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public int HallId { get; set; }
    public Hall Hall { get; set; } = null!;

    public int? VendorId { get; set; }
    [ForeignKey("VendorId")]
    public Vendor Vendor { get; set; } = null!;
}
