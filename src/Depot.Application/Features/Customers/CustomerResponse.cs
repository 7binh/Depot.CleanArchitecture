namespace Depot.Application.Features.Customers;

public sealed record CustomerResponse(Guid Id, string TaxCode, string Name);
