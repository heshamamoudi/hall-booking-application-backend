using HallApp.Application.DTOs.Vendors;
using HallApp.Application.Services;
using HallApp.Infrastructure.Services;
using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Exceptions;
using HallApp.Web.Controllers.Common;
using HallApp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HallApp.Web.Controllers.Vendor;

/// <summary>
/// Businesses applying to join, and the administrators who review them.
///
/// Three audiences on one resource:
///   - anonymous: the public registration form and the document checklist
///   - the applicant: upload papers, submit, follow progress
///   - administrators: the review queue and per-document decisions
///
/// An applicant may only ever see their own application; the route never takes an
/// id from a non-admin caller, it is resolved from their token.
/// </summary>
[Route("api/vendor-applications")]
[ApiController]
public class VendorApplicationsController : BaseApiController
{
    /// <summary>Papers, unlike photographs, are usually PDFs or scans.</summary>
    private static readonly string[] AllowedDocumentExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".webp" };
    private const long MaxDocumentBytes = 10 * 1024 * 1024;

    private readonly IVendorApplicationService _applications;
    private readonly IFileUploadService _fileUploadService;
    private readonly ILogger<VendorApplicationsController> _logger;

    public VendorApplicationsController(
        IVendorApplicationService applications,
        IFileUploadService fileUploadService,
        ILogger<VendorApplicationsController> logger)
    {
        _applications = applications;
        _fileUploadService = fileUploadService;
        _logger = logger;
    }

    // ===================================================================
    // Public
    // ===================================================================

    /// <summary>
    /// Vendor categories and the documents each one must supply. What the public
    /// registration page renders before anyone types anything.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("requirements")]
    [ProducesResponseType(typeof(ApiResponse<List<VendorApplicationRequirementsDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<VendorApplicationRequirementsDto>>>> GetRequirements(
        CancellationToken ct)
    {
        var requirements = await _applications.GetRequirementsAsync(ct);
        return Success(requirements, $"{requirements.Count} categories");
    }

    /// <summary>
    /// Register a business. Creates the account and a Draft application; the
    /// applicant then signs in to upload documents and submit.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<VendorApplicationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<VendorApplicationDto>>> Register(
        [FromBody] RegisterVendorApplicationDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
        {
            var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            return Error<VendorApplicationDto>($"Invalid data: {errors}", 400);
        }

        var result = await _applications.RegisterAsync(dto, ct);
        if (!result.Success)
            return Error<VendorApplicationDto>(result.Message, result.StatusCode);

        return StatusCode(201, new ApiResponse<VendorApplicationDto>
        {
            StatusCode = 201,
            Message = result.Message,
            IsSuccess = true,
            Data = result.Data
        });
    }

    // ===================================================================
    // Applicant
    // ===================================================================

    /// <summary>The signed-in applicant's own application.</summary>
    [Authorize]
    [HttpGet("mine")]
    [ProducesResponseType(typeof(ApiResponse<VendorApplicationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VendorApplicationDto>>> GetMine(CancellationToken ct)
    {
        var application = await _applications.GetForUserAsync(UserId, ct);
        return application == null
            ? Error<VendorApplicationDto>("You do not have an application", 404)
            : Success(application, "Application retrieved");
    }

    /// <summary>
    /// Upload or replace one document. Replacing a rejected paper resets it to
    /// pending and puts the application back in the review queue.
    /// </summary>
    [Authorize]
    [HttpPost("mine/documents/{documentType}")]
    [ProducesResponseType(typeof(ApiResponse<VendorApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<VendorApplicationDto>>> UploadDocument(
        string documentType, IFormFile file, CancellationToken ct)
    {
        var application = await _applications.GetForUserAsync(UserId, ct);
        if (application == null)
            return Error<VendorApplicationDto>("You do not have an application", 404);

        if (file == null || file.Length == 0)
            return Error<VendorApplicationDto>("No file was supplied", 400);

        if (file.Length > MaxDocumentBytes)
            return Error<VendorApplicationDto>($"File exceeds the {MaxDocumentBytes / 1024 / 1024}MB limit", 400);

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedDocumentExtensions.Contains(extension))
            return Error<VendorApplicationDto>(
                $"File type {extension} is not accepted. Use PDF or an image.", 400);

        string fileUrl;
        try
        {
            // Filed under vendor-documents/{applicationId}/, so everything belonging
            // to one application sits together and can be removed as a unit.
            fileUrl = await _fileUploadService.SaveDocumentAsync(
                file, UploadCategories.VendorDocuments, application.Id);
        }
        catch (ArgumentException ex)
        {
            return Error<VendorApplicationDto>(ex.Message, 400);
        }

        var result = await _applications.AttachDocumentAsync(
            application.Id, documentType, fileUrl, file.FileName,
            file.ContentType ?? string.Empty, file.Length, ct);

        return result.Success
            ? Success(result.Data!, result.Message)
            : Error<VendorApplicationDto>(result.Message, result.StatusCode);
    }

    /// <summary>Submit the application for review once every document is present.</summary>
    [Authorize]
    [HttpPost("mine/submit")]
    [ProducesResponseType(typeof(ApiResponse<VendorApplicationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VendorApplicationDto>>> Submit(CancellationToken ct)
    {
        var application = await _applications.GetForUserAsync(UserId, ct);
        if (application == null)
            return Error<VendorApplicationDto>("You do not have an application", 404);

        var result = await _applications.SubmitAsync(application.Id, ct);
        return result.Success
            ? Success(result.Data!, result.Message)
            : Error<VendorApplicationDto>(result.Message, result.StatusCode);
    }

    // ===================================================================
    // Administrator
    // ===================================================================

    /// <summary>The review queue, optionally filtered by status.</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<VendorApplicationDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<VendorApplicationDto>>>> GetQueue(
        [FromQuery] VendorApplicationStatus? status, CancellationToken ct)
    {
        var queue = await _applications.GetQueueAsync(status, ct);
        return Success(queue, $"{queue.Count} application(s)");
    }

    /// <summary>One application with all of its documents.</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<VendorApplicationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VendorApplicationDto>>> GetById(int id, CancellationToken ct)
    {
        var application = await _applications.GetByIdAsync(id, ct);
        return application == null
            ? Error<VendorApplicationDto>("Application not found", 404)
            : Success(application, "Application retrieved");
    }

    /// <summary>
    /// Approve or reject one document. Rejecting requires a comment and sends the
    /// application back for that document only. Approving the last outstanding
    /// document approves the whole application, which creates the vendor.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/documents/{documentId:int}/review")]
    [ProducesResponseType(typeof(ApiResponse<VendorApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<VendorApplicationDto>>> ReviewDocument(
        int id, int documentId, [FromBody] ReviewDocumentDto dto, CancellationToken ct)
    {
        var result = await _applications.ReviewDocumentAsync(
            id, documentId, dto.Approved, dto.Comment, UserId, ct);

        return result.Success
            ? Success(result.Data!, result.Message)
            : Error<VendorApplicationDto>(result.Message, result.StatusCode);
    }

    /// <summary>Turn down the whole application.</summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("{id:int}/reject")]
    [ProducesResponseType(typeof(ApiResponse<VendorApplicationDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VendorApplicationDto>>> Reject(
        int id, [FromBody] RejectApplicationDto dto, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return Error<VendorApplicationDto>("A reason is required", 400);

        var result = await _applications.RejectAsync(id, dto.Reason, UserId, ct);
        return result.Success
            ? Success(result.Data!, result.Message)
            : Error<VendorApplicationDto>(result.Message, result.StatusCode);
    }
}
