# NGÀY 6: TẦNG INFRASTRUCTURE - EF CORE 10, MULTI-TENANCY & HYBRIDCACHE
**Dự án**: Depot Management System (Clean Architecture .NET 10 - SNP)  
**Mục tiêu Ngày 6**: Cấu hình EF Core 10 (PostgreSQL), thiết lập Multi-Tenancy cô lập dữ liệu theo từng Depot/ICD bằng Global Query Filter, cấu hình bộ nhớ đệm phân tầng HybridCache (.NET 10) và Seeder nạp dữ liệu mẫu ban đầu cho Depot Cát Lái.

---

## 1. MỤC TIÊU HỌC TẬP & NỀN TẢNG LÝ THUYẾT

### 1.1. Multi-Tenancy chia sẻ bảng (Shared-table Multi-Tenancy)
Hệ thống Depot của Tân Cảng Sài Gòn phục vụ nhiều bãi vệ tinh: *Depot Cát Lái, Depot Hiệp Phước, ICD Sóng Thần...*
- Mỗi chi nhánh/Depot là một **Tenant** có `TenantId` riêng biệt.
- **Tại sao dùng Shared-table Multi-Tenancy?**
  - Tất cả các Depot dùng chung một Database duy nhất, mỗi bảng đều có cột `TenantId`.
  - Giảm chi phí vận hành và không cần tạo Database mới khi mở thêm Depot.
  - Sử dụng **Global Query Filter** của EF Core để tự động gắn điều kiện `WHERE TenantId = @CurrentTenantId` vào mọi câu truy vấn, ngăn ngừa nguy cơ nhân viên Cát Lái nhìn thấy dữ liệu bãi của Hiệp Phước!

### 1.2. Microsoft HybridCache (.NET 10)
- Thay thế cho `IMemoryCache` truyền thống.
- Kết hợp cả **L1 (In-Memory Cache tốc độ cực cao)** và **L2 (Distributed Cache Redis)**.
- Tự động chống **Cache Stampede** (khi cache hết hạn, hàng ngàn request cùng ùa vào Database làm sập server; HybridCache chỉ cho phép 1 request vào DB tải dữ liệu mới và chia sẻ cho các request khác).

---

## 2. TRIỂN KHAI CÁC FILE MÃ NGUỒN TẦNG INFRASTRUCTURE

### 2.1. Cấu hình Multi-Tenancy
Tạo file `src/Depot.CleanArchitecture.Infrastructure/Tenancy/ITenantProvider.cs`:
```csharp
namespace Depot.CleanArchitecture.Infrastructure.Tenancy;

public interface ITenantProvider
{
    Guid CurrentTenantId { get; }
    void SetTenantId(Guid tenantId);
}

public class TenantProvider : ITenantProvider
{
    // Mặc định gán ID của Depot Tân Cảng Cát Lái
    public static readonly Guid DefaultDepotCatLaiId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private Guid _currentTenantId = DefaultDepotCatLaiId;

    public Guid CurrentTenantId => _currentTenantId;

    public void SetTenantId(Guid tenantId)
    {
        _currentTenantId = tenantId;
    }
}
```

Tạo file `src/Depot.CleanArchitecture.Infrastructure/Tenancy/TenantMiddleware.cs`:
```csharp
namespace Depot.CleanArchitecture.Infrastructure.Tenancy;

using Microsoft.AspNetCore.Http;

public class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider)
    {
        // Đọc TenantId từ HTTP Header "X-Tenant-Id"
        if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantHeader) &&
            Guid.TryParse(tenantHeader, out var tenantId))
        {
            tenantProvider.SetTenantId(tenantId);
        }

        await _next(context);
    }
}
```

### 2.2. Lớp `AppDbContext.cs` với Global Query Filter
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

        // Tự động cấu hình Global Query Filter cho tất cả Entity có ITenantEntity
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

        // Tự động gán TenantId cho entity mới tạo
        foreach (var entry in ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
            {
                entry.Entity.TenantId = currentTenantId;
            }
        }

        // Tự động cập nhật thời gian kiểm toán
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added) entry.Entity.CreatedAt = DateTime.UtcNow;
            if (entry.State == EntityState.Modified) entry.Entity.LastModifiedAt = DateTime.UtcNow;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
```

### 2.3. Cấu hình Fluent API & Value Conversion cho `Container`
Tạo file `src/Depot.CleanArchitecture.Infrastructure/Persistence/Configurations/ContainerConfiguration.cs`:
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

        // Chuyển đổi Value Object ContainerNumber <--> Cột varchar(11) trong Database
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

### 2.4. Seeder Dữ liệu mẫu Ban đầu (`AppDbSeeder.cs`)
Tạo file `src/Depot.CleanArchitecture.Infrastructure/Persistence/AppDbSeeder.cs`:
```csharp
namespace Depot.CleanArchitecture.Infrastructure.Persistence;

using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using Depot.CleanArchitecture.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;

public static class AppDbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.YardBlocks.AnyAsync()) return;

        var tenantId = TenantProvider.DefaultDepotCatLaiId;

        // 1. Tạo Block A (Block thường)
        var blockA = YardBlock.CreatePhysical(tenantId, "BLK-A", "Khu Bãi A (Container Khô)", maxBay: 10, maxRow: 6, maxTier: 5);
        context.YardBlocks.Add(blockA);

        // 2. Tạo Block ảo M&R (Xưởng sửa chữa)
        var blockMR = YardBlock.CreateVirtual(tenantId, "BLK-MR", "Xưởng Sửa Chữa Container M&R", maxCapacity: 50);
        context.YardBlocks.Add(blockMR);

        // 3. Tạo một số Container mẫu
        var cont1Number = ContainerNumber.Create("CSQU3054383").Value;
        var cont1 = Container.Create(tenantId, cont1Number, "MAERSK", ContainerType.Dry, "22G1", 20, 2200, 30480, ContainerGrade.GradeA).Value;
        context.Containers.Add(cont1);

        // 4. Tạo Lệnh Giao Container (DO) mẫu của Maersk
        var doOrder = DeliveryOrder.Create(
            tenantId, 
            "DO-MAERSK-202610", 
            "MAERSK", 
            "Công ty Xuất Khẩu Gạo Tân Cảng", 
            "0301234567",
            DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            "MAERSK TACOMA", 
            "2601N");
        doOrder.AddItem(20, ContainerType.Dry, ContainerGrade.GradeA, 5);
        context.DeliveryOrders.Add(doOrder);

        await context.SaveChangesAsync();
    }
}
```

---

## 3. THỰC HÀNH NGÀY 6
Chạy Migration hoặc sử dụng DbContext kiểm tra nạp dữ liệu:
- Kiểm tra tính năng lọc tự động của Multi-Tenancy: Khi đổi `X-Tenant-Id`, dữ liệu của Depot khác không bị lộ ra.
- Kiểm tra dữ liệu được nạp đầy đủ qua Seeder.
