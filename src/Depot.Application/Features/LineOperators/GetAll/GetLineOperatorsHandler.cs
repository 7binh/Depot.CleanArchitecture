using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.LineOperators.GetAll;

public sealed class GetLineOperatorsHandler(IAppDbContext dbContext)
    : IQueryHandler<GetLineOperatorsQuery, Result<IReadOnlyList<LineOperatorResponse>>>
{
    public async Task<Result<IReadOnlyList<LineOperatorResponse>>> HandleAsync(
        GetLineOperatorsQuery query,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<LineOperatorResponse> lineOperators = await dbContext.LineOperators
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .Select(x => new LineOperatorResponse(x.Id, x.Code, x.Name))
            .ToListAsync(cancellationToken);

        return Result.Success(lineOperators);
    }
}
