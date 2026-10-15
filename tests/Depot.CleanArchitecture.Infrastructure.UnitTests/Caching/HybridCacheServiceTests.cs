namespace Depot.CleanArchitecture.Infrastructure.UnitTests.Caching;

using Depot.CleanArchitecture.Infrastructure.Caching;
using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class HybridCacheServiceTests
{
    private readonly HybridCacheService _cacheService;

    public HybridCacheServiceTests()
    {
        var services = new ServiceCollection();
        services.AddHybridCache();
        var sp = services.BuildServiceProvider();
        var hybridCache = sp.GetRequiredService<HybridCache>();
        _cacheService = new HybridCacheService(hybridCache);
    }

    [Fact]
    public async Task GetOrCreateAsync_Should_Call_Factory_On_Cache_Miss_And_Return_Cached_Value_On_Second_Call()
    {
        // Arrange
        var key = $"test-key-{Guid.NewGuid()}";
        var callCount = 0;

        Task<string> Factory(CancellationToken ct)
        {
            callCount++;
            return Task.FromResult($"Value-{callCount}");
        }

        // Act 1: First call (cache miss)
        var result1 = await _cacheService.GetOrCreateAsync(key, Factory, cancellationToken: TestContext.Current.CancellationToken);

        // Act 2: Second call (cache hit)
        var result2 = await _cacheService.GetOrCreateAsync(key, Factory, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        result1.Should().Be("Value-1");
        result2.Should().Be("Value-1");
        callCount.Should().Be(1, "Factory should only be invoked once due to caching");
    }

    [Fact]
    public async Task RemoveAsync_Should_Invalidate_Key()
    {
        // Arrange
        var key = $"test-remove-{Guid.NewGuid()}";
        var callCount = 0;

        Task<string> Factory(CancellationToken ct)
        {
            callCount++;
            return Task.FromResult($"Value-{callCount}");
        }

        // Act 1: Populate cache
        await _cacheService.GetOrCreateAsync(key, Factory, cancellationToken: TestContext.Current.CancellationToken);

        // Act 2: Remove key
        await _cacheService.RemoveAsync(key, TestContext.Current.CancellationToken);

        // Act 3: Query again
        var resultAfterRemove = await _cacheService.GetOrCreateAsync(key, Factory, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        resultAfterRemove.Should().Be("Value-2");
        callCount.Should().Be(2, "Factory should be called again after key removal");
    }
}
