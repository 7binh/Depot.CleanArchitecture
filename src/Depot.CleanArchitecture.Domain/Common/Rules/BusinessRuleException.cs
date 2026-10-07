namespace Depot.CleanArchitecture.Domain.Common.Rules;

public class BusinessRuleException : Exception
{
    public IBusinessRule BrokenRule { get; }

    public BusinessRuleException(IBusinessRule brokenRule)
        : base($"[Vi phạm quy tắc {brokenRule.RuleCode}]: {brokenRule.Message}")
    {
        BrokenRule = brokenRule;
    }
}
