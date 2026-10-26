using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Catalogs;
using Depot.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.ContainerTypes.Create;

public sealed class CreateContainerTypeHandler(IAppDbContext dbContext)
    : ICommandHandler<CreateContainerTypeCommand, Result<ContainerTypeResponse>>
{
    public async Task<Result<ContainerTypeResponse>> HandleAsync(
        CreateContainerTypeCommand command,
        CancellationToken cancellationToken = default)
    {
        var created = ContainerType.Create(command.Code, command.Name);
        if (created.IsFailure)
        {
            return Result.Failure<ContainerTypeResponse>(created.Error);
        }

        var containerType = created.Value;

        if (await CodeExistsAsync(containerType.Code, cancellationToken))
        {
            return CodeExistsFailure(containerType.Code);
        }

        dbContext.ContainerTypes.Add(containerType);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (!await CodeExistsAsync(containerType.Code, cancellationToken))
            {
                throw;
            }

            return CodeExistsFailure(containerType.Code);
        }

        return Result.Success(new ContainerTypeResponse(containerType.Id, containerType.Code, containerType.Name));
    }

    private Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken)
    {
        return dbContext.ContainerTypes.AnyAsync(x => x.Code == code, cancellationToken);
    }

    private static Result<ContainerTypeResponse> CodeExistsFailure(string code)
    {
        return Result.Failure<ContainerTypeResponse>(
            Error.Conflict("ContainerType.CodeExists", $"Container type code '{code}' already exists."));
    }
}
