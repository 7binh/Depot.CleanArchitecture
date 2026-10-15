namespace Depot.CleanArchitecture.Domain.Entities;

using Depot.CleanArchitecture.Domain.Common;

public sealed class MovementEvent : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid VisitId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public Guid? FromSlotId { get; private set; }
    public Guid? ToSlotId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string ActorId { get; private set; } = string.Empty;
    public string? Reason { get; private set; }

    private MovementEvent() { }

    public static MovementEvent Create(
        Guid tenantId,
        Guid visitId,
        string eventType,
        Guid? fromSlotId,
        Guid? toSlotId,
        string actorId,
        string? reason = null)
    {
        return new MovementEvent
        {
            TenantId = tenantId,
            VisitId = visitId,
            EventType = eventType,
            FromSlotId = fromSlotId,
            ToSlotId = toSlotId,
            OccurredAt = DateTime.UtcNow,
            ActorId = actorId,
            Reason = reason
        };
    }
}
