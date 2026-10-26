using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.ContainerStays.GetAll;

public sealed class GetContainerStaysHandler(IAppDbContext dbContext)
    : IQueryHandler<GetContainerStaysQuery, Result<IReadOnlyList<ContainerStayResponse>>>
{
    public async Task<Result<IReadOnlyList<ContainerStayResponse>>> HandleAsync(
        GetContainerStaysQuery query,
        CancellationToken cancellationToken = default)
    {
        var source = dbContext.ContainerStays.AsNoTracking();
        if (query.InYard)
        {
            source = source.Where(x => x.GateOutAt == null);
        }

        var stays = await source
            .OrderByDescending(x => x.GateInAt)
            .ToListAsync(cancellationToken);

        IReadOnlyList<ContainerStayResponse> responses = stays.Select(ContainerStayResponse.From).ToList();
        return Result.Success(responses);
    }
}
