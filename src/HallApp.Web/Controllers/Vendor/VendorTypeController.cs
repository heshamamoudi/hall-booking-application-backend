using HallApp.Web.Controllers.Common;
using HallApp.Core.Exceptions;
using HallApp.Application.DTOs.Vendors;
using HallApp.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;

namespace HallApp.Web.Controllers.Vendor
{
    /// <summary>
    /// Vendor type management controller
    /// Handles vendor type retrieval and categorization
    /// </summary>
    [AllowAnonymous]
    [Route("api/vendor-types")]
    public class VendorTypeController : BaseApiController
    {
        private readonly IVendorService _vendorService;
        private readonly IMapper _mapper;

        public VendorTypeController(IVendorService vendorService, IMapper mapper)
        {
            _vendorService = vendorService;
            _mapper = mapper;
        }

        /// <summary>
        /// Get all vendor types
        /// </summary>
        /// <returns>List of all vendor types</returns>
        [HttpGet]
        public async Task<ActionResult<ApiResponse<IEnumerable<VendorTypeDto>>>> GetVendorTypes()
        {
            try
            {
                var vendorTypes = await _vendorService.GetVendorTypesAsync();
                
                if (vendorTypes == null || !vendorTypes.Any())
                {
                    return Ok(new ApiResponse<IEnumerable<VendorTypeDto>>
                    {
                        StatusCode = 200,
                        Message = "No vendor types found",
                        IsSuccess = true,
                        Data = new List<VendorTypeDto>()
                    });
                }

                var vendorTypeDtos = _mapper.Map<IEnumerable<VendorTypeDto>>(vendorTypes);
                return Ok(new ApiResponse<IEnumerable<VendorTypeDto>>
                {
                    StatusCode = 200,
                    Message = "Vendor types retrieved successfully",
                    IsSuccess = true,
                    Data = vendorTypeDtos
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<IEnumerable<VendorTypeDto>>
                {
                    StatusCode = 500,
                    Message = $"Failed to retrieve vendor types: {ex.Message}",
                    IsSuccess = false
                });
            }
        }

        /// <summary>
        /// Get vendor type by ID
        /// </summary>
        /// <param name="id">Vendor type ID</param>
        /// <returns>Vendor type details</returns>
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ApiResponse<VendorTypeDto>>> GetVendorTypeById(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse<VendorTypeDto>
                    {
                        StatusCode = 400,
                        Message = "Invalid vendor type ID",
                        IsSuccess = false
                    });
                }

                var vendorType = await _vendorService.GetVendorTypeByIdAsync(id);
                
                if (vendorType == null)
                {
                    return NotFound(new ApiResponse<VendorTypeDto>
                    {
                        StatusCode = 404,
                        Message = $"Vendor type with ID {id} not found",
                        IsSuccess = false
                    });
                }

                var vendorTypeDto = _mapper.Map<VendorTypeDto>(vendorType);
                return Ok(new ApiResponse<VendorTypeDto>
                {
                    StatusCode = 200,
                    Message = "Vendor type retrieved successfully",
                    IsSuccess = true,
                    Data = vendorTypeDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<VendorTypeDto>
                {
                    StatusCode = 500,
                    Message = $"Failed to retrieve vendor type: {ex.Message}",
                    IsSuccess = false
                });
            }
        }

