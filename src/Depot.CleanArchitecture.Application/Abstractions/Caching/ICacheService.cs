namespace Depot.CleanArchitecture.Application.Abstractions.Caching;

/// <summary>
/// Cache abstraction cho tầng Application.
/// Bao bọc HybridCache (.NET 10: L1 In-Memory + L2 Redis) để Handlers không bao giờ phụ thuộc trực tiếp vào package hạ tầng.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Lấy giá trị từ cache hoặc tạo mới qua factory function.
    /// L1 (RAM) -> L2 (Redis) -> Factory fallback.
    /// </summary>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>Ghi trực tiếp giá trị vào cache cả L1 và L2.</summary>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default);

    /// <summary>Xóa một cache key cụ thể khỏi mọi tầng cache.</summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Vô hiệu hóa (invalidate) toàn bộ cache có gắn tag tương ứng.</summary>
    Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default);
}
