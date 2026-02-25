using HallApp.Core.Entities.GdprEntities;

namespace HallApp.Core.Interfaces.IRepositories;

/// <summary>
/// HIGH-012: Repository interface for GDPR data retention requests.
/// </summary>
public interface IDataRetentionRequestRepository : IGenericRepository<DataRetentionRequest>
{
    /// <summary>
    /// Gets all pending data retention requests.
    /// </summary>
    Task<IEnumerable<DataRetentionRequest>> GetPendingRequestsAsync();

    /// <summary>
    /// Gets all data retention requests for a specific user.
    /// </summary>
    Task<IEnumerable<DataRetentionRequest>> GetByUserIdAsync(int userId);

    /// <summary>
    /// Gets the most recent active request for a user (pending or processing).
    /// </summary>
    Task<DataRetentionRequest> GetActiveRequestForUserAsync(int userId);
}
