using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Yard.Rules;

/// <summary>A row number is a positive integer (A-20: upper bound is our assumption).</summary>
public sealed class RowNoRule(int rowNo) : IBusinessRule
{
    public string RuleCode => "RowNo.Invalid";
    public string Message => $"RowNo must be between 1 and {YardRow.MaxRowNo}.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => rowNo is < 1 or > YardRow.MaxRowNo;
}
