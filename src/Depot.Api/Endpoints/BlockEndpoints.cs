using Depot.Api.Extensions;
using Depot.Application.Abstractions.Messaging;
using Depot.Application.Features.Blocks;
using Depot.Application.Features.Blocks.Create;
using Depot.Application.Features.Blocks.Delete;
using Depot.Application.Features.Blocks.GenerateLayout;
using Depot.Application.Features.Blocks.GetAll;
using Depot.Application.Features.Blocks.GetById;
using Depot.Application.Features.Blocks.GetMap;
using Depot.Application.Features.Blocks.Update;
using Depot.Domain.Common;

namespace Depot.Api.Endpoints;

public static class BlockEndpoints
{
    public static void MapBlockEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/blocks")
            .WithTags("Blocks");

        group.MapPost("/", Create)
            .WithName("CreateBlock")
            .WithSummary("Create a new yard block (normal or virtual)");

        group.MapGet("/", GetAll)
            .WithName("GetBlocks")
            .WithSummary("List all blocks, ordered by code");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetBlockById")
            .WithSummary("Get one block by id");

        group.MapGet("/{id:guid}/map", GetMap)
            .WithName("GetBlockMap")
            .WithSummary("Get the map of a block to draw: its bays, rows and the container on each tier");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateBlock")
            .WithSummary("Rename a block (the code and the virtual flag cannot be changed)");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteBlock")
            .WithSummary("Remove a block with its bays and rows (refused when it ever held a container)");

        group.MapPost("/{id:guid}/layout", GenerateLayout)
            .WithName("GenerateBlockLayout")
            .WithSummary("Create the bays FromBay..ToBay of a block, each with rows 1..RowCount (existing ones are kept)");
    }

    private static async Task<IResult> GetMap(
        Guid id,
        IQueryHandler<GetBlockMapQuery, Result<BlockMapResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetBlockMapQuery(id), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Delete(
        Guid id,
        ICommandHandler<DeleteBlockCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteBlockCommand(id), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Update(
        Guid id,
        UpdateBlockRequest request,
        ICommandHandler<UpdateBlockCommand, Result<BlockResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new UpdateBlockCommand(id, request.Name), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GenerateLayout(
        Guid id,
        GenerateLayoutRequest request,
        ICommandHandler<GenerateLayoutCommand, Result<GenerateLayoutResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new GenerateLayoutCommand(id, request.FromBay, request.ToBay, request.RowCount, request.MaxTier);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Create(
        CreateBlockCommand command,
        ICommandHandler<CreateBlockCommand, Result<BlockResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? Results.CreatedAtRoute("GetBlockById", new { id = result.Value.Id }, result.Value)
            : result.Error.ToProblem();
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetBlocksQuery, Result<IReadOnlyList<BlockResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetBlocksQuery(), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetBlockByIdQuery, Result<BlockResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetBlockByIdQuery(id), cancellationToken);
        return result.ToHttpResult();
    }
}

/// <summary>Body of PUT /api/blocks/{id}; the block id comes from the route.</summary>
public sealed record UpdateBlockRequest(string? Name);

/// <summary>Body of POST /api/blocks/{id}/layout; the block id comes from the route.</summary>
public sealed record GenerateLayoutRequest(int FromBay, int ToBay, int RowCount, int MaxTier);
