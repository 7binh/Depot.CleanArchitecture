# MỤC LỤC GIÁO TRÌNH TRIỂN KHAI 7 NGÀY - DEPOT CLEAN ARCHITECTURE (.NET 10)

Chào mừng bạn đến với lộ trình thực tập **7 Ngày - Vừa Làm Vừa Học (Learn by Doing)** xây dựng Hệ thống Quản lý Depot Container (SNP DMS) theo chuẩn Clean Architecture 4 tầng của Mentor (`TechSpherex.CleanArchitecture`).

---

## DANH SÁCH 7 NGÀY TRIỂN KHAI

| Ngày | Tiêu đề & Trọng tâm Kỹ thuật | File Tài liệu Chi tiết |
| :---: | :--- | :--- |
| **Ngày 1** | **Setup Solution & Domain Primitives**<br>- Central Package Management (`Directory.Packages.props`)<br>- Result Pattern (`Result<T>`, `Error`)<br>- Domain Business Rule Engine (`IBusinessRule`, `BusinessRuleValidator`) | 📄 [NGAY_01_SETUP_VA_DOMAIN_PRIMITIVES.md](file:///c:/HanhTrinhThucTap%20-%20TCIS/Projects/Depot.CleanArchitecture/docs/7days/NGAY_01_SETUP_VA_DOMAIN_PRIMITIVES.md) |
| **Ngày 2** | **Hồ sơ Container & Thuật toán Modulo 11**<br>- Value Object `ContainerNumber` (ISO 6346)<br>- Thuật toán Modulo 11 chi tiết (xử lý phần dư 10 ra `'X'`)<br>- Entity `Container` và phân loại chất lượng vỏ Grade A - E | 📄 [NGAY_02_CONTAINER_VA_THUAT_TOAN_MODULO_11.md](file:///c:/HanhTrinhThucTap%20-%20TCIS/Projects/Depot.CleanArchitecture/docs/7days/NGAY_02_CONTAINER_VA_THUAT_TOAN_MODULO_11.md) |
| **Ngày 3** | **Không gian Bãi (Yard Topology) & Xung đột Bay**<br>- Hệ tọa độ 3D `Block - Bay - Row - Tier`<br>- Thuật toán khóa va chạm Bay chẵn 40ft vs Bay lẻ 20ft<br>- Stacking Rules (Trọng lực, cấm đặt 20ft lên nóc 40ft)<br>- Bản chất vật lý và xử lý **Block ảo** (Xưởng M&R, Sân rửa) | 📄 [NGAY_03_YARD_TOPOLOGY_VA_XUNG_DOT_BAY.md](file:///c:/HanhTrinhThucTap%20-%20TCIS/Projects/Depot.CleanArchitecture/docs/7days/NGAY_03_YARD_TOPOLOGY_VA_XUNG_DOT_BAY.md) |
| **Ngày 4** | **Vòng đời Container (Lifecycle) & Lệnh DO**<br>- State Machine `ContainerVisit` (Gate-In $\rightarrow$ Stack $\rightarrow$ Allocate $\rightarrow$ Gate-Out)<br>- Phiếu Cổng EIR & `VehicleInfo`<br>- Entity `DeliveryOrder` và bộ 5 Business Rules khi xuất bãi | 📄 [NGAY_04_VONG_DOI_CONTAINER_VA_LENH_DO.md](file:///c:/HanhTrinhThucTap%20-%20TCIS/Projects/Depot.CleanArchitecture/docs/7days/NGAY_04_VONG_DOI_CONTAINER_VA_LENH_DO.md) |
| **Ngày 5** | **Tầng Application - CQRS & Báo cáo Thống kê**<br>- Manual CQRS (`ICommand`, `IQuery`, Handlers - không dùng MediatR)<br>- **Báo cáo tồn bãi Aging** (0-10 ngày, $\ge$ 10 ngày)<br>- **Báo cáo sản lượng xuất/nhập** hàng ngày của từng Hãng tàu | 📄 [NGAY_05_APPLICATION_MANUAL_CQRS_VA_BAO_CAO.md](file:///c:/HanhTrinhThucTap%20-%20TCIS/Projects/Depot.CleanArchitecture/docs/7days/NGAY_05_APPLICATION_MANUAL_CQRS_VA_BAO_CAO.md) |
| **Ngày 6** | **Tầng Infrastructure - EF Core 10 & Multi-Tenancy**<br>- Cấu hình Fluent API & Value Conversions cho `ContainerNumber`<br>- **Multi-Tenancy** chia sẻ bảng lọc tự động theo từng Depot (`TenantId`)<br>- Bộ nhớ đệm phân tầng **HybridCache** (.NET 10)<br>- Seeder nạp dữ liệu mẫu thực tế cho Depot Cát Lái | 📄 [NGAY_06_INFRASTRUCTURE_EFCORE_VA_MULTI_TENANCY.md](file:///c:/HanhTrinhThucTap%20-%20TCIS/Projects/Depot.CleanArchitecture/docs/7days/NGAY_06_INFRASTRUCTURE_EFCORE_VA_MULTI_TENANCY.md) |
| **Ngày 7** | **Tầng Api, Scalar Docs & Architecture Tests**<br>- Minimal API Endpoints phân nhóm bằng `RouteGroupBuilder`<br>- Tài liệu tương tác thế hệ mới **Scalar**<br>- Kiểm thử kiến trúc tự động bằng **`NetArchTest`**<br>- Kịch bản kiểm thử End-to-End & **Bộ 5 câu hỏi phỏng vấn Mentor** | 📄 [NGAY_07_API_SCALAR_VA_ARCHITECTURE_TESTS.md](file:///c:/HanhTrinhThucTap%20-%20TCIS/Projects/Depot.CleanArchitecture/docs/7days/NGAY_07_API_SCALAR_VA_ARCHITECTURE_TESTS.md) |

---

## CÁC TÀI LIỆU LIÊN QUAN TRONG DỰ ÁN
- **Tài liệu Đặc tả Thiết kế Hệ thống**: `docs/superpowers/specs/2026-10-06-depot-system-design.md`
- **Kế hoạch Hành động Checkbox theo dõi**: `docs/superpowers/plans/2026-10-06-depot-cleanarchitecture-7days.md`
- **Giáo trình tổng hợp gộp 1 file**: `docs/HUONG_DAN_CHI_TIET_7_NGAY_DEPOT_CLEAN_ARCHITECTURE.md`
