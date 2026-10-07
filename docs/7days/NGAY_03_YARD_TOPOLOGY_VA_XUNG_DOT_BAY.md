# NGÀY 3: KHÔNG GIAN BÃI (YARD TOPOLOGY), THUẬT TOÁN XUNG ĐỘT BAY 20FT/40FT & BLOCK ẢO
**Dự án**: Depot Management System (Clean Architecture .NET 10 - SNP)  
**Mục tiêu Ngày 3**: Xây dựng mô hình không gian bãi container 3 chiều (Block - Bay - Row - Tier), viết thuật toán khóa va chạm vật lý giữa Bay chẵn 40ft và Bay lẻ 20ft, các quy tắc xếp tầng an toàn (Stacking rules) và cơ chế quản lý Block ảo (Virtual Block).

---

## 1. MỤC TIÊU HỌC TẬP & NỀN TẢNG LÝ THUYẾT

### 1.1. Hệ tọa độ 3D bãi container
Để quản lý hàng chục ngàn container, các cảng của Tân Cảng (SNP) chia bãi theo 4 cấp độ:
- **Block (Khu bãi)**: Một lô đất lớn (VD: Block A, Block B, Block C).
- **Bay (Chiều dài - Trục X)**: Các khoang dọc theo chiều dài bãi.
- **Row (Chiều rộng - Trục Y)**: Các dãy xếp song song từ mép đường chạy vào trong (Row 01, Row 02...).
- **Tier (Chiều cao - Trục Z)**: Tầng xếp chồng từ mặt đất lên cao (Tier 1, Tier 2, Tier 3...).

```
        ┌──────────────────────────────────────────────────┐
        │                 BLOCK A (Khu A)                  │
        └──────────────────────────────────────────────────┘
                      Chiêu dài (Bay) ────────►
         Bay 01 (20ft)      Bay 02 (40ft)      Bay 03 (20ft)
        ┌─────────────┐   ┌─────────────────┐   ┌─────────────┐
 Tier 3 │ [Container] │   │                 │   │ [Container] │ ▲
 Tier 2 │ [Container] │   │   [Cont 40ft]   │   │ [Container] │ │ Chiều cao
 Tier 1 │ [Container] │   │                 │   │ [Container] │ │ (Tier - Tầng)
        └─────────────┘   └─────────────────┘   └─────────────┘ ▼
        ◄─────────────── Chiều rộng (Row - Dãy) ──────────────►
```

### 1.2. Thuật toán Va chạm Không gian: Bay chẵn (40ft) vs Bay lẻ (20ft)
- **Quy ước quốc tế**:
  - Container 20ft dài ~6m $\rightarrow$ Xếp vào **Bay lẻ** (01, 03, 05, 07...).
  - Container 40ft dài ~12m $\rightarrow$ Xếp vào **Bay chẵn** (02, 04, 06...).
  - **Mối quan hệ không gian**: Một container 40ft tại Bay 02 sẽ chiếm trọn thể tích không gian của cả Bay 01 và Bay 03!
- **Luật kiểm tra 2 chiều (Invariants)**:
  1. Khi hạ cont 40ft vào Bay 02: Báo lỗi nếu Bay 01 HOẶC Bay 03 (cùng Row, Tier) đã có container!
  2. Khi hạ cont 20ft vào Bay 01 hoặc 03: Báo lỗi nếu Bay 02 (cùng Row, Tier) đang bị cont 40ft chiếm dụng!

### 1.3. Các Quy tắc Xếp tầng An toàn (Stacking Rules)
1. **Luật Trọng lực (Bottom Support)**: Để hạ container ở tầng $Z > 1$, thì tại đúng ô $Z - 1$ ngay bên dưới bắt buộc phải có một container làm bệ đỡ.
2. **Luật Kích cỡ khi xếp chồng (Size Stacking Safety)**:
   - **TUYỆT ĐỐI CẤM** đặt container 20ft lên nóc container 40ft (vì 4 góc gù chịu lực của cont 20ft sẽ ấn vào giữa nóc cont 40ft gây bẹp/thủng trần).
   - Cho phép đặt 1 cont 40ft lên trên 2 cont 20ft bên dưới nếu 2 cont 20ft nằm liền kề nhau và cùng chiều cao.
3. **Quy tắc Container Quá khổ (Open Top / Flat Rack)**: Container có hàng nhô lên cao bắt buộc phải ở **Tầng trên cùng (Top Tier)**, cấm xếp cont khác đè lên nóc.

---

## 2. TRIỂN KHAI CÁC FILE MÃ NGUỒN TẦNG DOMAIN

