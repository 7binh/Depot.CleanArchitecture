using Depot.Api.Extensions;
using Depot.Application.Abstractions.Messaging;
using Depot.Application.Features.Containers;
using Depot.Application.Features.Containers.Create;
using Depot.Application.Features.Containers.GetAll;
using Depot.Application.Features.Containers.GetById;
using Depot.Application.Features.Containers.Search;
using Depot.Domain.Common;

namespace Depot.Api.Endpoints;

public static class ContainerEndpoints
{
    public static void MapContainerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/containers")
            .WithTags("Containers");

        group.MapPost("/", Create)
            .WithName("CreateContainer")
            .WithSummary("Register a new container; the line operator is found from the number's owner code");

        group.MapGet("/", GetAll)
            .WithName("GetContainers")
            .WithSummary("List all containers, ordered by number");

        group.MapGet("/search", Search)
            .WithName("SearchContainers")
            .WithSummary("List containers one page at a time, filtered by number, operator, type, size or ISO code");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetContainerById")
            .WithSummary("Get one container by id");
    }

    private static async Task<IResult> Create(
        CreateContainerCommand command,
        ICommandHandler<CreateContainerCommand, Result<ContainerResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? Results.CreatedAtRoute("GetContainerById", new { id = result.Value.Id }, result.Value)
            : result.Error.ToProblem();
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetContainersQuery, Result<IReadOnlyList<ContainerResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetContainersQuery(), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> Search(
        [AsParameters] SearchContainersQuery query,
        IQueryHandler<SearchContainersQuery, Result<PagedResult<ContainerListItem>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetContainerByIdQuery, Result<ContainerResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetContainerByIdQuery(id), cancellationToken);
        return result.ToHttpResult();
    }
}
