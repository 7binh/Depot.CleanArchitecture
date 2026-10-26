using Depot.Application.Features.ContainerTypes.Create;
using Depot.Application.Features.Containers.Create;
using Depot.Application.Features.LineOperators.Create;
using Depot.Infrastructure.Persistence;

namespace Depot.Application.Tests;

internal static class ContainerTestData
{
    public const string ValidNumber = "CSQU3054383";

    public static readonly DateOnly Today = new(2026, 10, 26);

    public static TimeProvider Clock { get; } = new FixedTimeProvider(Today);

    public static async Task<Guid> CreateOperatorAsync(
        AppDbContext dbContext,
        string code,
        CancellationToken cancellationToken)
    {
        var lineOperator = await new CreateLineOperatorHandler(dbContext)
            .HandleAsync(new CreateLineOperatorCommand(code, $"Operator {code}"), cancellationToken);
        return lineOperator.Value.Id;
    }

    public static async Task<Guid> CreateTypeAsync(
        AppDbContext dbContext,
        string code,
        CancellationToken cancellationToken)
    {
        var type = await new CreateContainerTypeHandler(dbContext)
            .HandleAsync(new CreateContainerTypeCommand(code, $"Type {code}"), cancellationToken);
        return type.Value.Id;
    }

    public static CreateContainerCommand Command(
        Guid containerTypeId,
        string? number = ValidNumber,
        DateOnly? manufactureDate = null)
    {
        return new CreateContainerCommand(
            number,
            containerTypeId,
            "22G1",
            20,
            30480,
            2200,
            manufactureDate ?? new DateOnly(2020, 5, 1));
    }

    public static CreateContainerHandler CreateHandler(AppDbContext dbContext)
    {
        return new CreateContainerHandler(dbContext, Clock);
    }

    private sealed class FixedTimeProvider(DateOnly today) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow()
        {
            return new DateTimeOffset(today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);
        }
    }
}
