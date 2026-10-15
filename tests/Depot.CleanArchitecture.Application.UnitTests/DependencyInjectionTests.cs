namespace Depot.CleanArchitecture.Application.UnitTests;

using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Application.Features.Analytics.GetAgingReport;
using Depot.CleanArchitecture.Application.Features.Gate.GateIn;
using Depot.CleanArchitecture.Application.Features.Gate.GateOut;
using Depot.CleanArchitecture.Domain.Common;
using FluentAssertions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddApplication_Should_Register_Handlers_And_Validators()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddApplication();

        // Assert: Validators registered
        var provider = services.BuildServiceProvider();
        var gateInValidator = provider.GetService<IValidator<GateInCommand>>();
        gateInValidator.Should().NotBeNull();

        var gateOutValidator = provider.GetService<IValidator<GateOutCommand>>();
        gateOutValidator.Should().NotBeNull();

        // Assert: Handlers registered
        services.Should().Contain(d => d.ServiceType == typeof(ICommandHandler<GateInCommand, Result<GateInResponse>>));
        services.Should().Contain(d => d.ServiceType == typeof(ICommandHandler<GateOutCommand, Result<GateOutResponse>>));
        services.Should().Contain(d => d.ServiceType == typeof(IQueryHandler<GetAgingReportQuery, Result<List<LineOperatorAgingDto>>>));
    }
}
