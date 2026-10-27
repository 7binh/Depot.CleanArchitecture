using Depot.Api.Extensions;
using Depot.Application.Abstractions.Messaging;
using Depot.Application.Features.Reports.Throughput;
using Depot.Application.Features.Reports.YardStock;
using Depot.Domain.Common;

namespace Depot.Api.Endpoints;

public static class ReportEndpoints
{
    public static void MapReportEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reports")
            .WithTags("Reports");

        group.MapGet("/throughput", GetThroughput)
            .WithName("GetThroughputReport")
            .WithSummary("Gate ins and gate outs per line operator per day, from and to included (dates as yyyy-MM-dd)");

        group.MapGet("/yard-stock", GetYardStock)
            .WithName("GetYardStockReport")
            .WithSummary("Containers in the yard per line operator on a day, split into 0-9 days and 10+ days; date defaults to today");
    }

    private static async Task<IResult> GetThroughput(
        IQueryHandler<ThroughputQuery, Result<IReadOnlyList<ThroughputRow>>> handler,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new ThroughputQuery(from, to), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetYardStock(
        IQueryHandler<YardStockQuery, Result<IReadOnlyList<YardStockRow>>> handler,
        CancellationToken cancellationToken,
        DateOnly? date = null)
    {
        var result = await handler.HandleAsync(new YardStockQuery(date), cancellationToken);
        return result.ToHttpResult();
    }
}
