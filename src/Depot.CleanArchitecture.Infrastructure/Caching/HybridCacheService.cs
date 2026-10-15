namespace Depot.CleanArchitecture.Infrastructure.Caching;

using Microsoft.Extensions.Caching.Hybrid;
using Depot.CleanArchitecture.Application.Abstractions.Caching;

/// <summary>
/// Triển khai ICacheService bao bọc Microsoft HybridCache (.NET 10).
/// L1 = In-Memory (RAM), L2 = Redis (Distributed Cache).
/// Cơ chế fallthrough: RAM -> Redis -> Factory Delegate (chống Cache Stampede).
/// </summary>
public sealed class HybridCacheService(HybridCache hybridCache) : ICacheService
{
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var options = BuildOptions(expiration, localExpiration);

        return await hybridCache.GetOrCreateAsync(
            key,
            factory,
            static (state, ct) => new ValueTask<T>(state(ct)),
            options,
            tags: tags?.ToArray(),
            cancellationToken: cancellationToken);
    }

    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default)
    {
        var options = BuildOptions(expiration, localExpiration);

        await hybridCache.SetAsync(
            key,
            value,
            options,
            tags: tags?.ToArray(),
            cancellationToken: cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await hybridCache.RemoveAsync(key, cancellationToken);
    }

    public async Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        await hybridCache.RemoveByTagAsync(tag, cancellationToken);
    }

    private static HybridCacheEntryOptions? BuildOptions(TimeSpan? expiration, TimeSpan? localExpiration)
    {
        if (expiration is null && localExpiration is null) return null;

        return new HybridCacheEntryOptions
        {
            Expiration = expiration,
            LocalCacheExpiration = localExpiration
        };
    }
}
