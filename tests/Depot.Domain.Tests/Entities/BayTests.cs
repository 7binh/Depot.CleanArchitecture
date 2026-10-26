using Depot.Domain.Common;
using Depot.Domain.Yard;

namespace Depot.Domain.Tests.Entities;

public sealed class BayTests
{
    private static Block NormalBlock() => Block.Create("A", "Khu A", false).Value;

    private static Block VirtualBlock() => Block.Create("V", "Virtual", true).Value;

    [Fact]
    public void Create_BelongsToTheGivenBlock()
    {
        var block = NormalBlock();

        var result = Bay.Create(block, 3);

        Assert.True(result.IsSuccess);
        Assert.Equal(block.Id, result.Value.BlockId);
        Assert.Equal(3, result.Value.BayNo);
    }

    [Fact]
    public void CanBeRemoved_IsRefused_OnlyWhileAContainerStands()
    {
        Assert.True(Bay.CanBeRemoved(false).IsSuccess);

        var refused = Bay.CanBeRemoved(true);

        Assert.True(refused.IsFailure);
        Assert.Equal("Bay.HasContainers", refused.Error.Code);
        Assert.Equal(ErrorType.Conflict, refused.Error.Type);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(Bay.MaxBayNo)]
    public void Create_AcceptsBayNoInRange(int bayNo)
    {
        Assert.True(Bay.Create(NormalBlock(), bayNo).IsSuccess);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(Bay.MaxBayNo + 1)]
    public void Create_Fails_WhenBayNoIsOutOfRange(int bayNo)
    {
        var result = Bay.Create(NormalBlock(), bayNo);

        Assert.True(result.IsFailure);
        Assert.Equal("BayNo.Invalid", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public void Create_Fails_WhenBlockIsVirtual()
    {
        var result = Bay.Create(VirtualBlock(), 1);

        Assert.True(result.IsFailure);
        Assert.Equal("Bay.BlockIsVirtual", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }
}
