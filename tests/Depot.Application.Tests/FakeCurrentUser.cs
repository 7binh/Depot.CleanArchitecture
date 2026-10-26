using Depot.Application.Abstractions;

namespace Depot.Application.Tests;

internal sealed class FakeCurrentUser(string? userId) : ICurrentUser
{
    public string? UserId { get; } = userId;
}
