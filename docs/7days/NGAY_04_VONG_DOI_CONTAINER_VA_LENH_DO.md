# NGÀY 4: VÒNG ĐỜI CONTAINER (LIFECYCLE) & RÀNG BUỘC LỆNH GIAO NHẬN (DELIVERY ORDER)
**Dự án**: Depot Management System (Clean Architecture .NET 10 - SNP)  
**Mục tiêu Ngày 4**: Xây dựng Aggregate Root `ContainerVisit` quản lý State Machine vòng đời lưu bãi, phiếu cổng EIR, Entity `DeliveryOrder` (Lệnh DO của Hãng tàu) và bộ 5 Business Rules nghiêm ngặt khi xuất container.

---

## 1. MỤC TIÊU HỌC TẬP & NỀN TẢNG LÝ THUYẾT

### 1.1. Khái niệm "Vòng đời Container" (Container Visit)
Một chiếc container vật lý bằng thép có thể quay lại bãi depot hàng chục lần trong vòng đời 15-20 năm của nó:
- `Container` là **hồ sơ thiết bị** (số cont, kích thước, tải trọng).
- `ContainerVisit` là **một đợt lưu bãi cụ thể** (ghé bãi ngày nào, xe nào chở vào, hạ ở ô nào, xe nào lấy ra, xuất theo lệnh DO nào).

```
 [Container ở ngoài]
          │
          ▼  (1) GATE-IN (Xe đầu kéo chở cont qua cổng vào bãi)
   ┌──────────────┐
   │   GatedIn    │  ◄── Ghi nhận biển số xe vào, tài xế, giám định vỏ (EIR In)
   └──────┬───────┘
          │
          ▼  (2) STACK IN YARD (Xe nâng hạ cont từ xe tải xuống ô bãi)
   ┌──────────────┐
   │ StackedInYard│  ◄── Gắn tọa độ: Block - Bay - Row - Tier (hoặc Block ảo)
   └──────┬───────┘      (Có thể đảo chuyển vị trí - Shifting)
          │
          ▼  (3) ALLOCATE (Ghép cont vào Lệnh giao rỗng hợp lệ)
   ┌──────────────┐
   │  Allocated   │  ◄── Khóa cont lại, sẵn sàng chờ xe đến lấy
   └──────┬───────┘
          │
          ▼  (4) GATE-OUT (Xe đầu kéo chở cont qua cổng ra khỏi bãi)
   ┌──────────────┐
   │   GatedOut   │  ◄── Ghi nhận xe ra, trừ định ngạch Lệnh DO, giải phóng ô bãi
   └──────┬───────┘
          │
          ▼  (5) COMPLETED (Kết thúc vòng đời lưu bãi)
   ┌──────────────┐
   │  Completed   │  ◄── Tính toán thời gian lưu bãi (Dwell Time) cho báo cáo
   └──────────────┘
```

### 1.2. Ràng buộc Pháp lý: Lệnh Giao Container (Delivery Order - DO)
> **"Depot không tự ý giao container ra ngoài, phải có lệnh của hãng vận chuyển (line operator)"**

Container là tài sản của Hãng tàu (Maersk, CMA, MSC...). Depot chỉ giữ hộ. Để lấy cont rỗng ra khỏi bãi, tài xế bắt buộc phải trình Lệnh DO do Hãng tàu cấp:
- Phải còn trong thời hạn hiệu lực (`ExpirationDate`).
- Phải đúng Hãng tàu sở hữu cont.
- Phải đúng kích cỡ (20/40ft) và phân hạng chất lượng (Grade A/B/C) yêu cầu.
- Không được xuất vượt quá số lượng đăng ký trên Lệnh.
- Vỏ container bị hư hỏng (Grade D, E) **tuyệt đối cấm cấp** cho khách hàng.

---

## 2. TRIỂN KHAI CÁC FILE MÃ NGUỒN TẦNG DOMAIN

