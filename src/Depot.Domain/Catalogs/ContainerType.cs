using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Catalogs;

public sealed class ContainerType : AuditableEntity
{
    public const int CodeMinLength = 2;
    public const int CodeMaxLength = 15;
    public const int NameMaxLength = 100;

    private ContainerType(string code, string name)
    {
        Code = code;
        Name = name;
    }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public static Result<ContainerType> Create(string? code, string? name)
    {
        var cleanCode = code?.Trim().ToUpperInvariant();
        var cleanName = name?.Trim();

        var check = BusinessRuleValidator.Check(
            new NotEmptyRule(cleanCode, "Code"),
            new MinLengthRule(cleanCode, CodeMinLength, "Code"),
            new MaxLengthRule(cleanCode, CodeMaxLength, "Code"),
            new NotEmptyRule(cleanName, "Name"),
            new MaxLengthRule(cleanName, NameMaxLength, "Name"));

        if (check.IsFailure)
        {
            return Result.Failure<ContainerType>(check.Error);
        }

        return Result.Success(new ContainerType(cleanCode!, cleanName!));
    }
}
