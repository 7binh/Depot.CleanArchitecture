using Depot.Domain.Catalogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Depot.Application.Tests.Persistence;

public sealed class ContainerTypeSeedTests
{
    private static readonly string[] _expectedCodes =
        ["BUNKER", "DRY", "FLATRACK", "OPENTOP", "REEFER", "SPECIALIZED", "VENTILATED"];

    [Fact]
    public void Model_SeedsTheSevenContainerTypesFromBrCt05()
    {
        using var dbContext = TestDbContextFactory.Create();

        var seeded = GetSeededRows(dbContext);

        Assert.Equal(_expectedCodes, seeded.Select(x => (string)x["Code"]!).Order());
        Assert.Equal(seeded.Count, seeded.Select(x => x["Id"]).Distinct().Count());
    }

    [Fact]
    public void Model_SeededRowsSatisfyTheDomainRules()
    {
        using var dbContext = TestDbContextFactory.Create();

        foreach (var row in GetSeededRows(dbContext))
        {
            var created = ContainerType.Create((string)row["Code"]!, (string)row["Name"]!);

            Assert.True(created.IsSuccess, $"Seed '{row["Code"]}' breaks a Domain rule.");
            Assert.Equal(row["Code"], created.Value.Code);
        }
    }

    private static List<IDictionary<string, object?>> GetSeededRows(DbContext dbContext)
    {
        var entityType = dbContext.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(ContainerType))!;
        return entityType.GetSeedData().ToList();
    }
}
