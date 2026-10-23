namespace Depot.Domain.Common.Rules;

/// <summary>A business or validation rule. Each rule is one small class that knows only itself.</summary>
public interface IBusinessRule
{
    /// <summary>Stable identifier of the rule, e.g. "LineOperator.Code.Empty".</summary>
    string RuleCode { get; }

    /// <summary>Human-readable message shown when the rule is broken.</summary>
    string Message { get; }

    /// <summary>Kind of failure when the rule is broken; decides the HTTP status.</summary>
    ErrorType Type { get; }

    bool IsBroken();
}
