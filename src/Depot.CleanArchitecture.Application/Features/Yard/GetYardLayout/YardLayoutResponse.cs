namespace Depot.CleanArchitecture.Application.Features.Yard.GetYardLayout;

public sealed record YardLayoutResponse(
    IReadOnlyList<YardBlockDto> Blocks,
    IReadOnlyList<YardSlotDto> Slots);

public sealed record YardBlockDto(
    Guid Id,
    string Code,
    string Name,
    int? MaxBay,
    int? MaxRow,
    int? MaxTier,
    bool HasReeferPower,
    bool IsVirtual,
    int? MaxCapacity,
    int TotalContainers,
    IReadOnlyList<BlockTierStatDto> TierStats,
    IReadOnlyList<VirtualContainerItemDto> VirtualContainers);

public sealed record BlockTierStatDto(
    int Tier,
    int ContainerCount);

public sealed record VirtualContainerItemDto(
    Guid VisitId,
    Guid ContainerId,
    string ContainerNumber,
    string IsoCode,
    int Size,
    string TypeDescription,
    string LineOperator,
    string? Grade,
    string? Status,
    DateTime GateInDate,
    DateTime? EnteredWorkshopDate,
    int DwellDays,
    int ActiveHoldsCount,
    string? DamageNotes);

public sealed record YardSlotDto(
    Guid Id,
    Guid BlockId,
    string BlockCode,
    int Bay,
    int Row,
    int Tier,
    bool IsOccupied,
    Guid? ContainerId,
    string? ContainerNumber,
    string? IsoCode,
    int? Size,
    string? LineOperator,
    string? ColorScheme,
    Guid? VisitId = null,
    string? TypeDescription = null,
    string? Grade = null,
    string? Status = null,
    DateTime? GateInDate = null,
    int? DwellDays = null,
    string? TractorNo = null,
    string? DriverName = null,
    string? DamageNotes = null,
    int ActiveHoldsCount = 0);