### 2.1. `src/Depot.CleanArchitecture.Domain/ValueObjects/VehicleInfo.cs`
Value Object lưu trữ thông tin phương tiện vận chuyển và tài xế tại cổng:
```csharp
namespace Depot.CleanArchitecture.Domain.ValueObjects;

using Depot.CleanArchitecture.Domain.Common;

public sealed class VehicleInfo : ValueObject
{
    public string TractorNo { get; }  // Biển số xe đầu kéo (VD: 51C-123.45)
    public string TrailerNo { get; }  // Biển số rơ-moóc (VD: 51R-678.90)
    public string DriverName { get; } // Họ tên tài xế
    public string? DriverPhone { get; }

    private VehicleInfo(string tractorNo, string trailerNo, string driverName, string? driverPhone)
    {
        TractorNo = tractorNo;
        TrailerNo = trailerNo;
        DriverName = driverName;
        DriverPhone = driverPhone;
    }

    public static Result<VehicleInfo> Create(string tractorNo, string trailerNo, string driverName, string? driverPhone = null)
    {
        if (string.IsNullOrWhiteSpace(tractorNo) || string.IsNullOrWhiteSpace(trailerNo))
            return Result.Failure<VehicleInfo>(
                Error.Validation("Vehicle.InvalidLicense", "Biển số xe đầu kéo và rơ-moóc không được để trống."));

        if (string.IsNullOrWhiteSpace(driverName))
            return Result.Failure<VehicleInfo>(
                Error.Validation("Vehicle.InvalidDriver", "Họ tên tài xế không được để trống."));

        return Result.Success(new VehicleInfo(
            tractorNo.Trim().ToUpperInvariant(),
            trailerNo.Trim().ToUpperInvariant(),
            driverName.Trim(),
            driverPhone?.Trim()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TractorNo;
        yield return TrailerNo;
        yield return DriverName;
    }
}
```

### 2.2. `src/Depot.CleanArchitecture.Domain/Entities/ContainerVisit.cs`
Aggregate Root quản lý vòng đời lưu bãi:
```csharp
namespace Depot.CleanArchitecture.Domain.Entities;

using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;

public enum ContainerVisitStatus
{
    GatedIn = 1,
    StackedInYard = 2,
    Allocated = 3,
    GatedOut = 4,
    Completed = 5
}

public class ContainerVisit : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid ContainerId { get; private set; }
    public string LineOperator { get; private set; } = null!;
    public ContainerVisitStatus Status { get; private set; }
    public Guid? CurrentSlotId { get; private set; }
    public Guid? CurrentBlockId { get; private set; }

    // Thông tin Nhập bãi (Gate-In)
    public DateTime GateInDate { get; private set; }
    public VehicleInfo InVehicle { get; private set; } = null!;
    public ContainerGrade InSurveyGrade { get; private set; }
    public string? InDamageNotes { get; private set; }

    // Thông tin Xuất bãi (Gate-Out)
    public DateTime? GateOutDate { get; private set; }
    public VehicleInfo? OutVehicle { get; private set; }
    public Guid? DeliveryOrderId { get; private set; }

    private ContainerVisit() { }

    public static ContainerVisit CreateGateIn(
        Guid tenantId,
        Guid containerId,
        string lineOperator,
        VehicleInfo vehicle,
        ContainerGrade surveyGrade,
        string? damageNotes)
    {
        return new ContainerVisit
        {
            TenantId = tenantId,
            ContainerId = containerId,
            LineOperator = lineOperator.ToUpperInvariant(),
            Status = ContainerVisitStatus.GatedIn,
            GateInDate = DateTime.UtcNow,
            InVehicle = vehicle,
            InSurveyGrade = surveyGrade,
            InDamageNotes = damageNotes
        };
    }

    public void AssignSlot(Guid slotId, Guid blockId)
    {
        CurrentSlotId = slotId;
        CurrentBlockId = blockId;
        Status = ContainerVisitStatus.StackedInYard;
    }

    public void AssignVirtualBlock(Guid virtualBlockId)
    {
        CurrentSlotId = null;
        CurrentBlockId = virtualBlockId;
        Status = ContainerVisitStatus.StackedInYard;
    }

    public void AllocateToOrder(Guid deliveryOrderId)
    {
        DeliveryOrderId = deliveryOrderId;
        Status = ContainerVisitStatus.Allocated;
    }

    public void GateOut(VehicleInfo outVehicle)
    {
        OutVehicle = outVehicle;
        GateOutDate = DateTime.UtcNow;
        Status = ContainerVisitStatus.GatedOut;
        CurrentSlotId = null; // Giải phóng ô bãi
    }
}
```

