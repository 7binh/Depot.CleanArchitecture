namespace Depot.Domain.Common;

/// <summary>
/// Entity that records who created / last changed it and when.
/// Values are filled by Infrastructure when saving, never by business code.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTimeOffset CreatedAt { get; protected set; }

    public string? CreatedBy { get; protected set; }

    public DateTimeOffset? LastModifiedAt { get; protected set; }

    public string? LastModifiedBy { get; protected set; }
}
