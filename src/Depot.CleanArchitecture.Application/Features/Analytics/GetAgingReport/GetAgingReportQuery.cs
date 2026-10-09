namespace Depot.CleanArchitecture.Application.Features.Analytics.GetAgingReport;

using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;

public sealed record LineOperatorAgingDto(
    string LineOperator,
    int Count0To10Days,
    int CountOver10Days,
    int TotalInventory);

public sealed record GetAgingReportQuery : IQuery<Result<List<LineOperatorAgingDto>>>;