### 2.3. `src/Depot.CleanArchitecture.Domain/Entities/DeliveryOrder.cs`
Lệnh giao container của Hãng tàu:
```csharp
namespace Depot.CleanArchitecture.Domain.Entities;

using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Enums;

public enum DeliveryOrderStatus
{
    Active = 1,
    FullyDelivered = 2,
    Expired = 3,
    Cancelled = 4
}

public class DeliveryOrder : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public string OrderNumber { get; private set; } = null!;
    public string LineOperator { get; private set; } = null!;
    public string CustomerName { get; private set; } = null!;
    public string CustomerTaxCode { get; private set; } = null!;
    public DateOnly ExpirationDate { get; private set; }
    public string VesselName { get; private set; } = null!;
    public string VoyageNo { get; private set; } = null!;
    public DeliveryOrderStatus Status { get; private set; }

    private readonly List<DeliveryOrderItem> _items = new();
    public IReadOnlyCollection<DeliveryOrderItem> Items => _items.AsReadOnly();

    private DeliveryOrder() { }

    public static DeliveryOrder Create(
        Guid tenantId,
        string orderNumber,
        string lineOperator,
        string customerName,
        string customerTaxCode,
        DateOnly expirationDate,
        string vesselName,
        string voyageNo)
    {
        return new DeliveryOrder
        {
            TenantId = tenantId,
            OrderNumber = orderNumber.Trim().ToUpperInvariant(),
            LineOperator = lineOperator.Trim().ToUpperInvariant(),
            CustomerName = customerName.Trim(),
            CustomerTaxCode = customerTaxCode.Trim(),
            ExpirationDate = expirationDate,
            VesselName = vesselName.Trim().ToUpperInvariant(),
            VoyageNo = voyageNo.Trim().ToUpperInvariant(),
            Status = DeliveryOrderStatus.Active
        };
    }

    public void AddItem(int size, ContainerType type, ContainerGrade requiredGrade, int quantity)
    {
        _items.Add(new DeliveryOrderItem(Id, size, type, requiredGrade, quantity));
    }

    public void ExtendExpirationDate(DateOnly newExpirationDate)
    {
        if (newExpirationDate > ExpirationDate)
        {
            ExpirationDate = newExpirationDate;
            if (Status == DeliveryOrderStatus.Expired)
                Status = DeliveryOrderStatus.Active;
        }
    }
}

public class DeliveryOrderItem : BaseEntity
{
    public Guid DeliveryOrderId { get; private set; }
    public int Size { get; private set; }
    public ContainerType Type { get; private set; }
    public ContainerGrade RequiredGrade { get; private set; }
    public int OrderedQuantity { get; private set; }
    public int DeliveredQuantity { get; private set; }

    internal DeliveryOrderItem(Guid orderId, int size, ContainerType type, ContainerGrade grade, int orderedQty)
    {
        DeliveryOrderId = orderId;
        Size = size;
        Type = type;
        RequiredGrade = grade;
        OrderedQuantity = orderedQty;
        DeliveredQuantity = 0;
    }

    public void RecordDelivery()
    {
        if (DeliveredQuantity < OrderedQuantity)
            DeliveredQuantity++;
    }
}
```

### 2.4. Bộ Business Rules kiểm tra xuất bãi (Gate-Out Rules)
Tạo file `src/Depot.CleanArchitecture.Domain/Rules/Orders/DeliveryOrderNotExpiredRule.cs`:
```csharp
namespace Depot.CleanArchitecture.Domain.Rules.Orders;

using Depot.CleanArchitecture.Domain.Common.Rules;

public class DeliveryOrderNotExpiredRule : IBusinessRule
{
    private readonly DateOnly _expirationDate;
    private readonly DateOnly _currentDate;

    public DeliveryOrderNotExpiredRule(DateOnly expirationDate, DateOnly currentDate)
    {
        _expirationDate = expirationDate;
        _currentDate = currentDate;
    }

    public string RuleCode => "DeliveryOrder.Expired";
    public string Message => $"Lệnh giao container đã hết hạn vào ngày {_expirationDate:dd/MM/yyyy}! Vui lòng gia hạn với Hãng tàu.";
    public bool IsBroken() => _currentDate > _expirationDate;
}
```

