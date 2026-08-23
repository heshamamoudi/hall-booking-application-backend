using AutoMapper;
using HallApp.Application.DTOs.Vendors;
using HallApp.Core.Entities.VendorEntities;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Controllers.Common;
using HallApp.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HallApp.Web.Controllers.Vendor;

/// <summary>
/// What a vendor sells, and the images that go with it.
///
/// A vendor business could not previously manage either: GET {vendorId}/services
/// was read-only and there was no image upload at all, while halls have had both
/// for as long as they have existed. Every write is scoped to vendors the caller
/// actually owns or is assigned to.
/// </summary>
[Authorize(Roles = "Admin,VendorOrganizationManager,VendorManager")]
[Route("api/vendors")]
[ApiController]
public class VendorServiceItemsController : BaseApiController
{
    private readonly IServiceItemService _serviceItemService;
    private readonly IVendorService _vendorService;
    private readonly IOrganizationService _organizationService;
    private readonly IFileUploadService _fileUploadService;
    private readonly IMapper _mapper;
    private readonly ILogger<VendorServiceItemsController> _logger;

    public VendorServiceItemsController(
        IServiceItemService serviceItemService,
        IVendorService vendorService,
        IOrganizationService organizationService,
        IFileUploadService fileUploadService,
        IMapper mapper,
        ILogger<VendorServiceItemsController> logger)
    {
        _serviceItemService = serviceItemService;
        _vendorService = vendorService;
        _organizationService = organizationService;
        _fileUploadService = fileUploadService;
        _mapper = mapper;
        _logger = logger;
    }

    // ===================================================================
    // Service items
    // ===================================================================

