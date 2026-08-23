using System.ComponentModel.DataAnnotations;
using HallApp.Core.Entities;

namespace HallApp.Core.Entities.VendorEntities;

/// <summary>
/// Where a business sits in the joining process.
/// </summary>
public enum VendorApplicationStatus
{
    /// <summary>Registered, still uploading papers. Nothing for an admin to do yet.</summary>
    Draft = 0,

    /// <summary>Submitted for review. Appears in the admin queue.</summary>
    UnderReview = 1,

    /// <summary>
    /// At least one document was rejected. Back with the applicant, who re-uploads
    /// only the documents that failed.
    /// </summary>
    ChangesRequested = 2,

    /// <summary>
    /// Every required document approved. The vendor record is created and the owner
    /// can sign in, but it stays unlisted until they publish it themselves.
    /// </summary>
    Approved = 3,

    /// <summary>Turned down outright. RejectionReason says why.</summary>
    Rejected = 4
}

/// <summary>
/// A business asking to join the platform: who they are, what they do, and the
/// papers backing it up.
///
/// Deliberately separate from Vendor. An application is a request that may never be
/// granted, and keeping it apart means an unapproved applicant can never appear in
/// public vendor listings by accident. The Vendor row is created only on approval.
/// </summary>
public class VendorApplication
{
    public int Id { get; set; }

    // --- The business ------------------------------------------------------

    [Required]
    [StringLength(150)]
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>Catering, Photography, Restaurant, and so on.</summary>
    public int VendorTypeId { get; set; }
    public VendorType? VendorType { get; set; }

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [StringLength(150)]
    public string ContactEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string ContactPhone { get; set; } = string.Empty;

    [StringLength(150)]
    public string ContactPersonName { get; set; } = string.Empty;

    [StringLength(50)]
    public string CommercialRegistrationNumber { get; set; } = string.Empty;

    [StringLength(50)]
    public string VatNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(300)]
    public string Address { get; set; } = string.Empty;

    // --- The account -------------------------------------------------------

    /// <summary>
    /// The user created at registration. They can sign in immediately to manage the
    /// application, but hold no vendor role until it is approved.
    /// </summary>
    public int AppUserId { get; set; }
    public AppUser? AppUser { get; set; }

    // --- Review ------------------------------------------------------------

    public VendorApplicationStatus Status { get; set; } = VendorApplicationStatus.Draft;

    /// <summary>Set when the whole application is turned down, as opposed to one document.</summary>
    [StringLength(1000)]
    public string RejectionReason { get; set; } = string.Empty;

    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Set on approval; the vendor this application became.</summary>
    public int? CreatedVendorId { get; set; }

    /// <summary>Set on approval; the organization created to own that vendor.</summary>
    public int? CreatedOrganizationId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }

    public List<VendorDocument> Documents { get; set; } = new();
}
