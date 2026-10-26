using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Catalogs;

public sealed class LineOperator : AuditableEntity
{
    public const int CodeLength = 3;
    public const int NameMaxLength = 200;

    private LineOperator(string code, string name)
    {
        Code = code;
        Name = name;
    }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public static Result<LineOperator> Create(string? code, string? name)
    {
        var cleanCode = code?.Trim().ToUpperInvariant();
        var cleanName = name?.Trim();

        var check = BusinessRuleValidator.Check(
            new NotEmptyRule(cleanCode, "Code"),
            new ExactLengthRule(cleanCode, CodeLength, "Code"),
            new NotEmptyRule(cleanName, "Name"),
            new MaxLengthRule(cleanName, NameMaxLength, "Name"));

        if (check.IsFailure)
        {
            return Result.Failure<LineOperator>(check.Error);
        }

        return Result.Success(new LineOperator(cleanCode!, cleanName!));
    }
}
