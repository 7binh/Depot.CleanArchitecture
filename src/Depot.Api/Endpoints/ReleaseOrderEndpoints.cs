using Depot.Api.Extensions;
using Depot.Application.Abstractions.Messaging;
using Depot.Application.Features.ReleaseOrders;
using Depot.Application.Features.ReleaseOrders.Create;
using Depot.Application.Features.ReleaseOrders.GetAll;
using Depot.Application.Features.ReleaseOrders.GetById;
using Depot.Domain.Common;

namespace Depot.Api.Endpoints;

public static class ReleaseOrderEndpoints
{
    public static void MapReleaseOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/release-orders")
            .WithTags("Release orders");

        group.MapPost("/", Create)
            .WithName("CreateReleaseOrder")
            .WithSummary("Enter a release order (operator, customer, order number, expiry date, export vessel) with its lines");

        group.MapGet("/", GetAll)
            .WithName("GetReleaseOrders")
            .WithSummary("List release orders, newest first, with an IsExpired flag");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetReleaseOrderById")
            .WithSummary("Get one release order with its lines");
    }

    private static async Task<IResult> Create(
        CreateReleaseOrderCommand command,
        ICommandHandler<CreateReleaseOrderCommand, Result<ReleaseOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? Results.CreatedAtRoute("GetReleaseOrderById", new { id = result.Value.Id }, result.Value)
            : result.Error.ToProblem();
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetReleaseOrdersQuery, Result<IReadOnlyList<ReleaseOrderResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetReleaseOrdersQuery(), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetReleaseOrderByIdQuery, Result<ReleaseOrderResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetReleaseOrderByIdQuery(id), cancellationToken);
        return result.ToHttpResult();
    }
}
