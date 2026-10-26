namespace Depot.Application.Features.Customers.Create;

/// <summary>Raw input for creating a customer; the Domain trims and validates it.</summary>
public sealed record CreateCustomerCommand(string? TaxCode, string? Name);
