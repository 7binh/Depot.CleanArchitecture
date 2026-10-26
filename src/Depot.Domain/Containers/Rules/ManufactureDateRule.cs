using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Containers.Rules;

/// <summary>Our assumption: a container cannot be manufactured in the future.</summary>
public sealed class ManufactureDateRule(DateOnly manufactureDate, DateOnly today) : IBusinessRule
{
    public string RuleCode => "ManufactureDate.InFuture";
    public string Message => "ManufactureDate must not be in the future.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => manufactureDate > today;
}