Tạo file `src/Depot.CleanArchitecture.Domain/Rules/Orders/ContainerLineOperatorMatchesOrderRule.cs`:
```csharp
namespace Depot.CleanArchitecture.Domain.Rules.Orders;

using Depot.CleanArchitecture.Domain.Common.Rules;

public class ContainerLineOperatorMatchesOrderRule : IBusinessRule
{
    private readonly string _containerLineOperator;
    private readonly string _orderLineOperator;

    public ContainerLineOperatorMatchesOrderRule(string containerLineOperator, string orderLineOperator)
    {
        _containerLineOperator = containerLineOperator;
        _orderLineOperator = orderLineOperator;
    }

    public string RuleCode => "DeliveryOrder.LineOperatorMismatch";
    public string Message => $"Container thuộc hãng tàu '{_containerLineOperator}', không thể xuất cho Lệnh của hãng tàu '{_orderLineOperator}'!";
    public bool IsBroken() => !string.Equals(_containerLineOperator, _orderLineOperator, StringComparison.OrdinalIgnoreCase);
}
```

Tạo file `src/Depot.CleanArchitecture.Domain/Rules/Orders/DamagedContainerCannotBeAllocatedRule.cs`:
```csharp
namespace Depot.CleanArchitecture.Domain.Rules.Orders;

using Depot.CleanArchitecture.Domain.Common.Rules;
using Depot.CleanArchitecture.Domain.Enums;

public class DamagedContainerCannotBeAllocatedRule : IBusinessRule
{
    private readonly ContainerGrade _grade;

    public DamagedContainerCannotBeAllocatedRule(ContainerGrade grade) => _grade = grade;

    public string RuleCode => "Container.DamagedCannotBeAllocated";
    public string Message => "Container đang bị hư hỏng (Grade D) hoặc phế thải (Grade E), tuyệt đối không được phép cấp cho khách hàng!";
    public bool IsBroken() => _grade == ContainerGrade.GradeD_Damaged || _grade == ContainerGrade.GradeE_Scrap;
}
```

---

## 3. BÀI TEST THỰC HÀNH NGÀY 4

Tạo file `tests/Depot.CleanArchitecture.Domain.UnitTests/Orders/DeliveryOrderRuleTests.cs`:
```csharp
namespace Depot.CleanArchitecture.Domain.UnitTests.Orders;

using Depot.CleanArchitecture.Domain.Common.Rules;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.Rules.Orders;
using FluentAssertions;
using Xunit;

public class DeliveryOrderRuleTests
{
    [Fact]
    public void DeliveryOrderNotExpiredRule_WhenExpiredYesterday_ShouldBeBroken()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var rule = new DeliveryOrderNotExpiredRule(yesterday, today);
        rule.IsBroken().Should().BeTrue();
    }

    [Fact]
    public void ContainerLineOperatorMatchesOrderRule_WhenMismatched_ShouldBeBroken()
    {
        var rule = new ContainerLineOperatorMatchesOrderRule("MAERSK", "CMA-CGM");
        rule.IsBroken().Should().BeTrue();
    }

    [Theory]
    [InlineData(ContainerGrade.GradeD_Damaged)]
    [InlineData(ContainerGrade.GradeE_Scrap)]
    public void DamagedContainerCannotBeAllocatedRule_WhenDamaged_ShouldBeBroken(ContainerGrade damagedGrade)
    {
        var rule = new DamagedContainerCannotBeAllocatedRule(damagedGrade);
        rule.IsBroken().Should().BeTrue();
    }
}
```

**Lệnh chạy test**:
```bash
dotnet test tests/Depot.CleanArchitecture.Domain.UnitTests
```
Kết quả mong đợi: Toàn bộ test pass 100%.
