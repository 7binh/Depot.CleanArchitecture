using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Yard.Rules;

/// <summary>
/// BR-YD-09: a block that holds or ever held a container is kept, because every stay records its block.
/// Only a block set up by mistake, or never used, can be removed.
/// </summary>
public sealed class BlockNeverUsedRule(bool hasStays) : IBusinessRule
{
    public string RuleCode => "Block.InUse";
    public string Message => "The block holds or has held containers, so it cannot be removed.";
    public ErrorType Type => ErrorType.Conflict;

    public bool IsBroken() => hasStays;
}
