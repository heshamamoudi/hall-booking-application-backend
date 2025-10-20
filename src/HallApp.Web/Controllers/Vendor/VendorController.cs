using HallApp.Web.Controllers.Common;
using HallApp.Application.Common.Models;
using HallApp.Application.DTOs.Vendors;
using HallApp.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using System.Text.Json;

namespace HallApp.Web.Controllers.Vendor
{
    /// <summary>
    /// Vendor management controller
    /// Handles vendor CRUD operations, search, and validation
    /// </summary>
    [Route("api/vendors")]
    public class VendorController : BaseApiController
    {
        private readonly IVendorService _vendorService;
        private readonly IMapper _mapper;

        public VendorController(IVendorService vendorService, IMapper mapper)
        {
            _vendorService = vendorService;
            _mapper = mapper;
        }

        /// <summary>
        /// Get vendor categories for customer selection
        /// </summary>
        /// <returns>List of vendor types/categories</returns>
        [AllowAnonymous]
        [HttpGet("categories")]
        public async Task<ActionResult<ApiResponse<IEnumerable<VendorTypeDto>>>> GetVendorCategories()
        {
            try
            {
                var vendorTypes = await _vendorService.GetVendorTypesAsync();
                var vendorTypeDtos = _mapper.Map<List<VendorTypeDto>>(vendorTypes);
                return Success<IEnumerable<VendorTypeDto>>(vendorTypeDtos, "Vendor categories retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<VendorTypeDto>>($"Failed to retrieve vendor categories: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get vendors by category for customer booking workflow
        /// </summary>
        /// <param name="categoryId">Vendor category/type ID</param>
        /// <returns>List of vendors in the category</returns>
        [AllowAnonymous]
        [HttpGet("category/{categoryId:int}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<VendorListDto>>>> GetVendorsByCategory(int categoryId)
        {
            try
            {
                if (categoryId <= 0)
                {
                    return Error<IEnumerable<VendorListDto>>("Invalid category ID", 400);
                }

                var vendors = await _vendorService.GetVendorsByTypeAsync(categoryId);
                var vendorDtos = _mapper.Map<List<VendorListDto>>(vendors);
                return Success<IEnumerable<VendorListDto>>(vendorDtos, $"Vendors in category {categoryId} retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<VendorListDto>>($"Failed to retrieve vendors by category: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get vendor services/items for customer booking workflow
        /// </summary>
        /// <param name="vendorId">Vendor ID</param>
        /// <returns>List of available services for the vendor</returns>
        [AllowAnonymous]
        [HttpGet("{vendorId:int}/services")]
        public async Task<ActionResult<ApiResponse<IEnumerable<ServiceItemDto>>>> GetVendorServices(int vendorId)
        {
            try
            {
                if (vendorId <= 0)
                {
                    return Error<IEnumerable<ServiceItemDto>>("Invalid vendor ID", 400);
                }

                // Get vendor to verify it exists
                var vendor = await _vendorService.GetVendorByIdAsync(vendorId);
                if (vendor == null)
                {
                    return Error<IEnumerable<ServiceItemDto>>("Vendor not found", 404);
                }

                // Return services from vendor entity - map to DTOs
                var services = vendor.ServiceItems != null ? _mapper.Map<List<ServiceItemDto>>(vendor.ServiceItems) : new List<ServiceItemDto>();
                return Success<IEnumerable<ServiceItemDto>>(services, $"Services for vendor {vendorId} retrieved successfully");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<ServiceItemDto>>($"Failed to retrieve vendor services: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get alternative vendors available for the same date when one is rejected
        /// </summary>
        /// <param name="categoryId">Vendor category ID</param>
        /// <param name="eventDate">Event date</param>
        /// <param name="excludeVendorId">Vendor ID to exclude (the one that rejected)</param>
        /// <returns>List of available alternative vendors in the same category</returns>
        [AllowAnonymous]
        [HttpGet("alternatives/{categoryId:int}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<VendorListDto>>>> GetAlternativeVendors(
            int categoryId,
            [FromQuery] DateTime eventDate,
            [FromQuery] int excludeVendorId = 0)
        {
            try
            {
                if (categoryId <= 0)
                {
                    return Error<IEnumerable<VendorListDto>>("Invalid category ID", 400);
                }

                var vendors = await _vendorService.GetVendorsByTypeAsync(categoryId);
                var availableVendors = new List<HallApp.Core.Entities.VendorEntities.Vendor>();
                
                foreach (var vendor in vendors)
                {
                    // Skip the vendor that rejected
                    if (vendor.Id == excludeVendorId)
                        continue;
                        
                    // Skip inactive vendors
                    if (!vendor.IsActive)
                        continue;
                    
                    // TODO: Add vendor availability check for the specific date
                    // For now, assume all active vendors are available
                    availableVendors.Add(vendor);
                }
                
                var vendorDtos = _mapper.Map<List<VendorListDto>>(availableVendors);
                return Success<IEnumerable<VendorListDto>>(vendorDtos, $"Found {vendorDtos.Count} alternative vendors for {eventDate:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                return Error<IEnumerable<VendorListDto>>($"Failed to get alternative vendors: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Get all vendors with pagination and filtering
        /// </summary>
        /// <param name="vendorParams">Filter and pagination parameters</param>
        /// <returns>Paginated list of vendors</returns>
        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<PaginatedApiResponse<VendorDto>>> GetVendors([FromQuery] VendorParams vendorParams)
        {
            try
            {
                var vendors = await _vendorService.GetVendorsAsync(vendorParams);
                var vendorDtos = _mapper.Map<IEnumerable<VendorDto>>(vendors);
                
                var response = new PaginatedApiResponse<VendorDto>
                {
                    StatusCode = 200,
                    Message = "Vendors retrieved successfully",
                    IsSuccess = true,
                    Data = vendorDtos,
                    CurrentPage = 1,
                    PageSize = 10,
                    TotalCount = vendorDtos.Count(),
                    TotalPages = 1
                };

                Response.Headers["X-Pagination"] = JsonSerializer.Serialize(new
                {
                    totalCount = vendorDtos.Count(),
                    pageSize = 10,
                    currentPage = 1,
                    totalPages = 1,
                    hasNext = false,
                    hasPrevious = false
                });

                return response;
            }
            catch (Exception ex)
            {
                return StatusCode(500, new PaginatedApiResponse<VendorDto>
                {
                    StatusCode = 500,
                    Message = $"Failed to retrieve vendors: {ex.Message}",
                    IsSuccess = false
                });
            }
        }

        /// <summary>
        /// Get vendor by ID
        /// </summary>
        /// <param name="id">Vendor ID</param>
        /// <returns>Vendor details</returns>
        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<VendorDto>>> GetVendorById(int id)
        {
            try
            {
                var vendor = await _vendorService.GetVendorByIdAsync(id);
                
                if (vendor == null)
                {
                    return NotFound(new ApiResponse<VendorDto>
                {
                    StatusCode = 404,
                    Message = $"Vendor with ID {id} not found",
                    IsSuccess = false
                });
                }

                var vendorDto = _mapper.Map<VendorDto>(vendor);
                return Ok(new ApiResponse<VendorDto>
                {
                    StatusCode = 200,
                    Message = "Vendor retrieved successfully",
                    IsSuccess = true,
                    Data = vendorDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<VendorDto>
                {
                    StatusCode = 500,
                    Message = $"Failed to retrieve vendor: {ex.Message}",
                    IsSuccess = false
                });
            }
        }

        /// <summary>
        /// Get vendors by type/category
        /// </summary>
        /// <param name="typeId">Vendor type ID</param>
        /// <returns>List of vendors of specified type</returns>
        [AllowAnonymous]
        [HttpGet("type/{typeId:int}")]
        public async Task<ActionResult<ApiResponse<IEnumerable<VendorDto>>>> GetVendorsByType(int typeId)
        {
            try
            {
                var vendors = await _vendorService.GetVendorsByTypeAsync(typeId);
                var vendorDtos = _mapper.Map<IEnumerable<VendorDto>>(vendors);
                return Ok(new ApiResponse<IEnumerable<VendorDto>>
                {
                    StatusCode = 200,
                    Message = $"Vendors of type {typeId} retrieved successfully",
                    IsSuccess = true,
                    Data = vendorDtos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<IEnumerable<VendorDto>>
                {
                    StatusCode = 500,
                    Message = $"Failed to retrieve vendors by type: {ex.Message}",
                    IsSuccess = false
                });
            }
        }

        /// <summary>
        /// Search vendors by name or description
        /// </summary>
        /// <param name="searchTerm">Search term</param>
        /// <returns>List of matching vendors</returns>
        [AllowAnonymous]
        [HttpGet("search")]
        public async Task<ActionResult<ApiResponse<IEnumerable<VendorDto>>>> SearchVendors([FromQuery] string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return BadRequest(new ApiResponse<IEnumerable<VendorDto>>
                    {
                        StatusCode = 400,
                        Message = "Search term is required",
                        IsSuccess = false
                    });
                }

                var vendors = await _vendorService.SearchVendorsAsync(searchTerm);
                var vendorDtos = _mapper.Map<IEnumerable<VendorDto>>(vendors);
                return Ok(new ApiResponse<IEnumerable<VendorDto>>
                {
                    StatusCode = 200,
                    Message = $"Search completed for '{searchTerm}'",
                    IsSuccess = true,
                    Data = vendorDtos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<IEnumerable<VendorDto>>
                {
                    StatusCode = 500,
                    Message = $"Search failed: {ex.Message}",
                    IsSuccess = false
                });
            }
        }

        /// <summary>
        /// Create new vendor (Admin/HallManager only)
        /// </summary>
        /// <param name="vendorDto">Vendor creation data</param>
        /// <returns>Created vendor</returns>
        [Authorize(Roles = "Admin,HallManager")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<VendorDto>>> CreateVendor([FromForm] CreateVendorDto vendorDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return Error<VendorDto>($"Invalid data: {errors}", 400);
                }

                var vendorEntity = _mapper.Map<HallApp.Core.Entities.VendorEntities.Vendor>(vendorDto);
                var vendor = await _vendorService.CreateVendorAsync(vendorEntity);
                
                return CreatedAtAction(
                    nameof(GetVendorById), 
                    new { id = vendor.Id }, 
                    new ApiResponse<VendorDto>
                    {
                        StatusCode = 201,
                        Message = "Vendor created successfully",
                        IsSuccess = true,
                        Data = _mapper.Map<VendorDto>(vendor)
                    }
                );
            }
            catch (ArgumentException ex)
            {
                return Error<VendorDto>(ex.Message, 400);
            }
            catch (Exception ex)
            {
                return Error<VendorDto>($"Failed to create vendor: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Update existing vendor (Admin/HallManager only)
        /// </summary>
        /// <param name="id">Vendor ID</param>
        /// <param name="updateDto">Updated vendor data</param>
        /// <returns>Updated vendor</returns>
        [Authorize(Roles = "VendorManager")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<VendorDto>>> UpdateVendor(int id, [FromBody] UpdateVendorDto updateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(new ApiResponse<VendorDto>
                    {
                        StatusCode = 400,
                        Message = $"Invalid data: {errors}",
                        IsSuccess = false
                    });
                }

                var vendorEntity = _mapper.Map<HallApp.Core.Entities.VendorEntities.Vendor>(updateDto);
                var updatedVendor = await _vendorService.UpdateVendorAsync(id, vendorEntity);
                var updatedVendorDto = _mapper.Map<VendorDto>(updatedVendor);
                
                return Ok(new ApiResponse<VendorDto>
                {
                    StatusCode = 200,
                    Message = "Vendor updated successfully",
                    IsSuccess = true,
                    Data = updatedVendorDto
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new ApiResponse<VendorDto>
                {
                    StatusCode = 400,
                    Message = ex.Message,
                    IsSuccess = false
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<VendorDto>
                {
                    StatusCode = 500,
                    Message = $"Failed to update vendor: {ex.Message}",
                    IsSuccess = false
                });
            }
        }

        /// <summary>
        /// Delete vendor (Admin only)
        /// </summary>
        /// <param name="id">Vendor ID</param>
        /// <returns>Success response</returns>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse>> DeleteVendor(int id)
        {
            try
            {
                var result = await _vendorService.DeleteVendorAsync(id);
                
                if (!result)
                {
                    return Error($"Vendor with ID {id} not found", 404);
                }

                return Success("Vendor deleted successfully");
            }
            catch (Exception ex)
            {
                return Error($"Failed to delete vendor: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Validate vendor name uniqueness
        /// </summary>
        /// <param name="name">Vendor name to validate</param>
        /// <param name="excludeId">ID to exclude from validation (for updates)</param>
        /// <returns>True if name is unique</returns>
        [AllowAnonymous]
        [HttpGet("validate/name")]
        public async Task<ActionResult<ApiResponse<bool>>> ValidateVendorName([FromQuery] string name, [FromQuery] int? excludeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    return Error<bool>("Name is required for validation", 400);
                }

                var isUnique = await _vendorService.IsNameUniqueAsync(name, excludeId ?? 0);
                return Success(isUnique, $"Name validation completed");
            }
            catch (Exception ex)
            {
                return Error<bool>($"Validation failed: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Validate vendor email uniqueness
        /// </summary>
        /// <param name="email">Email to validate</param>
        /// <param name="excludeId">ID to exclude from validation (for updates)</param>
        /// <returns>True if email is unique</returns>
        [AllowAnonymous]
        [HttpGet("validate/email")]
        public async Task<ActionResult<ApiResponse<bool>>> ValidateVendorEmail([FromQuery] string email, [FromQuery] int? excludeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return Error<bool>("Email is required for validation", 400);
                }

                var isUnique = await _vendorService.IsEmailUniqueAsync(email, excludeId ?? 0);
                return Success(isUnique, "Email validation completed");
            }
            catch (Exception ex)
            {
                return Error<bool>($"Validation failed: {ex.Message}", 500);
            }
        }

        /// <summary>
        /// Validate vendor phone uniqueness
        /// </summary>
        /// <param name="phone">Phone number to validate</param>
        /// <param name="excludeId">ID to exclude from validation (for updates)</param>
        /// <returns>True if phone is unique</returns>
        [AllowAnonymous]
        [HttpGet("validate/phone")]
        public async Task<ActionResult<ApiResponse<bool>>> ValidateVendorPhone([FromQuery] string phone, [FromQuery] int? excludeId = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phone))
                {
                    return Error<bool>("Phone number is required for validation", 400);
                }

                var isUnique = await _vendorService.IsPhoneUniqueAsync(phone, excludeId ?? 0);
                return Success(isUnique, "Phone validation completed");
            }
            catch (Exception ex)
            {
                return Error<bool>($"Validation failed: {ex.Message}", 500);
            }
        }
    }
}
