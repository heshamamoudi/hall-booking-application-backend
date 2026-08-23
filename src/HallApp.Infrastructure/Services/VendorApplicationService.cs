using HallApp.Application.DTOs.Vendors;
using HallApp.Core.Constants;
using HallApp.Core.Entities;
using HallApp.Core.Entities.VendorEntities;
using HallApp.Application.Services;
using HallApp.Core.Interfaces.IServices;
using HallApp.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HallApp.Infrastructure.Services;

/// <summary>Outcome of an operation, so controllers can map to a status code without exceptions.</summary>
public record VendorApplicationResult(bool Success, string Message, VendorApplicationDto? Data = null, int StatusCode = 200)
{
    public static VendorApplicationResult Ok(VendorApplicationDto? data, string message = "OK") => new(true, message, data);
    public static VendorApplicationResult Fail(string message, int statusCode) => new(false, message, null, statusCode);
}

public interface IVendorApplicationService
{
    Task<List<VendorApplicationRequirementsDto>> GetRequirementsAsync(CancellationToken ct = default);
    Task<VendorApplicationResult> RegisterAsync(RegisterVendorApplicationDto dto, CancellationToken ct = default);
    Task<VendorApplicationDto?> GetForUserAsync(int appUserId, CancellationToken ct = default);
    Task<VendorApplicationDto?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<VendorApplicationDto>> GetQueueAsync(VendorApplicationStatus? status, CancellationToken ct = default);
    Task<VendorApplicationResult> AttachDocumentAsync(int applicationId, string documentType, string fileUrl, string originalFileName, string contentType, long sizeBytes, CancellationToken ct = default);
    Task<VendorApplicationResult> SubmitAsync(int applicationId, CancellationToken ct = default);
    Task<VendorApplicationResult> ReviewDocumentAsync(int applicationId, int documentId, bool approved, string comment, int reviewerUserId, CancellationToken ct = default);
    Task<VendorApplicationResult> RejectAsync(int applicationId, string reason, int reviewerUserId, CancellationToken ct = default);
}

/// <summary>
/// Registration and admin review for businesses joining the platform.
///
/// The shape of the flow: registering creates a user and a Draft application;
/// documents are uploaded against it; submitting moves it to UnderReview; an admin
/// decides each document individually. Rejecting one sends the application back as
/// ChangesRequested without touching the others. When the last required document is
/// approved the application is approved automatically - which creates the
/// organization, the vendor and the owner's role.
///
/// The vendor is created approved but INACTIVE. The owner can sign in and set up
/// services, photographs and hours, and nothing is publicly visible until they
/// publish it themselves.
/// </summary>
public class VendorApplicationService : IVendorApplicationService
{
    private readonly DataContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly IEmailService _emailService;
    private readonly ILogger<VendorApplicationService> _logger;

    public VendorApplicationService(
        DataContext context,
        UserManager<AppUser> userManager,
        IEmailService emailService,
        ILogger<VendorApplicationService> logger)
    {
        _context = context;
        _userManager = userManager;
        _emailService = emailService;
        _logger = logger;
    }

    private DbSet<VendorApplication> Applications => _context.Set<VendorApplication>();
    private DbSet<VendorDocument> Documents => _context.Set<VendorDocument>();
    private DbSet<VendorType> VendorTypes => _context.Set<VendorType>();
    private DbSet<Vendor> Vendors => _context.Set<Vendor>();
    private DbSet<Organization> Organizations => _context.Set<Organization>();

    // ===================================================================
    // Public
    // ===================================================================

    public async Task<List<VendorApplicationRequirementsDto>> GetRequirementsAsync(CancellationToken ct = default)
    {
        var types = await VendorTypes.Where(t => t.IsActive).OrderBy(t => t.SortOrder).ToListAsync(ct);

        return types.Select(t => new VendorApplicationRequirementsDto
        {
            VendorTypeId = t.Id,
            VendorTypeName = t.Name,
            RequiredDocuments = VendorDocumentTypes.RequiredFor(t.Name).ToList()
        }).ToList();
    }

