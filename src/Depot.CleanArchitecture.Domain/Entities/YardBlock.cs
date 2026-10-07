namespace Depot.CleanArchitecture.Domain.Entities;

using Depot.CleanArchitecture.Domain.Common;

public class YardBlock : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string BlockCode { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public int? MaxBay { get; private set; }
    public int? MaxRow { get; private set; }
    public int? MaxTier { get; private set; }
    public bool IsVirtual { get; private set; }
    public int? MaxCapacity { get; private set; } // Sức chứa tối đa nếu là Block ảo
    public bool HasReeferPower { get; private set; } // Có giàn cắm điện lạnh không

    private readonly List<YardSlot> _slots = new();
    public IReadOnlyCollection<YardSlot> Slots => _slots.AsReadOnly();

    private YardBlock() { }

    // Tạo Block vật lý có chia lưới 3D
    public static YardBlock CreatePhysical(
        Guid tenantId,
        string code,
        string name,
        int maxBay,
        int maxRow,
        int maxTier,
        bool hasReeferPower = false)
    {
        return new YardBlock
        {
            TenantId = tenantId,
            BlockCode = code.ToUpperInvariant(),
            Name = name,
            MaxBay = maxBay,
            MaxRow = maxRow,
            MaxTier = maxTier,
            IsVirtual = false,
            HasReeferPower = hasReeferPower
        };
    }

    // Tạo Block ảo (Xưởng M&R, Sân rửa cont, Bãi đệm cổng)
    public static YardBlock CreateVirtual(
        Guid tenantId,
        string code,
        string name,
        int maxCapacity)
    {
        return new YardBlock
        {
            TenantId = tenantId,
            BlockCode = code.ToUpperInvariant(),
            Name = name,
            IsVirtual = true,
            MaxCapacity = maxCapacity,
            HasReeferPower = false
        };
    }

    public void AddSlot(YardSlot slot)
    {
        _slots.Add(slot);
    }
}
