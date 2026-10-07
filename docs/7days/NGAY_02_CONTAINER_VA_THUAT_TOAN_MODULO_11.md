# NGÀY 2: HỒ SƠ CONTAINER & THUẬT TOÁN QUỐC TẾ ISO 6346 MODULO 11
**Dự án**: Depot Management System (Clean Architecture .NET 10 - SNP)  
**Mục tiêu Ngày 2**: Nắm vững khái niệm Value Object trong DDD, hiện thực hóa thuật toán kiểm tra số container Modulo 11 theo chuẩn quốc tế ISO 6346, xây dựng Entity Container và phân loại Grade A - E.

---

## 1. MỤC TIÊU HỌC TẬP & NỀN TẢNG LÝ THUYẾT

### 1.1. Khái niệm Value Object trong Domain-Driven Design (DDD)
- **Vấn đề "Primitive Obsession"**: Trong code sơ cấp, người ta thường dùng `string` cho số container, biển số xe, mã số thuế. Nhưng `string` có thể bị rỗng, sai độ dài, hoặc chứa ký tự đặc biệt.
- **Value Object là gì?**
  1. **Không có định danh (No Identity)**: Hai số cont giống hệt chuỗi ký tự thì được coi là bằng nhau.
  2. **Bất biến (Immutable)**: Sau khi tạo ra thì không thể thay đổi giá trị bên trong.
  3. **Tự bảo vệ tính hợp lệ (Self-Validating)**: Hàm khởi tạo `Create()` tự chạy thuật toán kiểm tra. Nếu dữ liệu sai, nó từ chối tạo đối tượng.

### 1.2. Thuật toán Modulo 11 (ISO 6346) chi tiết
Mỗi số container chuẩn quốc tế có đúng **11 ký tự**:
- **3 chữ cái đầu**: Mã chủ sở hữu / Hãng tàu (Owner Code, VD: `CMA`, `MSK`, `ONE`).
- **1 chữ cái thứ 4**: Mã loại thiết bị (Category Identifier: `U` cho container khô, `R` cho container lạnh).
- **6 chữ số tiếp theo**: Số sê-ri (`000001` đến `999999`).
- **1 chữ số cuối cùng**: Số kiểm tra (Check Digit) tính bằng Modulo 11.

#### Bảng quy đổi ký tự sang số:
Các chữ cái $A \dots Z$ có giá trị từ 10 đến 38, **loại bỏ các bội số của 11** (11, 22, 33):
- $A=10, B=12, C=13, D=14, E=15, F=16, G=17, H=18, I=19, J=20$
- $K=21, L=23, M=24, N=25, O=26, P=27, Q=28, R=29, S=30, T=31$
- $U=32, V=34, W=35, X=36, Y=37, Z=38$
- Các số $0 \dots 9$ giữ nguyên giá trị là $0 \dots 9$.

#### Công thức tính toán:
1. Nhân giá trị ký tự thứ $i$ ($i = 0 \dots 9$) với trọng số $2^i$ (tương ứng: $1, 2, 4, 8, 16, 32, 64, 128, 256, 512$).
2. Tính tổng: $\text{Tổng} = \sum_{i=0}^{9} (\text{Giá trị}_i \times 2^i)$.
3. Lấy phần dư: $\text{Phần dư} = \text{Tổng} \pmod{11}$.
4. Nếu phần dư là $0 \dots 9 \rightarrow$ Check Digit là chữ số đó.
5. Nếu phần dư là $10 \rightarrow$ Check Digit theo chuẩn SNP và ISO là ký tự `'X'`.

---

## 2. TRIỂN KHAI CÁC FILE MÃ NGUỒN TẦNG DOMAIN

### 2.1. `src/Depot.CleanArchitecture.Domain/Common/ValueObject.cs`
Lớp cơ sở cho mọi Value Object để so sánh giá trị thay vì so sánh tham chiếu:
```csharp
namespace Depot.CleanArchitecture.Domain.Common;

public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType()) return false;
        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }
}
```

