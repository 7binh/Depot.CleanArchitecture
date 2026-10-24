using Depot.Domain.Common;

namespace Depot.Domain.Tests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_HasNoError()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_CarriesTheError()
    {
        var error = Error.NotFound("Container.NotFound", "Container was not found.");

        var result = Result.Failure(error);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Success_WithValue()
    {
        var result = Result.Success(5);
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value);
    }

    [Fact]
    public void Failure_AccessingValue_ShouldThrow()
    {
        // Given
        var result = Result.Failure<int>(Error.NotFound("Container.NotFound", "Container was not found."));

        // When
        Assert.Throws<InvalidOperationException>(() => result.Value);
        // Then
    }

}
