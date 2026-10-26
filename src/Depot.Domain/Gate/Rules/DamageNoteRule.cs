using Depot.Domain.Common;
using Depot.Domain.Common.Rules;

namespace Depot.Domain.Gate.Rules;

/// <summary>BR-GI-06: a damaged container needs a description of the damage.</summary>
public sealed class DamageNoteRule(StayCondition condition, string? damageNote) : IBusinessRule
{
    public string RuleCode => "DamageNote.Required";
    public string Message => "DamageNote is required when the container is damaged.";
    public ErrorType Type => ErrorType.Validation;

    public bool IsBroken() => condition == StayCondition.Damaged && string.IsNullOrWhiteSpace(damageNote);
}
