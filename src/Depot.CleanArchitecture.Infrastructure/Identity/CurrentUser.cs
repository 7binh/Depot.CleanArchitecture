namespace Depot.CleanArchitecture.Infrastructure.Identity;

using System.Security.Claims;
using Depot.CleanArchitecture.Application.Abstractions.Identity;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Cung cấp thông tin danh tính của người dùng hiện tại lấy từ HTTP Context Claims.
/// </summary>
public sealed class CurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string? UserId => User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                             ?? User?.FindFirst("sub")?.Value;

    public string? Email => User?.FindFirst(ClaimTypes.Email)?.Value;

    public string? Role => User?.FindFirst(ClaimTypes.Role)?.Value;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;
}
