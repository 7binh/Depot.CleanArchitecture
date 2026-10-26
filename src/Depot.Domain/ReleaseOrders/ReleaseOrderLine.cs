namespace Depot.Domain.ReleaseOrders;

/// <summary>One requested line of a release order (BR-RO-02), grouped so that <see cref="ReleaseOrder.Create"/> stays short.</summary>
public sealed record ReleaseOrderLine(Guid ContainerTypeId, int SizeFeet, int Quantity);
