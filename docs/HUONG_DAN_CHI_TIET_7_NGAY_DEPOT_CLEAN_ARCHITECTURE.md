# GIÁO TRÌNH TRIỂN KHAI DỰ ÁN DEPOT MANAGEMENT SYSTEM (SNP)
## CLEAN ARCHITECTURE 4 TẦNG & BUSINESS RULES CHUYÊN SÂU (.NET 10)
**Thời lượng**: Lộ trình 7 Ngày (Vừa Làm Vừa Học - Hands-on Learning)  
**Dành cho**: Kỹ sư Phần mềm / Thực tập sinh Backend .NET  
**Dự án mẫu tham chiếu**: `TechSpherex.CleanArchitecture` (Mentor Repository)  
**Tiêu chuẩn**: Tân Cảng Sài Gòn (SNP TOS/DMS Standard)  

---

## MỤC LỤC TỔNG QUAN

1. [Lộ trình Tổng quan 7 Ngày & Chuẩn Doanh nghiệp](#1-lộ-trình-tổng-quan-7-ngày--chuẩn-doanh-nghiệp)
2. [NGÀY 1: Setup Nền tảng Solution & Domain Primitives (Shared Kernel)](#ngày-1-setup-nền-tảng-solution--domain-primitives-shared-kernel)
3. [NGÀY 2: Hồ sơ Container & Thuật toán Quốc tế ISO 6346 Modulo 11](#ngày-2-hồ-sơ-container--thuật-toán-quốc-tế-iso-6346-modulo-11)
4. [NGÀY 3: Không gian Bãi (Yard Topology), Thuật toán Xung đột Bay 20ft/40ft & Block Ảo](#ngày-3-không-gian-bãi-yard-topology-thuật-toán-xung-đột-bay-20ft40ft--block-ảo)
5. [NGÀY 4: Vòng đời Container (Lifecycle) & Ràng buộc Lệnh Giao Nhận (Delivery Order)](#ngày-4-vòng-đời-container-lifecycle--ràng-buộc-lệnh-giao-nhận-delivery-order)
6. [NGÀY 5: Tầng Application - Manual CQRS & Báo cáo Thống kê Nghiệp vụ](#ngày-5-tầng-application---manual-cqrs--báo-cáo-thống-kê-nghiệp-vụ)
7. [NGÀY 6: Tầng Infrastructure - EF Core 10, Multi-Tenancy & HybridCache](#ngày-6-tầng-infrastructure---ef-core-10-multi-tenancy--hybridcache)
8. [NGÀY 7: Tầng Api, Scalar OpenAPI, Architecture Tests & Kịch bản End-to-End](#ngày-7-tầng-api-scalar-openapi-architecture-tests--kịch-bản-end-to-end)
9. [Bộ Câu Hỏi & Trả Lời Vấn Đáp Kỹ Thuật Khi Báo Cáo Mentor](#9-bộ-câu-hỏi--trả-lời-vấn-đáp-kỹ-thuật-khi-báo-cáo-mentor)

---

## 1. LỘ TRÌNH TỔNG QUAN 7 NGÀY & CHUẨN DOANH NGHIỆP

### 1.1. Mục tiêu cốt lõi
Giáo trình này được thiết kế theo phương pháp **"Vừa Làm Vừa Học" (Learn by Doing)**, giúp bạn chuyển hóa từ một sinh viên/thực tập sinh mới làm quen với C# thành một lập trình viên nắm vững tư duy kiến trúc doanh nghiệp (**Enterprise Clean Architecture**) và nghiệp vụ cảng biển quốc tế.

```
       NGÀY 1                  NGÀY 2                  NGÀY 3                  NGÀY 4
 ┌────────────────┐      ┌────────────────┐      ┌────────────────┐      ┌────────────────┐
 │ Nền tảng Core  │ ───► │ Container &    │ ───► │ Yard Topology  │ ───► │ Vòng đời Visit │
 │ Result, Rules, │      │ Modulo 11,     │      │ Bay 20/40ft,   │      │ & Lệnh DO của  │
 │ Base Entities  │      │ Grading A - E  │      │ Block Ảo M&R   │      │ Hãng Tàu       │
 └────────────────┘      └────────────────┘      └────────────────┘      └────────────────┘
                                                                                 │
                                                                                 ▼
       NGÀY 7                  NGÀY 6                  NGÀY 5                    │
 ┌────────────────┐      ┌────────────────┐      ┌────────────────┐              │
 │ Minimal APIs,  │ ◄─── │ Infrastructure │ ◄─── │ Application    │ ◄────────────┘
 │ Scalar Docs,   │      │ EF Core 10,    │      │ Manual CQRS,   │
 │ NetArchTest    │      │ Multi-Tenancy  │      │ Báo cáo Aging  │
 └────────────────┘      └────────────────┘      └────────────────┘
```

### 1.2. Quy tắc Phụ thuộc Bất di bất dịch (The Dependency Rule)
> **Tầng bên ngoài phụ thuộc vào tầng bên trong. Tầng bên trong TUYỆT ĐỐI KHÔNG BIẾT GÌ về tầng bên ngoài.**

1. **Domain (Lõi trung tâm)**: Chứa thực thể, logic nghiệp vụ thuần túy, không chứa code kết nối cơ sở dữ liệu hay thư viện bên ngoài.
2. **Application (Điều phối nghiệp vụ)**: Chứa Use Cases (CQRS Commands, Queries), đóng vai trò nhạc trưởng điều phối các thực thể domain.
3. **Infrastructure (Hạ tầng kỹ thuật)**: Hiện thực hóa việc lưu trữ cơ sở dữ liệu (EF Core), lưu bộ nhớ đệm (HybridCache), gọi dịch vụ ngoài.
4. **Api (Điểm tiếp nhận yêu cầu)**: Cung cấp Minimal APIs, chuyển đổi HTTP request thành Command/Query và trả về HTTP response.

---

## NGÀY 1: SETUP NỀN TẢNG SOLUTION & DOMAIN PRIMITIVES (SHARED KERNEL)

### 1.1. Kiến thức lý thuyết nền tảng
- **Tại sao không dùng Exception cho luồng nghiệp vụ thông thường?**
  Việc ném ngoại lệ (`throw new Exception()`) tốn rất nhiều tài nguyên CPU (thu thập stack trace) và tạo ra luồng điều khiển ẩn (hidden control flow). Doanh nghiệp sử dụng **Result Pattern** (`Result<T>`): hàm trả về một đối tượng chứa kết quả thành công hoặc mã lỗi rõ ràng.
- **Tại sao dùng Domain Business Rules (`IBusinessRule`)?**
  Thay vì viết hàng chục câu lệnh `if-else` lộn xộn trong Controller hoặc Service, ta đóng gói từng luật nghiệp vụ thành một Class độc lập kế thừa `IBusinessRule`. Cách này tuân thủ nguyên lý **Single Responsibility (SRP)** và giúp viết Unit Test độc lập 100%.

### 1.2. Chi tiết các File cần tạo & Mã nguồn chuẩn

#### 1. File `Directory.Build.props` (Gốc Solution)
Quản lý cấu hình biên dịch thống nhất cho toàn bộ solution:
```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>false</TreatWarningsAsErrors>
  </PropertyGroup>
</Project>
```

#### 2. File `Directory.Packages.props` (Gốc Solution)
Quản lý phiên bản thư viện tập trung (Central Package Management):
```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup Label="EF Core">
    <PackageVersion Include="Microsoft.EntityFrameworkCore" Version="10.0.5" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.5" />
    <PackageVersion Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.1" />
  </ItemGroup>
  <ItemGroup Label="Validation & Caching">
    <PackageVersion Include="FluentValidation" Version="12.1.1" />
    <PackageVersion Include="Microsoft.Extensions.Caching.Hybrid" Version="10.4.0" />
  </ItemGroup>
  <ItemGroup Label="API & Docs">
    <PackageVersion Include="Scalar.AspNetCore" Version="2.13.14" />
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.5" />
  </ItemGroup>
  <ItemGroup Label="Testing">
    <PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.3.0" />
    <PackageVersion Include="xunit.v3" Version="3.2.2" />
    <PackageVersion Include="FluentAssertions" Version="8.9.0" />
    <PackageVersion Include="NetArchTest.Rules" Version="1.3.2" />
    <PackageVersion Include="Microsoft.EntityFrameworkCore.InMemory" Version="10.0.5" />
  </ItemGroup>
</Project>
```

#### 3. Bộ primitives trong `src/Depot.CleanArchitecture.Domain/Common/`
- **`BaseEntity.cs`**:
  ```csharp
  namespace Depot.CleanArchitecture.Domain.Common;

  public abstract class BaseEntity
  {
      public Guid Id { get; protected set; } = Guid.NewGuid();
  }
  ```
- **`AuditableEntity.cs`**:
  ```csharp
  namespace Depot.CleanArchitecture.Domain.Common;

  public abstract class AuditableEntity : BaseEntity
  {
      public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
      public string? CreatedBy { get; set; }
      public DateTime? LastModifiedAt { get; set; }
      public string? LastModifiedBy { get; set; }
  }
  ```
- **`ITenantEntity.cs`**:
  ```csharp
  namespace Depot.CleanArchitecture.Domain.Common;

  public interface ITenantEntity
  {
      public Guid TenantId { get; set; }
  }
  ```
- **`Error.cs` & `Result.cs`**:
  ```csharp
  namespace Depot.CleanArchitecture.Domain.Common;

  public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
  {
      public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
      public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
      public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
      public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
  }

  public enum ErrorType { Failure, Validation, NotFound, Conflict }

  public class Result
  {
      public bool IsSuccess { get; }
      public bool IsFailure => !IsSuccess;
      public Error Error { get; }

      protected Result(bool isSuccess, Error error)
      {
          IsSuccess = isSuccess;
          Error = error;
      }

      public static Result Success() => new(true, Error.None);
      public static Result Failure(Error error) => new(false, error);
      public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);
      public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);
  }

  public class Result<TValue> : Result
  {
      private readonly TValue? _value;
      public TValue Value => IsSuccess ? _value! : throw new InvalidOperationException("Không thể lấy giá trị khi kết quả thất bại.");

      internal Result(TValue? value, bool isSuccess, Error error) : base(isSuccess, error) => _value = value;
  }
  ```

#### 4. Domain Rule Engine trong `src/Depot.CleanArchitecture.Domain/Common/Rules/`
- **`IBusinessRule.cs`**:
  ```csharp
  namespace Depot.CleanArchitecture.Domain.Common.Rules;

  public interface IBusinessRule
  {
      string RuleCode { get; }
      string Message { get; }
      int Priority => 0;
      bool IsBroken();
  }
  ```
- **`BusinessRuleException.cs`**:
  ```csharp
  namespace Depot.CleanArchitecture.Domain.Common.Rules;

  public class BusinessRuleException : Exception
  {
      public IBusinessRule BrokenRule { get; }
      public BusinessRuleException(IBusinessRule brokenRule) 
          : base($"[Vi phạm quy tắc {brokenRule.RuleCode}]: {brokenRule.Message}")
      {
          BrokenRule = brokenRule;
      }
  }
  ```
- **`BusinessRuleValidator.cs`**:
  ```csharp
  namespace Depot.CleanArchitecture.Domain.Common.Rules;

  public static class BusinessRuleValidator
  {
      public static void CheckRule(IBusinessRule rule)
      {
          if (rule.IsBroken())
          {
              throw new BusinessRuleException(rule);
          }
      }

      public static void CheckRules(params IBusinessRule[] rules)
      {
          foreach (var rule in rules.OrderBy(r => r.Priority))
          {
              CheckRule(rule);
          }
      }
  }
  ```

### 1.3. Thực hành & Kiểm thử Ngày 1
Viết Unit Test kiểm tra tính hoạt động của `Result` và `BusinessRuleValidator`:
```csharp
public class BusinessRuleValidatorTests
{
    private class DummyRule(bool isBroken) : IBusinessRule
    {
        public string RuleCode => "Test.DummyRule";
        public string Message => "Luật kiểm thử bị vi phạm";
        public bool IsBroken() => isBroken;
    }

    [Fact]
    public void CheckRule_WhenRuleBroken_ThrowsBusinessRuleException()
    {
        var rule = new DummyRule(isBroken: true);
        var act = () => BusinessRuleValidator.CheckRule(rule);
        act.Should().Throw<BusinessRuleException>().WithMessage("*Test.DummyRule*");
    }

    [Fact]
    public void CheckRule_WhenRuleNotBroken_DoesNotThrow()
    {
        var rule = new DummyRule(isBroken: false);
        var act = () => BusinessRuleValidator.CheckRule(rule);
        act.Should().NotThrow();
    }
}
```

---

## NGÀY 2: HỒ SƠ CONTAINER & THUẬT TOÁN QUỐC TẾ ISO 6346 MODULO 11

### 2.1. Nghiệp vụ chuyên sâu
Mỗi container quốc tế có đúng **11 ký tự**:
- **3 chữ cái**: Mã chủ sở hữu/Hãng tàu (VD: `CMA`, `MSK`, `ONE`).
- **1 chữ cái**: Định danh loại thiết bị (`U` = Freight container khô, `R` = Reefer lạnh, `J` = Thiết bị đi kèm, `Z` = Rơ-moóc).
- **6 chữ số**: Số sê-ri từ `000001` đến `999999`.
- **1 ký tự cuối**: Số kiểm tra (Check digit) tính bằng **Modulo 11**.

#### Bảng tra cứu giá trị chữ cái ISO 6346:
| Ký tự | Giá trị | Ký tự | Giá trị | Ký tự | Giá trị | Ký tự | Giá trị |
| :---: | :---: | :---: | :---: | :---: | :---: | :---: | :---: |
| **A** | 10 | **H** | 18 | **O** | 26 | **V** | 34 |
| **B** | 12 | **I** | 19 | **P** | 27 | **W** | 35 |
| **C** | 13 | **J** | 20 | **Q** | 28 | **X** | 36 |
| **D** | 14 | **K** | 21 | **R** | 29 | **Y** | 37 |
| **E** | 15 | **L** | 23 | **S** | 30 | **Z** | 38 |
| **F** | 16 | **M** | 24 | **T** | 31 | | |
| **G** | 17 | **N** | 25 | **U** | 32 | | |

*(Lưu ý bắt buộc: Các bội số của 11 là 11, 22, 33 đều bị loại trừ).*

### 2.2. Triển khai Value Object `ContainerNumber.cs`

Tạo file `src/Depot.CleanArchitecture.Domain/ValueObjects/ContainerNumber.cs`:
```csharp
namespace Depot.CleanArchitecture.Domain.ValueObjects;

public sealed class ContainerNumber : IEquatable<ContainerNumber>
{
    private static readonly Dictionary<char, int> CharMap = new()
    {
        {'A', 10}, {'B', 12}, {'C', 13}, {'D', 14}, {'E', 15}, {'F', 16}, {'G', 17},
        {'H', 18}, {'I', 19}, {'J', 20}, {'K', 21}, {'L', 23}, {'M', 24}, {'N', 25},
        {'O', 26}, {'P', 27}, {'Q', 28}, {'R', 29}, {'S', 30}, {'T', 31}, {'U', 32},
        {'V', 34}, {'W', 35}, {'X', 36}, {'Y', 37}, {'Z', 38}
    };

    public string Value { get; }
    public string OwnerCode => Value[..3];
    public char CategoryIdentifier => Value[3];
    public string SerialNumber => Value.Substring(4, 6);
    public char CheckDigit => Value[10];

    private ContainerNumber(string value) => Value = value;

    public static Result<ContainerNumber> Create(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result.Failure<ContainerNumber>(Error.Validation("Container.Empty", "Số container không được để trống."));

        var upper = raw.Trim().ToUpperInvariant();
        if (upper.Length != 11)
            return Result.Failure<ContainerNumber>(Error.Validation("Container.InvalidLength", "Số container phải có đúng 11 ký tự."));

        if (!ValidateModulo11(upper))
            return Result.Failure<ContainerNumber>(Error.Validation("Container.InvalidCheckDigit", $"Số container {upper} không đúng số kiểm tra Modulo 11 (ISO 6346)."));

        return Result.Success(new ContainerNumber(upper));
    }

    public static bool ValidateModulo11(string containerNumber)
    {
        if (containerNumber.Length != 11) return false;

        int sum = 0;
        for (int i = 0; i < 10; i++)
        {
            char c = containerNumber[i];
            int value;
            if (char.IsLetter(c))
            {
                if (!CharMap.TryGetValue(c, out value)) return false;
            }
            else if (char.IsDigit(c))
            {
                value = c - '0';
            }
            else
            {
                return false;
            }

            int weight = 1 << i; // 2^i: 1, 2, 4, 8, 16, 32, 64, 128, 256, 512
            sum += value * weight;
        }

        int remainder = sum % 11;
        char calculatedCheckDigit = remainder == 10 ? 'X' : (char)('0' + remainder);

        return containerNumber[10] == calculatedCheckDigit;
    }

    public override string ToString() => Value;
    public bool Equals(ContainerNumber? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is ContainerNumber cn && Equals(cn);
    public override int GetHashCode() => Value.GetHashCode();
}
```

### 2.3. Triển khai Entity `Container.cs` và các Enums

Tạo file `src/Depot.CleanArchitecture.Domain/Enums/ContainerGrade.cs`:
```csharp
namespace Depot.CleanArchitecture.Domain.Enums;

public enum ContainerGrade
{
    GradeA = 1, // Đạt chuẩn đóng hàng cao cấp (thực phẩm, gạo, điện tử)
    GradeB = 2, // Hàng bách hóa thông thường, máy móc đóng kiện
    GradeC = 3, // Hàng thô, phế liệu, quặng (cho phép dơ/trầy nhẹ)
    GradeD_Damaged = 4, // Vỏ hỏng, cấm xuất, chuyển sang bãi sửa chữa M&R
    GradeE_Scrap = 5    // Phế thải, hư hỏng toàn phần
}
```

Tạo file `src/Depot.CleanArchitecture.Domain/Entities/Container.cs`:
```csharp
namespace Depot.CleanArchitecture.Domain.Entities;

using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;

public class Container : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public ContainerNumber Number { get; private set; } = null!;
    public string LineOperator { get; private set; } = null!;
    public ContainerType Type { get; private set; }
    public string IsoCode { get; private set; } = null!;
    public int Size { get; private set; } // 20, 40, 45
    public decimal TareWeight { get; private set; }
    public decimal MaxGrossWeight { get; private set; }
    public decimal PayloadWeight => MaxGrossWeight - TareWeight;
    public ContainerGrade Grade { get; private set; }
    public string? DamageNotes { get; private set; }

    private Container() { } // Dành cho EF Core

    public static Result<Container> Create(
        Guid tenantId,
        ContainerNumber number,
        string lineOperator,
        ContainerType type,
        string isoCode,
        int size,
        decimal tareWeight,
        decimal maxGrossWeight,
        ContainerGrade grade)
    {
        if (maxGrossWeight <= tareWeight)
            return Result.Failure<Container>(Error.Validation("Container.InvalidWeight", "Tải trọng tối đa phải lớn hơn trọng lượng vỏ (Tare weight)."));

        var container = new Container
        {
            TenantId = tenantId,
            Number = number,
            LineOperator = lineOperator.ToUpperInvariant(),
            Type = type,
            IsoCode = isoCode,
            Size = size,
            TareWeight = tareWeight,
            MaxGrossWeight = maxGrossWeight,
            Grade = grade
        };

        return Result.Success(container);
    }

    public void UpdateCondition(ContainerGrade newGrade, string? damageNotes)
    {
        Grade = newGrade;
        DamageNotes = damageNotes;
    }
}
```

---

## NGÀY 3: KHÔNG GIAN BÃI (YARD TOPOLOGY), THUẬT TOÁN XUNG ĐỘT BAY 20FT/40FT & BLOCK ẢO

### 3.1. Nghiệp vụ chuyên sâu
- **Không gian 3D**: `Block` (Khu) $\rightarrow$ `Bay` (Chiều dài) $\rightarrow$ `Row` (Chiều rộng) $\rightarrow$ `Tier` (Tầng cao).
- **Thuật toán Va chạm Bay (Even/Odd Bay Collision)**:
  - Bay lẻ ($01, 03, 05 \dots$): Cont 20ft.
  - Bay chẵn ($02, 04, 06 \dots$): Cont 40ft (chiếm cả 2 bay lẻ trước và sau nó).
  - Ví dụ: Hạ cont 40ft tại Bay 02 thì cả Bay 01 và Bay 03 tại cùng Row, cùng Tier phải bị **khóa**!
- **Block ảo (Virtual Block)**:
  - Không có Bay/Row/Tier (cho phép null).
  - Dành cho Xưởng sửa chữa (M&R), Sân rửa, Bãi đệm cổng. Quản lý theo `MaxCapacity`.

### 3.2. Triển khai Value Object `SlotCoordinate.cs`
Tạo file `src/Depot.CleanArchitecture.Domain/ValueObjects/SlotCoordinate.cs`:
```csharp
namespace Depot.CleanArchitecture.Domain.ValueObjects;

public sealed record SlotCoordinate(int Bay, int Row, int Tier)
{
    public bool IsOddBay => Bay % 2 != 0;   // Cont 20ft
    public bool IsEvenBay => Bay % 2 == 0;  // Cont 40ft

    // Nếu là Bay chẵn 40ft (VD: Bay 02) -> lấy 2 bay lẻ bị chiếm dụng (Bay 01, Bay 03)
    public (int PriorOddBay, int NextOddBay) GetOverlappingOddBays()
    {
        if (IsOddBay) throw new InvalidOperationException("Chỉ áp dụng cho bay chẵn 40ft.");
        return (Bay - 1, Bay + 1);
    }

    // Nếu là Bay lẻ 20ft (VD: Bay 01 hoặc 03) -> lấy bay chẵn 40ft có thể xung đột
    public IEnumerable<int> GetAdjacentEvenBays()
    {
        if (IsEvenBay) throw new InvalidOperationException("Chỉ áp dụng cho bay lẻ 20ft.");
        if (Bay > 1) yield return Bay - 1; // Bay chẵn phía trước
        yield return Bay + 1;             // Bay chẵn phía sau
    }
}
```

### 3.3. Triển khai Business Rule kiểm tra Va chạm Bay chẵn/lẻ
Tạo file `src/Depot.CleanArchitecture.Domain/Rules/Yard/EvenOddBayCollisionRule.cs`:
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
    public string Message => "Vị trí hạ container bị xung đột không gian vật lý với container ở bay liền kề!";

    public bool IsBroken()
    {
        var occupiedBays = _occupiedSlotsInSameRowAndTier.Select(s => s.Bay).ToHashSet();

        if (_containerSize == 40)
        {
            // Cont 40ft hạ vào bay chẵn: kiểm tra 2 bay lẻ trước và sau
            var (bay1, bay2) = _target.GetOverlappingOddBays();
            if (occupiedBays.Contains(bay1) || occupiedBays.Contains(bay2))
                return true; // Bị vướng cont 20ft đang có sẵn!
        }
        else if (_containerSize == 20)
        {
            // Cont 20ft hạ vào bay lẻ: kiểm tra các bay chẵn lân cận
            foreach (var evenBay in _target.GetAdjacentEvenBays())
            {
                if (occupiedBays.Contains(evenBay))
                    return true; // Bị vướng cont 40ft đang chiếm cả khoảng không!
            }
        }

        return false;
    }
}
```

### 3.4. Triển khai Rule An toàn Xếp tầng (Stacking Rules)
Tạo file `src/Depot.CleanArchitecture.Domain/Rules/Yard/TierMustHaveBottomSupportRule.cs`:
```csharp
namespace Depot.CleanArchitecture.Domain.Rules.Yard;

using Depot.CleanArchitecture.Domain.Common.Rules;

public class TierMustHaveBottomSupportRule : IBusinessRule
{
    private readonly int _tier;
    private readonly bool _hasContainerBelow;

    public TierMustHaveBottomSupportRule(int tier, bool hasContainerBelow)
    {
        _tier = tier;
        _hasContainerBelow = hasContainerBelow;
    }

    public string RuleCode => "Yard.TierMissingBottomSupport";
    public string Message => $"Không thể hạ container ở tầng {_tier} vì tầng {_tier - 1} chưa có container đỡ bên dưới!";
    public bool IsBroken() => _tier > 1 && !_hasContainerBelow;
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
    public string Message => "Tuyệt đối cấm đặt container 20ft lên nóc container 40ft (nguy cơ sập nóc vỏ cont dưới)!";
    
    // Vi phạm nếu cont trên là 20ft mà cont dưới là 40ft
    public bool IsBroken() => _topContainerSize == 20 && _bottomContainerSize == 40;
}
```

---

## NGÀY 4: VÒNG ĐỜI CONTAINER (LIFECYCLE) & RÀNG BUỘC LỆNH GIAO NHẬN (DELIVERY ORDER)

### 4.1. Nghiệp vụ chuyên sâu
- **Một vòng đời (ContainerVisit)**: Ghi nhận trọn vẹn từ lúc xe tải vào cổng (Gate-In) $\rightarrow$ hạ bãi (Stacked) $\rightarrow$ gán lệnh xuất (Allocated) $\rightarrow$ xe tải lấy ra khỏi cổng (Gate-Out).
- **Ràng buộc Lệnh DO của Hãng tàu**:
  - Không tự ý xuất cont nếu không có Lệnh hợp lệ.
  - Phải kiểm tra: Còn hạn lệnh, Đúng hãng tàu, Đúng phân hạng Grade yêu cầu, Còn hạn mức số lượng (Quota).
  - **Xử lý trễ hạn**: Quá hạn $\rightarrow$ Chặn cổng; Khách xin gia hạn $\rightarrow$ Điều độ viên chạy `ExtendDeliveryOrderCommand`.

### 4.2. Triển khai Aggregate Root `ContainerVisit.cs`

Tạo file `src/Depot.CleanArchitecture.Domain/Entities/ContainerVisit.cs`:
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
    public Guid? CurrentBlockId { get; private set; } // Dùng cho Block ảo

    // Thông tin Cổng Vào (Gate In)
    public DateTime GateInDate { get; private set; }
    public string InTractorNo { get; private set; } = null!;
    public string InTrailerNo { get; private set; } = null!;
    public string InDriverName { get; private set; } = null!;
    public ContainerGrade InSurveyGrade { get; private set; }
    public string? InDamageNotes { get; private set; }

    // Thông tin Cổng Ra (Gate Out)
    public DateTime? GateOutDate { get; private set; }
    public string? OutTractorNo { get; private set; }
    public string? OutTrailerNo { get; private set; }
    public string? OutDriverName { get; private set; }
    public Guid? DeliveryOrderId { get; private set; }

    private ContainerVisit() { }

    public static ContainerVisit CreateGateIn(
        Guid tenantId,
        Guid containerId,
        string lineOperator,
        string tractorNo,
        string trailerNo,
        string driverName,
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
            InTractorNo = tractorNo.ToUpperInvariant(),
            InTrailerNo = trailerNo.ToUpperInvariant(),
            InDriverName = driverName,
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

    public void Allocate(Guid deliveryOrderId)
    {
        DeliveryOrderId = deliveryOrderId;
        Status = ContainerVisitStatus.Allocated;
    }

    public void GateOut(string tractorNo, string trailerNo, string driverName)
    {
        OutTractorNo = tractorNo.ToUpperInvariant();
        OutTrailerNo = trailerNo.ToUpperInvariant();
        OutDriverName = driverName;
        GateOutDate = DateTime.UtcNow;
        Status = ContainerVisitStatus.GatedOut;
        CurrentSlotId = null; // Giải phóng ô bãi
    }
}
```

### 4.3. Triển khai Entity `DeliveryOrder.cs` & Business Rules

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

Tạo file `src/Depot.CleanArchitecture.Domain/Rules/Orders/DeliveryOrderQuotaRemainingRule.cs`:
```csharp
namespace Depot.CleanArchitecture.Domain.Rules.Orders;

using Depot.CleanArchitecture.Domain.Common.Rules;

public class DeliveryOrderQuotaRemainingRule : IBusinessRule
{
    private readonly int _orderedQty;
    private readonly int _deliveredQty;

    public DeliveryOrderQuotaRemainingRule(int orderedQty, int deliveredQty)
    {
        _orderedQty = orderedQty;
        _deliveredQty = deliveredQty;
    }

    public string RuleCode => "DeliveryOrder.QuotaExceeded";
    public string Message => "Lệnh giao container này đã xuất đủ số lượng cho phép!";
    public bool IsBroken() => _deliveredQty >= _orderedQty;
}
```

---

## NGÀY 5: TẦNG APPLICATION - MANUAL CQRS & BÁO CÁO THỐNG KÊ NGHIỆP VỤ

### 5.1. Kiến trúc Manual CQRS (Không dùng MediatR)

Tạo thư mục `src/Depot.CleanArchitecture.Application/Abstractions/Messaging/`:
```csharp
namespace Depot.CleanArchitecture.Application.Abstractions.Messaging;

using Depot.CleanArchitecture.Domain.Common;

public interface ICommand<TResponse> { }
public interface ICommand : ICommand<Result> { }

public interface ICommandHandler<in TCommand, TResponse> 
    where TCommand : ICommand<TResponse>
{
    Task<TResponse> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

public interface IQuery<TResponse> { }

public interface IQueryHandler<in TQuery, TResponse> 
    where TQuery : IQuery<TResponse>
{
    Task<TResponse> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
```

### 5.2. Use Case Báo cáo Thống kê Tồn bãi (Aging Dwell Time)

Tạo file `src/Depot.CleanArchitecture.Application/Features/Analytics/Queries/GetAgingReport/GetAgingReportQuery.cs`:
```csharp
namespace Depot.CleanArchitecture.Application.Features.Analytics.Queries.GetAgingReport;

using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;

public record LineOperatorAgingDto(
    string LineOperator,
    int Count0To10Days,
    int CountOver10Days,
    int TotalInventory);

public record GetAgingReportQuery : IQuery<Result<List<LineOperatorAgingDto>>>;
```

Tạo file `src/Depot.CleanArchitecture.Application/Features/Analytics/Queries/GetAgingReport/GetAgingReportQueryHandler.cs`:
```csharp
namespace Depot.CleanArchitecture.Application.Features.Analytics.Queries.GetAgingReport;

using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public class GetAgingReportQueryHandler : IQueryHandler<GetAgingReportQuery, Result<List<LineOperatorAgingDto>>>
{
    private readonly IAppDbContext _dbContext;

    public GetAgingReportQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<List<LineOperatorAgingDto>>> HandleAsync(
        GetAgingReportQuery query, 
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        // Container đang nằm trong bãi (chưa Gate-Out)
        var visits = await _dbContext.ContainerVisits
            .AsNoTracking()
            .Where(v => v.Status != ContainerVisitStatus.GatedOut && v.Status != ContainerVisitStatus.Completed)
            .ToListAsync(cancellationToken);

        var report = visits
            .GroupBy(v => v.LineOperator)
            .Select(g =>
            {
                int count0To10 = g.Count(v => (now - v.GateInDate).TotalDays <= 10);
                int countOver10 = g.Count(v => (now - v.GateInDate).TotalDays > 10);
                return new LineOperatorAgingDto(g.Key, count0To10, countOver10, g.Count());
            })
            .OrderByDescending(r => r.TotalInventory)
            .ToList();

        return Result.Success(report);
    }
}
```

### 5.3. Use Case Báo cáo Sản lượng Xuất/Nhập (Throughput) Hàng Ngày

Tạo file `src/Depot.CleanArchitecture.Application/Features/Analytics/Queries/GetThroughputReport/GetThroughputReportQueryHandler.cs`:
```csharp
namespace Depot.CleanArchitecture.Application.Features.Analytics.Queries.GetThroughputReport;

using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Microsoft.EntityFrameworkCore;

public record DailyThroughputDto(
    DateOnly ReportDate,
    string LineOperator,
    int GateInCount,
    int GateOutCount,
    int TotalMovements,
    decimal TotalTeu);

public record GetThroughputReportQuery(DateOnly FromDate, DateOnly ToDate) 
    : IQuery<Result<List<DailyThroughputDto>>>;

public class GetThroughputReportQueryHandler : IQueryHandler<GetThroughputReportQuery, Result<List<DailyThroughputDto>>>
{
    private readonly IAppDbContext _dbContext;
    public GetThroughputReportQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<List<DailyThroughputDto>>> HandleAsync(
        GetThroughputReportQuery query, 
        CancellationToken cancellationToken = default)
    {
        var fromDateTime = query.FromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toDateTime = query.ToDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var visits = await _dbContext.ContainerVisits
            .AsNoTracking()
            .Where(v => (v.GateInDate >= fromDateTime && v.GateInDate <= toDateTime) ||
                        (v.GateOutDate != null && v.GateOutDate >= fromDateTime && v.GateOutDate <= toDateTime))
            .ToListAsync(cancellationToken);

        // Nhóm và tính toán sản lượng
        var inGroups = visits
            .Where(v => v.GateInDate >= fromDateTime && v.GateInDate <= toDateTime)
            .GroupBy(v => new { Date = DateOnly.FromDateTime(v.GateInDate), v.LineOperator })
            .Select(g => new { g.Key.Date, g.Key.LineOperator, InCount = g.Count(), OutCount = 0 });

        var outGroups = visits
            .Where(v => v.GateOutDate != null && v.GateOutDate >= fromDateTime && v.GateOutDate <= toDateTime)
            .GroupBy(v => new { Date = DateOnly.FromDateTime(v.GateOutDate!.Value), v.LineOperator })
            .Select(g => new { g.Key.Date, g.Key.LineOperator, InCount = 0, OutCount = g.Count() });

        var combined = inGroups.Concat(outGroups)
            .GroupBy(x => new { x.Date, x.LineOperator })
            .Select(g => new DailyThroughputDto(
                g.Key.Date,
                g.Key.LineOperator,
                g.Sum(x => x.InCount),
                g.Sum(x => x.OutCount),
                g.Sum(x => x.InCount + x.OutCount),
                g.Sum(x => x.InCount + x.OutCount) // Tạm tính 1 cont = 1 TEU
            ))
            .OrderByDescending(r => r.ReportDate)
            .ThenByDescending(r => r.TotalMovements)
            .ToList();

        return Result.Success(combined);
    }
}
```

---

## NGÀY 6: TẦNG INFRASTRUCTURE - EF CORE 10, MULTI-TENANCY & HYBRIDCACHE

### 6.1. Cấu hình Multi-Tenancy với Global Query Filter trong EF Core 10
Mỗi chi nhánh/Depot là một Tenant. EF Core sẽ tự động lọc dữ liệu, ngăn chặn tuyệt đối việc Depot Cát Lái nhìn thấy dữ liệu của Depot Hiệp Phước!

Tạo file `src/Depot.CleanArchitecture.Infrastructure/Persistence/AppDbContext.cs`:
```csharp
namespace Depot.CleanArchitecture.Infrastructure.Persistence;

using System.Linq.Expressions;
using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly ITenantProvider _tenantProvider;

    public AppDbContext(DbContextOptions<AppDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantProvider = tenantProvider;
    }

    public DbSet<YardBlock> YardBlocks => Set<YardBlock>();
    public DbSet<YardSlot> YardSlots => Set<YardSlot>();
    public DbSet<Container> Containers => Set<Container>();
    public DbSet<ContainerVisit> ContainerVisits => Set<ContainerVisit>();
    public DbSet<DeliveryOrder> DeliveryOrders => Set<DeliveryOrder>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Tự động cấu hình Global Query Filter cho tất cả entity kế thừa ITenantEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
                var tenantIdExpression = Expression.Property(
                    Expression.Constant(_tenantProvider), 
                    nameof(ITenantProvider.CurrentTenantId));

                var filter = Expression.Lambda(Expression.Equal(property, tenantIdExpression), parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var currentTenantId = _tenantProvider.CurrentTenantId;

        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
            {
                entry.Entity.TenantId = currentTenantId;
            }
        }

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedAt = DateTime.UtcNow;
            if (entry.State == EntityState.Modified) entry.Entity.LastModifiedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
```

### 6.2. Cấu hình Value Conversion cho `ContainerNumber`
Trong `src/Depot.CleanArchitecture.Infrastructure/Persistence/Configurations/ContainerConfiguration.cs`:
```csharp
namespace Depot.CleanArchitecture.Infrastructure.Persistence.Configurations;

using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ContainerConfiguration : IEntityTypeConfiguration<Container>
{
    public void Configure(EntityTypeBuilder<Container> builder)
    {
        builder.HasKey(c => c.Id);

        // Chuyển đổi hai chiều: C# ContainerNumber <--> Database VARCHAR(11)
        builder.Property(c => c.Number)
            .HasConversion(
                number => number.Value,
                value => ContainerNumber.Create(value).Value)
            .HasMaxLength(11)
            .IsRequired();

        builder.HasIndex(c => new { c.TenantId, c.Number }).IsUnique();
        builder.Property(c => c.LineOperator).HasMaxLength(20).IsRequired();
        builder.Property(c => c.IsoCode).HasMaxLength(10).IsRequired();
    }
}
```

---

## NGÀY 7: TẦNG API, SCALAR OPENAPI, ARCHITECTURE TESTS & KỊCH BẢN END-TO-END

### 7.1. Minimal APIs trong Tầng Api
Tạo file `src/Depot.CleanArchitecture.Api/Endpoints/AnalyticsEndpoints.cs`:
```csharp
namespace Depot.CleanArchitecture.Api.Endpoints;

using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Application.Features.Analytics.Queries.GetAgingReport;
using Depot.CleanArchitecture.Application.Features.Analytics.Queries.GetThroughputReport;

public static class AnalyticsEndpoints
{
    public static RouteGroupBuilder MapAnalyticsEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/aging", async (
            IQueryHandler<GetAgingReportQuery, Result<List<LineOperatorAgingDto>>> handler, 
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new GetAgingReportQuery(), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetAgingReport")
        .WithSummary("Báo cáo container tồn bãi theo hãng tàu (0-10 ngày và trên 10 ngày)");

        group.MapGet("/throughput", async (
            DateOnly fromDate, 
            DateOnly toDate,
            IQueryHandler<GetThroughputReportQuery, Result<List<DailyThroughputDto>>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new GetThroughputReportQuery(fromDate, toDate), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Error);
        })
        .WithName("GetThroughputReport")
        .WithSummary("Báo cáo sản lượng xuất nhập theo ngày của từng hãng tàu");

        return group;
    }
}
```

### 7.2. Tích hợp Scalar API Documentation trong `Program.cs`
Thay thế hoàn toàn Swagger UI cũ kỹ bằng giao diện **Scalar** hiện đại:
```csharp
var builder = WebApplication.CreateBuilder(args);

// Đăng ký Application & Infrastructure
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Cấu hình OpenAPI & Scalar
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Hệ Thống Quản Lý Depot Tân Cảng (SNP DMS API)";
        options.Theme = ScalarTheme.Mars;
    });
}

// Đăng ký nhóm Endpoints
var api = app.MapGroup("/api");
api.MapGroup("/analytics").MapAnalyticsEndpoints();

app.Run();
```

### 7.3. Bộ Architecture Tests với NetArchTest
Đảm bảo sinh viên và thành viên dự án không bao giờ phá vỡ kiến trúc Clean Architecture!

Tạo file `tests/Depot.CleanArchitecture.Architecture.Tests/ArchitectureTests.cs`:
```csharp
namespace Depot.CleanArchitecture.Architecture.Tests;

using FluentAssertions;
using NetArchTest.Rules;
using Xunit;

public class ArchitectureTests
{
    private const string DomainNamespace = "Depot.CleanArchitecture.Domain";
    private const string ApplicationNamespace = "Depot.CleanArchitecture.Application";
    private const string InfrastructureNamespace = "Depot.CleanArchitecture.Infrastructure";
    private const string ApiNamespace = "Depot.CleanArchitecture.Api";

    [Fact]
    public void Domain_Should_Not_HaveDependencyOnOtherProjects()
    {
        var otherProjects = new[] { ApplicationNamespace, InfrastructureNamespace, ApiNamespace };
        var result = Types.InAssembly(typeof(Domain.Common.BaseEntity).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(otherProjects)
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Tầng Domain không được phụ thuộc vào bất kỳ tầng nào khác!");
    }

    [Fact]
    public void Application_Should_Not_HaveDependencyOn_Infrastructure_Or_Api()
    {
        var otherProjects = new[] { InfrastructureNamespace, ApiNamespace };
        var result = Types.InAssembly(typeof(Application.Abstractions.Data.IAppDbContext).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(otherProjects)
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Tầng Application không được phụ thuộc vào Infrastructure hoặc Api!");
    }
}
```

---

## 9. BỘ CÂU HỎI & TRẢ LỜI VẤN ĐÁP KỸ THUẬT KHI BÁO CÁO MENTOR

Khi bạn thuyết trình bảo vệ dự án với Mentor, dưới đây là các câu hỏi Mentor thường hỏi nhất và câu trả lời "chuẩn kỹ sư" giúp bạn ghi điểm tuyệt đối:

### Câu hỏi 1: "Tại sao trong dự án em lại tự viết Manual CQRS mà không dùng MediatR?"
- **Trả lời**:  
  *"Thưa anh/chị, em chọn Manual CQRS vì 3 lý do: Thứ nhất, thư viện MediatR từ phiên bản v13 trở đi đã chuyển sang mô hình thương mại có tính phí bản quyền (Commercial License), nên các hệ thống doanh nghiệp lớn hiện nay có xu hướng tự viết để tránh phụ thuộc pháp lý. Thứ hai, việc tự định nghĩa `ICommand`, `IQuery`, `ICommandHandler` giúp em hiểu sâu sắc nguyên lý Dependency Inversion (DIP) và cơ chế đăng ký Reflection trong `IServiceCollection`. Thứ ba, hiệu năng của việc gọi trực tiếp Interface trong DI container nhanh hơn và dễ debug stack trace hơn so với đi qua pipeline của MediatR."*

### Câu hỏi 2: "Tại sao số container em lại dùng Value Object mà không để kiểu string?"
- **Trả lời**:  
  *"Thưa anh/chị, số container tuân theo tiêu chuẩn quốc tế ISO 6346 với thuật toán kiểm tra Modulo 11. Nếu em dùng kiểu `string`, nó sẽ trở thành một 'Primitive Obsession', và ở bất cứ chỗ nào người dùng nhập số cont em cũng phải copy đoạn code validate `if (len != 11)...`. Bằng cách tạo Value Object `ContainerNumber`, em bảo đảm tính bất biến (Immutability). Một khi đối tượng `ContainerNumber` được khởi tạo thành công, toàn bộ hệ thống hoàn toàn yên tâm rằng số container đó 100% hợp lệ."*

### Câu hỏi 3: "Hệ thống của em giải quyết bài toán va chạm Bay chẵn 40ft và Bay lẻ 20ft như thế nào?"
- **Trả lời**:  
  *"Thưa anh/chị, theo quy ước cảng biển quốc tế, Bay lẻ (01, 03) chứa cont 20ft, Bay chẵn (02) chứa cont 40ft. Một cont 40ft tại Bay 02 chiếm trọn không gian vật lý của cả Bay 01 và 03. Em đã cài đặt `EvenOddBayCollisionRule`: Khi người dùng hạ cont 40ft tại Bay 02, hệ thống tự động kiểm tra xem Bay 01 hoặc 03 tại cùng Row, cùng Tier đã có cont chưa; nếu có thì chặn ngay. Ngược lại, khi hạ cont 20ft tại Bay 01 hoặc 03, hệ thống cũng kiểm tra xem Bay 02 có cont 40ft đang chiếm dụng hay không. Nhờ vậy ngăn chặn hoàn toàn việc xe nâng hạ đè cont ngoài thực tế."*

### Câu hỏi 4: "Block ảo là gì và em lưu trữ nó trong cơ sở dữ liệu ra sao?"
- **Trả lời**:  
  *"Thưa anh/chị, Block ảo là khu vực bãi logic không quản lý theo tọa độ 3D Bay/Row/Tier, ví dụ như Xưởng sửa chữa cơ khí M&R, Sân rửa vỏ container hay Bãi đệm cổng. Trong database, thực thể `YardBlock` có cờ `IsVirtual = true`, các trường `Bay, Row, Tier` trong `ContainerVisit` được phép `null`. Thay vì kiểm tra va chạm tọa độ, hệ thống kiểm tra Sức chứa tối đa (`MaxCapacity`). Khi số lượng cont trong xưởng M&R vượt quá sức chứa, rule `VirtualBlockCapacityRule` sẽ tự động cảnh báo."*

### Câu hỏi 5: "Làm thế nào để bảo đảm các bạn khác trong nhóm không code vi phạm kiến trúc Clean Architecture?"
- **Trả lời**:  
  *"Em đã thiết lập bộ kiểm thử kiến trúc tự động bằng thư viện `NetArchTest.Rules` trong project `Architecture.Tests`. Mỗi khi có ai đó trong tầng Domain cố tình `using` tầng Infrastructure hay Api, khi chạy lệnh `dotnet test` hoặc trong pipeline CI/CD, bài test sẽ Fail ngay lập tức và chặn việc tạo Pull Request."*

---

> **Tài liệu tham khảo bổ sung trong dự án**:
> - Spec Thiết kế Chi tiết: `docs/superpowers/specs/2026-10-06-depot-system-design.md`
> - Kế hoạch Hành động 7 Ngày: `docs/superpowers/plans/2026-10-06-depot-cleanarchitecture-7days.md`
> - Yêu cầu nghiệp vụ SNP: `SNP_HE_THONG_QUAN_LY_DEPOT.md`
