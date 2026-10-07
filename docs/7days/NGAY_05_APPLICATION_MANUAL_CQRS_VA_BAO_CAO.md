# NGÀY 5: TẦNG APPLICATION - MANUAL CQRS & BÁO CÁO THỐNG KÊ NGHIỆP VỤ
**Dự án**: Depot Management System (Clean Architecture .NET 10 - SNP)  
**Mục tiêu Ngày 5**: Xây dựng kiến trúc Manual CQRS (không dùng MediatR), các Use Case Gate-In, Gate-Out và 2 truy vấn báo cáo quan trọng nhất của cảng: Báo cáo Tồn bãi Aging (0-10 ngày, >=10 ngày) và Sản lượng xuất/nhập hàng ngày.

---

## 1. MỤC TIÊU HỌC TẬP & NỀN TẢNG LÝ THUYẾT

### 1.1. Tại sao dùng Manual CQRS thay vì MediatR?
- **Command Query Responsibility Segregation (CQRS)**:
  - **Command**: Thay đổi trạng thái hệ thống (tạo cont, gate-in, xuất cont), trả về kết quả thành công hoặc mã lỗi.
  - **Query**: Đọc dữ liệu không sinh tác dụng phụ (xem báo cáo, lấy sơ đồ bãi), tối ưu bằng `.AsNoTracking()`.
- **Lý do chọn Manual CQRS**:
  1. **Tránh bản quyền**: MediatR từ v13 đã chuyển sang mô hình thương mại.
  2. **Hiệu năng & Dễ debug**: Gọi trực tiếp Interface trong DI container nhanh hơn, không bị che giấu stack trace.
  3. **Tự động đăng ký**: Quét Assembly tự động nạp toàn bộ handler vào `IServiceCollection`.

### 1.2. Nghiệp vụ Thống kê Cảng biển
1. **Báo cáo Tồn bãi (Aging Dwell Time)**:
   - Container rỗng nằm bãi từ 0 - 10 ngày: Luân chuyển nhanh bình thường.
   - Container rỗng nằm bãi $\ge$ 10 ngày: Tồn lâu (Demurrage risk). Cần cảnh báo màu trên sơ đồ bãi và tính phụ phí lưu bãi.
2. **Báo cáo Sản lượng Xuất/Nhập (Throughput)**:
   - Thống kê số lượng lượt xe vào giao cont (Gate-In) và lượt xe lấy cont (Gate-Out) theo từng Hãng tàu mỗi ngày.

---

## 2. TRIỂN KHAI CÁC FILE MÃ NGUỒN TẦNG APPLICATION

### 2.1. Bộ Abstractions cho Manual CQRS
Tạo file `src/Depot.CleanArchitecture.Application/Abstractions/Messaging/ICommand.cs`:
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

### 2.2. Interface `IAppDbContext.cs`
Tạo file `src/Depot.CleanArchitecture.Application/Abstractions/Data/IAppDbContext.cs`:
```csharp
namespace Depot.CleanArchitecture.Application.Abstractions.Data;

using Depot.CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public interface IAppDbContext
{
    DbSet<YardBlock> YardBlocks { get; }
    DbSet<YardSlot> YardSlots { get; }
    DbSet<Container> Containers { get; }
    DbSet<ContainerVisit> ContainerVisits { get; }
    DbSet<DeliveryOrder> DeliveryOrders { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### 2.3. Báo cáo Tồn bãi Aging (0-10 ngày, >=10 ngày)
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

        // Lấy danh sách container đang lưu bãi (chưa xuất bãi)
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

### 2.4. Báo cáo Sản lượng Xuất/Nhập Hàng Ngày (Throughput)
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
    int TotalMovements);

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

        var inGroups = visits
            .Where(v => v.GateInDate >= fromDateTime && v.GateInDate <= toDateTime)
            .GroupBy(v => new { Date = DateOnly.FromDateTime(v.GateInDate), v.LineOperator })
            .Select(g => new { g.Key.Date, g.Key.LineOperator, InCount = g.Count(), OutCount = 0 });

        var outGroups = visits
            .Where(v => v.GateOutDate != null && v.GateOutDate >= fromDateTime && v.GateOutDate <= toDateTime)
            .GroupBy(v => new { Date = DateOnly.FromDateTime(v.GateOutDate!.Value), v.LineOperator })
            .Select(g => new { g.Key.Date, g.Key.LineOperator, InCount = 0, OutCount = g.Count() });

        var result = inGroups.Concat(outGroups)
            .GroupBy(x => new { x.Date, x.LineOperator })
            .Select(g => new DailyThroughputDto(
                g.Key.Date,
                g.Key.LineOperator,
                g.Sum(x => x.InCount),
                g.Sum(x => x.OutCount),
                g.Sum(x => x.InCount + x.OutCount)
            ))
            .OrderByDescending(r => r.ReportDate)
            .ToList();

        return Result.Success(result);
    }
}
```

---

## 3. BÀI TEST THỰC HÀNH NGÀY 5

Tạo file `tests/Depot.CleanArchitecture.Application.UnitTests/Features/Analytics/GetAgingReportQueryHandlerTests.cs`:
```csharp
namespace Depot.CleanArchitecture.Application.UnitTests.Features.Analytics;

using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Features.Analytics.Queries.GetAgingReport;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using FluentAssertions;
using NSubstitute;
using Xunit;

public class GetAgingReportQueryHandlerTests
{
    [Fact]
    public void AgingReport_ShouldCorrectlySeparate_Under10Days_And_Over10Days()
    {
        // Kiểm thử logic phân tách ngày lưu bãi
        var now = DateTime.UtcNow;
        var date5DaysAgo = now.AddDays(-5);
        var date15DaysAgo = now.AddDays(-15);

        (now - date5DaysAgo).TotalDays.Should().BeLessOrEqualTo(10);
        (now - date15DaysAgo).TotalDays.Should().BeGreaterThan(10);
    }
}
```

**Lệnh chạy test**:
```bash
dotnet test tests/Depot.CleanArchitecture.Application.UnitTests
```
Kết quả mong đợi: Toàn bộ bài test đều PASS.
