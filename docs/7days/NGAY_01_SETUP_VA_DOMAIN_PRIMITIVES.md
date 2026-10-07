# NGÀY 1: SETUP NỀN TẢNG SOLUTION & DOMAIN PRIMITIVES (SHARED KERNEL)
**Dự án**: Depot Management System (Clean Architecture .NET 10 - SNP)  
**Mục tiêu Ngày 1**: Thiết lập Solution chuẩn của Mentor, xây dựng các khối nền tảng Domain (Result Pattern, AuditableEntity, Multi-Tenancy, Domain Business Rule Engine).

---

## 1. MỤC TIÊU HỌC TẬP & NỀN TẢNG LÝ THUYẾT

### 1.1. Tại sao không dùng Exception cho luồng nghiệp vụ thông thường?
Trong lập trình truyền thống, khi gặp lỗi kiểm tra dữ liệu, người ta thường viết:
```csharp
if (string.IsNullOrEmpty(ten)) 
    throw new Exception("Tên không được rỗng!");
```
- **Nhược điểm lớn trong môi trường doanh nghiệp**:
  1. **Hiệu năng chậm**: Khi CLR ném ngoại lệ, nó phải dừng luồng và thu thập toàn bộ Stack Trace từ sâu trong hệ thống, tốn rất nhiều chu kỳ CPU.
  2. **Luồng điều khiển ẩn (Hidden Control Flow)**: Bất kỳ hàm nào gọi đến cũng không biết hàm con có ném lỗi hay không nếu không đọc code chi tiết.
- **Giải pháp của Mentor**: Sử dụng **Result Pattern** (`Result<T>`):
  - Phương thức trả về một hộp kết quả: Nếu thành công thì có `Value`, nếu thất bại thì có `Error` rõ ràng.
  - Người gọi hàm bắt buộc phải kiểm tra `if (result.IsSuccess)` tường minh.

### 1.2. Tại sao dùng Domain Business Rules (`IBusinessRule`)?
Thay vì nhồi nhét hàng tá câu lệnh `if-else` vào Service hoặc Controller, ta áp dụng mô hình của Mentor:
- Mỗi quy tắc nghiệp vụ là một Class riêng kế thừa `IBusinessRule`.
- Tuân thủ nguyên lý **Single Responsibility Principle (SRP)**: Mỗi class chỉ có 1 lý do duy nhất để thay đổi.
- Dễ dàng viết Unit Test độc lập cho từng luật nghiệp vụ mà không cần bật Database.

---

## 2. CẤU HÌNH SOLUTION VÀ QUẢN LÝ PACKAGE TẬP TRUNG

### 2.1. File `Directory.Build.props` (Gốc Solution)
File này áp dụng các cài đặt trình biên dịch chung cho toàn bộ dự án trong solution.

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

### 2.2. File `Directory.Packages.props` (Gốc Solution)
Quản lý phiên bản tập trung (Central Package Management), giúp tránh tình trạng project này dùng EF Core 10.0.1, project kia dùng 10.0.5 gây xung đột.

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
  <ItemGroup Label="Validation &amp; Caching">
    <PackageVersion Include="FluentValidation" Version="12.1.1" />
    <PackageVersion Include="Microsoft.Extensions.Caching.Hybrid" Version="10.4.0" />
  </ItemGroup>
  <ItemGroup Label="API &amp; Docs">
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

---

## 3. TRIỂN KHAI CÁC FILE MÃ NGUỒN TẦNG DOMAIN

### 3.1. `src/Depot.CleanArchitecture.Domain/Common/BaseEntity.cs`
Định danh duy nhất bằng `Guid` cho tất cả các thực thể:
```csharp
namespace Depot.CleanArchitecture.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
}
```

### 3.2. `src/Depot.CleanArchitecture.Domain/Common/AuditableEntity.cs`
Theo dõi lịch sử tạo và sửa đổi của bản ghi:
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

### 3.3. `src/Depot.CleanArchitecture.Domain/Common/ITenantEntity.cs`
Giao diện đánh dấu hỗ trợ Multi-Tenancy (phân tách theo từng Depot):
```csharp
namespace Depot.CleanArchitecture.Domain.Common;

public interface ITenantEntity
{
    public Guid TenantId { get; set; }
}
```

### 3.4. `src/Depot.CleanArchitecture.Domain/Common/Error.cs` & `Result.cs`
Triển khai Result Pattern:
```csharp
namespace Depot.CleanArchitecture.Domain.Common;

public enum ErrorType
{
    Failure,
    Validation,
    NotFound,
    Conflict
}

public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
    public static Error Validation(string code, string message) => new(code, message, ErrorType.Validation);
    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);
    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);
}

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

    public TValue Value => IsSuccess 
        ? _value! 
        : throw new InvalidOperationException("Không thể lấy giá trị khi kết quả thất bại.");

    internal Result(TValue? value, bool isSuccess, Error error) 
        : base(isSuccess, error) 
    {
        _value = value;
    }
}
```

### 3.5. `src/Depot.CleanArchitecture.Domain/Common/Rules/IBusinessRule.cs`
Interface cốt lõi cho mọi quy tắc nghiệp vụ:
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

### 3.6. `src/Depot.CleanArchitecture.Domain/Common/Rules/BusinessRuleException.cs`
Ngoại lệ ném ra khi vi phạm luật bất biến:
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

### 3.7. `src/Depot.CleanArchitecture.Domain/Common/Rules/BusinessRuleValidator.cs`
Bộ công cụ kiểm tra luật nghiệp vụ tĩnh:
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

---

## 4. BÀI TEST THỰC HÀNH NGÀY 1

Tạo file `tests/Depot.CleanArchitecture.Domain.UnitTests/Common/BusinessRuleValidatorTests.cs`:
```csharp
namespace Depot.CleanArchitecture.Domain.UnitTests.Common;

using Depot.CleanArchitecture.Domain.Common.Rules;
using FluentAssertions;
using Xunit;

public class BusinessRuleValidatorTests
{
    private class SampleBrokenRule : IBusinessRule
    {
        public string RuleCode => "Test.BrokenRule";
        public string Message => "Quy tắc kiểm thử bị vi phạm.";
        public bool IsBroken() => true;
    }

    private class SampleValidRule : IBusinessRule
    {
        public string RuleCode => "Test.ValidRule";
        public string Message => "Quy tắc kiểm thử hợp lệ.";
        public bool IsBroken() => false;
    }

    [Fact]
    public void CheckRule_WhenRuleIsBroken_ShouldThrowBusinessRuleException()
    {
        var rule = new SampleBrokenRule();
        var act = () => BusinessRuleValidator.CheckRule(rule);

        act.Should().Throw<BusinessRuleException>()
            .WithMessage("*Test.BrokenRule*");
    }

    [Fact]
    public void CheckRule_WhenRuleIsValid_ShouldNotThrow()
    {
        var rule = new SampleValidRule();
        var act = () => BusinessRuleValidator.CheckRule(rule);

        act.Should().NotThrow();
    }
}
```

**Lệnh chạy test**:
```bash
dotnet test tests/Depot.CleanArchitecture.Domain.UnitTests
```
Kết quả mong đợi: `Passed: 2, Failed: 0`.
