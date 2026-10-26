namespace Depot.Application.Abstractions.Messaging;

/// <summary>Handles one read-only query of type <typeparamref name="TQuery"/> and answers with <typeparamref name="TResponse"/>.</summary>
public interface IQueryHandler<in TQuery, TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
