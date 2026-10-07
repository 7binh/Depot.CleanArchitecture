namespace Depot.CleanArchitecture.Domain.Common;

//Sealed để không cho phép kế thừa
//Record để tạo immutable object
//Record dùng để tự động tạo các method như Equals, GetHashCode, ToString, và properties
public sealed record Error(string Code, string Name, ErrorType Type, string? Description = null)
{
    
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
}

//tạo enum ErrorType
public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict,
}

