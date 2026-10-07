namespace Depot.CleanArchitecture.Domain.Common.Rules;

public interface IBusinessRule
{
    string RuleCode { get; }
    string Message { get; }
    int Priority => 0;
    bool IsBroken();
}