    /// <summary>Create a service item for a vendor.</summary>
    /// <response code="201">The created service item</response>
    /// <response code="403">The caller does not manage this vendor</response>
    [HttpPost("{vendorId:int}/services")]
    [ProducesResponseType(typeof(ApiResponse<ServiceItemDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ServiceItemDto>>> CreateServiceItem(
        int vendorId, [FromBody] CreateServiceItemDto dto)
    {
        try
        {
            if (!await UserOwnsVendor(vendorId))
                return Error<ServiceItemDto>("You do not have permission to manage this vendor", 403);

            if (!ModelState.IsValid)
            {
                var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return Error<ServiceItemDto>($"Invalid data: {errors}", 400);
            }

            var vendor = await _vendorService.GetVendorByIdAsync(vendorId);
            if (vendor == null)
                return Error<ServiceItemDto>("Vendor not found", 404);

            var entity = _mapper.Map<ServiceItem>(dto);

            // The route is the authority on which vendor this belongs to. Trusting the
            // body would let a caller create items against someone else's vendor.
            entity.VendorId = vendorId;

            // VendorTypeId is a required foreign key and the create DTO has no field
            // for it, so it is inherited from the vendor: a catering vendor's items
            // are catering. Without this the insert fails on the FK constraint.
            entity.VendorTypeId = vendor.VendorTypeId;

            var created = await _serviceItemService.CreateServiceItemAsync(vendorId, entity);

            return StatusCode(201, new ApiResponse<ServiceItemDto>
            {
                StatusCode = 201,
                Message = "Service item created successfully",
                IsSuccess = true,
                Data = _mapper.Map<ServiceItemDto>(created)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating service item for vendor {VendorId}", vendorId);
            return Error<ServiceItemDto>("An error occurred while creating the service item", 500);
        }
    }

    /// <summary>Update a service item.</summary>
    [HttpPut("{vendorId:int}/services/{serviceItemId:int}")]
    [ProducesResponseType(typeof(ApiResponse<ServiceItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ServiceItemDto>>> UpdateServiceItem(
        int vendorId, int serviceItemId, [FromBody] UpdateServiceItemDto dto)
    {
        try
        {
            if (!await UserOwnsVendor(vendorId))
                return Error<ServiceItemDto>("You do not have permission to manage this vendor", 403);

            var existing = await _serviceItemService.GetServiceItemByIdAsync(serviceItemId);
            if (existing == null)
                return Error<ServiceItemDto>("Service item not found", 404);

            // Guards against updating an item that belongs to a different vendor by
            // pairing someone else's item id with a vendor you do manage.
            if (existing.VendorId != vendorId)
                return Error<ServiceItemDto>("Service item does not belong to this vendor", 404);

            if (!string.IsNullOrWhiteSpace(dto.Name)) existing.Name = dto.Name;
            if (!string.IsNullOrWhiteSpace(dto.Description)) existing.Description = dto.Description;
            if (!string.IsNullOrWhiteSpace(dto.ServiceType)) existing.ServiceType = dto.ServiceType;
            if (dto.Price.HasValue) existing.Price = dto.Price.Value;
            if (dto.DiscountedPrice.HasValue) existing.DiscountedPrice = dto.DiscountedPrice;
            if (!string.IsNullOrWhiteSpace(dto.ImageUrl)) existing.ImageUrl = dto.ImageUrl;
            if (dto.IsAvailable.HasValue) existing.IsAvailable = dto.IsAvailable.Value;
            if (dto.SortOrder.HasValue) existing.SortOrder = dto.SortOrder.Value;
            existing.UpdatedAt = DateTime.UtcNow;

            var updated = await _serviceItemService.UpdateServiceItemAsync(serviceItemId, existing);
            return Success(_mapper.Map<ServiceItemDto>(updated), "Service item updated successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating service item {ServiceItemId}", serviceItemId);
            return Error<ServiceItemDto>("An error occurred while updating the service item", 500);
        }
    }

    /// <summary>Delete a service item.</summary>
    [HttpDelete("{vendorId:int}/services/{serviceItemId:int}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse>> DeleteServiceItem(int vendorId, int serviceItemId)
    {
        try
        {
            if (!await UserOwnsVendor(vendorId))
                return Error("You do not have permission to manage this vendor", 403);

            var existing = await _serviceItemService.GetServiceItemByIdAsync(serviceItemId);
            if (existing == null)
                return Error("Service item not found", 404);

            if (existing.VendorId != vendorId)
                return Error("Service item does not belong to this vendor", 404);

            var deleted = await _serviceItemService.DeleteServiceItemAsync(serviceItemId);
            return deleted
                ? Success("Service item deleted successfully")
                : Error("Failed to delete the service item", 500);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting service item {ServiceItemId}", serviceItemId);
            return Error("An error occurred while deleting the service item", 500);
        }
    }

    // ===================================================================
    // Vendor images
    // ===================================================================

    /// <summary>
    /// Upload images for a vendor. Stored under vendors/{vendorId}/ on the mounted
    /// uploads volume, so everything belonging to one vendor stays together and
    /// survives a redeploy.
    /// </summary>
    /// <response code="200">Public URLs of the stored images</response>
    [HttpPost("{vendorId:int}/images")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<List<string>>>> UploadVendorImages(
        int vendorId, [FromForm] List<IFormFile> images)
    {
        try
        {
            if (!await UserOwnsVendor(vendorId))
                return Error<List<string>>("You do not have permission to manage this vendor", 403);

            if (images == null || images.Count == 0)
                return Error<List<string>>("No files were supplied", 400);

            var urls = await _fileUploadService.SaveImagesAsync(
                images, UploadCategories.Vendors, vendorId);

            _logger.LogInformation(
                "Stored {Count} image(s) for vendor {VendorId}", urls.Count, vendorId);

            return Success(urls, $"Uploaded {urls.Count} image(s) successfully");
        }
        catch (ArgumentException ex)
        {
            return Error<List<string>>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading images for vendor {VendorId}", vendorId);
            return Error<List<string>>("An error occurred while uploading images", 500);
        }
    }

    /// <summary>List the images currently stored for a vendor.</summary>
    [HttpGet("{vendorId:int}/images")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<string>>>> ListVendorImages(int vendorId)
    {
        try
        {
            if (!await UserOwnsVendor(vendorId))
                return Error<List<string>>("You do not have permission to manage this vendor", 403);

            var urls = await _fileUploadService.ListOwnerFilesAsync(UploadCategories.Vendors, vendorId);
            return Success(urls, $"Found {urls.Count} image(s)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing images for vendor {VendorId}", vendorId);
            return Error<List<string>>("An error occurred while listing images", 500);
        }
    }

    /// <summary>Delete one stored vendor image by its public URL.</summary>
    [HttpDelete("{vendorId:int}/images")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> DeleteVendorImage(
        int vendorId, [FromQuery] string url)
    {
        try
        {
            if (!await UserOwnsVendor(vendorId))
                return Error("You do not have permission to manage this vendor", 403);

            if (string.IsNullOrWhiteSpace(url))
                return Error("A url query parameter is required", 400);

            // Only files filed under this vendor may be deleted through this route,
            // so a crafted url cannot reach another vendor's directory.
            var expectedPrefix = $"/uploads/{UploadCategories.Vendors}/{vendorId}/";
            if (!url.StartsWith(expectedPrefix, StringComparison.Ordinal))
                return Error("That file does not belong to this vendor", 400);

            var deleted = await _fileUploadService.DeleteImageAsync(url);
            return deleted ? Success("Image deleted successfully") : Error("Image not found", 404);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting image for vendor {VendorId}", vendorId);
            return Error("An error occurred while deleting the image", 500);
        }
    }

    // ===================================================================
    // Authorization
    // ===================================================================

    /// <summary>
    /// Same rule as VendorController.UserOwnsVendor: assigned vendors for a manager,
    /// every vendor in the organization for its owner, everything for an admin.
    /// </summary>
    private async Task<bool> UserOwnsVendor(int vendorId)
    {
        if (IsAdmin) return true;

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return false;

        var assigned = await _vendorService.GetVendorsByManagerIdAsync(userId);
        if (assigned.Any(v => v.Id == vendorId))
            return true;

        if (User.IsInRole("VendorOrganizationManager"))
        {
            var organization = await _organizationService.GetOrganizationByOwnerId(UserId);
            if (organization != null && organization.Type == "VendorManagement")
            {
                var vendor = await _vendorService.GetVendorByIdAsync(vendorId);
                if (vendor != null && vendor.OrganizationId == organization.Id)
                    return true;
            }
        }

        return false;
    }
}
