using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Yard.Rules;

/// <summary>
/// BR-YD-09: a row cannot be removed while a container in the yard stands in it, or on its floor space
/// (a 40ft container in the even bay beside an odd bay's row).
/// </summary>
public sealed class RowIsEmptyRule(bool containerStands) : IBusinessRule
{
    public string RuleCode => "Row.HasContainers";
    public string Message => "A container still stands in this row; move it out before removing the row.";
    public ErrorType Type => ErrorType.Conflict;

    public bool IsBroken() => containerStands;
}
