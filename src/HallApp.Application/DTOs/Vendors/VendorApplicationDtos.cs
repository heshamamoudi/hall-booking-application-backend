using System.ComponentModel.DataAnnotations;
using HallApp.Core.Entities.VendorEntities;

namespace HallApp.Application.DTOs.Vendors;

/// <summary>What a business fills in on the public registration form.</summary>
public class RegisterVendorApplicationDto
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string BusinessName { get; set; } = string.Empty;

    /// <summary>"Hall" or "Vendor". Defaults to Vendor when omitted.</summary>
    [StringLength(20)]
    public string ApplicationType { get; set; } = "Vendor";

    /// <summary>Required for a vendor, ignored for a hall.</summary>
    public int? VendorTypeId { get; set; }

    [StringLength(1000)]
    public string Description { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string ContactEmail { get; set; } = string.Empty;

    [Required, StringLength(30, MinimumLength = 8)]
    public string ContactPhone { get; set; } = string.Empty;

    [Required, StringLength(150)]
    public string ContactPersonName { get; set; } = string.Empty;

    [StringLength(50)]
    public string CommercialRegistrationNumber { get; set; } = string.Empty;

    [StringLength(50)]
    public string VatNumber { get; set; } = string.Empty;

    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [StringLength(300)]
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// Password for the account created alongside the application, so the applicant
    /// can sign back in to upload documents and follow progress.
    /// </summary>
    [Required, StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;
}

public class VendorDocumentDto
{
    public int Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ReviewComment { get; set; } = string.Empty;
    public DateTime? ReviewedAt { get; set; }
    public DateTime UploadedAt { get; set; }
    public int Version { get; set; }
}

public class VendorApplicationDto
{
    public int Id { get; set; }
    public string BusinessName { get; set; } = string.Empty;
    public string ApplicationType { get; set; } = string.Empty;
    public int? VendorTypeId { get; set; }
    public string VendorTypeName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string ContactPersonName { get; set; } = string.Empty;
    public string CommercialRegistrationNumber { get; set; } = string.Empty;
    public string VatNumber { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string RejectionReason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? CreatedVendorId { get; set; }
    public int? CreatedHallId { get; set; }

    public List<VendorDocumentDto> Documents { get; set; } = new();

    /// <summary>Document types this applicant still has to supply or replace.</summary>
    public List<string> OutstandingDocuments { get; set; } = new();

    /// <summary>True when every required document is approved.</summary>
    public bool IsComplete { get; set; }
}

/// <summary>An administrator's decision on one document.</summary>
public class ReviewDocumentDto
{
    [Required]
    public bool Approved { get; set; }

    /// <summary>
    /// Required when rejecting. An applicant cannot fix a paper without being told
    /// what is wrong with it.
    /// </summary>
    [StringLength(1000)]
    public string Comment { get; set; } = string.Empty;
}

/// <summary>An administrator turning down a whole application.</summary>
public class RejectApplicationDto
{
    [Required, StringLength(1000, MinimumLength = 3)]
    public string Reason { get; set; } = string.Empty;
}

/// <summary>What the public registration page needs to render its category list.</summary>
public class VendorApplicationRequirementsDto
{
    /// <summary>"Hall" or "Vendor" - which kind of business this option registers.</summary>
    public string ApplicationType { get; set; } = string.Empty;

    public int? VendorTypeId { get; set; }
    public string VendorTypeName { get; set; } = string.Empty;
    public List<string> RequiredDocuments { get; set; } = new();
}
