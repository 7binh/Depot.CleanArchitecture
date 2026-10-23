namespace Depot.Domain.Common.Rules;

public static class BusinessRuleValidator
{
    /// <summary>Evaluates the rules in the given order and returns the first broken one as a failure.</summary>
    public static Result Check(params IBusinessRule[] rules)
    {
        var broken = rules.FirstOrDefault(rule => rule.IsBroken());
        return broken is null ? Result.Success() : Result.Failure(ToError(broken));
    }

    private static Error ToError(IBusinessRule rule) => new(rule.RuleCode, rule.Message, rule.Type);
}
