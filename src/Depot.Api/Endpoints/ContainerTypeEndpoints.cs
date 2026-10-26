using Depot.Api.Extensions;
using Depot.Application.Abstractions.Messaging;
using Depot.Application.Features.ContainerTypes;
using Depot.Application.Features.ContainerTypes.Create;
using Depot.Application.Features.ContainerTypes.GetAll;
using Depot.Application.Features.ContainerTypes.GetById;
using Depot.Domain.Common;

namespace Depot.Api.Endpoints;

public static class ContainerTypeEndpoints
{
    public static void MapContainerTypeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/container-types")
            .WithTags("Container types");

        group.MapPost("/", Create)
            .WithName("CreateContainerType")
            .WithSummary("Create a new container type");

        group.MapGet("/", GetAll)
            .WithName("GetContainerTypes")
            .WithSummary("List all container types, ordered by code");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetContainerTypeById")
            .WithSummary("Get one container type by id");
    }

    private static async Task<IResult> Create(
        CreateContainerTypeCommand command,
        ICommandHandler<CreateContainerTypeCommand, Result<ContainerTypeResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? Results.CreatedAtRoute("GetContainerTypeById", new { id = result.Value.Id }, result.Value)
            : result.Error.ToProblem();
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetContainerTypesQuery, Result<IReadOnlyList<ContainerTypeResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetContainerTypesQuery(), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetContainerTypeByIdQuery, Result<ContainerTypeResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetContainerTypeByIdQuery(id), cancellationToken);
        return result.ToHttpResult();
    }
}
