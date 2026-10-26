namespace Depot.Application.Features.Blocks.GenerateLayout;

/// <summary>Input for creating the bays FromBay..ToBay of a block, each with rows 1..RowCount of MaxTier tiers.</summary>
public sealed record GenerateLayoutCommand(Guid BlockId, int FromBay, int ToBay, int RowCount, int MaxTier);

/// <summary>What the call added; bays and rows that already existed are left alone and not counted.</summary>
public sealed record GenerateLayoutResponse(int BaysCreated, int RowsCreated);
