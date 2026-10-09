namespace Depot.CleanArchitecture.Application.Abstractions.Rules;

/// <summary>
/// Application-layer rule engine abstraction.
/// Evaluates configurable business rules against a context object.
/// </summary>
public interface IRuleEngine
{
    /// <summary>
    /// Evaluates all rules in the specified rule set against the given context.
    /// </summary>
    RuleResult Evaluate(string ruleSetName, IDictionary<string, object?> context);

    /// <summary>
    /// Evaluates a single inline rule expression against the given context.
    /// </summary>
    bool EvaluateExpression(string expression, IDictionary<string, object?> context);

    /// <summary>
    /// Gets all available rule set names.
    /// </summary>
    IReadOnlyList<string> GetRuleSetNames();
}