        /// <summary>
        /// Get vendors by type ID
        /// </summary>
        /// <param name="id">Vendor type ID</param>
        /// <returns>List of vendors of the specified type</returns>
        [HttpGet("{id:int}/vendors")]
        public async Task<ActionResult<ApiResponse<IEnumerable<VendorDto>>>> GetVendorsByTypeId(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse<IEnumerable<VendorDto>>
                    {
                        StatusCode = 400,
                        Message = "Invalid vendor type ID",
                        IsSuccess = false
                    });
                }

                // First check if the vendor type exists
                var vendorType = await _vendorService.GetVendorTypeByIdAsync(id);
                if (vendorType == null)
                {
                    return NotFound(new ApiResponse<IEnumerable<VendorDto>>
                    {
                        StatusCode = 404,
                        Message = $"Vendor type with ID {id} not found",
                        IsSuccess = false
                    });
                }

                var vendors = await _vendorService.GetVendorsByTypeAsync(id);
                
                if (vendors == null || !vendors.Any())
                {
                    return Ok(new ApiResponse<IEnumerable<VendorDto>>
                    {
                        StatusCode = 200,
                        Message = $"No vendors found for vendor type '{vendorType.Name}'",
                        IsSuccess = true,
                        Data = new List<VendorDto>()
                    });
                }

                var vendorDtos = _mapper.Map<IEnumerable<VendorDto>>(vendors);
                return Ok(new ApiResponse<IEnumerable<VendorDto>>
                {
                    StatusCode = 200,
                    Message = $"Vendors of type '{vendorType.Name}' retrieved successfully",
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
        /// Create new vendor type (Admin only)
        /// </summary>
        /// <param name="vendorTypeDto">Vendor type creation data</param>
        /// <returns>Created vendor type</returns>
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<ApiResponse<VendorTypeDto>>> CreateVendorType([FromBody] CreateVendorTypeDto vendorTypeDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(new ApiResponse<VendorTypeDto>
                    {
                        StatusCode = 400,
                        Message = $"Invalid data: {errors}",
                        IsSuccess = false
                    });
                }

                var vendorTypeEntity = _mapper.Map<HallApp.Core.Entities.VendorEntities.VendorType>(vendorTypeDto);
                var createdVendorType = await _vendorService.CreateVendorTypeAsync(vendorTypeEntity);
                var createdVendorTypeDto = _mapper.Map<VendorTypeDto>(createdVendorType);
                
                return Ok(new ApiResponse<VendorTypeDto>
                {
                    StatusCode = 201,
                    Message = "Vendor type created successfully",
                    IsSuccess = true,
                    Data = createdVendorTypeDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<VendorTypeDto>
                {
                    StatusCode = 500,
                    Message = $"Failed to create vendor type: {ex.Message}",
                    IsSuccess = false
                });
            }
        }

        /// <summary>
        /// Update vendor type (Admin only)
        /// </summary>
        /// <param name="id">Vendor type ID</param>
        /// <param name="vendorTypeDto">Updated vendor type data</param>
        /// <returns>Updated vendor type</returns>
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ApiResponse<VendorTypeDto>>> UpdateVendorType(int id, [FromBody] UpdateVendorTypeDto vendorTypeDto)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse<VendorTypeDto>
                    {
                        StatusCode = 400,
                        Message = "Invalid vendor type ID",
                        IsSuccess = false
                    });
                }

                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    return BadRequest(new ApiResponse<VendorTypeDto>
                    {
                        StatusCode = 400,
                        Message = $"Invalid data: {errors}",
                        IsSuccess = false
                    });
                }

                vendorTypeDto.Id = id;
                var vendorTypeEntity = _mapper.Map<HallApp.Core.Entities.VendorEntities.VendorType>(vendorTypeDto);
                var updatedVendorType = await _vendorService.UpdateVendorTypeAsync(vendorTypeEntity);
                var updatedVendorTypeDto = _mapper.Map<VendorTypeDto>(updatedVendorType);
                
                return Ok(new ApiResponse<VendorTypeDto>
                {
                    StatusCode = 200,
                    Message = "Vendor type updated successfully",
                    IsSuccess = true,
                    Data = updatedVendorTypeDto
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<VendorTypeDto>
                {
                    StatusCode = 500,
                    Message = $"Failed to update vendor type: {ex.Message}",
                    IsSuccess = false
                });
            }
        }

        /// <summary>
        /// Delete vendor type (Admin only)
        /// </summary>
        /// <param name="id">Vendor type ID</param>
        /// <returns>Success response</returns>
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<ActionResult<ApiResponse<VendorTypeDto>>> DeleteVendorType(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new ApiResponse<VendorTypeDto>
                    {
                        StatusCode = 400,
                        Message = "Invalid vendor type ID",
                        IsSuccess = false
                    });
                }

                var result = await _vendorService.DeleteVendorTypeAsync(id);
                if (!result)
                {
                    return NotFound(new ApiResponse<VendorTypeDto>
                    {
                        StatusCode = 404,
                        Message = $"Vendor type with ID {id} not found",
                        IsSuccess = false
                    });
                }
                
                return Ok(new ApiResponse<VendorTypeDto>
                {
                    StatusCode = 200,
                    Message = "Vendor type deleted successfully",
                    IsSuccess = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new ApiResponse<VendorTypeDto>
                {
                    StatusCode = 500,
                    Message = $"Failed to delete vendor type: {ex.Message}",
                    IsSuccess = false
                });
            }
        }
    }
}
