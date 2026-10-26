namespace Depot.Application.Abstractions;

public interface ICurrentUser
{
    /// <summary>Id of the signed-in user, or null when there is none (anonymous call, seed, tool).</summary>
    string? UserId { get; }
}
