namespace Depot.CleanArchitecture.Infrastructure.UnitTests.Persistence;

using Depot.CleanArchitecture.Application.Abstractions.Tenancy;
using Depot.CleanArchitecture.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class AppDbSeederTests
{
    private sealed class CatLaiTenantProvider : ITenantProvider
    {
        public Guid CurrentTenantId => TenantInfo.DefaultDepotCatLaiId;
        public string? TenantId => TenantInfo.DefaultDepotCatLaiId.ToString();
        public TenantInfo? CurrentTenant => TenantInfo.Default;
    }

    [Fact]
    public async Task SeedAsync_Should_Populate_CatLai_Comprehensive_Depot_Data_When_Empty()
    {
        // Arrange
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var tenantProvider = new CatLaiTenantProvider();
        using var context = new AppDbContext(options, tenantProvider);

        // Act 1: Initial seed
        await AppDbSeeder.SeedAsync(context, force: false, TestContext.Current.CancellationToken);

        // Assert
        var blocks = await context.YardBlocks.Include(b => b.Slots).ToListAsync(TestContext.Current.CancellationToken);
        var containers = await context.Containers.ToListAsync(TestContext.Current.CancellationToken);
        var orders = await context.DeliveryOrders.Include(o => o.Items).ToListAsync(TestContext.Current.CancellationToken);
        var visits = await context.ContainerVisits.Include(v => v.Holds).Include(v => v.Surveys).ToListAsync(TestContext.Current.CancellationToken);
        var slots = await context.YardSlots.ToListAsync(TestContext.Current.CancellationToken);
        var movements = await context.MovementEvents.ToListAsync(TestContext.Current.CancellationToken);

        // Blocks: A (984), B (738), C (738), MR (virtual), WASH (virtual)
        blocks.Should().HaveCount(5);
        blocks.Should().Contain(b => b.BlockCode == "A" && b.Slots.Count == 984);
        blocks.Should().Contain(b => b.BlockCode == "B" && b.Slots.Count == 738 && b.HasReeferPower);
        blocks.Should().Contain(b => b.BlockCode == "C" && b.Slots.Count == 738);
        blocks.Should().Contain(b => b.BlockCode == "MR" && b.IsVirtual);
        blocks.Should().Contain(b => b.BlockCode == "WASH" && b.IsVirtual);

        // Containers & Visits
        containers.Should().HaveCount(19);
        visits.Should().HaveCount(19);
        slots.Count(s => s.IsOccupied).Should().Be(15); // 8 in A + 4 in B + 3 in C = 15 occupied

        // Orders
        orders.Should().HaveCount(3);

        // Holds & Surveys
        var holds = await context.StayHolds.ToListAsync(TestContext.Current.CancellationToken);
        holds.Should().NotBeEmpty();
        holds.Should().Contain(h => h.HoldCode == "DAMAGE_HOLD");
        holds.Should().Contain(h => h.HoldCode == "CUSTOMS_HOLD");

        // Movements
        movements.Should().NotBeEmpty();

        // Act 2: Seed again (idempotent, force = false)
        await AppDbSeeder.SeedAsync(context, force: false, TestContext.Current.CancellationToken);

        // Assert idempotency
        (await context.YardBlocks.CountAsync(TestContext.Current.CancellationToken)).Should().Be(5);
        (await context.Containers.CountAsync(TestContext.Current.CancellationToken)).Should().Be(19);
        (await context.ContainerVisits.CountAsync(TestContext.Current.CancellationToken)).Should().Be(19);

        // Act 3: Force re-seed (force = true)
        await AppDbSeeder.SeedAsync(context, force: true, TestContext.Current.CancellationToken);

        // Assert force re-seed integrity
        (await context.YardBlocks.CountAsync(TestContext.Current.CancellationToken)).Should().Be(5);
        (await context.Containers.CountAsync(TestContext.Current.CancellationToken)).Should().Be(19);
        (await context.ContainerVisits.CountAsync(TestContext.Current.CancellationToken)).Should().Be(19);
    }
}
