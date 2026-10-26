using Depot.Api.Extensions;
using Depot.Application.Abstractions.Messaging;
using Depot.Application.Features.YardRows;
using Depot.Application.Features.YardRows.Create;
using Depot.Application.Features.YardRows.Delete;
using Depot.Application.Features.YardRows.GetAll;
using Depot.Application.Features.YardRows.GetById;
using Depot.Application.Features.YardRows.Update;
using Depot.Domain.Common;

namespace Depot.Api.Endpoints;

public static class YardRowEndpoints
{
    public static void MapYardRowEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/blocks/{blockId:guid}/bays/{bayId:guid}/rows")
            .WithTags("Rows");

        group.MapPost("/", Create)
            .WithName("CreateYardRow")
            .WithSummary("Add a row to a bay");

        group.MapGet("/", GetAll)
            .WithName("GetYardRows")
            .WithSummary("List the rows of a bay, ordered by row number");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetYardRowById")
            .WithSummary("Get one row of a bay by id");

        group.MapPut("/{id:guid}", Update)
            .WithName("UpdateYardRow")
            .WithSummary("Change the maximum tier of a row (lowering it is refused while a container stands above it)");

        group.MapDelete("/{id:guid}", Delete)
            .WithName("DeleteYardRow")
            .WithSummary("Remove a row (refused while a container stands in it)");
    }

    private static async Task<IResult> Delete(
        Guid blockId,
        Guid bayId,
        Guid id,
        ICommandHandler<DeleteYardRowCommand, Result> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new DeleteYardRowCommand(blockId, bayId, id), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Update(
        Guid blockId,
        Guid bayId,
        Guid id,
        UpdateYardRowRequest request,
        ICommandHandler<UpdateYardRowCommand, Result<YardRowResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateYardRowCommand(blockId, bayId, id, request.MaxTier);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Create(
        Guid blockId,
        Guid bayId,
        CreateYardRowRequest request,
        ICommandHandler<CreateYardRowCommand, Result<YardRowResponse>> handler,
        CancellationToken cancellationToken)
    {
        var command = new CreateYardRowCommand(blockId, bayId, request.RowNo, request.MaxTier);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? Results.CreatedAtRoute("GetYardRowById", new { blockId, bayId, id = result.Value.Id }, result.Value)
            : result.Error.ToProblem();
    }

    private static async Task<IResult> GetAll(
        Guid blockId,
        Guid bayId,
        IQueryHandler<GetYardRowsQuery, Result<IReadOnlyList<YardRowResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetYardRowsQuery(blockId, bayId), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetById(
        Guid blockId,
        Guid bayId,
        Guid id,
        IQueryHandler<GetYardRowByIdQuery, Result<YardRowResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetYardRowByIdQuery(blockId, bayId, id), cancellationToken);
        return result.ToHttpResult();
    }
}

/// <summary>Body of POST .../rows; the block and bay ids come from the route.</summary>
public sealed record CreateYardRowRequest(int RowNo, int MaxTier);

/// <summary>Body of PUT .../rows/{id}; the block, bay and row ids come from the route.</summary>
public sealed record UpdateYardRowRequest(int MaxTier);
