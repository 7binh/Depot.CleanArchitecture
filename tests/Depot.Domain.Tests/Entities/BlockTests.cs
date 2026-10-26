using Depot.Domain.Common;
using Depot.Domain.Yard;

namespace Depot.Domain.Tests.Entities;

public sealed class BlockTests
{
    [Fact]
    public void Create_TrimsAndUppercasesCode_AndTrimsName()
    {
        var result = Block.Create("  a1 ", "  Khu A1  ", false);

        Assert.True(result.IsSuccess);
        Assert.Equal("A1", result.Value.Code);
        Assert.Equal("Khu A1", result.Value.Name);
        Assert.False(result.Value.IsVirtual);
    }

    [Fact]
    public void Create_KeepsVirtualFlag()
    {
        var result = Block.Create("V1", "Virtual 1", true);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.IsVirtual);
    }

    [Fact]
    public void CanBeRemoved_IsRefused_OnlyWhenTheBlockEverHeldAContainer()
    {
        Assert.True(Block.CanBeRemoved(false).IsSuccess);

        var refused = Block.CanBeRemoved(true);

        Assert.True(refused.IsFailure);
        Assert.Equal("Block.InUse", refused.Error.Code);
        Assert.Equal(ErrorType.Conflict, refused.Error.Type);
    }

    [Fact]
    public void Rename_TrimsTheName_AndKeepsCodeAndVirtualFlag()
    {
        var block = Block.Create("A1", "Khu A1", true).Value;

        var result = block.Rename("  Khu moi  ");

        Assert.True(result.IsSuccess);
        Assert.Equal("Khu moi", block.Name);
        Assert.Equal("A1", block.Code);
        Assert.True(block.IsVirtual);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Rename_Fails_AndKeepsTheName_WhenTheNameIsEmpty(string? name)
    {
        var block = Block.Create("A1", "Khu A1", false).Value;

        var result = block.Rename(name);

        Assert.True(result.IsFailure);
        Assert.Equal("Name.Empty", result.Error.Code);
        Assert.Equal("Khu A1", block.Name);
    }

    [Fact]
    public void Rename_Fails_WhenTheNameIsTooLong()
    {
        var block = Block.Create("A1", "Khu A1", false).Value;

        var result = block.Rename(new string('x', Block.NameMaxLength + 1));

        Assert.True(result.IsFailure);
        Assert.Equal("Name.TooLong", result.Error.Code);
        Assert.Equal("Khu A1", block.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_Fails_WhenCodeIsEmpty(string? code)
    {
        var result = Block.Create(code, "Khu A", false);

        Assert.True(result.IsFailure);
        Assert.Equal("Code.Empty", result.Error.Code);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
    }

    [Fact]
    public void Create_AcceptsCodeOfMaxLength()
    {
        var result = Block.Create(new string('A', Block.CodeMaxLength), "Khu A", false);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Create_Fails_WhenCodeIsTooLong()
    {
        var result = Block.Create(new string('A', Block.CodeMaxLength + 1), "Khu A", false);

        Assert.True(result.IsFailure);
        Assert.Equal("Code.TooLong", result.Error.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Create_Fails_WhenNameIsEmpty(string? name)
    {
        var result = Block.Create("A", name, false);

        Assert.True(result.IsFailure);
        Assert.Equal("Name.Empty", result.Error.Code);
    }

    [Fact]
    public void Create_Fails_WhenNameIsTooLong()
    {
        var result = Block.Create("A", new string('x', Block.NameMaxLength + 1), false);

        Assert.True(result.IsFailure);
        Assert.Equal("Name.TooLong", result.Error.Code);
    }
}
