using Depot.Application.Abstractions.Data;
using Depot.Application.Abstractions.Messaging;
using Depot.Domain.Catalogs;
using Depot.Domain.Common;
using Depot.Domain.Containers;
using Microsoft.EntityFrameworkCore;

namespace Depot.Application.Features.Containers.Create;

public sealed class CreateContainerHandler(IAppDbContext dbContext, TimeProvider timeProvider)
    : ICommandHandler<CreateContainerCommand, Result<ContainerResponse>>
{
    public async Task<Result<ContainerResponse>> HandleAsync(
        CreateContainerCommand command,
        CancellationToken cancellationToken = default)
    {
        var ownerCode = OwnerCodeOf(command.Number);
        if (ownerCode is null)
        {
            return Result.Failure<ContainerResponse>(ContainerErrors.OwnerCodeMissing());
        }

        var lineOperator = await dbContext.LineOperators
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == ownerCode, cancellationToken);
        if (lineOperator is null)
        {
            return Result.Failure<ContainerResponse>(ContainerErrors.OperatorNotFound(ownerCode));
        }

        var typeExists = await dbContext.ContainerTypes
            .AnyAsync(x => x.Id == command.ContainerTypeId, cancellationToken);
        if (!typeExists)
        {
            return Result.Failure<ContainerResponse>(ContainerErrors.ContainerTypeNotFound(command.ContainerTypeId));
        }

        var created = CreateContainer(command, lineOperator);
        if (created.IsFailure)
        {
            return Result.Failure<ContainerResponse>(created.Error);
        }

        var container = created.Value;

        if (await NumberExistsAsync(container.Number, cancellationToken))
        {
            return Result.Failure<ContainerResponse>(ContainerErrors.NumberExists(container.Number));
        }

        dbContext.Containers.Add(container);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            if (!await NumberExistsAsync(container.Number, cancellationToken))
            {
                throw;
            }

            return Result.Failure<ContainerResponse>(ContainerErrors.NumberExists(container.Number));
        }

        return Result.Success(ToResponse(container));
    }

    // Same normalisation as the Domain (trim, uppercase); only the prefix is read, the Domain still validates it.
    private static string? OwnerCodeOf(string? number)
    {
        var clean = number?.Trim().ToUpperInvariant();
        return clean is { Length: >= LineOperator.CodeLength } ? clean[..LineOperator.CodeLength] : null;
    }

    private Result<Container> CreateContainer(CreateContainerCommand command, LineOperator lineOperator)
    {
        var spec = new ContainerSpec(
            command.IsoCode,
            command.SizeFeet,
            command.MaxWeightKg,
            command.TareWeightKg,
            command.ManufactureDate);
        var today = DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime);

        return Container.Create(command.Number, lineOperator, command.ContainerTypeId, spec, today);
    }

    private Task<bool> NumberExistsAsync(string number, CancellationToken cancellationToken)
    {
        return dbContext.Containers.AnyAsync(x => x.Number == number, cancellationToken);
    }

    private static ContainerResponse ToResponse(Container container)
    {
        return new ContainerResponse(
            container.Id,
            container.Number,
            container.LineOperatorId,
            container.ContainerTypeId,
            container.IsoCode,
            container.SizeFeet,
            container.MaxWeightKg,
            container.TareWeightKg,
            container.ManufactureDate);
    }
}