### 2.1. `src/Depot.CleanArchitecture.Domain/ValueObjects/SlotCoordinate.cs`
Value Object đóng gói tọa độ và các phép toán hình học bãi:
```csharp
namespace Depot.CleanArchitecture.Domain.ValueObjects;

using Depot.CleanArchitecture.Domain.Common;

public sealed class SlotCoordinate : ValueObject
{
    public int Bay { get; }
    public int Row { get; }
    public int Tier { get; }

    public bool IsOddBay => Bay % 2 != 0;   // Bay lẻ: cont 20ft
    public bool IsEvenBay => Bay % 2 == 0;  // Bay chẵn: cont 40ft

    private SlotCoordinate(int bay, int row, int tier)
    {
        Bay = bay;
        Row = row;
        Tier = tier;
    }

    public static Result<SlotCoordinate> Create(int bay, int row, int tier)
    {
        if (bay <= 0 || row <= 0 || tier <= 0)
            return Result.Failure<SlotCoordinate>(
                Error.Validation("Coordinate.Invalid", "Chỉ số Bay, Row, Tier phải là các số nguyên dương lớn hơn 0."));

        return Result.Success(new SlotCoordinate(bay, row, tier));
    }

    // Với Bay chẵn 40ft (VD: Bay 02) -> lấy 2 bay lẻ bị chiếm dụng (Bay 01, Bay 03)
    public (int PriorOddBay, int NextOddBay) GetOverlappingOddBays()
    {
        if (IsOddBay) throw new InvalidOperationException("Phương thức này chỉ áp dụng cho bay chẵn 40ft.");
        return (Bay - 1, Bay + 1);
    }

    // Với Bay lẻ 20ft (VD: Bay 01 hoặc Bay 03) -> lấy các bay chẵn lân cận có thể xung đột
    public IEnumerable<int> GetAdjacentEvenBays()
    {
        if (IsEvenBay) throw new InvalidOperationException("Phương thức này chỉ áp dụng cho bay lẻ 20ft.");
        if (Bay > 1) yield return Bay - 1;
        yield return Bay + 1;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Bay;
        yield return Row;
        yield return Tier;
    }

    public override string ToString() => $"Bay {Bay:D2} - Row {Row:D2} - Tier {Tier:D2}";
}
```

### 2.2. `src/Depot.CleanArchitecture.Domain/Entities/YardBlock.cs`
Thực thể phân khu bãi (hỗ trợ cả Block vật lý và Block ảo):
```csharp
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
}
```

### 2.3. `src/Depot.CleanArchitecture.Domain/Entities/YardSlot.cs`
Từng ô vị trí trên bãi:
```csharp
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
```

### 2.4. `src/Depot.CleanArchitecture.Domain/Rules/Yard/EvenOddBayCollisionRule.cs`
Thuật toán kiểm tra va chạm vật lý:
```csharp
namespace Depot.CleanArchitecture.Domain.Rules.Yard;

using Depot.CleanArchitecture.Domain.Common.Rules;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.ValueObjects;

public class EvenOddBayCollisionRule : IBusinessRule
{
    private readonly SlotCoordinate _target;
    private readonly int _containerSize;
    private readonly IEnumerable<YardSlot> _occupiedSlotsInSameRowAndTier;

    public EvenOddBayCollisionRule(
        SlotCoordinate target,
        int containerSize,
        IEnumerable<YardSlot> occupiedSlotsInSameRowAndTier)
    {
        _target = target;
        _containerSize = containerSize;
        _occupiedSlotsInSameRowAndTier = occupiedSlotsInSameRowAndTier;
    }

    public string RuleCode => "Yard.EvenOddBayCollision";
    public string Message => "Không thể hạ container: Vị trí bị xung đột không gian vật lý với container 20ft/40ft ở bay liền kề!";

    public bool IsBroken()
    {
        var occupiedBays = _occupiedSlotsInSameRowAndTier.Select(s => s.Bay).ToHashSet();

        if (_containerSize == 40)
        {
            // Cont 40ft hạ vào bay chẵn: kiểm tra 2 bay lẻ trước và sau
            var (bay1, bay2) = _target.GetOverlappingOddBays();
            if (occupiedBays.Contains(bay1) || occupiedBays.Contains(bay2))
                return true; // Xung đột với cont 20ft đang có sẵn!
        }
        else if (_containerSize == 20)
        {
            // Cont 20ft hạ vào bay lẻ: kiểm tra các bay chẵn lân cận
            foreach (var evenBay in _target.GetAdjacentEvenBays())
            {
                if (occupiedBays.Contains(evenBay))
                    return true; // Xung đột với cont 40ft đang chiếm dụng khoảng không!
            }
        }

        return false;
    }
}
```

