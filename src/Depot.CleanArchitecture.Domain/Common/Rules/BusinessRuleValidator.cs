namespace Depot.CleanArchitecture.Domain.Common.Rules;

public static class BusinessRuleValidator
{
    public static void CheckRule(IBusinessRule rule)
    {
        if (rule.IsBroken())
        {
            throw new BusinessRuleException(rule);
        }
    }

    public static void CheckRules(params IBusinessRule[] rules)
    {
        foreach (var rule in rules.OrderBy(r => r.Priority))
        {
            CheckRule(rule);
        }
    }
}
