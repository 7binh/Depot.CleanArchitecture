namespace Depot.Application.Features.ReleaseOrders.Create;

public sealed record ReleaseOrderLineRequest(Guid ContainerTypeId, int SizeFeet, int Quantity);

/// <summary>Raw input for entering a release order with its lines (BR-RO-01, BR-RO-02).</summary>
public sealed record CreateReleaseOrderCommand(
    Guid LineOperatorId,
    Guid CustomerId,
    string? OrderNo,
    DateOnly IssuedDate,
    DateOnly ExpiryDate,
    string? ExportVessel,
    IReadOnlyList<ReleaseOrderLineRequest>? Lines);
