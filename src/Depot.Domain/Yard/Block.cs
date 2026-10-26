using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Yard;

public sealed class Block : AuditableEntity
{
    public const int CodeMaxLength = 10;
    public const int NameMaxLength = 100;

    private Block(string code, string name, bool isVirtual)
    {
        Code = code;
        Name = name;
        IsVirtual = isVirtual;
    }

    public string Code { get; private set; }

    public string Name { get; private set; }

    /// <summary>Block ảo không quản lý Bay, Row, Tier (BR-YD-10).</summary>
    public bool IsVirtual { get; private set; }

    public static Result<Block> Create(string? code, string? name, bool isVirtual)
    {
        var cleanCode = code?.Trim().ToUpperInvariant();
        var cleanName = name?.Trim();

        var check = BusinessRuleValidator.Check(
            new NotEmptyRule(cleanCode, "Code"),
            new MaxLengthRule(cleanCode, CodeMaxLength, "Code"),
            new NotEmptyRule(cleanName, "Name"),
            new MaxLengthRule(cleanName, NameMaxLength, "Name"));

        if (check.IsFailure)
        {
            return Result.Failure<Block>(check.Error);
        }

        return Result.Success(new Block(cleanCode!, cleanName!, isVirtual));
    }
}
