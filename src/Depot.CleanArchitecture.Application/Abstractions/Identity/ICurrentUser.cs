namespace Depot.CleanArchitecture.Application.Abstractions.Identity;

/// <summary>
/// Cung cấp thông tin danh tính của người dùng đang thực hiện request hiện tại.
/// </summary>
public interface ICurrentUser
{
    string? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
}
