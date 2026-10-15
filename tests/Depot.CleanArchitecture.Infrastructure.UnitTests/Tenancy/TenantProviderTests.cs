namespace Depot.CleanArchitecture.Infrastructure.UnitTests.Tenancy;

using System.Security.Claims;
using Depot.CleanArchitecture.Application.Abstractions.Tenancy;
using Depot.CleanArchitecture.Infrastructure.Tenancy;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

public sealed class TenantProviderTests
{
    [Fact]
    public void TenantProvider_Should_Resolve_Tenant_From_Header_When_Header_Is_Present()
    {
        // Arrange
        var expectedTenantId = Guid.NewGuid();
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Tenant-Id"] = expectedTenantId.ToString();

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var provider = new TenantProvider(httpContextAccessor);

        // Act & Assert
        provider.CurrentTenantId.Should().Be(expectedTenantId);
        provider.TenantId.Should().Be(expectedTenantId.ToString());
        provider.CurrentTenant.Should().NotBeNull();
        provider.CurrentTenant!.Id.Should().Be(expectedTenantId.ToString());
    }

    [Fact]
    public void TenantProvider_Should_Resolve_Tenant_From_JwtClaim_When_Header_Is_Missing()
    {
        // Arrange
        var expectedTenantId = Guid.NewGuid();
        var claims = new[]
        {
            new Claim("tenant_id", expectedTenantId.ToString())
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };

        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var provider = new TenantProvider(httpContextAccessor);

        // Act & Assert
        provider.CurrentTenantId.Should().Be(expectedTenantId);
        provider.TenantId.Should().Be(expectedTenantId.ToString());
    }

    [Fact]
    public void TenantProvider_Should_Fallback_To_Default_CatLai_When_No_Header_And_No_Claim()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var provider = new TenantProvider(httpContextAccessor);

        // Act & Assert
        provider.CurrentTenantId.Should().Be(TenantInfo.DefaultDepotCatLaiId);
        provider.TenantId.Should().Be(TenantInfo.Default.Id);
        provider.CurrentTenant.Should().NotBeNull();
        provider.CurrentTenant!.Name.Should().Be("Tân Cảng Cát Lái");
    }

    [Fact]
    public void TenantProvider_Should_Fallback_To_Default_CatLai_When_HttpContext_Is_Null()
    {
        // Arrange
        var httpContextAccessor = new HttpContextAccessor { HttpContext = null };
        var provider = new TenantProvider(httpContextAccessor);

        // Act & Assert
        provider.CurrentTenantId.Should().Be(TenantInfo.DefaultDepotCatLaiId);
        provider.TenantId.Should().Be(TenantInfo.Default.Id);
    }
}
