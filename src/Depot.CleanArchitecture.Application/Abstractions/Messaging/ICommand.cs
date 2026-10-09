namespace Depot.CleanArchitecture.Application.Abstractions.Messaging;

using Depot.CleanArchitecture.Domain.Common;

public interface ICommand : ICommand<Result>;

public interface ICommand<TResponse>;
