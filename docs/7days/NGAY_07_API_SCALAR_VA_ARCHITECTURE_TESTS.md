# NGÀY 7: TẦNG API, SCALAR OPENAPI, ARCHITECTURE TESTS & KIỂM THỬ END-TO-END
**Dự án**: Depot Management System (Clean Architecture .NET 10 - SNP)  
**Mục tiêu Ngày 7**: Xây dựng Minimal APIs, tích hợp tài liệu tương tác Scalar, viết bộ kiểm thử kiến trúc tự động bằng NetArchTest, chạy luồng kiểm thử toàn diện End-to-End và chuẩn bị bộ câu hỏi vấn đáp bảo vệ dự án trước Mentor.

---

## 1. MỤC TIÊU HỌC TẬP & NỀN TẢNG LÝ THUYẾT

### 1.1. Minimal APIs & Scalar thay thế Swagger UI
- **Minimal APIs trong .NET 10**: Nhẹ, nhanh, giảm thiểu overhead của Controller truyền thống, cấu trúc theo từng nhóm route (`RouteGroupBuilder`).
- **Scalar.AspNetCore**: Thư viện tài liệu API thế hệ mới thay thế Swagger UI, giao diện tối màu (Dark Mode) tuyệt đẹp, tốc độ render cực nhanh và hỗ trợ gọi thử API trực tiếp.

### 1.2. Architecture Tests với NetArchTest
Làm sao để bảo đảm các lập trình viên khác trong nhóm không "vô tình" `using` tầng Infrastructure trong tầng Domain?
- Viết bài kiểm thử tự động bằng **`NetArchTest.Rules`**.
- Nếu có bất kỳ sự vi phạm nào về Dependency Rule, bài test sẽ báo ĐỎ ngay khi chạy `dotnet test` hoặc trong pipeline CI/CD, ngăn chặn việc merge mã nguồn sai kiến trúc!

---

## 2. TRIỂN KHAI CÁC FILE MÃ NGUỒN TẦNG API

### 2.1. Nhóm Endpoints Báo cáo Thống kê (`AnalyticsEndpoints.cs`)
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
        .WithSummary("Thống kê container tồn bãi theo hãng tàu (0-10 ngày và trên 10 ngày)");

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
        .WithSummary("Thống kê sản lượng xuất/nhập theo ngày của từng hãng tàu");

        return group;
    }
}
```

### 2.2. Điểm Khởi động Ứng dụng `Program.cs`
Cấu hình nạp DI, Scalar và Middleware:
```csharp
using Depot.CleanArchitecture.Api.Endpoints;
using Depot.CleanArchitecture.Infrastructure.Persistence;
using Depot.CleanArchitecture.Infrastructure.Tenancy;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký dịch vụ tầng Infrastructure & Tenancy
builder.Services.AddSingleton<ITenantProvider, TenantProvider>();

// Cấu hình OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

// Cấu hình Middleware Multi-Tenancy
app.UseMiddleware<TenantMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.Title = "Hệ Thống Quản Lý Depot Tân Cảng (SNP DMS API)";
        options.Theme = ScalarTheme.Mars;
    });

    // Tự động nạp dữ liệu mẫu ban đầu
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await AppDbSeeder.SeedAsync(dbContext);
}

// Định tuyến API theo nhóm
var apiGroup = app.MapGroup("/api");
apiGroup.MapGroup("/analytics").MapAnalyticsEndpoints();

app.Run();
```

---

## 3. BỘ KIỂM THỬ KIẾN TRÚC VỚI NETARCHTEST

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
    public void Domain_ShouldNot_DependOn_OtherLayers()
    {
        var otherProjects = new[] { ApplicationNamespace, InfrastructureNamespace, ApiNamespace };
        
        var result = Types.InAssembly(typeof(Domain.Common.BaseEntity).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(otherProjects)
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Tầng Domain không được phép phụ thuộc vào bất kỳ tầng nào khác!");
    }

    [Fact]
    public void Application_ShouldNot_DependOn_Infrastructure_Or_Api()
    {
        var otherProjects = new[] { InfrastructureNamespace, ApiNamespace };

        var result = Types.InAssembly(typeof(Application.Abstractions.Data.IAppDbContext).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(otherProjects)
            .GetResult();

        result.IsSuccessful.Should().BeTrue("Tầng Application không được phép phụ thuộc vào Infrastructure hoặc Api!");
    }
}
```

**Lệnh chạy kiểm thử kiến trúc**:
```bash
dotnet test tests/Depot.CleanArchitecture.Architecture.Tests
```
Kết quả mong đợi: `Passed: 2, Failed: 0`.

---

## 4. KỊCH BẢN KIỂM THỬ TOÀN DIỆN END-TO-END

Khi mở giao diện Scalar API tại `https://localhost:5001/scalar/v1`, bạn có thể thực hiện kiểm thử theo chu trình nghiệp vụ thực tế:

