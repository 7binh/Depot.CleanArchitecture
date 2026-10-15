namespace Depot.CleanArchitecture.Infrastructure.Tenancy;

using Depot.CleanArchitecture.Application.Abstractions.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Middleware xác thực trạng thái hoạt động của Tenant và thiết lập ngữ cảnh cho từng HTTP request.
/// </summary>
public sealed class TenantMiddleware(RequestDelegate next, ILogger<TenantMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
    {
        var tenant = tenantProvider.CurrentTenant;

        if (tenant is not null && !tenant.IsActive)
        {
            logger.LogWarning("Chi nhánh Depot {TenantId} hiện đang bị tạm dừng hoạt động. Từ chối yêu cầu.", tenantProvider.TenantId);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.4",
                title = "Forbidden",
                status = 403,
                detail = $"Chi nhánh Depot '{tenantProvider.TenantId}' đang tạm ngưng hoạt động."
            });
            return;
        }

        await next(context);
    }
}
