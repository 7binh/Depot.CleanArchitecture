namespace Depot.CleanArchitecture.Domain.Common;

public sealed record Error(string Code, string Name, ErrorType Type, string? Description = null)
{
    
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
    public static Error Failure(string code, string message) => new(code, message, ErrorType.Failure);
}

//tạo enum ErrorType
public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict,
}

