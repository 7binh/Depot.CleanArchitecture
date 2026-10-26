# Quyết định thiết kế

Ghi lại chỗ dự án chọn khác mẫu của mentor (`TechSpherex.CleanArchitecture`) hoặc tự đưa ra giả định, kèm lý do. Thêm một dòng mỗi khi có quyết định mới.

Nguyên tắc: theo mentor ở quy ước chung của team; chỉ lệch khi Depot có yêu cầu thật và giải thích được.

## Theo mentor

| Chủ đề | Quyết định |
|---|---|
| Cấu trúc | 4 tầng, mỗi hành động một thư mục `Features/<Tên>/<Hành động>/` |
| Truy cập dữ liệu | Application dùng `IAppDbContext`, không có repository |
| Use case | `ICommandHandler`/`IQueryHandler` viết tay, không MediatR |
| Test Application | `AppDbContext` thật trên EF InMemory, không dùng repository giả |
| Cơ sở dữ liệu | PostgreSQL (EF Core + Npgsql), như `README.md` của mentor. Đặc tả thực tập không chỉ định loại DB. Phase 3 từng dựng SQL Server trong Docker; đã đổi sang `postgres:17-alpine` khi bắt đầu bước Infrastructure của feature đầu tiên (chưa có migration nào nên chi phí đổi thấp) |

## Lệch khỏi mentor

| Chủ đề | Mentor | Depot | Lý do |
|---|---|---|---|
| Tạo entity | `new TodoItem { ... }`, setter công khai | `LineOperator.Create(...)` trả `Result`, setter `private` | Depot có nhiều luật nghiệp vụ; không có đường tạo đối tượng sai |
| Kiểm tra dữ liệu | FluentValidation ở endpoint | `IBusinessRule` + `BusinessRuleValidator` ở Domain | Một nơi cho mỗi luật, không viết hai lần. Thêm FluentValidation sau nếu mentor muốn |
| Thư mục Domain | `Entities/` gom tất cả | `Catalogs/`, `Yard/`, `Containers/` (sau này `ReleaseOrders/`, `Gate/`), mỗi nhóm giữ entity ở gốc và rule trong thư mục con `Rules/` (như `Common/Rules/`); namespace theo thư mục | Entity kéo theo nhiều file rule, một thư mục phẳng sẽ khó đọc; chia theo nhóm nghiệp vụ khớp với `Features/` bên Application |
| Interface `ICommand<TResponse>` | Có (marker rỗng) | Không | Analyzer S2326 báo lỗi tham số kiểu không dùng; `ICommandHandler` đã đủ |
| Đăng nhập (Identity + JWT) | `ApplicationUser : IdentityUser` nằm trong Domain; handler gọi thẳng `UserManager`; lỗi đăng nhập ném exception | `ApplicationUser` nằm ở Infrastructure; Application chỉ biết `IIdentityService`, `ITokenService`, `ICurrentUser`; lỗi trả `Result` (`ErrorType.Unauthorized` → 401) | Giữ Domain không phụ thuộc thư viện ngoài và không throw cho lỗi dự kiến (AGENTS.md). **Chưa hỏi mentor** xem cách của mentor có chủ ý hay không |
| Multi-tenant, Aspire, ELK, Agents | Có | Không | Ngoài phạm vi (A-13, hệ thống một depot) |

## Giả định tự đặt (ngoài đặc tả)

