using Depot.Api.Extensions;
using Depot.Application.Abstractions.Messaging;
using Depot.Application.Features.Customers;
using Depot.Application.Features.Customers.Create;
using Depot.Application.Features.Customers.GetAll;
using Depot.Application.Features.Customers.GetById;
using Depot.Domain.Common;

namespace Depot.Api.Endpoints;

public static class CustomerEndpoints
{
    public static void MapCustomerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/customers")
            .WithTags("Customers");

        group.MapPost("/", Create)
            .WithName("CreateCustomer")
            .WithSummary("Create a new customer");

        group.MapGet("/", GetAll)
            .WithName("GetCustomers")
            .WithSummary("List all customers, ordered by tax code");

        group.MapGet("/{id:guid}", GetById)
            .WithName("GetCustomerById")
            .WithSummary("Get one customer by id");
    }

    private static async Task<IResult> Create(
        CreateCustomerCommand command,
        ICommandHandler<CreateCustomerCommand, Result<CustomerResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsSuccess
            ? Results.CreatedAtRoute("GetCustomerById", new { id = result.Value.Id }, result.Value)
            : result.Error.ToProblem();
    }

    private static async Task<IResult> GetAll(
        IQueryHandler<GetCustomersQuery, Result<IReadOnlyList<CustomerResponse>>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCustomersQuery(), cancellationToken);
        return result.ToHttpResult();
    }

    private static async Task<IResult> GetById(
        Guid id,
        IQueryHandler<GetCustomerByIdQuery, Result<CustomerResponse>> handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new GetCustomerByIdQuery(id), cancellationToken);
        return result.ToHttpResult();
    }
}
