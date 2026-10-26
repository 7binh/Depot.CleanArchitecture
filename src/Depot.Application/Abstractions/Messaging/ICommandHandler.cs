namespace Depot.Application.Abstractions.Messaging;

/// <summary>Handles one command of type <typeparamref name="TCommand"/> and answers with <typeparamref name="TResponse"/>.</summary>
public interface ICommandHandler<in TCommand, TResponse>{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}
