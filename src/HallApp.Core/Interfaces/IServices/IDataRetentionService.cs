using HallApp.Core.Entities.GdprEntities;

namespace HallApp.Core.Interfaces.IServices;

/// <summary>
/// HIGH-012: Service interface for GDPR data retention and anonymization operations.
/// Handles right-to-erasure requests, scheduled anonymization, and data retention policy management.
/// </summary>
public interface IDataRetentionService
{
    /// <summary>
    /// Submits a GDPR data deletion/anonymization request for a user.
    /// Validates that the user exists, is not already anonymized, and has no blocking conditions.
    /// </summary>
    /// <param name="targetUserId">The user whose data should be deleted/anonymized.</param>
    /// <param name="requestedByUserId">The user who initiated the request (self or admin).</param>
    /// <param name="reason">Reason for the request.</param>
    /// <returns>The created DataRetentionRequest.</returns>
    Task<DataRetentionRequest> SubmitDeletionRequestAsync(int targetUserId, int requestedByUserId, string reason);

    /// <summary>
    /// Anonymizes a specific user's personal data.
    /// Replaces PII with hashed/placeholder values while preserving aggregated financial data
    /// for accounting compliance.
    /// </summary>
    /// <param name="userId">The user to anonymize.</param>
    /// <param name="processedByUserId">The admin or system user processing the request.</param>
    /// <returns>Summary of the anonymization operation.</returns>
    Task<AnonymizationResult> AnonymizeUserDataAsync(int userId, int? processedByUserId = null);

    /// <summary>
    /// Processes all pending deletion requests that are eligible for anonymization.
    /// Called by the background service on a scheduled basis.
    /// </summary>
    /// <returns>Number of users anonymized in this run.</returns>
    Task<int> ProcessPendingDeletionRequestsAsync();

    /// <summary>
    /// Approves one pending request and anonymises the user immediately, rather than
    /// waiting for the next scheduled retention cycle. This is what the Approve
    /// button on the admin GDPR screen calls.
    /// </summary>
    /// <param name="requestId">The request to approve.</param>
    /// <param name="processedByUserId">The administrator approving it.</param>
    /// <returns>The updated request, or null when no such request exists.</returns>
    Task<DataRetentionRequest?> ApproveRequestAsync(int requestId, int processedByUserId);

    /// <summary>
    /// Rejects one pending request. A reason is required: a subject whose erasure
    /// request is refused is entitled to know why.
    /// </summary>
    /// <param name="requestId">The request to reject.</param>
    /// <param name="processedByUserId">The administrator rejecting it.</param>
    /// <param name="reason">Why it was refused.</param>
    /// <returns>The updated request, or null when no such request exists.</returns>
    Task<DataRetentionRequest?> RejectRequestAsync(int requestId, int processedByUserId, string reason);

    /// <summary>
    /// Processes users whose data retention period has expired based on their policy.
    /// Called by the background service on a scheduled basis.
    /// </summary>
    /// <param name="standardRetentionDays">Number of days for standard retention (default: 1095 = 3 years).</param>
    /// <returns>Number of users anonymized in this run.</returns>
    Task<int> ProcessExpiredRetentionAsync(int standardRetentionDays = 1095);

    /// <summary>
    /// Updates the data retention policy for a specific user.
    /// </summary>
    /// <param name="userId">The user whose policy to update.</param>
    /// <param name="policy">The new retention policy.</param>
    /// <param name="updatedByUserId">The admin who updated the policy.</param>
    Task UpdateRetentionPolicyAsync(int userId, DataRetentionPolicy policy, int updatedByUserId);

    /// <summary>
    /// Gets the data retention request by ID.
    /// </summary>
    Task<DataRetentionRequest?> GetRequestByIdAsync(int requestId);

    /// <summary>
    /// Gets all data retention requests for a specific user.
    /// </summary>
    Task<IEnumerable<DataRetentionRequest>> GetRequestsByUserIdAsync(int userId);

    /// <summary>
    /// Gets all pending data retention requests.
    /// </summary>
    Task<IEnumerable<DataRetentionRequest>> GetPendingRequestsAsync();
}

/// <summary>
/// Result of a user data anonymization operation.
/// </summary>
public class AnonymizationResult
{
    public int UserId { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int AffectedEntities { get; set; }
    public string Summary { get; set; } = string.Empty;
}
