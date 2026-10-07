# KẾ HOẠCH TRIỂN KHAI DỰ ÁN DEPOT CLEAN ARCHITECTURE (7 NGÀY - VỪA LÀM VỪA HỌC)

> **Dành cho Agentic Worker & Thực tập sinh:** Kỹ năng thực thi kế hoạch: Dùng `sp-executing-plans` (Native) hoặc `sp-subagent-driven-development`. Mỗi task kết thúc với việc chạy test thành công và commit. Các bước theo dõi bằng checkbox (`- [ ]`).

**Mục tiêu (Goal):** Xây dựng hoàn chỉnh Hệ thống Quản lý Depot Container (SNP DMS) theo chuẩn Clean Architecture 4 tầng (.NET 10, C# 13/14, Manual CQRS, HybridCache, Multi-Tenancy) kế thừa mẫu doanh nghiệp của Mentor (`TechSpherex.CleanArchitecture`), bao quát toàn bộ Business Rules chuyên sâu của Tổng công ty Tân Cảng Sài Gòn.

**Kiến trúc (Architecture):** Clean Architecture 4 tầng (`Domain` $\leftarrow$ `Application` $\leftarrow$ `Infrastructure` $\leftarrow$ `Api`). Tầng Domain không phụ thuộc NuGet ngoài, bảo vệ Invariants bằng pattern `IBusinessRule`. Tầng Application sử dụng Manual CQRS (không dùng MediatR) chia theo Vertical Slice. Tầng Infrastructure sử dụng EF Core 10 (PostgreSQL), HybridCache và Multi-tenancy phân tách theo từng Depot. Tầng Api sử dụng Minimal API và Scalar.

**Công nghệ (Tech Stack):** .NET 10, C# 13/14, Entity Framework Core 10, HybridCache, FluentValidation, Scalar.AspNetCore, xUnit v3, FluentAssertions, NetArchTest.

**Tài liệu thiết kế (Spec):** [docs/superpowers/specs/2026-10-06-depot-system-design.md](file:///c:/HanhTrinhThucTap%20-%20TCIS/Projects/Depot.CleanArchitecture/docs/superpowers/specs/2026-10-06-depot-system-design.md)

---

## CÁC RÀNG BUỘC TOÀN CỤC (GLOBAL CONSTRAINTS)
- Target Framework: `net10.0` trên toàn bộ các project C#.
- Quản lý gói tập trung: Cấu hình `Directory.Packages.props` và `Directory.Build.props` tương tự template của mentor.
- Tuyệt đối không cài MediatR (sử dụng Manual CQRS: `ICommand`, `IQuery`, `ICommandHandler`, `IQueryHandler`).
- Tầng `Domain` không được tham chiếu bất kỳ thư viện ngoài nào (trừ C# runtime cơ bản).
- Mọi luật nghiệp vụ bắt buộc phải kế thừa `IBusinessRule` và kiểm tra qua `BusinessRuleValidator.CheckRule(rule)`.
- Mọi thực thể chính đều kế thừa `AuditableEntity` và `ITenantEntity` (`TenantId` định danh cho Depot/Chi nhánh).
- Mã lỗi và thông điệp vi phạm nghiệp vụ phải rõ ràng, bằng tiếng Việt có dấu hỗ trợ người vận hành cảng.

---

## 5 ĐIỂM KIỂM SOÁT ĐẶC BIỆT CẦN LƯU Ý (REVIEW FOCUS)
1. **Thuật toán Modulo 11 với phần dư 10**: Khi tổng dư cho 11 bằng 10, mã kiểm tra phải là ký tự `'X'` (theo chuẩn SNP và ISO 6346), không được gây crash kiểu số.
2. **Xung đột Bay chẵn / Bay lẻ (20ft vs 40ft)**: Phải kiểm tra 2 chiều: Đặt cont 40ft ở Bay 02 phải khóa Bay 01 & 03; Đặt cont 20ft ở Bay 01 hoặc 03 phải khóa Bay 02.
3. **Quy tắc xếp tầng trọng lực**: Cấm hạ container ở Tier > 1 nếu ngay bên dưới không có container đỡ; Cấm đặt cont 20ft lên nóc cont 40ft.
4. **Quy tắc Block ảo (M&R, Rửa cont, Đệm)**: Cho phép tọa độ Bay/Row/Tier nhận giá trị `null`, chỉ kiểm tra sức chứa tối đa `MaxCapacity`.
5. **Ràng buộc Lệnh giao cont (DO)**: Nghiêm cấm Gate-Out nếu quá hạn lệnh, sai hãng tàu, hoặc vượt quá định mức số lượng cho phép.

---

## LỘ TRÌNH 7 NGÀY TRIỂN KHAI (7 TASKS LỚN)

```
 Ngày 1: Setup Solution, Dependencies & Domain Primitives (Result, Error, IBusinessRule, Entities Cơ sở)
    │
 Ngày 2: Module Container & Thuật toán Modulo 11 (Value Object ContainerNumber, Phân hạng Grade A-E)
    │
 Ngày 3: Module Bãi Container (Yard Topology, Bay chẵn/lẻ 20ft/40ft Collision, Block ảo, Stacking Rules)
    │
 Ngày 4: Vòng đời Container & Lệnh Giao Nhận DO (ContainerVisit State Machine, Gate EIR, DeliveryOrder)
    │
 Ngày 5: Tầng Application - CQRS Handlers & Validation (Vertical Slices: GateIn, GateOut, Sơ đồ bãi, Báo cáo)
    │
 Ngày 6: Tầng Infrastructure - EF Core 10, Multi-Tenancy & HybridCache (AppDbContext, Seeder Cát Lái)
    │
 Ngày 7: Tầng Api, Scalar Docs, Architecture Tests & Kiểm thử Toàn diện End-to-End
```

---

### NGÀY 1 - TASK 1: SETUP NỀN TẢNG SOLUTION & DOMAIN PRIMITIVES

**Mục tiêu học tập & làm việc**:
- Hiểu cách tổ chức Solution Clean Architecture chuẩn với `.slnx`, `Directory.Build.props`, `Directory.Packages.props`.
- Nắm vững kiến trúc xử lý lỗi không dùng Exception: **Result Pattern** (`Result<T>`, `Error`).
- Nắm vững cơ chế đóng gói luật nghiệp vụ của Mentor: **`IBusinessRule`** và **`BusinessRuleValidator`**.

**File tạo mới / chỉnh sửa**:
- Tạo: `Directory.Build.props` (Cấu hình C# 13/14, net10.0, Nullable, ImplicitUsings)
- Tạo: `Directory.Packages.props` (Khai báo tập trung các package EF Core 10, xUnit v3, FluentValidation, NetArchTest)
- Tạo: `src/Depot.CleanArchitecture.Domain/Common/BaseEntity.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Common/AuditableEntity.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Common/ITenantEntity.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Common/Error.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Common/Result.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Common/Rules/IBusinessRule.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Common/Rules/BusinessRuleException.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Common/Rules/BusinessRuleValidator.cs`
- Test: `tests/Depot.CleanArchitecture.Domain.UnitTests/Common/ResultTests.cs`
- Test: `tests/Depot.CleanArchitecture.Domain.UnitTests/Common/BusinessRuleValidatorTests.cs`

#### Các bước thực hiện chi tiết:

- [ ] **Bước 1.1: Tạo Directory.Build.props và Directory.Packages.props**
  Sao chép cấu hình quản lý package tập trung tương tự `TechSpherex.CleanArchitecture` với các package: `Microsoft.EntityFrameworkCore (10.0.5)`, `FluentValidation (12.1.1)`, `Microsoft.Extensions.Caching.Hybrid (10.4.0)`, `Scalar.AspNetCore (2.13.14)`, `xunit.v3 (3.2.2)`, `FluentAssertions (8.9.0)`, `NetArchTest.Rules (1.3.2)`.

- [ ] **Bước 1.2: Triển khai BaseEntity, AuditableEntity, ITenantEntity**
  - `BaseEntity`: chứa thuộc tính `public Guid Id { get; protected set; } = Guid.NewGuid();`.
  - `AuditableEntity : BaseEntity`: bổ sung `CreatedAt`, `LastModifiedAt`, `CreatedBy`, `LastModifiedBy`.
  - `ITenantEntity`: interface với `Guid TenantId { get; set; }`.

- [ ] **Bước 1.3: Triển khai Result Pattern (`Error.cs` và `Result.cs`)**
  - `Error`: chứa `Code`, `Message`, `ErrorType` (Failure, Validation, NotFound, Conflict).
  - `Result<TValue>`: chứa `IsSuccess`, `IsFailure`, `Value`, `Error`.

- [ ] **Bước 1.4: Triển khai Domain Business Rules (`IBusinessRule.cs`, `BusinessRuleValidator.cs`)**
  - `IBusinessRule`:
    ```csharp
    public interface IBusinessRule
    {
        string RuleCode { get; }
        string Message { get; }
        int Priority => 0;
        bool IsBroken();
    }
    ```
  - `BusinessRuleValidator`: phương thức tĩnh `CheckRule(IBusinessRule rule)` ném `BusinessRuleException` nếu `rule.IsBroken() == true`.

- [ ] **Bước 1.5: Viết Unit Test và chạy kiểm thử Ngày 1**
  Tạo project test `Depot.CleanArchitecture.Domain.UnitTests`. Viết test kiểm tra `Result.Success`, `Result.Failure` và `BusinessRuleValidator.CheckRule`.
  Lệnh chạy: `dotnet test tests/Depot.CleanArchitecture.Domain.UnitTests`
  Kết quả mong đợi: Toàn bộ pass 100%.

---

### NGÀY 2 - TASK 2: DOMAIN MODEL CHO CONTAINER & THUẬT TOÁN MODULO 11

**Mục tiêu học tập & làm việc**:
- Hiểu khái niệm **Value Object** trong DDD: Tính bất biến (Immutability), tự kiểm tra tính hợp lệ khi khởi tạo.
- Hiện thực hóa thuật toán kiểm tra số container chuẩn quốc tế **ISO 6346 Modulo 11** (xử lý chính xác trường hợp số dư bằng 10 ra `'X'`).
- Thiết lập hồ sơ kỹ thuật container và phân hạng chất lượng vỏ (Grade A, B, C, D, E).

**File tạo mới / chỉnh sửa**:
- Tạo: `src/Depot.CleanArchitecture.Domain/Common/ValueObject.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/ValueObjects/ContainerNumber.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Enums/ContainerType.cs` (Dry, Reefer, OpenTop, FlatRack, Tank, Ventilated)
- Tạo: `src/Depot.CleanArchitecture.Domain/Enums/ContainerGrade.cs` (GradeA, GradeB, GradeC, GradeD_Damaged, GradeE_Scrap)
- Tạo: `src/Depot.CleanArchitecture.Domain/Entities/Container.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Rules/Containers/ContainerCheckDigitRule.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Rules/Containers/ContainerPayloadMustBePositiveRule.cs`
- Test: `tests/Depot.CleanArchitecture.Domain.UnitTests/ValueObjects/ContainerNumberTests.cs`
- Test: `tests/Depot.CleanArchitecture.Domain.UnitTests/Entities/ContainerTests.cs`

#### Các bước thực hiện chi tiết:

- [ ] **Bước 2.1: Triển khai Value Object `ContainerNumber` với thuật toán Modulo 11**
  ```csharp
  public sealed class ContainerNumber : ValueObject
  {
      public string Value { get; }
      public string OwnerCode => Value[..3];
      public char CategoryIdentifier => Value[3];
      public string SerialNumber => Value.Substring(4, 6);
      public char CheckDigit => Value[10];

      public static Result<ContainerNumber> Create(string raw) { ... }
      public static bool ValidateModulo11(string raw) { ... }
  }
  ```
  Xây dựng bảng mã chữ cái: $A=10, B=12 \dots Z=38$ (bỏ qua 11, 22, 33). Nhân trọng số $2^{i-1}$, lấy tổng $\pmod{11}$. Nếu dư 10 thì `CheckDigit == 'X'`.

- [ ] **Bước 2.2: Triển khai Entity `Container`**
  Kế thừa `AuditableEntity, ITenantEntity`. Thuộc tính:
  - `ContainerNumber Number { get; private set; }`
  - `string LineOperator { get; private set; }`
  - `ContainerType Type { get; private set; }`
  - `string IsoCode { get; private set; }`
  - `int Size { get; private set; }` (20, 40, 45)
  - `decimal TareWeight { get; private set; }`
  - `decimal MaxGrossWeight { get; private set; }`
  - `ContainerGrade Grade { get; private set; }`
  - Phương thức: `UpdateGrade(...)`, `UpdateDamageDetails(...)`.

- [ ] **Bước 2.3: Viết Unit Test cho ContainerNumber**
  Kiểm tra các số cont thực tế:
  - `CMAU1234567` (giả định tính đúng check digit)
  - `MSKU0123456`
  - Trường hợp phần dư ra 10 ('X')
  - Trường hợp số cont sai độ dài, chứa ký tự đặc biệt $\rightarrow$ Báo lỗi `Result.Failure`.

- [ ] **Bước 2.4: Chạy kiểm thử Ngày 2**
  Lệnh chạy: `dotnet test tests/Depot.CleanArchitecture.Domain.UnitTests`
  Kết quả: Tất cả test Value Object và Entity Container đều PASS.

---

### NGÀY 3 - TASK 3: QUẢN LÝ BÃI CONTAINER (YARD TOPOLOGY & COLLISION ENGINE)

**Mục tiêu học tập & làm việc**:
- Hiểu mô hình không gian bãi 3 chiều: `Block - Bay - Row - Tier`.
- Viết thuật toán then chốt: **Khóa và ghép Bay chẵn 40ft với 2 Bay lẻ 20ft liền kề**.
- Thiết lập quy tắc an toàn xếp tầng (Stacking rules: không xếp cont 20ft lên cont 40ft; kiểm tra tầng đỡ bên dưới).
- Hiện thực hóa mô hình **Block ảo** (Xưởng M&R, Sân rửa, Bãi đệm cổng).

**File tạo mới / chỉnh sửa**:
- Tạo: `src/Depot.CleanArchitecture.Domain/ValueObjects/SlotCoordinate.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Entities/YardBlock.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Entities/YardSlot.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Rules/Yard/EvenOddBayCollisionRule.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Rules/Yard/TierMustHaveBottomSupportRule.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Rules/Yard/SizeStackingSafetyRule.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Rules/Yard/BlockMaxTierExceededRule.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Rules/Yard/VirtualBlockCapacityRule.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Rules/Yard/ReeferYardAssignmentRule.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Rules/Yard/OverheightStackingRule.cs`
- Test: `tests/Depot.CleanArchitecture.Domain.UnitTests/Yard/YardCollisionTests.cs`
- Test: `tests/Depot.CleanArchitecture.Domain.UnitTests/Yard/YardStackingSafetyTests.cs`

#### Các bước thực hiện chi tiết:

- [ ] **Bước 3.1: Triển khai Value Object `SlotCoordinate`**
  Chứa `(int Bay, int Row, int Tier)`. Bổ sung các helper methods:
  - `bool IsOddBay => Bay % 2 != 0;` (Cont 20ft)
  - `bool IsEvenBay => Bay % 2 == 0;` (Cont 40ft)
  - `(int Bay1, int Bay2) GetUnderlyingOddBays()`: Nếu Bay chẵn 02 $\rightarrow$ trả về (01, 03).
  - `int GetOverlyingEvenBay()`: Nếu Bay 01 hoặc 03 $\rightarrow$ trả về Bay 02.

- [ ] **Bước 3.2: Triển khai Entity `YardBlock` và `YardSlot`**
  - `YardBlock`: `BlockCode`, `Name`, `MaxBay`, `MaxRow`, `MaxTier`, `IsVirtual`, `MaxCapacity`, `HasReeferPower`.
    Hỗ trợ phương thức: `AddSlot(...)`, `Resize(...)`.
  - `YardSlot`: `BlockId`, `Bay`, `Row`, `Tier`, `IsOccupied`, `CurrentContainerId`.

- [ ] **Bước 3.3: Triển khai Business Rule va chạm Bay chẵn/lẻ (`EvenOddBayCollisionRule`)**
  Kiểm tra danh sách các slot đang có cont trong cùng Row, Tier:
  - Nếu hạ cont 40ft vào Bay chẵn: Báo lỗi nếu Bay lẻ trước hoặc sau đã bị chiếm dụng.
  - Nếu hạ cont 20ft vào Bay lẻ: Báo lỗi nếu Bay chẵn tương ứng đang có cont 40ft.

- [ ] **Bước 3.4: Triển khai các Stacking Rules & Virtual Block Rules**
  - `TierMustHaveBottomSupportRule`: Nếu Tier > 1, bắt buộc slot tại Tier - 1 phải `IsOccupied == true`.
  - `SizeStackingSafetyRule`: Nếu cont hạ là 20ft, cấm hạ nếu cont ở tầng dưới là 40ft!
  - `VirtualBlockCapacityRule`: Kiểm tra `CurrentCount < MaxCapacity`.

- [ ] **Bước 3.5: Viết Unit Test và chạy kiểm thử Ngày 3**
  Viết test case mô phỏng bãi Block A:
  - Đặt cont 20ft ở Bay 01, Row 01, Tier 01 $\rightarrow$ Thử đặt cont 40ft ở Bay 02, Row 01, Tier 01 $\rightarrow$ Kỳ vọng ném `BusinessRuleException` với mã `Yard.EvenOddBayCollision`.
  - Thử đặt cont ở Tier 2 khi Tier 1 trống $\rightarrow$ Kỳ vọng ném lỗi `Yard.TierMissingBottomSupport`.
  - Chạy `dotnet test tests/Depot.CleanArchitecture.Domain.UnitTests` $\rightarrow$ PASS.

---

### NGÀY 4 - TASK 4: VÒNG ĐỜI CONTAINER & LỆNH GIAO CONTAINER (DO)

**Mục tiêu học tập & làm việc**:
- Thiết kế **ContainerVisit** theo mô hình State Machine: `GatedIn -> StackedInYard -> Allocated -> GatedOut -> Completed`.
- Xây dựng mô hình chứng từ Cổng (EIR) ghi nhận biển số xe đầu kéo, rơ-moóc, tài xế, kết quả giám định vỏ.
- Xây dựng **DeliveryOrder (DO)** và bộ quy tắc kiểm tra xuất bãi (Hạn lệnh, Hãng tàu, Quota, Grade).

**File tạo mới / chỉnh sửa**:
- Tạo: `src/Depot.CleanArchitecture.Domain/Enums/ContainerVisitStatus.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Enums/DeliveryOrderStatus.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/ValueObjects/VehicleInfo.cs` (TractorNo, TrailerNo, DriverName, DriverPhone)
- Tạo: `src/Depot.CleanArchitecture.Domain/Entities/ContainerVisit.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Entities/DeliveryOrder.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Entities/DeliveryOrderItem.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Rules/Orders/DeliveryOrderNotExpiredRule.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Rules/Orders/ContainerLineOperatorMatchesOrderRule.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Rules/Orders/ContainerMatchesOrderSpecificationRule.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Rules/Orders/DeliveryOrderQuotaRemainingRule.cs`
- Tạo: `src/Depot.CleanArchitecture.Domain/Rules/Orders/DamagedContainerCannotBeAllocatedRule.cs`
- Test: `tests/Depot.CleanArchitecture.Domain.UnitTests/Orders/DeliveryOrderRuleTests.cs`
- Test: `tests/Depot.CleanArchitecture.Domain.UnitTests/Visits/ContainerVisitLifecycleTests.cs`

#### Các bước thực hiện chi tiết:

- [ ] **Bước 4.1: Triển khai Aggregate Root `ContainerVisit`**
  Chứa trạng thái vòng đời, ngày giờ Gate-In, Gate-Out, thông tin xe ra/vào, vị trí slot hiện tại.
  Các phương thức chuyển đổi trạng thái:
  - `GateIn(VehicleInfo vehicle, ContainerGrade grade, string damageNotes)`
  - `AssignSlot(Guid slotId)`
  - `ShiftSlot(Guid newSlotId)`
  - `AllocateToOrder(Guid deliveryOrderId)`
  - `GateOut(VehicleInfo vehicle, Guid deliveryOrderId)`

- [ ] **Bước 4.2: Triển khai `DeliveryOrder` và `DeliveryOrderItem`**
  - `DeliveryOrder`: `OrderNumber`, `LineOperator`, `CustomerName`, `CustomerTaxCode`, `ExpirationDate`, `VesselName`, `VoyageNo`, `Status`.
  - `DeliveryOrderItem`: `Size` (20/40), `ContainerType`, `RequiredGrade`, `OrderedQuantity`, `DeliveredQuantity`.
  - Phương thức: `AllocateContainer(...)`, `RecordDeliveredContainer(...)`, `ExtendExpirationDate(DateTime newDate, string reason)`.

- [ ] **Bước 4.3: Triển khai các Business Rules kiểm tra Lệnh DO**
  - `DeliveryOrderNotExpiredRule`: `CurrentDate <= ExpirationDate`.
  - `ContainerLineOperatorMatchesOrderRule`: `Container.LineOperator == Order.LineOperator`.
  - `DamagedContainerCannotBeAllocatedRule`: Cấm gán cont Grade D (hư hỏng) hoặc E (phế thải).
  - `DeliveryOrderQuotaRemainingRule`: `DeliveredQuantity < OrderedQuantity`.

- [ ] **Bước 4.4: Viết Unit Test và chạy kiểm thử Ngày 4**
  - Test case: Thử Gate-Out với lệnh hết hạn hôm qua $\rightarrow$ Kiểm tra chặn thành công.
  - Test case: Thử gán cont hãng MAERSK cho lệnh của hãng CMA $\rightarrow$ Kiểm tra chặn thành công.
  - Test case: Thử cấp cont Grade D cho khách $\rightarrow$ Kiểm tra chặn thành công.
  - Chạy `dotnet test tests/Depot.CleanArchitecture.Domain.UnitTests` $\rightarrow$ PASS.

---

### NGÀY 5 - TASK 5: TẦNG APPLICATION - CQRS HANDLERS & BÁO CÁO THỐNG KÊ

**Mục tiêu học tập & làm việc**:
- Hiểu kiến trúc **Manual CQRS**: Tách bạch Command (thay đổi dữ liệu) và Query (đọc dữ liệu không sinh side-effect).
- Xây dựng các Use Cases (Vertical Slices) thực tế cho người vận hành bãi cảng.
- Viết câu truy vấn thống kê **Tồn bãi Aging (0-10 ngày, >=10 ngày)** và **Sản lượng xuất nhập theo ngày**.

**File tạo mới / chỉnh sửa**:
- Tạo: `src/Depot.CleanArchitecture.Application/Abstractions/Messaging/ICommand.cs`
- Tạo: `src/Depot.CleanArchitecture.Application/Abstractions/Messaging/ICommandHandler.cs`
- Tạo: `src/Depot.CleanArchitecture.Application/Abstractions/Messaging/IQuery.cs`
- Tạo: `src/Depot.CleanArchitecture.Application/Abstractions/Messaging/IQueryHandler.cs`
- Tạo: `src/Depot.CleanArchitecture.Application/Abstractions/Data/IAppDbContext.cs`
- Tạo: `src/Depot.CleanArchitecture.Application/Features/Gate/Commands/GateIn/` (Command, Handler, Validator)
- Tạo: `src/Depot.CleanArchitecture.Application/Features/Gate/Commands/GateOut/` (Command, Handler, Validator)
- Tạo: `src/Depot.CleanArchitecture.Application/Features/Yard/Commands/PlaceContainer/` (Command, Handler)
- Tạo: `src/Depot.CleanArchitecture.Application/Features/Orders/Commands/CreateOrder/` (Command, Handler)
- Tạo: `src/Depot.CleanArchitecture.Application/Features/Orders/Commands/ExtendOrder/` (Command, Handler)
- Tạo: `src/Depot.CleanArchitecture.Application/Features/Analytics/Queries/GetAgingReport/` (Query, Handler, DTO)
- Tạo: `src/Depot.CleanArchitecture.Application/Features/Analytics/Queries/GetDailyThroughputReport/` (Query, Handler, DTO)
- Test: `tests/Depot.CleanArchitecture.Application.UnitTests/Features/Gate/GateInCommandHandlerTests.cs`
- Test: `tests/Depot.CleanArchitecture.Application.UnitTests/Features/Analytics/GetAgingReportQueryHandlerTests.cs`

#### Các bước thực hiện chi tiết:

- [ ] **Bước 5.1: Xây dựng Abstractions cho Manual CQRS và IAppDbContext**
  - Tự định nghĩa `ICommand<TResult>`, `ICommandHandler<TCommand, TResult>`, `IQuery<TResult>`, `IQueryHandler<TQuery, TResult>`.
  - `IAppDbContext`: Khai báo các `DbSet<YardBlock>`, `DbSet<YardSlot>`, `DbSet<Container>`, `DbSet<ContainerVisit>`, `DbSet<DeliveryOrder>`, phương thức `SaveChangesAsync(...)`.

- [ ] **Bước 5.2: Triển khai Gate-In Command Handler & Gate-Out Command Handler**
  - `GateInCommandHandler`: Tạo `Container` (nếu chưa có), tạo `ContainerVisit` ở trạng thái `GatedIn`, lưu biên bản giám định vỏ.
  - `GateOutCommandHandler`: Tìm `DeliveryOrder`, kiểm tra toàn bộ 5 business rules, cập nhật `ContainerVisit` thành `GatedOut`, giải phóng `YardSlot` thành trống (`IsOccupied = false`), cập nhật `DeliveredQuantity` của DO.

- [ ] **Bước 5.3: Triển khai Query Handlers Báo cáo Thống kê**
  - `GetAgingReportQueryHandler`: Dùng LINQ nhóm theo `LineOperator`, tính số ngày tồn bãi bằng chênh lệch `GateInDate` với ngày hiện tại:
    - Nhóm `0-10 ngày`: `DateDiffDay <= 10`
    - Nhóm `>10 ngày`: `DateDiffDay > 10`
  - `GetDailyThroughputReportQueryHandler`: Nhóm theo `Date` và `LineOperator`, đếm lượt nhập/xuất và tính tổng TEU.

- [ ] **Bước 5.4: Viết Unit Test cho Handlers Ngày 5**
  Tạo project test `Depot.CleanArchitecture.Application.UnitTests`. Dùng EF Core In-Memory để test:
  - Test xử lý Gate-In thành công lưu đúng dữ liệu.
  - Test xử lý báo cáo Aging phân chia đúng 2 nhóm 0-10 ngày và >10 ngày.
  - Lệnh chạy: `dotnet test tests/Depot.CleanArchitecture.Application.UnitTests` $\rightarrow$ PASS.

---

### NGÀY 6 - TASK 6: TẦNG INFRASTRUCTURE - EF CORE 10, CACHING & MULTI-TENANCY

**Mục tiêu học tập & làm việc**:
- Thiết lập **AppDbContext** trong EF Core 10 với Fluent API Configurations hoàn chỉnh.
- Cấu hình **Shared-table Multi-Tenancy**: Mỗi Depot (Cát Lái, Hiệp Phước...) có một `TenantId` riêng biệt, EF Core tự động lọc dữ liệu qua Global Query Filter.
- Cấu hình **HybridCache** (.NET 10) để cache cấu hình bãi và danh mục Hãng tàu.
- Xây dựng **AppDbSeeder** nạp dữ liệu mẫu ban đầu mô phỏng thực tế Depot Cát Lái.

**File tạo mới / chỉnh sửa**:
- Tạo: `src/Depot.CleanArchitecture.Infrastructure/Persistence/AppDbContext.cs`
- Tạo: `src/Depot.CleanArchitecture.Infrastructure/Persistence/Configurations/YardBlockConfiguration.cs`
- Tạo: `src/Depot.CleanArchitecture.Infrastructure/Persistence/Configurations/YardSlotConfiguration.cs`
- Tạo: `src/Depot.CleanArchitecture.Infrastructure/Persistence/Configurations/ContainerConfiguration.cs`
- Tạo: `src/Depot.CleanArchitecture.Infrastructure/Persistence/Configurations/ContainerVisitConfiguration.cs`
- Tạo: `src/Depot.CleanArchitecture.Infrastructure/Persistence/Configurations/DeliveryOrderConfiguration.cs`
- Tạo: `src/Depot.CleanArchitecture.Infrastructure/Tenancy/TenantProvider.cs`
- Tạo: `src/Depot.CleanArchitecture.Infrastructure/Tenancy/TenantMiddleware.cs`
- Tạo: `src/Depot.CleanArchitecture.Infrastructure/Caching/HybridCacheService.cs`
- Tạo: `src/Depot.CleanArchitecture.Infrastructure/Persistence/AppDbSeeder.cs`
- Tạo: `src/Depot.CleanArchitecture.Infrastructure/DependencyInjection.cs`

#### Các bước thực hiện chi tiết:

- [ ] **Bước 6.1: Viết Entity Configurations trong EF Core 10**
  - Cấu hình Value Conversion cho `ContainerNumber` (chuyển đổi hai chiều giữa Value Object và cột `varchar(11)`).
  - Cấu hình composite indexes cho tọa độ slot `(TenantId, BlockId, Bay, Row, Tier)`.
  - Cấu hình quan hệ 1-n giữa `YardBlock` và `YardSlot`, giữa `DeliveryOrder` và `DeliveryOrderItems`.

- [ ] **Bước 6.2: Cấu hình Multi-Tenancy với Global Query Filter**
  Trong `AppDbContext.OnModelCreating`:
  ```csharp
  foreach (var entityType in modelBuilder.Model.GetEntityTypes())
  {
      if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
      {
          // Tự động thêm điều kiện e.TenantId == _tenantProvider.GetCurrentTenantId()
      }
  }
  ```
  Trong `SaveChangesAsync`: Tự động gán `TenantId` cho các entity mới tạo.

- [ ] **Bước 6.3: Triển khai HybridCacheService**
  Đăng ký `AddHybridCache` của .NET 10. Cache sơ đồ bãi `GetYardMap` với thời gian hết hạn (expiration) 30 phút, hỗ trợ invalidate khi có xe hạ bãi hoặc chuyển slot.

- [ ] **Bước 6.4: Tạo Seeder dữ liệu mẫu cho Depot Cát Lái**
  Tạo sẵn:
  - 1 Depot Tenant: `Depot Tân Cảng Cát Lái`
  - 2 Block thường: Block A (10 bay, 6 row, 5 tier), Block B (có giàn điện Reefer)
  - 1 Block ảo: `BLK-MR` (Xưởng sửa chữa M&R, Capacity = 50 cont)
  - 1 Lệnh DO mẫu của hãng `MAERSK` cấp 5 cont 20DC Grade A.
  - 10 container mẫu với số cont chuẩn Modulo 11 để sẵn sàng chạy thử.

---

### NGÀY 7 - TASK 7: TẦNG API, SCALAR DOCS, ARCHITECTURE TESTS & END-TO-END

**Mục tiêu học tập & làm việc**:
- Xây dựng các Minimal API Endpoints phân nhóm theo `RouteGroupBuilder`.
- Tích hợp tài liệu API hiện đại **Scalar** thay thế Swagger UI theo chuẩn của mentor.
- Viết bộ kiểm thử kiến trúc **NetArchTest** để đảm bảo tuân thủ 100% Dependency Rule của Clean Architecture.
- Chạy kiểm thử End-to-End toàn bộ luồng nghiệp vụ từ Cổng vào $\rightarrow$ Hạ bãi $\rightarrow$ Cấp lệnh $\rightarrow$ Cổng ra $\rightarrow$ Xem báo cáo.

**File tạo mới / chỉnh sửa**:
- Tạo: `src/Depot.CleanArchitecture.Api/Endpoints/YardEndpoints.cs`
- Tạo: `src/Depot.CleanArchitecture.Api/Endpoints/ContainerEndpoints.cs`
- Tạo: `src/Depot.CleanArchitecture.Api/Endpoints/GateEndpoints.cs`
- Tạo: `src/Depot.CleanArchitecture.Api/Endpoints/OrderEndpoints.cs`
- Tạo: `src/Depot.CleanArchitecture.Api/Endpoints/AnalyticsEndpoints.cs`
- Tạo: `src/Depot.CleanArchitecture.Api/Extensions/GlobalExceptionHandler.cs`
- Tạo: `src/Depot.CleanArchitecture.Api/Extensions/ResultExtensions.cs`
- Tạo: `src/Depot.CleanArchitecture.Api/Program.cs`
- Tạo: `tests/Depot.CleanArchitecture.Architecture.Tests/ArchitectureTests.cs`

#### Các bước thực hiện chi tiết:

- [ ] **Bước 7.1: Triển khai Minimal API Endpoints**
  - `/api/gate/in` [POST]: Làm thủ tục xe chở cont vào bãi (Gate-In & EIR).
  - `/api/gate/out` [POST]: Kiểm tra lệnh DO và làm thủ tục xuất cont (Gate-Out).
  - `/api/yard/blocks` [GET/POST]: Lấy sơ đồ bãi / Tạo block bãi.
  - `/api/yard/place` [POST]: Điều xe nâng hạ cont vào slot.
  - `/api/orders` [POST]: Tạo lệnh giao cont của Hãng tàu.
  - `/api/orders/{id}/extend` [PUT]: Gia hạn lệnh DO quá hạn.
  - `/api/analytics/aging` [GET]: Xem báo cáo tồn bãi (0-10 ngày, >=10 ngày).
  - `/api/analytics/throughput` [GET]: Xem báo cáo sản lượng xuất/nhập theo ngày.

- [ ] **Bước 7.2: Tích hợp Scalar API Documentation**
  Cấu hình trong `Program.cs`:
  ```csharp
  app.MapOpenApi();
  app.MapScalarApiReference(options => {
      options.Title = "SNP Depot Management System API (Clean Architecture)";
      options.Theme = ScalarTheme.Mars;
  });
  ```

- [ ] **Bước 7.3: Viết Architecture Tests với NetArchTest**
  Kiểm tra nghiêm ngặt 3 quy tắc:
  1. Tầng `Domain` không được tham chiếu `Application`, `Infrastructure`, `Api`.
  2. Tầng `Application` không được tham chiếu `Infrastructure` hoặc `Api`.
  3. Mọi Command Handler phải kế thừa `ICommandHandler`.
  Chạy test: `dotnet test tests/Depot.CleanArchitecture.Architecture.Tests` $\rightarrow$ PASS.

- [ ] **Bước 7.4: Chạy kiểm thử End-to-End toàn hệ thống**
  Khởi động ứng dụng, truy cập giao diện Scalar:
  1. Gọi API Gate-In container `CMAU1234567`.
  2. Gọi API Place đặt vào Block A, Bay 01, Row 01, Tier 01.
  3. Thử đặt cont 40ft vào Bay 02 cùng Row/Tier $\rightarrow$ Xác nhận hệ thống chặn báo lỗi va chạm.
  4. Tạo Lệnh DO của hãng CMA.
  5. Gọi API Gate-Out cont `CMAU1234567` theo Lệnh DO $\rightarrow$ Xuất thành công, slot bãi được giải phóng.
  6. Gọi API Báo cáo Aging & Sản lượng $\rightarrow$ Xác nhận số liệu hiển thị chuẩn xác.

---

## TỔNG KẾT & PHƯƠNG THỨC THỰC THI

Kế hoạch 7 ngày này bao quát toàn diện từ nền tảng OOP, SOLID, Clean Architecture đến từng bài toán nghiệp vụ thực tế của Tân Cảng Sài Gòn.

Bạn có thể lựa chọn 1 trong 2 phương thức thực thi theo chuẩn Superpowers:
1. **Native (Khuyến nghị cho trường hợp vừa làm vừa học)**: Tôi sẽ trực tiếp cùng bạn đồng hành, viết code và giải thích từng bước trong phiên làm việc này. Bạn theo dõi từng file được tạo và hiểu sâu sắc lý do tại sao lại code như vậy.
2. **Subagent-driven**: Phân chia cho các subagent thực thi độc lập và review từng task.