1. **Bước 1 (Nhập bãi)**: Gọi API Gate-In nhập container `CSQU3054383` (Hãng MAERSK, loại 20ft Khô, Grade A).
2. **Bước 2 (Xếp bãi)**: Gọi API điều xe nâng hạ cont vào Block A, Bay 01, Row 01, Tier 01.
3. **Bước 3 (Kiểm tra va chạm)**: Gọi API thử hạ cont 40ft vào Bay 02, Row 01, Tier 01 $\rightarrow$ Hệ thống tự động chặn và trả về lỗi: *"Xung đột không gian vật lý với container ở Bay 01"*.
4. **Bước 4 (Xuất bãi)**: Gọi API Gate-Out cont theo Lệnh DO của hãng MAERSK $\rightarrow$ Xuất thành công, ô bãi Bay 01 được giải phóng về trạng thái trống.
5. **Bước 5 (Xem báo cáo)**: Gọi API `/api/analytics/aging` và `/api/analytics/throughput` $\rightarrow$ Số liệu thống kê sản lượng và thời gian lưu bãi được cập nhật tức thì.

---

## 5. BỘ CÂU HỎI & TRẢ LỜI VẤN ĐÁP KỸ THUẬT VỚI MENTOR

Dưới đây là 5 câu hỏi kinh điển Mentor thường hỏi và câu trả lời chuẩn mực giúp bạn ghi điểm tuyệt đối:

### Câu 1: "Tại sao em tự viết Manual CQRS mà không dùng MediatR?"
- **Trả lời**:  
  *"Thưa anh/chị, em chọn Manual CQRS vì: Thứ nhất, MediatR v13 trở đi đã chuyển sang thu phí thương mại, nên các dự án doanh nghiệp ưu tiên tự triển khai để tránh phụ thuộc pháp lý. Thứ hai, việc tự dựng `ICommand`, `IQuery`, `ICommandHandler` giúp em hiểu sâu nguyên lý Dependency Inversion (DIP) và cơ chế đăng ký Reflection trong DI Container. Thứ ba, hiệu năng gọi trực tiếp trong DI nhanh hơn và dễ debug stack trace hơn so với pipeline trung gian của MediatR."*

### Câu 2: "Tại sao số container em lại dùng Value Object thay vì string?"
- **Trả lời**:  
  *"Thưa anh/chị, số container tuân theo chuẩn quốc tế ISO 6346 với thuật toán kiểm tra Modulo 11. Nếu em dùng `string`, nó sẽ trở thành lỗi thiết kế 'Primitive Obsession' và phải copy logic validate ở khắp mọi nơi. Sử dụng Value Object `ContainerNumber` bảo đảm tính bất biến (Immutability). Một khi đối tượng được tạo ra, toàn bộ hệ thống hoàn toàn yên tâm rằng số container đó 100% hợp lệ."*

### Câu 3: "Hệ thống của em giải quyết va chạm Bay chẵn 40ft và Bay lẻ 20ft như thế nào?"
- **Trả lời**:  
  *"Thưa anh/chị, theo quy ước cảng biển, Bay lẻ (01, 03) chứa cont 20ft, Bay chẵn (02) chứa cont 40ft chiếm trọn không gian của cả Bay 01 và 03. Em đã cài đặt `EvenOddBayCollisionRule`: Khi hạ cont 40ft tại Bay 02, hệ thống tự động kiểm tra xem Bay 01 hoặc 03 tại cùng Row, cùng Tier đã có cont chưa; nếu có thì chặn ngay. Ngược lại, khi hạ cont 20ft tại Bay 01/03, hệ thống cũng kiểm tra xem Bay 02 có đang bị cont 40ft chiếm dụng không. Nhờ vậy ngăn chặn hoàn toàn việc xe nâng hạ đè cont ngoài bãi."*

### Câu 4: "Block ảo là gì và em lưu trữ nó trong CSDL ra sao?"
- **Trả lời**:  
  *"Thưa anh/chị, Block ảo là khu vực bãi logic không chia lưới 3D Bay/Row/Tier, ví dụ như Xưởng sửa chữa M&R, Sân rửa vỏ hay Bãi đệm cổng. Trong database, thực thể `YardBlock` có cờ `IsVirtual = true`, các trường `Bay, Row, Tier` được phép `null`. Thay vì kiểm tra va chạm tọa độ, hệ thống kiểm tra Sức chứa tối đa (`MaxCapacity`) qua rule `VirtualBlockCapacityRule`."*

### Câu 5: "Làm sao bảo đảm các thành viên trong nhóm không code vi phạm kiến trúc Clean Architecture?"
- **Trả lời**:  
  *"Em đã thiết lập bộ kiểm thử kiến trúc tự động bằng thư viện `NetArchTest.Rules` trong project `Architecture.Tests`. Mỗi khi có ai đó trong tầng Domain cố tình tham chiếu tầng Infrastructure hay Api, khi chạy lệnh `dotnet test` hoặc trong pipeline CI/CD, bài test sẽ Fail ngay lập tức và chặn việc tạo Pull Request."*
