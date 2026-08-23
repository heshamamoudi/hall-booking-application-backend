using System.ComponentModel.DataAnnotations;

namespace HallApp.Core.Entities.VendorEntities;

/// <summary>
/// Where a single uploaded paper stands in review. Status is per document, not per
/// applicant: an admin can accept the commercial registration and reject the VAT
/// certificate in the same sitting, and the applicant re-uploads only what failed.
/// </summary>
public enum VendorDocumentStatus
{
    /// <summary>Uploaded, not yet looked at.</summary>
    Pending = 0,

    /// <summary>Accepted by an administrator.</summary>
    Approved = 1,

    /// <summary>Rejected. ReviewComment says why, and is always required.</summary>
    Rejected = 2
}

/// <summary>
/// The kinds of paper a business is asked for. Stored as a string so adding a type
/// later does not require a migration of existing rows.
/// </summary>
public static class VendorDocumentTypes
{
    public const string CommercialRegistration = "CommercialRegistration";
    public const string VatCertificate = "VatCertificate";
    public const string NationalAddress = "NationalAddress";
    public const string OwnerIdentification = "OwnerIdentification";
    public const string MunicipalityLicence = "MunicipalityLicence";
    public const string FoodSafetyCertificate = "FoodSafetyCertificate";
    public const string Other = "Other";

    /// <summary>
    /// What every applicant must supply. FoodSafetyCertificate is deliberately not
    /// here - it is added for restaurants and caterers by RequiredFor.
    /// </summary>
    public static readonly string[] AlwaysRequired =
    {
        CommercialRegistration,
        VatCertificate,
        NationalAddress,
        OwnerIdentification
    };

    /// <summary>
    /// The document set required of a given vendor category. Anyone handling food
    /// is additionally asked for a food safety certificate.
    /// </summary>
    public static string[] RequiredFor(string? vendorTypeName)
    {
        var handlesFood = vendorTypeName is not null
            && (vendorTypeName.Contains("Catering", StringComparison.OrdinalIgnoreCase)
                || vendorTypeName.Contains("Restaurant", StringComparison.OrdinalIgnoreCase));

        return handlesFood
            ? AlwaysRequired.Append(FoodSafetyCertificate).ToArray()
            : AlwaysRequired;
    }
}

public class VendorDocument
{
    public int Id { get; set; }

    /// <summary>The application this paper belongs to.</summary>
    public int VendorApplicationId { get; set; }
    public VendorApplication VendorApplication { get; set; } = null!;

    [Required]
    [StringLength(60)]
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Name the applicant uploaded it under, kept for display only.</summary>
    [StringLength(260)]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Public URL under /uploads/vendor-documents/{applicationId}/. The stored name
    /// is a GUID, never the client-supplied one.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string FileUrl { get; set; } = string.Empty;

    [StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public VendorDocumentStatus Status { get; set; } = VendorDocumentStatus.Pending;

    /// <summary>
    /// Why it was rejected, or an optional note on approval. Required to reject:
    /// an applicant cannot fix a paper without being told what is wrong with it.
    /// </summary>
    [StringLength(1000)]
    public string ReviewComment { get; set; } = string.Empty;

    /// <summary>AppUser id of the administrator who decided.</summary>
    public int? ReviewedByUserId { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Bumped when a rejected document is replaced. The row is reused rather than
    /// duplicated so the review history stays on one line per document type.
    /// </summary>
    public int Version { get; set; } = 1;
}
