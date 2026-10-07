# TÀI LIỆU THIẾT KẾ HỆ THỐNG QUẢN LÝ DEPOT CONTAINER (SNP DMS)
**Dự án**: Hệ Thống Quản Lý Depot Container (Tân Cảng Sài Gòn - SNP)  
**Kiến trúc**: Clean Architecture 4 Tầng (.NET 10, C# 13/14, Manual CQRS, Aspire, HybridCache, Multi-Tenancy)  
**Ngày lập**: 06/10/2026  
**Trạng thái**: Bản Thiết Kế Chi Tiết (Approved Design Spec)  

---

## 1. TỔNG QUAN HỆ THỐNG & BỐI CẢNH DỰ ÁN

### 1.1. Bối cảnh Nghiệp vụ
Tổng công ty Tân Cảng Sài Gòn (SNP) là doanh nghiệp khai thác cảng và logistics hàng đầu Việt Nam. Để điều hành mạng lưới các bãi container vệ tinh (Depot) và cảng cạn (ICD), hệ thống **Depot Management System (DMS)** cần quản lý toàn diện các hoạt động giao nhận, sắp xếp vị trí container trên bãi, giám định tình trạng vỏ, kiểm soát thủ tục cổng và đáp ứng các yêu cầu báo cáo sản lượng, tồn bãi cho các Hãng tàu quốc tế (Line Operators).

### 1.2. Mục tiêu Thiết kế
1. **Chuẩn hóa nghiệp vụ Cảng biển - Logistics**:
   - Quản lý sơ đồ bãi container 3D (Block - Bay - Row - Tier).
   - Xử lý thuật toán xung đột không gian vật lý giữa container 20 feet và 40 feet (Bay chẵn / Bay lẻ).
   - Hỗ trợ Block ảo (Virtual Block) cho bãi sửa chữa (M&R), bãi rửa cont, bãi đệm cổng.
   - Kiểm tra tính hợp lệ của số container theo chuẩn quốc tế **ISO 6346** bằng thuật toán **Modulo 11**.
   - Kiểm soát vòng đời container (Container Visit) và ràng buộc pháp lý của Lệnh giao vỏ container (Delivery Order - DO) từ Hãng tàu.
   - Cung cấp các báo cáo thống kê quan trọng: Tồn bãi theo thời gian (0-10 ngày, $\ge$ 10 ngày) và Sản lượng xuất/nhập theo ngày của từng Hãng tàu.
2. **Kế thừa và chuẩn hóa theo Template Doanh nghiệp của Mentor (`TechSpherex.CleanArchitecture`)**:
   - 4 tầng Clean Architecture không phụ thuộc vòng lặp (Dependency Rule).
   - CQRS thủ công (Manual CQRS: `ICommand`, `IQuery`, `ICommandHandler`, `IQueryHandler`), không phụ thuộc thư viện thương mại MediatR.
   - Đóng gói luật nghiệp vụ bất biến bằng mô hình `IBusinessRule` và `BusinessRuleValidator`.
   - Cơ chế Multi-Tenancy chia sẻ bảng (Shared-table Multi-Tenancy) với `ITenantEntity` đại diện cho từng Depot/ICD.
   - Bộ nhớ đệm phân tầng **HybridCache** (.NET 10 L1 Memory + L2 Redis).
   - API Minimal endpoints với Scalar documentation, kiểm soát lỗi tập trung với `Result<T>` và `GlobalExceptionHandler`.

---

## 2. KIẾN TRÚC KỸ THUẬT & ÁNH XẠ CÔNG NGHỆ

### 2.1. Phân tầng Kiến trúc (Clean Architecture Layering)

```
┌────────────────────────────────────────────────────────────────────────┐
│                   Depot.CleanArchitecture.Api                          │
│   - Minimal API Endpoints (Yard, Containers, Gate, Orders, Analytics)   │
│   - Scalar OpenAPI UI & Documentation                                  │
│   - Extensions: GlobalExceptionHandler, ResultExtensions, Validation   │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ Phụ thuộc vào (Depends on)
┌───────────────────────────────────▼────────────────────────────────────┐
│              Depot.CleanArchitecture.Infrastructure                    │
│   - Persistence: AppDbContext (EF Core 10), Entity Configurations     │
│   - Caching: HybridCacheService (L1 InMemory + L2 Redis)              │
│   - Tenancy: TenantProvider, TenantMiddleware (Depot Context)          │
│   - Database: PostgreSQL 16+ / SQL Server                              │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ Phụ thuộc vào (Depends on)
┌───────────────────────────────────▼────────────────────────────────────┐
│               Depot.CleanArchitecture.Application                      │
│   - Manual CQRS (Commands, Queries, Handlers - No MediatR)             │
│   - Features Vertical Slice: Yard/, Containers/, Gate/, Orders/        │
│   - Validation: FluentValidation Rules & Pipeline                      │
│   - Abstractions: IAppDbContext, ICacheService, ITenantProvider        │
└───────────────────────────────────┬────────────────────────────────────┘
                                    │ Phụ thuộc vào (Depends on)
┌───────────────────────────────────▼────────────────────────────────────┐
│                  Depot.CleanArchitecture.Domain                        │
│   - Entities: YardBlock, YardSlot, Container, ContainerVisit, DO...    │
│   - Value Objects: ContainerNumber (ISO 6346), SlotCoordinate, Weight   │
│   - Business Rules: IBusinessRule, BusinessRuleValidator, Exceptions   │
│   - Common: Result<T>, Error, BaseEntity, AuditableEntity, ITenant     │
└────────────────────────────────────────────────────────────────────────┘
```

### 2.2. Chi tiết các Quyết định Công nghệ (Design Decisions)

| Thành phần | Lựa chọn công nghệ | Lý do kỹ thuật & Lợi ích |
| :--- | :--- | :--- |
| **Framework** | .NET 10 & C# 13/14 | Hiệu năng cao nhất, tính năng ngôn ngữ mới, tương thích template mentor. |
| **Mô hình CQRS** | Manual CQRS (Không dùng MediatR) | Tránh rủi ro bản quyền của MediatR v13+; cơ chế reflection tự động đăng ký handlers qua DI. |
| **Quản lý Lỗi** | Result Pattern (`Result<T>`, `Error`) | Tránh dùng exception cho luồng xử lý thông thường, mã lỗi tường minh, an toàn kiểu dữ liệu. |
| **Business Rule** | `IBusinessRule` + Validator | Đóng gói logic nghiệp vụ vào Domain, dễ dàng viết Unit Test độc lập 100%. |
| **ORM & Database** | EF Core 10 + PostgreSQL | Hỗ trợ Global Query Filters cho multi-tenancy, Jsonb, và các hàm DateDiff tối ưu. |
| **Bộ nhớ đệm** | Microsoft HybridCache | Chống cache stampede, kết hợp L1 (in-process) cho cấu hình bãi và L2 (Redis) cho toàn hệ thống. |
| **Multi-Tenancy** | Shared-table với `TenantId` | Mỗi Depot (Cát Lái, Hiệp Phước, Sóng Thần...) là một tenant độc lập dữ liệu. |
| **API Docs** | Scalar.AspNetCore | Giao diện hiện đại, tốc độ tải nhanh, thay thế hoàn toàn Swagger UI cổ điển. |
| **Kiểm thử** | xUnit v3, FluentAssertions, NetArchTest | Kiểm thử logic xử lý và tự động chặn các vi phạm phụ thuộc giữa các tầng kiến trúc. |

---

## 3. PHÂN TÍCH CHI TIẾT NGHIỆP VỤ & QUY TẮC BẤT BIẾN (BUSINESS RULES)

### MODULE 1: QUẢN LÝ BÃI CONTAINER (YARD TOPOLOGY)

#### 1. Hệ tọa độ 3D bãi: Block - Bay - Row - Tier
- **Block**: Khu vực bãi đất vật lý hoặc logic (VD: Block A, B, C, D).
- **Bay (Trục X - Chiều dài)**: Đánh số dọc theo chiều dài của Block.
- **Row (Trục Y - Chiều rộng)**: Đánh số các hàng container xếp song song (hướng từ lối xe chạy vào trong).
- **Tier (Trục Z - Chiều cao tầng)**: Đánh số các tầng container xếp chồng lên nhau (Tier 1 là tầng sát đất, Tier 2 đè lên Tier 1...).

#### 2. Thuật toán Xung đột Bay chẵn / Bay lẻ (Even/Odd Bay Collision)
- **Quy ước quốc tế**:
  - Bay lẻ ($01, 03, 05, 07, \dots$): Dùng cho **container 20 feet**.
  - Bay chẵn ($02, 04, 06, 08, \dots$): Dùng cho **container 40 feet**.
  - Một container 40 feet tại Bay $2k$ sẽ chiếm trọn không gian vật lý của hai bay lẻ liền kề $2k-1$ và $2k+1$ (cùng Row, cùng Tier).
- **Quy tắc bất biến (Invariants)**:
  - **Khóa 40ft khi đã có 20ft**: Nếu tại `(Bay 2k-1, Row, Tier)` HOẶC `(Bay 2k+1, Row, Tier)` đã có container $\rightarrow$ **Cấm hạ** container 40ft vào `(Bay 2k, Row, Tier)`.
  - **Khóa 20ft khi đã có 40ft**: Nếu tại `(Bay 2k, Row, Tier)` đang có container 40ft $\rightarrow$ **Cấm hạ** container 20ft vào cả `(Bay 2k-1, Row, Tier)` và `(Bay 2k+1, Row, Tier)`.
  - **Mã Rule**: `Yard.EvenOddBayCollision` (`EvenOddBayCollisionRule`).

#### 3. Quy tắc Xếp tầng an toàn (Stacking & Safety Rules)
- **Quy tắc Trọng lực / Tầng đỡ (Foundation Support)**:
  - Để hạ container ở `Tier > 1`, thì tại đúng tọa độ `(Bay, Row, Tier - 1)` bắt buộc phải có một container làm bệ đỡ.
  - **Mã Rule**: `Yard.TierMissingBottomSupport` (`TierMustHaveBottomSupportRule`).
- **Quy tắc Chiều dài khi xếp chồng (Size Stacking Rule)**:
  - **TUYỆT ĐỐI CẤM** đặt container 20ft lên trên container 40ft (vì 4 góc khóa gù của cont 20ft sẽ đè vào giữa nóc cont 40ft gây bẹp/thủng nóc).
  - Cho phép đặt 1 container 40ft lên trên 2 container 20ft liền kề bên dưới (nếu cùng chiều cao).
  - **Mã Rule**: `Yard.InvalidSizeStacking` (`SizeStackingSafetyRule`).
- **Quy tắc Chiều cao tối đa (Max Tier Limit)**:
  - Chiều cao tầng không được vượt quá cấu hình `MaxTier` của Block (thông thường bãi rỗng tối đa 6-7 tầng, bãi có hàng tối đa 4 tầng).
  - **Mã Rule**: `Yard.MaxTierExceeded` (`BlockMaxTierExceededRule`).
- **Quy tắc Khu vực bãi theo Loại Container (Reefer, Dry, OOG)**:
  - *Container Khô (Dry)*: Chiếm 80-90% bãi, xếp chồng 5-7 tầng đối với vỏ rỗng.
  - *Container Lạnh (Reefer)*: Bắt buộc xếp ở Block chuyên dụng có giàn trụ cắm điện lạnh (Reefer Power Receptacles), giới hạn chiều cao 3-4 tầng để thợ điện lạnh theo dõi nhiệt độ và cắm/rút giắc an toàn. Mã Rule: `Yard.ReeferRequiresPowerReceptacle` (`ReeferYardAssignmentRule`).
  - *Container Mở Nóc (Open Top) & Flat Rack quá khổ (OOG)*: Nếu có hàng nhô cao, bắt buộc phải xếp ở **Tầng trên cùng (Top Tier)**, cấm xếp cont khác đè lên nóc. Mã Rule: `Yard.OverheightMustBeTopTier` (`OverheightStackingRule`). Vỏ Flat Rack rỗng gập vách có thể bó lại thành cụm (Bundle).

#### 4. Quy tắc Block ảo (Virtual Block) & Bản chất Vật lý
- **Bản chất thực tế**: Container vật lý vẫn nằm trên mặt đất thật trong khuôn viên Depot, nhưng thuộc các khu vực chuyên biệt không kẻ vạch sơn chia tọa độ 3D Bay/Row/Tier:
  - *Xưởng sửa chữa M&R (Maintenance & Repair)*: Nơi thợ cơ khí bắc thang, hàn gò, thay đà vách; cont đặt rải rác trên mặt sàn xưởng, luân chuyển theo tiến độ sửa chữa.
  - *Sân rửa / Vệ sinh vỏ cont (Washing Area)*: Nơi xịt rửa sàn gỗ dơ, luân chuyển nhanh (30-45 phút/cont), không xếp tầng.
  - *Bãi đệm cổng (Gate Buffer Area)*: Nơi hạ cont tạm vào giờ cao điểm kẹt xe cổng để giải phóng xe đầu kéo, sau đó xe nâng mới chuyển về Block chính.
  - *Khu kiểm hóa Hải quan (Customs Hold)*: Nơi niêm phong chờ mở kẹp chì kiểm tra.
- **Mô hình hóa phần mềm**:
  - `IsVirtual = true`. Các giá trị `Bay, Row, Tier = null`.
  - Quản lý theo **Sức chứa tổng (Capacity / Max TEU)**: Khi nhận container vào Block ảo, hệ thống kiểm tra $\text{CurrentCount} < \text{MaxCapacity}$.
  - **Mã Rule**: `Yard.VirtualBlockCapacityExceeded` (`VirtualBlockCapacityRule`).

---

### MODULE 2: CHUẨN HÓA DỮ LIỆU CONTAINER & THUẬT TOÁN MODULO 11

#### 1. Cấu trúc Số Container (ISO 6346)
Số container gồm đúng **11 ký tự**:
- **Owner Code**: 3 chữ cái viết hoa đại diện Hãng tàu / Chủ sở hữu (VD: `CMA`, `MSK`, `ONE`, `HMM`).
- **Category Identifier**: 1 chữ cái viết hoa (`U`: container thông thường, `R`: container lạnh, `J`: thiết bị tháo rời, `Z`: rơ-moóc).
- **Serial Number**: Đúng 6 chữ số từ `000001` đến `999999`.
- **Check Digit**: 1 chữ số (hoặc ký tự 'X') ở vị trí thứ 11, tính theo thuật toán Modulo 11.

#### 2. Thuật toán Kiểm tra Check Digit Modulo 11
- **Bảng quy đổi ký tự**:
  - Chữ cái: $A=10, B=12, C=13, D=14, E=15, F=16, G=17, H=18, I=19, J=20,$
    $K=21, L=23, M=24, N=25, O=26, P=27, Q=28, R=29, S=30, T=31,$
    $U=32, V=34, W=35, X=36, Y=37, Z=38$ *(Lưu ý: Bỏ qua 11, 22, 33)*.
  - Chữ số $0 \dots 9$: Giá trị giữ nguyên $0 \dots 9$.
- **Trọng số vị trí**: Ký tự thứ $i$ ($i = 1 \dots 10$) nhân với trọng số $2^{i-1}$.
- **Tính toán**:
  $$\text{Tổng} = \sum_{i=1}^{10} (\text{Giá trị ký tự } i \times 2^{i-1})$$
  $$\text{Phần dư} = \text{Tổng} \pmod{11}$$
  - Nếu $\text{Phần dư} \in [0, 9] \rightarrow \text{CheckDigit} = \text{Phần dư}$.
  - Nếu $\text{Phần dư} = 10 \rightarrow \text{CheckDigit} = \text{'X'}$ (theo tài liệu SNP).
- **Thiết kế Value Object**: `ContainerNumber` tự động validate khi khởi tạo; nếu sai thuật toán Modulo 11 $\rightarrow$ Trả về lỗi `Container.InvalidCheckDigit`.

#### 3. Tiêu chuẩn Giám định & Phân loại Chất lượng Vỏ (Grading A - E)
- **Grade A**: Vỏ loại 1, xuất sắc (Food/Electronic grade). Kín nước kín sáng 100%, sàn gỗ sạch đẹp không mùi hôi. Cấp đóng gạo, thực phẩm, hạt điều, dệt may, điện tử.
- **Grade B**: Vỏ loại 2, trung bình (Cargo worthy - General dry cargo). Trầy xước nhẹ, sàn gỗ ố mờ nhưng không thủng. Cấp đóng bách hóa tổng hợp, máy móc đóng kiện.
- **Grade C**: Vỏ loại 3, xuống cấp bề mặt (Rough cargo). Sàn trầy xước nhiều, có vết dầu mỡ hoặc rỉ sét ngoài bề mặt, nhưng kết cấu khung chịu lực vẫn an toàn. **Chỉ được cấp cho khách đóng hàng thô**: quặng, phế liệu, gỗ, xơ dừa. Cấm cấp cho khách yêu cầu Grade A/B.
- **Grade D / Damaged**: Vỏ hư hỏng (rách vách, thủng nóc, cong đà đáy, hỏng bản lề cửa). **Cấm cấp ra cổng cho khách hàng**. Tự động khóa và chuyển sang Block ảo M&R để lập báo giá (Estimate of Repair) và sửa chữa.
- **Grade E / Scrap**: Vỏ biến dạng toàn phần, sập góc gù, cháy nổ không thể phục hồi kinh tế. Cách ly chờ Hãng tàu thanh lý bán sắt vụn hoặc chuyển làm kho dã chiến, loại khỏi luồng khai thác.
- **Mã Rule**: `Container.DamagedCannotBeAllocated` (`DamagedContainerCannotBeAllocatedRule`), `Container.GradeMismatch` (`ContainerGradeMismatchRule`).

---

### MODULE 3: VÒNG ĐỜI CONTAINER & QUY TẮC CỔNG - LỆNH GIAO NHẬN (DO)

#### 1. Vòng đời Container (Container Visit Lifecycle)
Một lượt ghé bãi của container được mô hình hóa thành State Machine:
$$\text{Expected} \xrightarrow{\text{Gate In}} \text{GatedIn} \xrightarrow{\text{Hạ bãi}} \text{StackedInYard} \xrightarrow{\text{Gán lệnh}} \text{Allocated} \xrightarrow{\text{Gate Out}} \text{GatedOut} \rightarrow \text{Completed}$$

- **Phiếu Cổng (EIR - Equipment Interchange Receipt)**:
  - Khi xe vào/ra cổng, ghi nhận: Biển số xe đầu kéo (`TractorNo`), Biển số rơ-moóc (`TrailerNo`), Tài xế (`DriverName`, `DriverPhone`), Giờ vào/ra.
  - Biên bản giám định tình trạng vỏ cont tại cổng (móp méo, thủng rách, cấp chất lượng Grade).

#### 2. Luật Bắt buộc: Lệnh Giao Container (Delivery Order - DO)
Depot tuyệt đối không tự ý giao container rỗng ra ngoài nếu không có Lệnh của Hãng tàu (Line Operator). Trước khi Gate-Out, hệ thống bắt buộc kiểm tra các rule sau:

1. **Lệnh còn hạn (`DeliveryOrderNotExpiredRule`)**:
   - $\text{Ngày xuất hiện tại} \le \text{ExpirationDate}$. Quá hạn $\rightarrow$ Báo lỗi `DeliveryOrder.Expired`.
2. **Khớp Hãng tàu (`ContainerLineOperatorMatchesOrderRule`)**:
   - Container xuất phải thuộc quyền khai thác của Hãng tàu phát hành Lệnh. Không được giao cont hãng khác $\rightarrow$ Báo lỗi `DeliveryOrder.LineOperatorMismatch`.
3. **Khớp Chủng loại & Phân hạng (`ContainerMatchesSpecificationRule`)**:
   - Kích thước (20ft/40ft), loại cont (Dry/Reefer) và Grade (A/B/C) của cont phải khớp với mục hàng yêu cầu trên Lệnh $\rightarrow$ Báo lỗi `DeliveryOrder.SpecificationMismatch`.
4. **Còn Định ngạch cấp cont (`DeliveryOrderQuotaRemainingRule`)**:
   - Số lượng cont đã xuất thực tế không được vượt quá số lượng đăng ký trên Lệnh ($\text{DeliveredQuantity} < \text{OrderedQuantity}$) $\rightarrow$ Báo lỗi `DeliveryOrder.QuotaExceeded`.
5. **Container đang sẵn sàng trong bãi (`ContainerReadyInYardRule`)**:
   - Container phải đang ở trạng thái `StackedInYard` hoặc `Allocated`, và không bị giữ (Hold) bởi giám định/sửa chữa $\rightarrow$ Báo lỗi `Container.NotAvailableForRelease`.

#### 3. Quy trình Xử lý Trễ hạn Lệnh giao cont (DO Overdue Handling)
- Khi `CurrentDate > ExpirationDate`: Hệ thống tự động **khóa cổng tuyệt đối (Hard Stop)**, không in phiếu Gate-Out và cấm xe nâng gắp cont.
- **Quy trình gia hạn**: Khách hàng nộp phí gia hạn cho Hãng tàu $\rightarrow$ Hãng tàu gửi điện/xác nhận $\rightarrow$ Điều độ viên chạy Command `ExtendDeliveryOrderCommand(OrderId, NewExpirationDate, Reason)` cập nhật hạn mới và lưu vết Audit $\rightarrow$ Cổng tự động mở lại cho xe nhận cont.

---

### MODULE 4: BÁO CÁO THỐNG KÊ VẬN HÀNH

#### 1. Báo cáo Tồn bãi theo Hãng tàu và Thời gian tồn (Aging Report)
- Thời gian lưu bãi tính theo ngày:
  $$\text{DwellTimeDays} = \text{DateDiffDay}(\text{GateInDate}, \text{CurrentDate})$$
- Phân nhóm theo yêu cầu SNP:
  - **Nhóm 0 - 10 ngày**: Luân chuyển bình thường (hiển thị màu xanh lá trên sơ đồ bãi).
  - **Nhóm $\ge$ 10 ngày**: Tồn bãi lâu ngày (Slow-moving / Demurrage risk - hiển thị cảnh báo màu cam/đỏ).
- **Xử lý container tồn bãi quá hạn (Container Overstay)**:
  - Tính phụ phí lưu bãi quá hạn (Storage Demurrage): $\text{Phụ phí} = (\text{Số ngày tồn} - 10) \times \text{Đơn giá lưu bãi/ngày}$.
  - Hệ thống tự động lập danh sách cảnh báo gửi Hãng tàu định kỳ để lên kế hoạch tái xuất (Re-positioning) vỏ rỗng lên tàu giải phóng mặt bằng bãi.
- Nhóm theo `LineOperator` và đếm số lượng container tương ứng.

#### 2. Báo cáo Sản lượng Xuất/Nhập (Throughput Report) theo Ngày
- Thống kê theo từng ngày trong khoảng thời gian `FromDate` đến `ToDate`.
- Nhóm theo từng `LineOperator`.
- Thống kê số lượng lượt Nhập (Gate-In) và số lượng lượt Xuất (Gate-Out).
- Quy đổi sang đơn vị TEU (Twenty-foot Equivalent Unit: 1 cont 20ft = 1 TEU, 1 cont 40ft = 2 TEU, 1 cont 45ft = 2.25 TEU).

---

## 4. THIẾT KẾ MÔ HÌNH DỮ LIỆU (DATABASE SCHEMA)

### 4.1. Sơ đồ Quan hệ Thực thể (ERD)

```
┌────────────────────────┐                   ┌────────────────────────┐
│       yard_blocks      │1                 n│       yard_slots       │
│------------------------│───────────────────│------------------------│
│ id (PK, UUID)          │                   │ id (PK, UUID)          │
│ tenant_id (UUID)       │                   │ tenant_id (UUID)       │
│ block_code (VARCHAR)   │                   │ block_id (FK, UUID)    │
│ name (VARCHAR)         │                   │ bay (INT)              │
│ max_bay, row, tier     │                   │ row (INT)              │
│ is_virtual (BOOL)      │                   │ tier (INT)             │
│ max_capacity (INT)     │                   │ is_occupied (BOOL)     │
└────────────────────────┘                   │ current_visit_id (FK)  │
                                             └───────────┬────────────┘
                                                         │ 0..1
                                                         ▼
┌────────────────────────┐1                 n┌────────────────────────┐
│       containers       │                   │    container_visits    │
│------------------------│───────────────────│------------------------│
│ id (PK, UUID)          │                   │ id (PK, UUID)          │
│ tenant_id (UUID)       │                   │ tenant_id (UUID)       │
│ container_number (UQ)  │                   │ container_id (FK, UUID)│
│ line_operator (VARCHAR)│                   │ line_operator (VARCHAR)│
│ container_type (ENUM)  │                   │ status (VARCHAR)       │
│ iso_code (VARCHAR)     │                   │ current_slot_id (FK)   │
│ size (INT: 20, 40, 45) │                   │ gate_in_date (TIMESTMP)│
│ tare_weight (DECIMAL)  │                   │ gate_out_date (TIMESTMP│
│ max_gross_weight (DEC) │                   │ in_tractor_no (VARCHAR)│
│ current_grade (VARCHAR)│                   │ in_survey_grade (VARCH)│
└────────────────────────┘                   │ out_tractor_no (VARCH) │
                                             │ delivery_order_id (FK) │
                                             └───────────▲────────────┘
                                                         │ n
                                                         │ 1
┌────────────────────────┐1                 n┌───────────┴────────────┐
│    delivery_orders     │                   │  delivery_order_items  │
│------------------------│───────────────────│------------------------│
│ id (PK, UUID)          │                   │ id (PK, UUID)          │
│ tenant_id (UUID)       │                   │ delivery_order_id (FK) │
│ order_number (VARCHAR) │                   │ container_type (VARCHAR│
│ line_operator (VARCHAR)│                   │ size (INT)             │
│ customer_name (VARCHAR)│                   │ required_grade (VARCH) │
│ customer_tax_code (VAR)│                   │ ordered_quantity (INT) │
│ expiration_date (DATE) │                   │ delivered_quantity(INT)│
│ vessel_name (VARCHAR)  │                   └────────────────────────┘
│ voyage_no (VARCHAR)    │
│ status (VARCHAR)       │
└────────────────────────┘
```

### 4.2. Chỉ mục Tối ưu Hiệu năng (Database Indexes)
- `idx_yard_slots_coords`: B-Tree trên `(tenant_id, block_id, bay, row, tier)` phục vụ tra cứu slot cực nhanh.
- `idx_containers_number`: Unique B-Tree trên `(tenant_id, container_number)` chống trùng lặp cont trong bãi.
- `idx_container_visits_active`: B-Tree trên `(tenant_id, status)` có điều kiện `status IN ('GatedIn', 'StackedInYard', 'Allocated')` phục vụ truy vấn tồn bãi.
- `idx_container_visits_aging`: B-Tree trên `(tenant_id, line_operator, gate_in_date)` phục vụ báo cáo Aging Dwell Time.
- `idx_delivery_orders_lookup`: B-Tree trên `(tenant_id, order_number, line_operator)` phục vụ kiểm tra Lệnh lúc Gate-Out.

---

## 5. THIẾT KẾ CÁC TẦNG TRONG CODE THEO CHUẨN MENTOR

### 5.1. Tầng Domain: Danh sách Entity, Value Object & Business Rules

#### Value Objects
1. `ContainerNumber`: Chứa chuỗi 11 ký tự, đóng gói thuật toán kiểm tra Modulo 11 ISO 6346.
2. `SlotCoordinate`: Đóng gói `(BlockCode, Bay, Row, Tier)`, cung cấp phương thức `IsOddBay()`, `IsEvenBay()`, `GetOverlappingBayNumbers()`.

#### Business Rules (Kế thừa `IBusinessRule`)
1. `ContainerNumberModulo11Rule`: Kiểm tra thuật toán số container.
2. `EvenOddBayCollisionRule`: Kiểm tra va chạm vật lý giữa cont 20ft và 40ft trên các bay liền kề.
3. `TierMustHaveBottomSupportRule`: Kiểm tra tầng đỡ bên dưới khi xếp chồng cont.
4. `SizeStackingSafetyRule`: Ngăn cấm đặt cont 20ft lên nóc cont 40ft.
5. `BlockMaxTierExceededRule`: Kiểm tra giới hạn số tầng tối đa của Block.
6. `VirtualBlockCapacityRule`: Kiểm tra sức chứa tối đa của Block ảo.
7. `DeliveryOrderNotExpiredRule`: Kiểm tra hạn lệnh giao cont.
8. `ContainerLineOperatorMatchesOrderRule`: Kiểm tra cont xuất có đúng hãng tàu cấp lệnh.
9. `ContainerMatchesOrderSpecificationRule`: Kiểm tra đúng kích cỡ (20/40) và phân hạng Grade (A/B/C).
10. `DeliveryOrderQuotaRemainingRule`: Kiểm tra hạn mức số lượng của lệnh.

### 5.2. Tầng Application: Manual CQRS & Vertical Slices
- `Features/Yard/`:
  - `Commands/CreateBlock`: Tạo Block mới (vật lý hoặc ảo).
  - `Commands/PlaceContainerInSlot`: Xếp container vào vị trí bãi (chạy kiểm tra collision, stacking).
  - `Commands/ShiftContainer`: Đảo chuyển container sang vị trí khác trong bãi.
  - `Queries/GetYardMap`: Lấy toàn bộ sơ đồ Block/Bay/Row/Tier phục vụ vẽ giao diện bãi 2D.
  - `Queries/CheckSlotAvailability`: Kiểm tra ô bãi có sẵn sàng hạ cont được không.
- `Features/Containers/`:
  - `Commands/RegisterContainer`: Khai báo hồ sơ container mới.
  - `Queries/GetContainerByNumber`: Tra cứu thông tin và lịch sử cont.
- `Features/Gate/`:
  - `Commands/GateInContainer`: Làm thủ tục xe vào, ghi nhận EIR và kết quả giám định vỏ.
  - `Commands/GateOutContainer`: Kiểm tra Lệnh DO, làm thủ tục xe ra, giải phóng slot bãi.
- `Features/Orders/`:
  - `Commands/CreateDeliveryOrder`: Khai báo Lệnh giao cont của Hãng tàu.
  - `Queries/GetActiveOrders`: Tra cứu các lệnh còn hạn và còn định mức.
- `Features/Analytics/`:
  - `Queries/GetAgingReport`: Thống kê tồn bãi 0-10 ngày và $\ge$ 10 ngày theo Hãng tàu.
  - `Queries/GetDailyThroughputReport`: Thống kê sản lượng xuất/nhập theo ngày theo Hãng tàu.

### 5.3. Tầng Infrastructure & Api
- **`AppDbContext`**: Cấu hình Fluent API cho tất cả các bảng, đăng ký Global Query Filter cho `ITenantEntity`.
- **`HybridCacheService`**: Cache thông tin Yard Layout và Line Operators.
- **Endpoints**:
  - `MapYardEndpoints()` (`/api/yard/...`)
  - `MapContainerEndpoints()` (`/api/containers/...`)
  - `MapGateEndpoints()` (`/api/gate/...`)
  - `MapOrderEndpoints()` (`/api/orders/...`)
  - `MapAnalyticsEndpoints()` (`/api/analytics/...`)

---

## 6. KẾ HOẠCH BẢO ĐẢM CHẤT LƯỢNG & TESTING

1. **Architecture Tests (NetArchTest)**:
   - Đảm bảo `Domain` không phụ thuộc vào bất kỳ tầng nào khác.
   - Đảm bảo `Application` không phụ thuộc vào `Infrastructure` hoặc `Api`.
   - Đảm bảo tất cả Handlers đều kế thừa `ICommandHandler` hoặc `IQueryHandler`.
2. **Domain Unit Tests (Đạt Coverage $\ge$ 80% theo yêu cầu thực tập)**:
   - Unit test riêng cho `ContainerNumber` với các test case Modulo 11 hợp lệ và bất hợp lệ (kể cả trường hợp phần dư = 10 ra 'X').
   - Unit test cho các `IBusinessRule` bãi: Va chạm Bay 01, 02, 03; Đặt cont thiếu tầng đỡ; Đặt cont 20ft lên 40ft.
   - Unit test cho các `IBusinessRule` Lệnh DO: Lệnh hết hạn, sai hãng tàu, vượt quá số lượng cho phép.
3. **Application Unit Tests**:
   - Sử dụng `TestDbContextFactory` (EF Core In-Memory) tương tự như template của mentor để test các CommandHandler và QueryHandler độc lập.

---

## 7. KẾT LUẬN & BƯỚC TIẾP THEO

Tài liệu này đã bao quát đầy đủ và chi tiết toàn bộ nghiệp vụ Quản lý Depot container Tân Cảng, kết hợp hoàn hảo với kiến trúc phần mềm Clean Architecture chuẩn doanh nghiệp của mentor.

Sau khi tài liệu này được phê duyệt, hệ thống sẽ sẵn sàng bước vào giai đoạn **Lập kế hoạch triển khai chi tiết (Writing Implementation Plan)** để hiện thực hóa từng file mã nguồn trong giải pháp.
