using Depot.Api.Extensions;
using Depot.Application.Abstractions.Messaging;
using Depot.Application.Features.Bays;
using Depot.Application.Features.Bays.Create;
using Depot.Application.Features.Bays.Delete;
using Depot.Application.Features.Bays.GetAll;
using Depot.Application.Features.Bays.GetById;
using Depot.Domain.Common;

namespace Depot.Api.Endpoints;

public static class BayEndpoints
{
    public static void MapBayEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/blocks/{blockId:guid}/bays")
            .WithTags("Bays");

        group.MapPost("/", Create)
            .WithName("CreateBay")
            .WithSummary("Add a bay to a block (not allowed on a virtual block)");

        group.MapGet("/", GetAll)
            .WithName("GetBays")
            .WithSummary("List the bays of a block, ordered by bay number");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetBayById")
            .WithSummary("Get one bay of a block by id");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteBay")
            .WithSummary("Remove a bay with its rows (refused while a container stands in it or on it)");
    }

    private static async Task<IResult> Delete(
        Guid blockId,
        Guid id,
        ICommandHandler<DeleteBayCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteBayCommand(blockId, id), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Create(
        Guid blockId,
        CreateBayRequest request,
        ICommandHandler<CreateBayCommand, Result<BayResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new CreateBayCommand(blockId, request.BayNo), cancellationToken);
        return result.IsSuccess
            ? Results.CreatedAtRoute("GetBayById", new { blockId, id = result.Value.Id }, result.Value)
            : result.Error.ToProblem();
    }

    private static async Task<IResult> GetAll(
        Guid blockId,
        IQueryHandler<GetBaysQuery, Result<IReadOnlyList<BayResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetBaysQuery(blockId), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetById(
        Guid blockId,
        Guid id,
        IQueryHandler<GetBayByIdQuery, Result<BayResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetBayByIdQuery(blockId, id), cancellationToken);
        return result.ToHttpResult();
    }
}

/// <summary>Body of POST /api/blocks/{blockId}/bays; the block id comes from the route.</summary>
public sealed record CreateBayRequest(int BayNo);
