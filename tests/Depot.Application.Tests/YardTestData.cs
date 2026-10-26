using Depot.Application.Features.Bays.Create;
using Depot.Application.Features.Blocks.Create;
using Depot.Infrastructure.Persistence;

namespace Depot.Application.Tests;

internal static class YardTestData
{
    public static async Task<Guid> CreateBlockAsync(
        AppDbContext dbContext,
        string code,
        CancellationToken cancellationToken)
    {
        var block = await new CreateBlockHandler(dbContext)
            .HandleAsync(new CreateBlockCommand(code, $"Block {code}"), cancellationToken);
        return block.Value.Id;
    }

    public static async Task<Guid> CreateBayAsync(
        AppDbContext dbContext,
        Guid blockId,
        int bayNo,
        CancellationToken cancellationToken)
    {
        var bay = await new CreateBayHandler(dbContext)
            .HandleAsync(new CreateBayCommand(blockId, bayNo), cancellationToken);
        return bay.Value.Id;
    }
}
