namespace Depot.CleanArchitecture.Application.Features.Analytics.GetDailyThroughputReport;

using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;

public sealed record DailyThroughputDto(
    DateOnly ReportDate,
    string LineOperator,
    int GateInCount,
    int GateOutCount,
    int TotalMovements,
    decimal TotalTeu);

public sealed record GetDailyThroughputReportQuery(DateOnly FromDate, DateOnly ToDate)
    : IQuery<Result<List<DailyThroughputDto>>>;
