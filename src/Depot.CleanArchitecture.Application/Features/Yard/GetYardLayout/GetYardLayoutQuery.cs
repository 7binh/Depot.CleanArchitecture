namespace Depot.CleanArchitecture.Application.Features.Yard.GetYardLayout;

using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;

public sealed record GetYardLayoutQuery(string? BlockCode = null)
    : IQuery<Result<YardLayoutResponse>>;
