namespace Depot.CleanArchitecture.Application.Features.Gate.GateIn;

using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

public sealed class GateInCommandHandler(IAppDbContext dbContext) 
    : ICommandHandler<GateInCommand, Result<GateInResponse>>
{
    public async Task<Result<GateInResponse>> HandleAsync(
        GateInCommand command, 
        CancellationToken cancellationToken = default)
    {
        var containerNumberResult = ContainerNumber.Create(command.ContainerNumber);
        if (containerNumberResult.IsFailure)
        {
            return Result.Failure<GateInResponse>(containerNumberResult.Error);
        }

        var containerNumber = containerNumberResult.Value!;

        // Tìm container qua primitive Value
        var container = await dbContext.Containers
            .FirstOrDefaultAsync(c => c.Number.Value == containerNumber.Value, cancellationToken);

        if (container is null)
        {
            decimal tare = command.Size == 20 ? 2200m : 3800m;
            decimal maxGross = command.Size == 20 ? 30480m : 32500m;
            string isoCode = command.Size == 20 ? "22G1" : "42G1";

            var createContainerResult = Container.Create(
                command.TenantId,
                containerNumber,
                command.LineOperator,
                command.Type,
                isoCode,
                command.Size,
                tare,
                maxGross,
                command.Grade);

            if (createContainerResult.IsFailure)
            {
                return Result.Failure<GateInResponse>(createContainerResult.Error);
            }

            container = createContainerResult.Value!;
            dbContext.Containers.Add(container);
        }

        // Kiểm tra xem container có đang ở bãi không (active visit)
        var activeVisit = await dbContext.ContainerVisits
            .FirstOrDefaultAsync(v => v.ContainerId == container.Id &&
                                      v.Status != ContainerVisitStatus.GatedOut &&
                                      v.Status != ContainerVisitStatus.Completed, cancellationToken);

        if (activeVisit is not null)
        {
            return Result.Failure<GateInResponse>(
                Error.Conflict("Visit.AlreadyActive", $"Container '{container.Number.Value}' hiện đang có lượt vào bãi chưa xuất!"));
        }

        var vehicleResult = VehicleInfo.Create(
            command.TractorNo,
            command.TrailerNo,
            command.DriverName,
            command.DriverPhone);

        if (vehicleResult.IsFailure)
        {
            return Result.Failure<GateInResponse>(vehicleResult.Error);
        }

        var visit = ContainerVisit.CreateGateIn(
            command.TenantId,
            container.Id,
            command.LineOperator,
            vehicleResult.Value!,
            command.Grade,
            command.DamageNotes);

        dbContext.ContainerVisits.Add(visit);
        await dbContext.SaveChangesAsync(cancellationToken);

        var response = new GateInResponse(
            visit.Id,
            container.Id,
            container.Number.Value,
            visit.Status.ToString(),
            visit.GateInDate);

        return Result.Success(response);
    }
}