| Giả định | Lý do |
|---|---|
| Tên hãng khai thác tối đa 200 ký tự | Đặc tả không nêu; chọn theo cỡ cột thông thường |
| Mã loại container 2 đến 15 ký tự, tự trim và viết hoa; tên tối đa 100 ký tự | Đặc tả chỉ liệt kê tên loại, không nêu mã. Tối đa 15 để chứa mã `Specialized` (11 ký tự) trong ERD; seed dùng đúng 7 mã của ERD viết hoa |
| Loại container là bảng `ContainerTypes` có seed sẵn 7 loại (BR-CT-05), không phải `enum` | ERD đã coi `CONTAINER_TYPE` là bảng và dùng khóa ngoại `ContainerTypeId`; bảng cho phép sau này gắn thêm ràng buộc (ví dụ khu vực) mà không đổi ERD. Endpoint tạo loại mới vẫn còn, cần hỏi mentor có giữ không |
| Mã block tối đa 10 ký tự (tự trim, viết hoa), tên block tối đa 100 ký tự | Đặc tả không nêu độ dài; chọn theo cỡ mã bãi thông thường (ví dụ `A1`) |
| Số bay 1 đến 999; số row 1 đến 99 | Đặc tả chỉ nói bay và row là số thứ tự; cần cận trên để chặn dữ liệu rác |
| Số tầng tối đa của row (MaxTier) từ 2 đến 5 | Cận dưới 2 theo BR-YD-07; cận trên 5 do bạn thực tập sinh chốt (đặc tả không nêu), cần hỏi mentor có đồng ý không |
| Block ảo không có bay (BR-YD-10), nên cũng không có row | Row nằm trong bay, bay không có thì row không có |
| Bay và Row là hai entity riêng (bảng `Bays`, `YardRows`), khóa ngoại `Restrict` | Chặn xóa bay khi còn row, xóa block khi còn bay; số bay duy nhất trong block, số row duy nhất trong bay (chỉ mục duy nhất ghép) |
| Tạo mới trả 201 kèm header `Location`; danh sách chưa phân trang | Làm gọn cho bản đầu; phân trang dùng `PagedResult` khi dữ liệu lớn |
| Đăng nhập bằng email + mật khẩu, JWT HS256 sống 15 phút kèm refresh token sống 7 ngày, chưa có vai trò (role), chưa khóa tài khoản khi sai nhiều lần | Đủ để biết ai làm gì (audit); làm đơn giản trước, thêm sau khi cần. Mọi endpoint mặc định yêu cầu token, chỉ `/api/auth/login` và trang tài liệu API mở |
| Tài khoản đầu tiên (admin) tạo khi API khởi động từ cấu hình `Seed:AdminEmail` / `Seed:AdminPassword`; không có màn hình đăng ký | Tài khoản nội bộ do quản trị tạo; mật khẩu và `Jwt:Secret` chỉ nằm trong user-secrets / biến môi trường, không vào Git |
| Audit: mọi entity có `CreatedAt/CreatedBy/LastModifiedAt/LastModifiedBy`, tự điền khi lưu từ `ICurrentUser` + `TimeProvider`; `CreatedBy` là chuỗi id người dùng, `null` khi không có người dùng (seed, công cụ) | Biết nhân viên nào tạo/sửa; Domain không đọc đồng hồ hay người dùng. Dòng có sẵn trước khi thêm audit mang `CreatedAt = 0001-01-01` |
| Refresh token: lưu băm SHA-256 trong bảng `RefreshTokens`, mỗi lần dùng đổi sang token mới (xoay vòng); token đã dùng bị dùng lại thì thu hồi mọi phiên của người đó; `POST /api/auth/logout` thu hồi refresh token | Đăng xuất thật sự và hạn chế thiệt hại khi lộ token. Access token đang cầm vẫn dùng được tới khi hết hạn (tối đa 15 phút). Chưa dọn token hết hạn, chưa xử lý hai request đổi cùng một token đồng thời |
| Container: hãng khai thác lấy từ 3 chữ cái đầu của số container, không gửi `LineOperatorId` trong request (A-15) | Tránh để người dùng nhập hãng mâu thuẫn với số container (BR-CT-09) |
| Mã ISO của container gồm đúng 4 ký tự chữ-số; ngày sản xuất không được ở tương lai; trọng lượng bì không vượt trọng lượng tối đa | Đặc tả chỉ nêu có các trường này; cận kiểm tra là giả định của dự án |
| Hãng khai thác hoặc loại container không tồn tại trả 400 (không phải 404) | Hai giá trị này nằm trong nội dung request, không nằm trên URL; 404 chỉ dùng khi URL trỏ tới thứ không có |
| Container chưa có Update/Delete | Chờ hỏi mentor về chiến lược xóa, vì lệnh giao và gate sau này sẽ tham chiếu container |
| Lệnh giao: hãng, khách hàng, loại container nằm trong body nên không tồn tại trả 400; số lệnh tối đa 50 ký tự, chuyến tàu xuất bắt buộc và tối đa 100 ký tự | Đặc tả không nêu độ dài; chọn theo cỡ cột thông thường. Chuyến tàu xuất là một trong các thông tin bắt buộc của lệnh (SNP mục 3) |
| Mỗi lệnh có tối đa một dòng cho mỗi cặp (loại, kích thước); số lượng thêm thì cộng vào dòng đó | Khớp unique index của ERD; tránh hai dòng cùng loại làm sai phép đếm số lượng còn lại khi gate out |
| Lệnh chưa có Update/Delete. Response có `isExpired` (quá hạn khi hôm nay sau ngày hạn lệnh) và `status` (`NotDelivered` / `Delivering` / `Delivered`); mỗi dòng có thêm `delivered` và `remaining` | Trạng thái suy ra từ số container đã gate out theo từng dòng (đếm `ContainerStays.ReleaseOrderItemId`), không lưu cột nên không thể lệch với dữ liệu gate out. `Delivered` khi mọi dòng đã đủ. Hết hạn là cờ riêng, một lệnh có thể vừa `Delivering` vừa `isExpired`. Danh sách đếm bằng một truy vấn `GROUP BY` chung cho mọi lệnh |
| Gate in: bay lẻ nhận 20ft, bay chẵn nhận 40ft; chưa kiểm tra việc 40ft chiếm hai bay lẻ liền kề | Làm bản cơ bản trước (BR-YD-03); phần chiếm chỗ hai bay là mở rộng sau, hỏi mentor có cần không |
| Gate in: giờ vào do nhân viên nhập (mặc định là giờ hiện tại, không ở tương lai); mô tả hư hỏng tối đa 500 ký tự, số hiệu phương tiện tối đa 20 ký tự; tình trạng Bình thường thì bỏ mô tả hư hỏng | Đặc tả không nêu; cho phép nhập bù khi ghi chậm |
| `Grade`, `Condition`, `InVehicleType` lưu dạng chuỗi (`A`, `Damaged`, `Truck`) trong bảng `ContainerStays` | SQL thống kê đọc được trực tiếp, không phải nhớ số |
| Unique index của `ContainerStays` (`ContainerId`; `RowId`+`Tier`) chỉ tính lần lưu bãi đang mở (`WHERE "GateOutAt" IS NULL`) | Container ra rồi vào lại được (BR-GI-04), vị trí được giải phóng khi container ra (BR-GO-09) |
| Gate out: nhận `ContainerId` (không nhận id lần lưu bãi), tìm lần lưu bãi đang mở của container; ngày xét hạn lệnh là ngày của giờ ra theo múi giờ của depot; danh sách lần lưu bãi có bộ lọc `InYard` | Nhân viên cổng biết container; hạn lệnh là ngày lịch của depot, không phải ngày UTC |
| Gate out chặn hai người thao tác cùng lúc bằng hai lớp: (1) khoá dòng lệnh giao `SELECT ... FOR UPDATE` trong transaction, nên các lần gate out trên cùng một lệnh chạy lần lượt và số thùng đã giao luôn đúng (BR-RO-06); (2) concurrency token `xmin` trên `ContainerStay`, nên cùng một thùng gate out bằng hai lệnh khác nhau thì người lưu sau nhận 409 `GateOut.ContainerNotInYard` | Cột `xmin` là cột hệ thống của PostgreSQL nên migration `AddContainerStayConcurrencyToken` để trống, chỉ cập nhật model. Test dùng InMemory nên bỏ qua khoá dòng; đã thử thật trên PostgreSQL: hai thùng cùng tranh suất cuối thì một 200 một 409 `ReleaseOrder.LineFull`, bốn request cùng một thùng thì một 200 ba 409 |
| Gate out bị chặn khi còn container ở tầng cao hơn trong cùng row (409 `Stay.HasContainerAbove`); container phải ra từ tầng trên xuống | BR-YD-06 nói tầng chỉ dùng khi tầng dưới có container; nếu cho tầng dưới ra trước thì container phía trên treo lơ lửng. Chưa khoá row, nên gate in tầng trên và gate out tầng dưới cùng lúc vẫn có thể lọt (cùng mức rủi ro với gate in hiện nay) |

## Việc làm sau (đã thống nhất, chưa làm)

| Việc | Khi nào | Ghi chú |
|---|---|---|
| Ràng buộc loại container theo khu vực: container lạnh (Reefer) chỉ vào block dành riêng cho lạnh, container khô vào block khác | Tính năng 2 (cấu trúc bãi) và 5 (gate in) | Đề bài chưa có luật này. Hướng đề xuất: `Block` có `AllowedContainerTypeId` (có thể trống) và Domain có `block.CanAccept(container)`; Application chỉ tải dữ liệu rồi gọi luật. Hỏi mentor: chặn hay chỉ cảnh báo |
| Sửa và xóa danh mục (hãng tàu, loại container, khách hàng) | Sau khi có Container và lệnh giao | Chọn chặn xóa (409), xóa mềm hay xóa luôn tùy khóa ngoại thực tế; hỏi mentor |
