namespace Depot.CleanArchitecture.Infrastructure.UnitTests.DependencyInjection;

using Depot.CleanArchitecture.Application.Abstractions.Caching;
using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Identity;
using Depot.CleanArchitecture.Application.Abstractions.Tenancy;
using Depot.CleanArchitecture.Infrastructure;
using Depot.CleanArchitecture.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_Should_Register_All_Required_Services_Successfully()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        services.AddInfrastructure(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        serviceProvider.GetService<IAppDbContext>().Should().NotBeNull();
        serviceProvider.GetService<AppDbContext>().Should().NotBeNull();
        serviceProvider.GetService<ITenantProvider>().Should().NotBeNull();
        serviceProvider.GetService<ICacheService>().Should().NotBeNull();
        serviceProvider.GetService<ICurrentUser>().Should().NotBeNull();
    }
}