    public async Task<VendorApplicationResult> RegisterAsync(RegisterVendorApplicationDto dto, CancellationToken ct = default)
    {
        var vendorType = await VendorTypes.FirstOrDefaultAsync(t => t.Id == dto.VendorTypeId, ct);
        if (vendorType == null)
            return VendorApplicationResult.Fail("Unknown vendor category", 400);

        var email = dto.ContactEmail.Trim();

        if (await _userManager.FindByEmailAsync(email) is not null)
        {
            // Deliberately the same wording a duplicate customer sign-up gets, so
            // this endpoint cannot be used to enumerate registered addresses.
            return VendorApplicationResult.Fail(
                "If that email can be registered, you will receive a confirmation shortly.", 409);
        }

        var user = new AppUser
        {
            UserName = email,
            Email = email,
            PhoneNumber = dto.ContactPhone,
            FirstName = dto.ContactPersonName,
            LastName = string.Empty,
            Created = DateTime.UtcNow,
            Updated = DateTime.UtcNow,
            Active = true
        };

        var created = await _userManager.CreateAsync(user, dto.Password);
        if (!created.Succeeded)
        {
            return VendorApplicationResult.Fail(
                string.Join("; ", created.Errors.Select(e => e.Description)), 400);
        }

        // No vendor role yet. The account exists so the applicant can sign back in
        // and manage the application; it gains VendorOrganizationManager only on
        // approval, so an unapproved user can never reach vendor endpoints.

        var application = new VendorApplication
        {
            BusinessName = dto.BusinessName.Trim(),
            VendorTypeId = dto.VendorTypeId,
            Description = dto.Description,
            ContactEmail = email,
            ContactPhone = dto.ContactPhone,
            ContactPersonName = dto.ContactPersonName,
            CommercialRegistrationNumber = dto.CommercialRegistrationNumber,
            VatNumber = dto.VatNumber,
            City = dto.City,
            Address = dto.Address,
            AppUserId = user.Id,
            Status = VendorApplicationStatus.Draft
        };

        Applications.Add(application);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Vendor application {ApplicationId} registered for {BusinessName}",
            application.Id, application.BusinessName);

