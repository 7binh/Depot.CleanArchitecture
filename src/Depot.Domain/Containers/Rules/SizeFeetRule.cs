using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Containers.Rules;

/// <summary>BR-CT-06 (A-16): a container is 20 or 40 feet.</summary>
public sealed class SizeFeetRule(int sizeFeet) : IBusinessRule
{
    public string RuleCode => "SizeFeet.Invalid";
    public string Message => $"SizeFeet must be {Container.SizeSmallFeet} or {Container.SizeLargeFeet}.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => sizeFeet is not (Container.SizeSmallFeet or Container.SizeLargeFeet);
}
