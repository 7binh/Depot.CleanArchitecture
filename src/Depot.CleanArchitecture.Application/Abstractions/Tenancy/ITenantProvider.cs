namespace Depot.CleanArchitecture.Application.Abstractions.Tenancy;

/// <summary>
/// Cung cấp ngữ cảnh Tenant (Depot/ICD) cho request hiện tại.
/// Được resolve từ HTTP Header (X-Tenant-Id), JWT claims hoặc cấu hình mặc định.
/// </summary>
public interface ITenantProvider
{
    /// <summary>ID của Tenant hiện tại (ví dụ: Depot Cát Lái, Hiệp Phước...). Null cho thao tác system/global.</summary>
    string? TenantId { get; }

    /// <summary>Metadata chi tiết của Tenant hiện tại.</summary>
    TenantInfo? CurrentTenant { get; }
}
