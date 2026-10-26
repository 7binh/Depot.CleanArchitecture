using Depot.Api.Extensions;
using Depot.Application.Abstractions.Messaging;
using Depot.Application.Features.Blocks;
using Depot.Application.Features.Blocks.Create;
using Depot.Application.Features.Blocks.GetAll;
using Depot.Application.Features.Blocks.GetById;
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
