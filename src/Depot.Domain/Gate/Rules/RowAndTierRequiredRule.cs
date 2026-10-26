using Depot.Domain.Common;
using Depot.Domain.Common.Rules;
using Depot.Domain.Yard;

namespace Depot.Domain.Gate.Rules;

/// <summary>BR-GI-07: in a normal block the position is a row and a tier.</summary>
public sealed class RowAndTierRequiredRule(Block block, YardRow? row, int? tier) : IBusinessRule
{
    public string RuleCode => "Position.RowAndTierRequired";
    public string Message => "A row and a tier are required in a normal block.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => !block.IsVirtual && (row is null || tier is null);
}