### 2.2. `src/Depot.CleanArchitecture.Domain/ValueObjects/ContainerNumber.cs`
Triển khai Value Object số container với thuật toán Modulo 11:
```csharp
namespace Depot.CleanArchitecture.Domain.ValueObjects;

using Depot.CleanArchitecture.Domain.Common;

public sealed class ContainerNumber : ValueObject
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

    public static Result<ContainerNumber> Create(string? raw)
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
            int val;
            if (char.IsLetter(c))
            {
                if (!CharMap.TryGetValue(c, out val)) return false;
            }
            else if (char.IsDigit(c))
            {
                val = c - '0';
            }
            else
            {
                return false;
            }

            int weight = 1 << i; // 2^i
            sum += val * weight;
        }

        int remainder = sum % 11;
        char expectedCheckDigit = remainder == 10 ? 'X' : (char)('0' + remainder);

        return containerNumber[10] == expectedCheckDigit;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
```

### 2.3. Các Enums phân loại container
Tạo file `src/Depot.CleanArchitecture.Domain/Enums/ContainerType.cs`:
```csharp
namespace Depot.CleanArchitecture.Domain.Enums;

public enum ContainerType
{
    Dry = 1,          // Container Khô
    Reefer = 2,       // Container Lạnh
    OpenTop = 3,      // Container Mở Nóc
    FlatRack = 4,     // Container Mặt Phẳng
    Tank = 5,         // Container Bồn / Hóa chất
    Ventilated = 6,   // Container Thông Gió
    Specialized = 7   // Container Chuyên Dụng
}
```

Tạo file `src/Depot.CleanArchitecture.Domain/Enums/ContainerGrade.cs`:
```csharp
namespace Depot.CleanArchitecture.Domain.Enums;

public enum ContainerGrade
{
    GradeA = 1,         // Loại 1: Hàng gạo, thực phẩm, hạt điều, điện tử
    GradeB = 2,         // Loại 2: Hàng bách hóa tổng hợp, máy móc đóng kiện
    GradeC = 3,         // Loại 3: Hàng thô, quặng, phế liệu, xơ dừa
    GradeD_Damaged = 4, // Hư hỏng: Cấm xuất bãi, chuyển xưởng M&R sửa chữa
    GradeE_Scrap = 5    // Phế thải: Biến dạng toàn phần, chờ thanh lý sắt vụn
}
```

### 2.4. `src/Depot.CleanArchitecture.Domain/Entities/Container.cs`
Thực thể Container hoàn chỉnh:
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
    public int Size { get; private set; } // 20, 40, 45 feet
    public decimal TareWeight { get; private set; } // Trọng lượng vỏ cont
    public decimal MaxGrossWeight { get; private set; } // Tải trọng tối đa
    public decimal PayloadWeight => MaxGrossWeight - TareWeight; // Tải trọng hữu ích
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
            return Result.Failure<Container>(
                Error.Validation("Container.InvalidWeight", "Tải trọng tối đa (Max Gross Weight) phải lớn hơn trọng lượng vỏ (Tare Weight)."));

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

## 3. BÀI TEST THỰC HÀNH NGÀY 2

Tạo file `tests/Depot.CleanArchitecture.Domain.UnitTests/ValueObjects/ContainerNumberTests.cs`:
```csharp
namespace Depot.CleanArchitecture.Domain.UnitTests.ValueObjects;

using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using Xunit;

public class ContainerNumberTests
{
    [Theory]
    [InlineData("CSQU3054383")] // Số cont chuẩn quốc tế hợp lệ
    public void Create_WithValidContainerNumber_ShouldSucceed(string raw)
    {
        var result = ContainerNumber.Create(raw);

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be(raw);
        result.Value.OwnerCode.Should().Be("CSQ");
        result.Value.CategoryIdentifier.Should().Be('U');
        result.Value.SerialNumber.Should().Be("305438");
        result.Value.CheckDigit.Should().Be('3');
    }

    [Fact]
    public void Create_WithInvalidCheckDigit_ShouldFail()
    {
        // CSQU3054383 số đúng là số 3 ở đuôi, ta đổi thành 9
        var result = ContainerNumber.Create("CSQU3054389");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Container.InvalidCheckDigit");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("CMAU12345")] // Thiếu ký tự
    [InlineData("CMAU1234567890")] // Dư ký tự
    public void Create_WithInvalidLength_ShouldFail(string raw)
    {
        var result = ContainerNumber.Create(raw);
        result.IsFailure.Should().BeTrue();
    }
}
```

**Lệnh chạy test**:
```bash
dotnet test tests/Depot.CleanArchitecture.Domain.UnitTests
```
Kết quả mong đợi: Toàn bộ test pass 100%.
