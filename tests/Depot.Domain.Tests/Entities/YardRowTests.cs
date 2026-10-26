using Depot.Domain.Common;
using Depot.Domain.Yard;

namespace Depot.Domain.Tests.Entities;

public sealed class YardRowTests
{
    [Fact]
    public void Create_BelongsToTheGivenBay()
    {
        var bayId = Guid.NewGuid();

        var result = YardRow.Create(bayId, 3, 4);

        Assert.True(result.IsSuccess);
        Assert.Equal(bayId, result.Value.BayId);
        Assert.Equal(3, result.Value.RowNo);
        Assert.Equal(4, result.Value.MaxTier);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(YardRow.MaxRowNo)]
    public void Create_AcceptsRowNoInRange(int rowNo)
    {
        Assert.True(YardRow.Create(Guid.NewGuid(), rowNo, 3).IsSuccess);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(YardRow.MaxRowNo + 1)]
    public void Create_Fails_WhenRowNoIsOutOfRange(int rowNo)
    {
        var result = YardRow.Create(Guid.NewGuid(), rowNo, 3);

        Assert.True(result.IsFailure);
        Assert.Equal("RowNo.Invalid", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Theory]
    [InlineData(YardRow.MinTierCount)]
    [InlineData(YardRow.MaxTierCount)]
    public void Create_AcceptsMaxTierInRange(int maxTier)
    {
        Assert.True(YardRow.Create(Guid.NewGuid(), 1, maxTier).IsSuccess);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(-3)]
    [InlineData(YardRow.MaxTierCount + 1)]
    public void Create_Fails_WhenMaxTierIsOutOfRange(int maxTier)
    {
        var result = YardRow.Create(Guid.NewGuid(), 1, maxTier);

        Assert.True(result.IsFailure);
        Assert.Equal("MaxTier.Invalid", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }
}
