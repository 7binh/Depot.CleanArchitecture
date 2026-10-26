namespace Depot.Domain.Common;

/// <summary>The kind of failure; the Api layer maps each kind to an HTTP status code.</summary>
public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict,
    Unauthorized
}