        return VendorApplicationResult.Ok(await MapAsync(application, ct), "Application created");
    }

    // ===================================================================
    // Applicant
    // ===================================================================

    public async Task<VendorApplicationDto?> GetForUserAsync(int appUserId, CancellationToken ct = default)
    {
        var application = await LoadQuery().FirstOrDefaultAsync(a => a.AppUserId == appUserId, ct);
        return application == null ? null : await MapAsync(application, ct);
    }

    public async Task<VendorApplicationDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var application = await LoadQuery().FirstOrDefaultAsync(a => a.Id == id, ct);
        return application == null ? null : await MapAsync(application, ct);
    }

    public async Task<List<VendorApplicationDto>> GetQueueAsync(VendorApplicationStatus? status, CancellationToken ct = default)
    {
        var query = LoadQuery();
        if (status.HasValue) query = query.Where(a => a.Status == status.Value);

        var applications = await query
            .OrderBy(a => a.SubmittedAt ?? a.CreatedAt)
            .ToListAsync(ct);

        var result = new List<VendorApplicationDto>();
        foreach (var application in applications)
        {
            result.Add(await MapAsync(application, ct));
        }
        return result;
    }

    public async Task<VendorApplicationResult> AttachDocumentAsync(
        int applicationId, string documentType, string fileUrl, string originalFileName,
        string contentType, long sizeBytes, CancellationToken ct = default)
    {
        var application = await LoadQuery().FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (application == null)
            return VendorApplicationResult.Fail("Application not found", 404);

        if (application.Status is VendorApplicationStatus.Approved or VendorApplicationStatus.Rejected)
            return VendorApplicationResult.Fail("This application is closed and cannot be changed", 409);

        var existing = application.Documents.FirstOrDefault(d => d.DocumentType == documentType);

        if (existing == null)
        {
            application.Documents.Add(new VendorDocument
            {
                VendorApplicationId = application.Id,
                DocumentType = documentType,
                OriginalFileName = originalFileName,
                FileUrl = fileUrl,
                ContentType = contentType,
                FileSizeBytes = sizeBytes,
                Status = VendorDocumentStatus.Pending
            });
        }
        else
        {
            // Replacing a document resets its decision. Reusing the row keeps one
            // line per document type rather than accumulating rejected copies.
            existing.OriginalFileName = originalFileName;
            existing.FileUrl = fileUrl;
            existing.ContentType = contentType;
            existing.FileSizeBytes = sizeBytes;
            existing.Status = VendorDocumentStatus.Pending;
            existing.ReviewComment = string.Empty;
            existing.ReviewedAt = null;
            existing.ReviewedByUserId = null;
            existing.UploadedAt = DateTime.UtcNow;
            existing.Version += 1;
        }

        // A re-upload after changes were requested puts it back in the queue.
        if (application.Status == VendorApplicationStatus.ChangesRequested)
        {
            application.Status = VendorApplicationStatus.UnderReview;
        }

        application.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return VendorApplicationResult.Ok(await MapAsync(application, ct), "Document uploaded");
    }

    public async Task<VendorApplicationResult> SubmitAsync(int applicationId, CancellationToken ct = default)
    {
        var application = await LoadQuery().FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (application == null)
            return VendorApplicationResult.Fail("Application not found", 404);

        if (application.Status is VendorApplicationStatus.Approved or VendorApplicationStatus.Rejected)
            return VendorApplicationResult.Fail("This application is closed", 409);

        var missing = AwaitingUpload(application);
        if (missing.Count > 0)
        {
            return VendorApplicationResult.Fail(
                $"Still missing: {string.Join(", ", missing)}", 400);
        }

        application.Status = VendorApplicationStatus.UnderReview;
        application.SubmittedAt ??= DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        await _emailService.SendAsync(VendorApplicationEmails.Submitted(application), ct);

        return VendorApplicationResult.Ok(await MapAsync(application, ct), "Application submitted for review");
    }

    // ===================================================================
    // Administrator
    // ===================================================================

    public async Task<VendorApplicationResult> ReviewDocumentAsync(
        int applicationId, int documentId, bool approved, string comment, int reviewerUserId,
        CancellationToken ct = default)
    {
        var application = await LoadQuery().FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (application == null)
            return VendorApplicationResult.Fail("Application not found", 404);

        var document = application.Documents.FirstOrDefault(d => d.Id == documentId);
        if (document == null)
            return VendorApplicationResult.Fail("Document not found on this application", 404);

        // A rejection the applicant cannot act on is worse than no rejection.
        if (!approved && string.IsNullOrWhiteSpace(comment))
            return VendorApplicationResult.Fail("A comment is required when rejecting a document", 400);

        document.Status = approved ? VendorDocumentStatus.Approved : VendorDocumentStatus.Rejected;
        document.ReviewComment = comment ?? string.Empty;
        document.ReviewedByUserId = reviewerUserId;
        document.ReviewedAt = DateTime.UtcNow;

        if (!approved)
        {
            // Back to the applicant, but only this document needs replacing.
            application.Status = VendorApplicationStatus.ChangesRequested;
        }

        application.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        await _emailService.SendAsync(
            approved
                ? VendorApplicationEmails.DocumentApproved(application, document)
                : VendorApplicationEmails.DocumentRejected(application, document),
            ct);

        // Last required document approved: the application is granted.
        if (approved && AwaitingApproval(application).Count == 0)
        {
            return await ApproveAsync(application, reviewerUserId, ct);
        }

        return VendorApplicationResult.Ok(await MapAsync(application, ct),
            approved ? "Document approved" : "Document rejected");
    }

    public async Task<VendorApplicationResult> RejectAsync(
        int applicationId, string reason, int reviewerUserId, CancellationToken ct = default)
    {
        var application = await LoadQuery().FirstOrDefaultAsync(a => a.Id == applicationId, ct);
        if (application == null)
            return VendorApplicationResult.Fail("Application not found", 404);

        if (application.Status == VendorApplicationStatus.Approved)
            return VendorApplicationResult.Fail("This application has already been approved", 409);

        application.Status = VendorApplicationStatus.Rejected;
        application.RejectionReason = reason;
        application.ReviewedByUserId = reviewerUserId;
        application.ReviewedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        await _emailService.SendAsync(VendorApplicationEmails.Rejected(application), ct);

        return VendorApplicationResult.Ok(await MapAsync(application, ct), "Application rejected");
    }

    /// <summary>
    /// Grants the application: creates the organization, the vendor and the owner's
    /// role. The vendor is IsApproved but IsActive = false, so the owner can set it
    /// up privately and publish when ready.
    /// </summary>
    private async Task<VendorApplicationResult> ApproveAsync(
        VendorApplication application, int reviewerUserId, CancellationToken ct)
    {
        if (application.CreatedVendorId.HasValue)
        {
            return VendorApplicationResult.Ok(await MapAsync(application, ct), "Application already approved");
        }

        var owner = await _userManager.FindByIdAsync(application.AppUserId.ToString());
        if (owner == null)
            return VendorApplicationResult.Fail("The applicant's account no longer exists", 409);

        var organization = new Organization
        {
            Name = application.BusinessName,
            Type = "VendorManagement",
            OwnerId = application.AppUserId,
            CommercialRegistrationNumber = application.CommercialRegistrationNumber,
            VatNumber = application.VatNumber,
            LegalName = application.BusinessName,
            City = application.City,
            IsActive = true
        };
        Organizations.Add(organization);
        await _context.SaveChangesAsync(ct);

        var vendor = new Vendor
        {
            Name = application.BusinessName,
            Description = application.Description,
            Email = application.ContactEmail,
            Phone = application.ContactPhone,
            VendorTypeId = application.VendorTypeId,
            OrganizationId = organization.Id,
            IsApproved = true,
            ApprovedAt = DateTime.UtcNow,

            // Approved but not yet listed. The owner publishes it via
            // PUT /api/vendors/{id}/toggle-active once their profile is ready.
            IsActive = false
        };
        Vendors.Add(vendor);
        await _context.SaveChangesAsync(ct);

        if (!await _userManager.IsInRoleAsync(owner, AppRoles.VendorOrganizationManager))
        {
            await _userManager.AddToRoleAsync(owner, AppRoles.VendorOrganizationManager);
        }

        application.Status = VendorApplicationStatus.Approved;
        application.CreatedVendorId = vendor.Id;
        application.CreatedOrganizationId = organization.Id;
        application.ReviewedByUserId = reviewerUserId;
        application.ReviewedAt = DateTime.UtcNow;
        application.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Application {ApplicationId} approved: organization {OrganizationId}, vendor {VendorId}, owner {UserId}",
            application.Id, organization.Id, vendor.Id, owner.Id);

        await _emailService.SendAsync(VendorApplicationEmails.Approved(application), ct);

        return VendorApplicationResult.Ok(await MapAsync(application, ct), "Application approved");
    }

    // ===================================================================
    // Helpers
    // ===================================================================

    private IQueryable<VendorApplication> LoadQuery() =>
        Applications
            .Include(a => a.Documents)
            .Include(a => a.VendorType);

    /// <summary>
    /// Required documents the applicant still has to upload - absent entirely, or
    /// rejected and awaiting a replacement. This is the submit gate: at submission
    /// nothing has been reviewed yet, so requiring approval here would make it
    /// impossible to ever submit.
    /// </summary>
    private static List<string> AwaitingUpload(VendorApplication application)
    {
        var required = VendorDocumentTypes.RequiredFor(application.VendorType?.Name);

        return required
            .Where(type =>
            {
                var document = application.Documents.FirstOrDefault(d => d.DocumentType == type);
                return document is null || document.Status == VendorDocumentStatus.Rejected;
            })
            .ToList();
    }

    /// <summary>
    /// Required documents not yet approved. This is the approval gate: when it is
    /// empty every paper has been accepted and the application can be granted.
    /// </summary>
    private static List<string> AwaitingApproval(VendorApplication application)
    {
        var required = VendorDocumentTypes.RequiredFor(application.VendorType?.Name);

        return required
            .Where(type => application.Documents
                .FirstOrDefault(d => d.DocumentType == type) is not { Status: VendorDocumentStatus.Approved })
            .ToList();
    }

    private Task<VendorApplicationDto> MapAsync(VendorApplication application, CancellationToken ct)
    {
        var outstanding = AwaitingUpload(application);
        var unapproved = AwaitingApproval(application);

        return Task.FromResult(new VendorApplicationDto
        {
            Id = application.Id,
            BusinessName = application.BusinessName,
            VendorTypeId = application.VendorTypeId,
            VendorTypeName = application.VendorType?.Name ?? string.Empty,
            Description = application.Description,
            ContactEmail = application.ContactEmail,
            ContactPhone = application.ContactPhone,
            ContactPersonName = application.ContactPersonName,
            CommercialRegistrationNumber = application.CommercialRegistrationNumber,
            VatNumber = application.VatNumber,
            City = application.City,
            Address = application.Address,
            Status = application.Status.ToString(),
            RejectionReason = application.RejectionReason,
            CreatedAt = application.CreatedAt,
            SubmittedAt = application.SubmittedAt,
            ReviewedAt = application.ReviewedAt,
            CreatedVendorId = application.CreatedVendorId,
            OutstandingDocuments = outstanding,
            IsComplete = unapproved.Count == 0,
            Documents = application.Documents
                .OrderBy(d => d.DocumentType)
                .Select(d => new VendorDocumentDto
                {
                    Id = d.Id,
                    DocumentType = d.DocumentType,
                    OriginalFileName = d.OriginalFileName,
                    FileUrl = d.FileUrl,
                    ContentType = d.ContentType,
                    FileSizeBytes = d.FileSizeBytes,
                    Status = d.Status.ToString(),
                    ReviewComment = d.ReviewComment,
                    ReviewedAt = d.ReviewedAt,
                    UploadedAt = d.UploadedAt,
                    Version = d.Version
                })
                .ToList()
        });
    }
}
