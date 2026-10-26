using Depot.Api.Extensions;
using Depot.Application.Abstractions.Messaging;
using Depot.Application.Features.LineOperators;
using Depot.Application.Features.LineOperators.Create;
using Depot.Application.Features.LineOperators.GetAll;
using Depot.Application.Features.LineOperators.GetById;
using Depot.Domain.Common;

namespace Depot.Api.Endpoints;

public static class LineOperatorEndpoints
{
    public static void MapLineOperatorEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/line-operators")
            .WithTags("Line operators");

        group.MapPost("/", Create)
            .WithName("CreateLineOperator")
            .WithSummary("Create a new line operator");

        group.MapGet("/", GetAll)
            .WithName("GetLineOperators")
            .WithSummary("List all line operators, ordered by code");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetLineOperatorById")
            .WithSummary("Get one line operator by id");
    }

    private static async Task<IResult> Create(
        CreateLineOperatorCommand command,
        ICommandHandler<CreateLineOperatorCommand, Result<LineOperatorResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? Results.CreatedAtRoute("GetLineOperatorById", new { id = result.Value.Id }, result.Value)
            : result.Error.ToProblem();
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetLineOperatorsQuery, Result<IReadOnlyList<LineOperatorResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetLineOperatorsQuery(), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetLineOperatorByIdQuery, Result<LineOperatorResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetLineOperatorByIdQuery(id), cancellationToken);
        return result.ToHttpResult();
    }
}
