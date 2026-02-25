using HallApp.Core.Entities.GdprEntities;
using HallApp.Core.Exceptions;
using HallApp.Core.Interfaces.IServices;
using HallApp.Web.Controllers.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HallApp.Web.Controllers;

/// <summary>
/// HIGH-012: Controller for GDPR data retention and right-to-erasure operations.
/// Provides endpoints for users to request data deletion and for admins to manage retention policies.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class GdprController : BaseApiController
{
    private readonly IDataRetentionService _retentionService;
    private readonly ILogger<GdprController> _logger;

    public GdprController(
        IDataRetentionService retentionService,
        ILogger<GdprController> logger)
    {
        _retentionService = retentionService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a GDPR data deletion request for the current user (Right to Erasure - Article 17).
    /// The request will be processed by the system during the next scheduled retention cycle.
    /// </summary>
    [HttpPost("delete-my-data")]
    public async Task<ActionResult<ApiResponse<GdprRequestResponseDto>>> RequestDataDeletion(
        [FromBody] GdprDeletionRequestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
            {
                return Error<GdprRequestResponseDto>("A reason for the data deletion request is required.", 400);
            }

            var userId = UserId;

            _logger.LogInformation(
                "HIGH-012: GDPR data deletion request received from user {UserId}",
                userId);

            var request = await _retentionService.SubmitDeletionRequestAsync(userId, userId, dto.Reason);

            var response = new GdprRequestResponseDto
            {
                RequestId = request.Id,
                Status = request.Status,
                RequestType = request.RequestType,
                RequestedAt = request.RequestedAt,
                Message = "Your data deletion request has been submitted and will be processed within 30 days as per GDPR requirements."
            };

            return Success(response, "Data deletion request submitted successfully.");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(
                "HIGH-012: GDPR deletion request rejected for user {UserId}: {Reason}",
                UserId, ex.Message);
            return Error<GdprRequestResponseDto>(ex.Message, 409);
        }
        catch (ArgumentException ex)
        {
            return Error<GdprRequestResponseDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "HIGH-012: Failed to process GDPR deletion request for user {UserId}",
                UserId);
            return Error<GdprRequestResponseDto>("Failed to process data deletion request.", 500);
        }
    }

    /// <summary>
    /// Admin: Submit a GDPR data deletion request on behalf of a specific user.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("user/{userId:int}/delete-data")]
    public async Task<ActionResult<ApiResponse<GdprRequestResponseDto>>> RequestUserDataDeletion(
        int userId,
        [FromBody] GdprDeletionRequestDto dto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dto.Reason))
            {
                return Error<GdprRequestResponseDto>("A reason for the data deletion request is required.", 400);
            }

            _logger.LogInformation(
                "HIGH-012: Admin {AdminUserId} submitting GDPR deletion request for user {TargetUserId}",
                UserId, userId);

            var request = await _retentionService.SubmitDeletionRequestAsync(userId, UserId, dto.Reason);

            var response = new GdprRequestResponseDto
            {
                RequestId = request.Id,
                Status = request.Status,
                RequestType = request.RequestType,
                RequestedAt = request.RequestedAt,
                Message = $"Data deletion request submitted for user {userId}."
            };

            return Success(response, "Data deletion request submitted successfully.");
        }
        catch (InvalidOperationException ex)
        {
            return Error<GdprRequestResponseDto>(ex.Message, 409);
        }
        catch (ArgumentException ex)
        {
            return Error<GdprRequestResponseDto>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "HIGH-012: Failed to process admin GDPR deletion request for user {UserId}",
                userId);
            return Error<GdprRequestResponseDto>("Failed to process data deletion request.", 500);
        }
    }

    /// <summary>
    /// Admin: Immediately anonymize a user's data (bypasses the scheduled processing).
    /// Use with caution - this operation is irreversible.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPost("user/{userId:int}/anonymize")]
    public async Task<ActionResult<ApiResponse<AnonymizationResultDto>>> AnonymizeUserData(int userId)
    {
        try
        {
            _logger.LogInformation(
                "HIGH-012: Admin {AdminUserId} initiating immediate anonymization for user {TargetUserId}",
                UserId, userId);

            var result = await _retentionService.AnonymizeUserDataAsync(userId, UserId);

            var response = new AnonymizationResultDto
            {
                UserId = result.UserId,
                Success = result.Success,
                Message = result.Message,
                AffectedEntities = result.AffectedEntities,
                Summary = result.Summary
            };

            if (!result.Success)
            {
                return Error<AnonymizationResultDto>(result.Message, 409);
            }

            return Success(response, "User data anonymized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "HIGH-012: Failed to anonymize user {UserId}",
                userId);
            return Error<AnonymizationResultDto>("Failed to anonymize user data.", 500);
        }
    }

    /// <summary>
    /// Admin: Update the data retention policy for a specific user.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("user/{userId:int}/retention-policy")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateRetentionPolicy(
        int userId,
        [FromBody] UpdateRetentionPolicyDto dto)
    {
        try
        {
            if (!Enum.IsDefined(typeof(DataRetentionPolicy), dto.Policy))
            {
                return Error<bool>($"Invalid retention policy value: {dto.Policy}.", 400);
            }

            _logger.LogInformation(
                "HIGH-012: Admin {AdminUserId} updating retention policy for user {TargetUserId} to {Policy}",
                UserId, userId, dto.Policy);

            await _retentionService.UpdateRetentionPolicyAsync(userId, dto.Policy, UserId);

            return Success(true, $"Retention policy updated to {dto.Policy} for user {userId}.");
        }
        catch (ArgumentException ex)
        {
            return Error<bool>(ex.Message, 400);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "HIGH-012: Failed to update retention policy for user {UserId}",
                userId);
            return Error<bool>("Failed to update retention policy.", 500);
        }
    }

    /// <summary>
    /// Admin: Get all pending GDPR data retention requests.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("requests/pending")]
    public async Task<ActionResult<ApiResponse<IEnumerable<GdprRequestResponseDto>>>> GetPendingRequests()
    {
        try
        {
            var requests = await _retentionService.GetPendingRequestsAsync();
            var dtos = requests.Select(r => new GdprRequestResponseDto
            {
                RequestId = r.Id,
                Status = r.Status,
                RequestType = r.RequestType,
                RequestedAt = r.RequestedAt,
                UserId = r.UserId,
                Reason = r.Reason,
                Message = $"Request for user {r.UserId}"
            });

            return Success(dtos, "Pending requests retrieved.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HIGH-012: Failed to retrieve pending GDPR requests");
            return Error<IEnumerable<GdprRequestResponseDto>>("Failed to retrieve pending requests.", 500);
        }
    }

    /// <summary>
    /// Get GDPR data retention requests for the current user.
    /// </summary>
    [HttpGet("my-requests")]
    public async Task<ActionResult<ApiResponse<IEnumerable<GdprRequestResponseDto>>>> GetMyRequests()
    {
        try
        {
            var requests = await _retentionService.GetRequestsByUserIdAsync(UserId);
            var dtos = requests.Select(r => new GdprRequestResponseDto
            {
                RequestId = r.Id,
                Status = r.Status,
                RequestType = r.RequestType,
                RequestedAt = r.RequestedAt,
                ProcessedAt = r.ProcessedAt,
                CompletedAt = r.CompletedAt,
                Message = GetStatusMessage(r)
            });

            return Success(dtos, "Your data retention requests retrieved.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HIGH-012: Failed to retrieve GDPR requests for user {UserId}", UserId);
            return Error<IEnumerable<GdprRequestResponseDto>>("Failed to retrieve requests.", 500);
        }
    }

    /// <summary>
    /// Admin: Get GDPR data retention requests for a specific user.
    /// </summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("user/{userId:int}/requests")]
    public async Task<ActionResult<ApiResponse<IEnumerable<GdprRequestResponseDto>>>> GetUserRequests(int userId)
    {
        try
        {
            var requests = await _retentionService.GetRequestsByUserIdAsync(userId);
            var dtos = requests.Select(r => new GdprRequestResponseDto
            {
                RequestId = r.Id,
                Status = r.Status,
                RequestType = r.RequestType,
                RequestedAt = r.RequestedAt,
                ProcessedAt = r.ProcessedAt,
                CompletedAt = r.CompletedAt,
                UserId = r.UserId,
                Reason = r.Reason,
                Message = GetStatusMessage(r)
            });

            return Success(dtos, $"Data retention requests for user {userId} retrieved.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HIGH-012: Failed to retrieve GDPR requests for user {UserId}", userId);
            return Error<IEnumerable<GdprRequestResponseDto>>("Failed to retrieve requests.", 500);
        }
    }

    #region Private Helpers

    private static string GetStatusMessage(DataRetentionRequest request)
    {
        return request.Status switch
        {
            GdprRequestStatus.Pending => "Your request is pending and will be processed within 30 days.",
            GdprRequestStatus.Processing => "Your request is currently being processed.",
            GdprRequestStatus.Completed => $"Your request was completed on {request.CompletedAt:yyyy-MM-dd}. {request.AffectedEntityCount} data items were anonymized.",
            GdprRequestStatus.Rejected => $"Your request was rejected. Reason: {request.RejectionReason}",
            GdprRequestStatus.Blocked => $"Your request is blocked. Reason: {request.RejectionReason}",
            _ => "Unknown status."
        };
    }

    #endregion
}

#region DTOs

/// <summary>
/// DTO for submitting a GDPR data deletion request.
/// </summary>
public class GdprDeletionRequestDto
{
    /// <summary>
    /// Reason for requesting data deletion.
    /// </summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// DTO for GDPR request responses.
/// </summary>
public class GdprRequestResponseDto
{
    public int RequestId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string RequestType { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int? UserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO for anonymization results.
/// </summary>
public class AnonymizationResultDto
{
    public int UserId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int AffectedEntities { get; set; }
    public string Summary { get; set; } = string.Empty;
}

/// <summary>
/// DTO for updating a user's data retention policy.
/// </summary>
public class UpdateRetentionPolicyDto
{
    /// <summary>
    /// The new retention policy to apply.
    /// 0=Standard, 1=Extended, 2=LegalHold, 3=DeletionRequested, 4=Anonymized.
    /// </summary>
    public DataRetentionPolicy Policy { get; set; }
}

#endregion