### 2.5. Các Stacking Rules & Virtual Block Rule
Tạo file `src/Depot.CleanArchitecture.Domain/Rules/Yard/TierMustHaveBottomSupportRule.cs`:
```csharp
namespace Depot.CleanArchitecture.Domain.Rules.Yard;

using Depot.CleanArchitecture.Domain.Common.Rules;

public class TierMustHaveBottomSupportRule : IBusinessRule
{
    private readonly int _tier;
    private readonly bool _hasBottomSupport;

    public TierMustHaveBottomSupportRule(int tier, bool hasBottomSupport)
    {
        _tier = tier;
        _hasBottomSupport = hasBottomSupport;
    }

    public string RuleCode => "Yard.TierMissingBottomSupport";
    public string Message => $"Không thể hạ container ở tầng {_tier} vì tầng {_tier - 1} ngay bên dưới chưa có container làm bệ đỡ!";
    public bool IsBroken() => _tier > 1 && !_hasBottomSupport;
}
```

Tạo file `src/Depot.CleanArchitecture.Domain/Rules/Yard/SizeStackingSafetyRule.cs`:
```csharp
namespace Depot.CleanArchitecture.Domain.Rules.Yard;

using Depot.CleanArchitecture.Domain.Common.Rules;

public class SizeStackingSafetyRule : IBusinessRule
{
    private readonly int _topContainerSize;
    private readonly int? _bottomContainerSize;

    public SizeStackingSafetyRule(int topContainerSize, int? bottomContainerSize)
    {
        _topContainerSize = topContainerSize;
        _bottomContainerSize = bottomContainerSize;
    }

    public string RuleCode => "Yard.InvalidSizeStacking";
    public string Message => "Tuyệt đối cấm đặt container 20ft lên nóc container 40ft (nguy cơ sập trần vỏ container bên dưới)!";
    public bool IsBroken() => _topContainerSize == 20 && _bottomContainerSize == 40;
}
```

Tạo file `src/Depot.CleanArchitecture.Domain/Rules/Yard/VirtualBlockCapacityRule.cs`:
```csharp
namespace Depot.CleanArchitecture.Domain.Rules.Yard;

using Depot.CleanArchitecture.Domain.Common.Rules;

public class VirtualBlockCapacityRule : IBusinessRule
{
    private readonly int _currentCount;
    private readonly int _maxCapacity;

    public VirtualBlockCapacityRule(int currentCount, int maxCapacity)
    {
        _currentCount = currentCount;
        _maxCapacity = maxCapacity;
    }

    public string RuleCode => "Yard.VirtualBlockCapacityExceeded";
    public string Message => $"Khu vực bãi ảo đã đầy sức chứa (Đang chứa: {_currentCount}/{_maxCapacity} cont)!";
    public bool IsBroken() => _currentCount >= _maxCapacity;
}
```

---

## 3. BÀI TEST THỰC HÀNH NGÀY 3

Tạo file `tests/Depot.CleanArchitecture.Domain.UnitTests/Yard/EvenOddBayCollisionRuleTests.cs`:
```csharp
namespace Depot.CleanArchitecture.Domain.UnitTests.Yard;

using Depot.CleanArchitecture.Domain.Common.Rules;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Rules.Yard;
using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class EvenOddBayCollisionRuleTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _blockId = Guid.NewGuid();

    [Fact]
    public void Placing40ftContainer_WhenAdjacentOddBayIsOccupied_ShouldBeBroken()
    {
        // Giả lập: Bay 01 (20ft) đang có cont tại Row 01, Tier 01
        var slot01 = YardSlot.Create(_tenantId, _blockId, bay: 1, row: 1, tier: 1);
        slot01.Occupy(Guid.NewGuid(), containerSize: 20);

        var existingOccupiedSlots = new List<YardSlot> { slot01 };

        // Thử đặt cont 40ft vào Bay 02 tại cùng Row 01, Tier 01
        var targetCoord = SlotCoordinate.Create(bay: 2, row: 1, tier: 1).Value;
        var rule = new EvenOddBayCollisionRule(targetCoord, containerSize: 40, existingOccupiedSlots);

        rule.IsBroken().Should().BeTrue();
    }

    [Fact]
    public void Placing20ftContainer_WhenOverlyingEvenBayIsOccupied_ShouldBeBroken()
    {
        // Giả lập: Bay 02 (40ft) đang có cont tại Row 01, Tier 01
        var slot02 = YardSlot.Create(_tenantId, _blockId, bay: 2, row: 1, tier: 1);
        slot02.Occupy(Guid.NewGuid(), containerSize: 40);

        var existingOccupiedSlots = new List<YardSlot> { slot02 };

        // Thử đặt cont 20ft vào Bay 03 tại cùng Row 01, Tier 01
        var targetCoord = SlotCoordinate.Create(bay: 3, row: 1, tier: 1).Value;
        var rule = new EvenOddBayCollisionRule(targetCoord, containerSize: 20, existingOccupiedSlots);

        rule.IsBroken().Should().BeTrue();
    }
}
```

**Lệnh chạy test**:
```bash
dotnet test tests/Depot.CleanArchitecture.Domain.UnitTests
```
Kết quả mong đợi: Toàn bộ bài test va chạm bãi đều PASS.
