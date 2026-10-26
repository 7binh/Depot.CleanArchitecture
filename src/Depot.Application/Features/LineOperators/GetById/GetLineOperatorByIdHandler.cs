using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.LineOperators.GetById;

public sealed class GetLineOperatorByIdHandler(IAppDbContext dbContext)
    : IQueryHandler<GetLineOperatorByIdQuery, Result<LineOperatorResponse>>
{
    public async Task<Result<LineOperatorResponse>> HandleAsync(
        GetLineOperatorByIdQuery query,
        CancellationToken cancellationToken = default)
    {
        var lineOperator = await dbContext.LineOperators
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new LineOperatorResponse(x.Id, x.Code, x.Name))
            .FirstOrDefaultAsync(cancellationToken);

        return lineOperator is null
            ? Result.Failure<LineOperatorResponse>(
                Error.NotFound("LineOperator.NotFound", $"Line operator '{query.Id}' was not found."))
            : Result.Success(lineOperator);
    }
}
