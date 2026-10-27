namespace Depot.Application.Features.Reports.Throughput;

/// <param name="From">First depot day of the report.</param>
/// <param name="To">Last depot day of the report, included.</param>
public sealed record ThroughputQuery(DateOnly From, DateOnly To);
