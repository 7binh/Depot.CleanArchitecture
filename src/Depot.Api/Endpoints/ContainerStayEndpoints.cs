using Depot.Api.Extensions;
using Depot.Application.Abstractions.Messaging;
using Depot.Application.Features.ContainerStays;
using Depot.Application.Features.ContainerStays.GateIn;
using Depot.Application.Features.ContainerStays.GateOut;
using Depot.Application.Features.ContainerStays.GetAll;
using Depot.Application.Features.ContainerStays.GetById;
using Depot.Domain.Common;

namespace Depot.Api.Endpoints;

public static class ContainerStayEndpoints
{
    public static void MapContainerStayEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/gate-in", GateIn)
            .WithTags("Gate")
            .WithName("GateIn")
            .WithSummary("Record a container entering the depot: time, grade, condition, vehicle and yard position");

        app.MapPost("/api/gate-out", GateOut)
            .WithTags("Gate")
            .WithName("GateOut")
            .WithSummary("Record a container leaving the depot on a release order");

        var group = app.MapGroup("/api/container-stays")
            .WithTags("Container stays");

        group.MapGet("/", GetAll)
            .WithName("GetContainerStays")
            .WithSummary("List container stays, newest gate in first; inYard=true keeps only containers now in the yard");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetContainerStayById")
            .WithSummary("Get one container stay by id");
    }

    private static async Task<IResult> GateIn(
        GateInCommand command,
        ICommandHandler<GateInCommand, Result<ContainerStayResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? Results.CreatedAtRoute("GetContainerStayById", new { id = result.Value.Id }, result.Value)
            : result.Error.ToProblem();
    }

    private static async Task<IResult> GateOut(
        GateOutCommand command,
        ICommandHandler<GateOutCommand, Result<ContainerStayResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetContainerStaysQuery, Result<IReadOnlyList<ContainerStayResponse>>> handler,
        CancellationToken cancellationToken,
        bool inYard = false)
    {
        var result = await handler.HandleAsync(new GetContainerStaysQuery(inYard), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetContainerStayByIdQuery, Result<ContainerStayResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetContainerStayByIdQuery(id), cancellationToken);
        return result.ToHttpResult();
    }
}
