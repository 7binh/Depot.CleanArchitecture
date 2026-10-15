namespace Depot.CleanArchitecture.Infrastructure.Tenancy;

using Depot.CleanArchitecture.Application.Abstractions.Tenancy;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Giải quyết Tenant hiện tại từ HTTP request context.
/// Thứ tự ưu tiên: Header X-Tenant-Id -> JWT Claim "tenant_id" -> Fallback Default Tân Cảng Cát Lái.
/// </summary>
public sealed class TenantProvider(IHttpContextAccessor httpContextAccessor) : ITenantProvider
{
    private const string TenantHeader = "X-Tenant-Id";
    private const string TenantClaimType = "tenant_id";

    public Guid CurrentTenantId => ResolveTenantGuid();

    public string? TenantId => CurrentTenantId.ToString();

    public TenantInfo? CurrentTenant => CurrentTenantId == TenantInfo.DefaultDepotCatLaiId
        ? TenantInfo.Default
        : new TenantInfo
        {
            Id = CurrentTenantId.ToString(),
            Name = $"Depot {CurrentTenantId}"
        };

    private Guid ResolveTenantGuid()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return TenantInfo.DefaultDepotCatLaiId;
        }

        // 1. Kiểm tra HTTP Header X-Tenant-Id
        if (context.Request.Headers.TryGetValue(TenantHeader, out var headerValue) &&
            Guid.TryParse(headerValue.ToString(), out var headerGuid) &&
            headerGuid != Guid.Empty)
        {
            return headerGuid;
        }

        // 2. Kiểm tra JWT Claim tenant_id
        var tenantClaim = context.User.FindFirst(TenantClaimType);
        if (tenantClaim is not null &&
            Guid.TryParse(tenantClaim.Value, out var claimGuid) &&
            claimGuid != Guid.Empty)
        {
            return claimGuid;
        }

        // 3. Mặc định fallback về Depot Tân Cảng Cát Lái
        return TenantInfo.DefaultDepotCatLaiId;
    }
}
