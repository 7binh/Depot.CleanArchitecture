namespace Depot.CleanArchitecture.Domain.Entities;

using Depot.CleanArchitecture.Domain.Common;

public class YardSlot : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid BlockId { get; private set; }
    public int Bay { get; private set; }
    public int Row { get; private set; }
    public int Tier { get; private set; }
    public bool IsOccupied { get; private set; }
    public Guid? CurrentContainerId { get; private set; }
    public int? OccupiedContainerSize { get; private set; } // 20 hoặc 40 feet

    private YardSlot() { }

    public static YardSlot Create(Guid tenantId, Guid blockId, int bay, int row, int tier)
    {
        return new YardSlot
        {
            TenantId = tenantId,
            BlockId = blockId,
            Bay = bay,
            Row = row,
            Tier = tier,
            IsOccupied = false
        };
    }

    public void Occupy(Guid containerId, int containerSize)
    {
        IsOccupied = true;
        CurrentContainerId = containerId;
        OccupiedContainerSize = containerSize;
    }

    public void Release()
    {
        IsOccupied = false;
        CurrentContainerId = null;
        OccupiedContainerSize = null;
    }
}
