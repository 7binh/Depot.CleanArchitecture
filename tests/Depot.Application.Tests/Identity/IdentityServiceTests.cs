using Depot.Domain.Common;
using Depot.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Depot.Application.Tests.Identity;

public sealed class IdentityServiceTests
{
    private const string Email = "user@example.com";
    private const string Password = "Passw0rd!";

    private static readonly CancellationToken _ct = TestContext.Current.CancellationToken;

    [Fact]
    public async Task CheckCredentialsAsync_ReturnsTheUser_WhenEmailAndPasswordAreCorrect()
    {
        await using var provider = await BuildWithUserAsync();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IdentityService>();

        var result = await service.CheckCredentialsAsync(Email, Password, _ct);

        Assert.True(result.IsSuccess);
        Assert.Equal(Email, result.Value.Email);
    }

    [Fact]
    public async Task CheckCredentialsAsync_FailsTheSameWay_WhenThePasswordIsWrong_AndWhenTheEmailIsUnknown()
    {
        await using var provider = await BuildWithUserAsync();
        using var scope = provider.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<IdentityService>();

        var wrongPassword = await service.CheckCredentialsAsync(Email, "Wrong-pass1!", _ct);
        var unknownEmail = await service.CheckCredentialsAsync("nobody@example.com", Password, _ct);

        Assert.True(wrongPassword.IsFailure);
        Assert.True(unknownEmail.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, wrongPassword.Error.Type);
        Assert.Equal(wrongPassword.Error, unknownEmail.Error);
    }

    private static async Task<ServiceProvider> BuildWithUserAsync()
    {
        var provider = IdentityTestHost.Build();
        using var scope = provider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var created = await userManager.CreateAsync(
            new ApplicationUser { UserName = Email, Email = Email },
            Password);
        Assert.True(created.Succeeded);
        return provider;
    }
}
