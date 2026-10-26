using Depot.Domain.Catalogs.Rules;
using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Catalogs;

public sealed class Customer : AuditableEntity
{
    public const int ShortTaxCodeLength = 10;
    public const int LongTaxCodeLength = 13;
    public const int NameMaxLength = 200;

    private Customer(string taxCode, string name)
    {
        TaxCode = taxCode;
        Name = name;
    }

    public string TaxCode { get; private set; }

    public string Name { get; private set; }

    public static Result<Customer> Create(string? taxCode, string? name)
    {
        var cleanTaxCode = taxCode?.Trim();
        var cleanName = name?.Trim();

        var check = BusinessRuleValidator.Check(
            new NotEmptyRule(cleanTaxCode, "TaxCode"),
            new TaxCodeRule(cleanTaxCode),
            new NotEmptyRule(cleanName, "Name"),
            new MaxLengthRule(cleanName, NameMaxLength, "Name"));

        if (check.IsFailure)
        {
            return Result.Failure<Customer>(check.Error);
        }

        return Result.Success(new Customer(cleanTaxCode!, cleanName!));
    }
}
