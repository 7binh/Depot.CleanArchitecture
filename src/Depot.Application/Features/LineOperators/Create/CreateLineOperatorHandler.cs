using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Catalogs;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.LineOperators.Create;

public sealed class CreateLineOperatorHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateLineOperatorCommand, Result<LineOperatorResponse>>
{
    public async Task<Result<LineOperatorResponse>> HandleAsync(
        CreateLineOperatorCommand command,
        CancellationToken cancellationToken = default)
    {
        var created = LineOperator.Create(command.Code, command.Name);
        if (created.IsFailure)
        {
            return Result.Failure<LineOperatorResponse>(created.Error);
        }

        var lineOperator = created.Value;

        if (await CodeExistsAsync(lineOperator.Code, cancellationToken))
        {
            return CodeExistsFailure(lineOperator.Code);
        }

        dbContext.LineOperators.Add(lineOperator);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (!await CodeExistsAsync(lineOperator.Code, cancellationToken))
            {
                throw;
            }

            return CodeExistsFailure(lineOperator.Code);
        }

        return Result.Success(new LineOperatorResponse(lineOperator.Id, lineOperator.Code, lineOperator.Name));
    }

    private Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken)
    {
        return dbContext.LineOperators.AnyAsync(x => x.Code == code, cancellationToken);
    }

    private static Result<LineOperatorResponse> CodeExistsFailure(string code)
    {
        return Result.Failure<LineOperatorResponse>(
            Error.Conflict("LineOperator.CodeExists", $"Line operator code '{code}' already exists."));
    }
}
