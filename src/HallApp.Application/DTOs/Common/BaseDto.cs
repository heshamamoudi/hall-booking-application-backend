namespace HallApp.Application.DTOs.Common;

/// <summary>
/// Base DTO with common properties for all entities
/// </summary>
public abstract class BaseDto
{
    public int Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime? Updated { get; set; }
}

/// <summary>
/// Base DTO with audit properties for entities requiring status tracking
/// </summary>
public abstract class BaseAuditDto : BaseDto
{
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Base DTO for entities with approval workflow
/// </summary>
public abstract class BaseApprovalDto : BaseAuditDto
{
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
