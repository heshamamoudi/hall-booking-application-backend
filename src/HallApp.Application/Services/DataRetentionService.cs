using System.Security.Cryptography;
using System.Text;
using HallApp.Core.Entities;
using HallApp.Core.Entities.GdprEntities;
using HallApp.Core.Interfaces;
using HallApp.Core.Interfaces.IRepositories;
using HallApp.Core.Interfaces.IServices;
using Microsoft.Extensions.Logging;

namespace HallApp.Application.Services;

/// <summary>
/// HIGH-012: Service implementation for GDPR data retention and anonymization.
/// Handles right-to-erasure requests, cascading anonymization of personal data,
/// and scheduled retention policy enforcement. Financial data is preserved in
/// anonymized form for accounting compliance (ZATCA requirements).
/// </summary>
public class DataRetentionService : IDataRetentionService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<DataRetentionService> _logger;
    private readonly IFinancialAuditService _auditService;

    public DataRetentionService(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        ILogger<DataRetentionService> logger,
        IFinancialAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _logger = logger;
        _auditService = auditService;
    }

    /// <inheritdoc />
    public async Task<DataRetentionRequest> SubmitDeletionRequestAsync(
        int targetUserId, int requestedByUserId, string reason)
    {
        _logger.LogInformation(
            "HIGH-012: Data deletion request submitted - TargetUserId: {TargetUserId}, " +
            "RequestedBy: {RequestedBy}, Reason: {Reason}",
            targetUserId, requestedByUserId, reason);

        // Validate the target user exists
        var user = await _userRepository.GetUserById(targetUserId);
        if (user == null)
        {
            throw new ArgumentException($"User {targetUserId} not found.");
        }

        // Check if already anonymized
        if (user.IsAnonymized)
        {
            throw new InvalidOperationException($"User {targetUserId} has already been anonymized.");
        }

        // Check if there's already an active request
        var existingRequest = await _unitOfWork.DataRetentionRequestRepository
            .GetActiveRequestForUserAsync(targetUserId);
        if (existingRequest != null)
        {
            throw new InvalidOperationException(
                $"User {targetUserId} already has an active data deletion request (ID: {existingRequest.Id}, Status: {existingRequest.Status}).");
        }

        // Check for blocking conditions (legal hold)
        if (user.DataRetentionPolicy == DataRetentionPolicy.LegalHold)
        {
            throw new InvalidOperationException(
                $"User {targetUserId} is under a legal hold. Data cannot be deleted until the hold is released.");
        }

        var request = new DataRetentionRequest
        {
            UserId = targetUserId,
            RequestType = GdprRequestType.Deletion,
            Status = GdprRequestStatus.Pending,
            Reason = reason,
            RequestedByUserId = requestedByUserId,
            RequestedAt = DateTime.UtcNow
        };

        await _unitOfWork.DataRetentionRequestRepository.AddAsync(request);

        // Update user's retention policy to indicate deletion was requested
        user.DataRetentionPolicy = DataRetentionPolicy.DeletionRequested;
        user.Updated = DateTime.UtcNow;

        await _unitOfWork.Complete();

        _logger.LogInformation(
            "HIGH-012: Data deletion request created - RequestId: {RequestId}, " +
            "TargetUserId: {TargetUserId}, Status: {Status}",
            request.Id, targetUserId, request.Status);

        return request;
    }

    /// <inheritdoc />
    public async Task<AnonymizationResult> AnonymizeUserDataAsync(int userId, int? processedByUserId = null)
    {
        var correlationId = Guid.NewGuid().ToString("N")[..12];

        _logger.LogInformation(
            "HIGH-012: Starting user data anonymization - UserId: {UserId}, " +
            "ProcessedBy: {ProcessedBy}, CorrelationId: {CorrelationId}",
            userId, processedByUserId?.ToString() ?? "system", correlationId);

        var user = await _userRepository.GetUserById(userId);
        if (user == null)
        {
            return new AnonymizationResult
            {
                UserId = userId,
                Success = false,
                Message = $"User {userId} not found."
            };
        }

        if (user.IsAnonymized)
        {
            return new AnonymizationResult
            {
                UserId = userId,
                Success = false,
                Message = $"User {userId} is already anonymized."
            };
        }

        if (user.DataRetentionPolicy == DataRetentionPolicy.LegalHold)
        {
            return new AnonymizationResult
            {
                UserId = userId,
                Success = false,
                Message = $"User {userId} is under legal hold. Cannot anonymize."
            };
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync();

        try
        {
            var affectedCount = 0;
            var summaryParts = new List<string>();

            // Generate anonymized identifiers
            var anonHash = GenerateAnonymizedHash(userId);
            var anonEmail = $"anon-{anonHash}@anonymized.local";
            var anonName = $"Anonymized User {anonHash[..6]}";
            var anonPhone = $"+0000000{anonHash[..4]}";

            // 1. Anonymize AppUser PII
            var originalEmail = user.Email;
            var originalName = $"{user.FirstName} {user.LastName}";

            user.FirstName = "Anonymized";
            user.LastName = anonHash[..8];
            user.Email = anonEmail;
            user.NormalizedEmail = anonEmail.ToUpperInvariant();
            user.UserName = anonEmail;
            user.NormalizedUserName = anonEmail.ToUpperInvariant();
            user.PhoneNumber = anonPhone;
            user.Gender = "";
            user.PhotoUrl = null;
            user.RefreshToken = string.Empty;
            user.RefreshTokenExpiryTime = DateTime.MinValue;
            user.InvitationToken = null;
            user.InvitationTokenExpiry = null;
            user.PasswordHash = string.Empty; // Disable login
            user.SecurityStamp = Guid.NewGuid().ToString(); // Invalidate all sessions
            user.Active = false;
            user.IsAnonymized = true;
            user.AnonymizedAt = DateTime.UtcNow;
            user.DataRetentionPolicy = DataRetentionPolicy.Anonymized;
            user.Updated = DateTime.UtcNow;

            affectedCount++;
            summaryParts.Add("AppUser PII anonymized");

            // 2. Anonymize Customer data (addresses, favorites)
            var customer = await _unitOfWork.CustomerRepository.GetCustomerByAppUserIdAsync(userId);
            if (customer != null)
            {
                // Anonymize addresses
                if (customer.Addresses != null)
                {
                    foreach (var address in customer.Addresses)
                    {
                        address.Street = "Anonymized";
                        address.City = "Anonymized";
                        address.ZipCode = "00000";
                        affectedCount++;
                    }
                    summaryParts.Add($"{customer.Addresses.Count} addresses anonymized");
                }

                // Remove favorites (non-financial, no retention requirement)
                if (customer.Favorites != null && customer.Favorites.Any())
                {
                    var favoriteCount = customer.Favorites.Count;
                    foreach (var favorite in customer.Favorites.ToList())
                    {
                        _unitOfWork.FavoriteRepository.Delete(favorite);
                    }
                    affectedCount += favoriteCount;
                    summaryParts.Add($"{favoriteCount} favorites deleted");
                }

                customer.Active = false;
                customer.Updated = DateTime.UtcNow;
                affectedCount++;
                summaryParts.Add("Customer profile anonymized");
            }

            // 3. Anonymize invoice buyer information (preserve financial totals for ZATCA compliance)
            // NOTE: GetInvoicesByCustomerIdAsync expects the Customer entity ID, not the AppUser ID
            var invoiceCustomerId = customer?.Id ?? 0;
            var invoices = invoiceCustomerId > 0
                ? await _unitOfWork.InvoiceRepository.GetInvoicesByCustomerIdAsync(invoiceCustomerId)
                : Enumerable.Empty<Core.Entities.BookingEntities.Invoice>();
            var invoiceList = invoices.ToList();
            if (invoiceList.Any())
            {
                foreach (var invoice in invoiceList)
                {
                    invoice.BuyerName = anonName;
                    invoice.BuyerAddress = "Anonymized";
                    invoice.BuyerCity = "Anonymized";
                    invoice.BuyerPostalCode = "00000";
                    invoice.BuyerVatNumber = "";
                    // Preserve: InvoiceNumber, TotalAmountWithTax, TaxAmount, PaymentStatus, etc.
                    // These are required for ZATCA compliance and financial reporting.
                    invoice.UpdatedAt = DateTime.UtcNow;
                    _unitOfWork.InvoiceRepository.Update(invoice);
                    affectedCount++;
                }
                summaryParts.Add($"{invoiceList.Count} invoices anonymized (financial data preserved)");
            }

            // 4. Anonymize review content (preserve ratings for hall statistics)
            if (customer != null && customer.Reviews != null && customer.Reviews.Any())
            {
                foreach (var review in customer.Reviews)
                {
                    review.Content = "[Content removed per GDPR request]";
                    // Preserve: Rating, HallId, VendorId for aggregate statistics
                    affectedCount++;
                }
                summaryParts.Add($"{customer.Reviews.Count} reviews anonymized (ratings preserved)");
            }

            // 5. Delete notifications (non-financial, no retention requirement)
            await _unitOfWork.NotificationRepository.DeleteAllByUserAsync(userId);
            summaryParts.Add("Notifications deleted");

            await _unitOfWork.Complete();
            await transaction.CommitAsync();

            var summary = string.Join("; ", summaryParts);

            _logger.LogInformation(
                "HIGH-012: User data anonymization completed - UserId: {UserId}, " +
                "AffectedEntities: {AffectedEntities}, Summary: {Summary}, CorrelationId: {CorrelationId}",
                userId, affectedCount, summary, correlationId);

            // Audit trail
            await _auditService.LogAsync(
                processedByUserId,
                "gdpr_anonymization",
                "AppUser",
                userId.ToString(),
                beforeState: new { OriginalEmail = originalEmail, OriginalName = originalName },
                afterState: new { AnonymizedEmail = anonEmail, AnonymizedName = anonName, AffectedEntities = affectedCount },
                correlationId: correlationId,
                description: $"GDPR anonymization completed for user {userId}: {summary}");

            return new AnonymizationResult
            {
                UserId = userId,
                Success = true,
                Message = "User data anonymized successfully.",
                AffectedEntities = affectedCount,
                Summary = summary
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();

            _logger.LogError(ex,
                "HIGH-012: User data anonymization failed - UserId: {UserId}, CorrelationId: {CorrelationId}",
                userId, correlationId);

            return new AnonymizationResult
            {
                UserId = userId,
                Success = false,
                Message = $"Anonymization failed: {ex.Message}"
            };
        }
    }

    /// <inheritdoc />
    /// <inheritdoc />
    public async Task<DataRetentionRequest?> ApproveRequestAsync(int requestId, int processedByUserId)
    {
        var request = await _unitOfWork.DataRetentionRequestRepository.GetByIdAsync(requestId);
        if (request == null)
        {
            return null;
        }

        if (request.Status != GdprRequestStatus.Pending)
        {
            _logger.LogWarning(
                "GDPR request {RequestId} cannot be approved from status {Status}",
                requestId, request.Status);
            return request;
        }

        request.Status = GdprRequestStatus.Processing;
        request.ProcessedByUserId = processedByUserId;
        request.ProcessedAt = DateTime.UtcNow;
        _unitOfWork.DataRetentionRequestRepository.Update(request);
        await _unitOfWork.Complete();

        // Same anonymisation the scheduled cycle performs - approving simply runs
        // it now instead of waiting for the next pass.
        var result = await AnonymizeUserDataAsync(request.UserId, processedByUserId);

        if (result.Success)
        {
            request.Status = GdprRequestStatus.Completed;
            request.CompletedAt = DateTime.UtcNow;
            request.AffectedEntityCount = result.AffectedEntities;
            request.AffectedDataSummary = result.Summary;
        }
        else
        {
            // Something blocks erasure - an open booking, a retention hold. The
            // request stays on the books with the reason rather than disappearing.
            request.Status = GdprRequestStatus.Blocked;
            request.RejectionReason = result.Message;
        }

        _unitOfWork.DataRetentionRequestRepository.Update(request);
        await _unitOfWork.Complete();

        _logger.LogInformation(
            "GDPR request {RequestId} approved by {AdminId} - final status {Status}",
            requestId, processedByUserId, request.Status);

        return request;
    }

    /// <inheritdoc />
    public async Task<DataRetentionRequest?> RejectRequestAsync(int requestId, int processedByUserId, string reason)
    {
        var request = await _unitOfWork.DataRetentionRequestRepository.GetByIdAsync(requestId);
        if (request == null)
        {
            return null;
        }

        if (request.Status != GdprRequestStatus.Pending)
        {
            _logger.LogWarning(
                "GDPR request {RequestId} cannot be rejected from status {Status}",
                requestId, request.Status);
            return request;
        }

        request.Status = GdprRequestStatus.Rejected;
        request.RejectionReason = reason;
        request.ProcessedByUserId = processedByUserId;
        request.ProcessedAt = DateTime.UtcNow;

        _unitOfWork.DataRetentionRequestRepository.Update(request);
        await _unitOfWork.Complete();

        _logger.LogInformation(
            "GDPR request {RequestId} rejected by {AdminId}", requestId, processedByUserId);

        return request;
    }

    public async Task<int> ProcessPendingDeletionRequestsAsync()
    {
        _logger.LogInformation("HIGH-012: Processing pending GDPR deletion requests");

        var pendingRequests = await _unitOfWork.DataRetentionRequestRepository.GetPendingRequestsAsync();
        var processedCount = 0;

        foreach (var request in pendingRequests)
        {
            try
            {
                // Mark as processing
                request.Status = GdprRequestStatus.Processing;
                request.ProcessedAt = DateTime.UtcNow;
                _unitOfWork.DataRetentionRequestRepository.Update(request);
                await _unitOfWork.Complete();

                // Perform anonymization
                var result = await AnonymizeUserDataAsync(request.UserId, request.ProcessedByUserId);

                // Update request with result
                if (result.Success)
                {
                    request.Status = GdprRequestStatus.Completed;
                    request.CompletedAt = DateTime.UtcNow;
                    request.AffectedEntityCount = result.AffectedEntities;
                    request.AffectedDataSummary = result.Summary;
                    processedCount++;
                }
                else
                {
                    request.Status = GdprRequestStatus.Blocked;
                    request.RejectionReason = result.Message;
                }

                _unitOfWork.DataRetentionRequestRepository.Update(request);
                await _unitOfWork.Complete();

                _logger.LogInformation(
                    "HIGH-012: Deletion request processed - RequestId: {RequestId}, " +
                    "UserId: {UserId}, Status: {Status}",
                    request.Id, request.UserId, request.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "HIGH-012: Failed to process deletion request {RequestId} for user {UserId}",
                    request.Id, request.UserId);

                request.Status = GdprRequestStatus.Blocked;
                request.RejectionReason = $"Processing error: {ex.Message}";
                _unitOfWork.DataRetentionRequestRepository.Update(request);
                await _unitOfWork.Complete();
            }
        }

        _logger.LogInformation(
            "HIGH-012: Pending deletion requests processing completed - " +
            "TotalPending: {TotalPending}, Processed: {Processed}",
            pendingRequests.Count(), processedCount);

        return processedCount;
    }

    /// <inheritdoc />
    public async Task<int> ProcessExpiredRetentionAsync(int standardRetentionDays = 1095)
    {
        _logger.LogInformation(
            "HIGH-012: Processing expired data retention - StandardRetentionDays: {Days}",
            standardRetentionDays);

        var cutoffDate = DateTime.UtcNow.AddDays(-standardRetentionDays);
        var processedCount = 0;

        // Find users whose last activity exceeds the retention period
        // and have standard retention policy (not legal hold, not already anonymized)
        var allUsers = await _userRepository.GetUsersAsync();
        var eligibleUsers = allUsers
            .Where(u => !u.IsAnonymized
                        && u.DataRetentionPolicy == DataRetentionPolicy.Standard
                        && u.Active == false // Only inactive users
                        && (u.LastActivityAt ?? u.Updated) < cutoffDate)
            .ToList();

        foreach (var user in eligibleUsers)
        {
            try
            {
                var result = await AnonymizeUserDataAsync(user.Id);
                if (result.Success)
                {
                    processedCount++;

                    _logger.LogInformation(
                        "HIGH-012: Expired retention anonymization completed - " +
                        "UserId: {UserId}, LastActivity: {LastActivity}",
                        user.Id, user.LastActivityAt ?? user.Updated);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "HIGH-012: Failed to anonymize expired user {UserId}",
                    user.Id);
            }
        }

        _logger.LogInformation(
            "HIGH-012: Expired retention processing completed - " +
            "EligibleUsers: {Eligible}, Anonymized: {Anonymized}",
            eligibleUsers.Count, processedCount);

        return processedCount;
    }

    /// <inheritdoc />
    public async Task UpdateRetentionPolicyAsync(int userId, DataRetentionPolicy policy, int updatedByUserId)
    {
        var user = await _userRepository.GetUserById(userId);
        if (user == null)
        {
            throw new ArgumentException($"User {userId} not found.");
        }

        var previousPolicy = user.DataRetentionPolicy;
        user.DataRetentionPolicy = policy;
        user.Updated = DateTime.UtcNow;

        await _unitOfWork.Complete();

        _logger.LogInformation(
            "HIGH-012: Retention policy updated - UserId: {UserId}, " +
            "PreviousPolicy: {PreviousPolicy}, NewPolicy: {NewPolicy}, UpdatedBy: {UpdatedBy}",
            userId, previousPolicy, policy, updatedByUserId);

        await _auditService.LogAsync(
            updatedByUserId,
            "gdpr_policy_updated",
            "AppUser",
            userId.ToString(),
            beforeState: new { Policy = previousPolicy.ToString() },
            afterState: new { Policy = policy.ToString() },
            description: $"Data retention policy changed from {previousPolicy} to {policy}");
    }

    /// <inheritdoc />
    public async Task<DataRetentionRequest?> GetRequestByIdAsync(int requestId)
    {
        return await _unitOfWork.DataRetentionRequestRepository.GetByIdAsync(requestId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DataRetentionRequest>> GetRequestsByUserIdAsync(int userId)
    {
        return await _unitOfWork.DataRetentionRequestRepository.GetByUserIdAsync(userId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<DataRetentionRequest>> GetPendingRequestsAsync()
    {
        return await _unitOfWork.DataRetentionRequestRepository.GetPendingRequestsAsync();
    }

    /// <summary>
    /// Generates a deterministic anonymized hash for a user ID.
    /// Used to create consistent anonymized identifiers across the system.
    /// </summary>
    private static string GenerateAnonymizedHash(int userId)
    {
        var input = $"gdpr-anon-{userId}-{DateTime.UtcNow:yyyyMMdd}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }
}
