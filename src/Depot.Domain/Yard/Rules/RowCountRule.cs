using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Yard.Rules;

/// <summary>Rows are numbered from 1, so a bay gets between 1 and the highest row number of them.</summary>
public sealed class RowCountRule(int rowCount) : IBusinessRule
{
    public string RuleCode => "RowCount.Invalid";
    public string Message => $"RowCount must be between 1 and {YardRow.MaxRowNo}.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => rowCount is < 1 or > YardRow.MaxRowNo;
}
