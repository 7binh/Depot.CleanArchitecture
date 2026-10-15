namespace Depot.CleanArchitecture.Infrastructure.UnitTests.Persistence;

using Depot.CleanArchitecture.Application.Abstractions.Tenancy;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.ValueObjects;
using Depot.CleanArchitecture.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class MultiTenancyGlobalQueryFilterTests
{
    private sealed class MockTenantProvider(Guid tenantId) : ITenantProvider
    {
        public Guid CurrentTenantId => tenantId;
        public string? TenantId => tenantId.ToString();
        public TenantInfo? CurrentTenant => new TenantInfo
        {
            Id = tenantId.ToString(),
            Name = $"Test Tenant {tenantId}"
        };
    }

    [Fact]
    public async Task QueryFilter_Should_Only_Return_Entities_Of_Current_Tenant()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        // Seed data for both tenants bypassing query filter
        using (var seedContext = new AppDbContext(options, new MockTenantProvider(tenantA)))
        {
            var blockA = YardBlock.CreatePhysical(tenantA, "A", "Block A Tenant A", 10, 6, 5);
            var blockB = YardBlock.CreatePhysical(tenantB, "B", "Block B Tenant B", 10, 6, 5);

            seedContext.YardBlocks.AddRange(blockA, blockB);
            await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Act - Query as Tenant A
        using (var contextA = new AppDbContext(options, new MockTenantProvider(tenantA)))
        {
            var blocks = await contextA.YardBlocks.ToListAsync(TestContext.Current.CancellationToken);

            // Assert
            blocks.Should().HaveCount(1);
            blocks.Single().BlockCode.Should().Be("A");
            blocks.Single().TenantId.Should().Be(tenantA);
        }

        // Act - Query as Tenant B
        using (var contextB = new AppDbContext(options, new MockTenantProvider(tenantB)))
        {
            var blocks = await contextB.YardBlocks.ToListAsync(TestContext.Current.CancellationToken);

            // Assert
            blocks.Should().HaveCount(1);
            blocks.Single().BlockCode.Should().Be("B");
            blocks.Single().TenantId.Should().Be(tenantB);
        }
    }

    [Fact]
    public async Task SaveChangesAsync_Should_Auto_Assign_Current_TenantId_And_CreatedAt()
    {
        // Arrange
        var tenantA = Guid.NewGuid();
        var dbName = Guid.NewGuid().ToString();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        using var context = new AppDbContext(options, new MockTenantProvider(tenantA));

        // Create block with Empty TenantId
        var block = YardBlock.CreatePhysical(Guid.Empty, "C", "Block C", 5, 5, 4);
        context.YardBlocks.Add(block);

        // Act
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        block.TenantId.Should().Be(tenantA);
        block.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
