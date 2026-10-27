# Lộ trình Frontend

Phạm vi: màn hình cho nhân viên depot (BR-NF-03). Chỉ làm những gì API đã có. Mỗi giai đoạn là một nhánh, một PR, qua `sh scripts/check.sh` mới xong.

## Hiện trạng

| Có sẵn | Chưa có |
|---|---|
| Angular 22, Tailwind 4, Vitest, ESLint + Prettier | `HttpClient`, interceptor, guard |
| `shared/ui`: `button`, `icon`, `text-field` | App shell (sidebar, header), bảng, chip, dialog, select, toast |
| Trang login, register (form + validate, **chưa gọi API**) | Lưu token, refresh token |
| `Design.md` đầy đủ token | CORS hoặc proxy dev tới API |

Ba điểm cần chốt trước khi viết code:

1. **Kết nối API.** `Program.cs` chưa có CORS. Chọn dev proxy (`proxy.conf.json` trỏ `/api` tới API) để khỏi đụng backend. Khuyến nghị cách này.
2. **Trang register.** Backend không có API đăng ký (tài khoản admin tạo từ seed, xem `QUYET_DINH_THIET_KE.md`). Khuyến nghị xóa trang này.
3. **Phân trang.** Danh sách API chưa phân trang, nên bảng ở bản đầu hiển thị toàn bộ, lọc phía client.

## Màn hình và API

| Màn hình | API dùng | BR |
|---|---|---|
| Đăng nhập | `POST /api/auth/login`, `refresh`, `logout` | |
| Tổng quan bãi (sơ đồ block) | `GET /api/blocks`, `GET /api/blocks/{id}/map` | BR-YD |
| Cấu trúc bãi (block, bay, row) | `/api/blocks`, `/bays`, `/rows`, `POST /blocks/{id}/layout` | BR-YD-02, 08, 09 |
| Danh mục: hãng, loại container, khách hàng | `/api/line-operators`, `/container-types`, `/customers` | BR-CT-05, 08 |
| Container | `/api/containers` | BR-CT |
| Lệnh giao | `/api/release-orders` | BR-RO |
| Gate in | `POST /api/gate-in` | BR-GI |
| Gate out | `POST /api/gate-out` | BR-GO |
| Lịch sử lưu bãi | `GET /api/container-stays` (lọc `InYard`) | |
| Báo cáo sản lượng | `GET /api/reports/throughput` | BR-RP-01 |
| Báo cáo tồn bãi | `GET /api/reports/yard-stock` | BR-RP-02 |

## Giai đoạn

### F0. Nền tảng (làm trước, mọi thứ sau dựa vào)
- Dev proxy `/api`; `provideHttpClient(withInterceptors([...]))`.
- `core/auth`: `AuthService` (signal `isAuthenticated`, lưu access + refresh token), `authInterceptor` gắn `Authorization`, tự gọi `refresh` khi gặp 401 rồi thử lại một lần; `authGuard`.
- `core/api`: kiểu response viết tay theo từng endpoint; hàm đọc lỗi API (`code`, `message`) thành thông báo tiếng Việt.
- Nối trang login với API; xóa trang register; `logout`.
- Xong khi: đăng nhập thật, F5 vẫn còn phiên, token hết hạn tự refresh, đăng xuất thu hồi token.

### F1. App shell và bộ component dùng chung
- Shell theo Design.md: nền canvas xám ấm, sidebar nền tinted có đường kẻ dọc với nhóm menu, top bar, nội dung trên canvas. Route con bọc `authGuard`.
- Thêm vào `shared/ui`: `data-table`, `status-chip`, `select-field`, `dialog`, `toast`, `empty-state`, `page-header`, `spinner`/skeleton. Mỗi cái làm khi màn hình đầu tiên cần, không làm trước hàng loạt.
- Xong khi: có một trang mẫu rỗng trong shell, dùng đủ token, không có màu/radius ngoài Design.md.

### F2. Danh mục (màn hình đơn giản nhất, để luyện mẫu CRUD)
- Hãng khai thác, loại container, khách hàng: bảng + dialog tạo mới (API chỉ có tạo và xem).
- Rút ra mẫu chung: `Service` gọi API, trang dùng `signal`, form reactive, hiển thị lỗi 400/409 từ API.
- Xong khi: tạo được 3 loại danh mục, lỗi trùng/không hợp lệ hiện đúng ô.

### F3. Container và lệnh giao
- Container: danh sách (lọc theo hãng, loại), form tạo (số container kiểm check digit ISO 6346 phía client để báo sớm, backend vẫn là chốt cuối; hãng tự suy từ 3 chữ đầu nên không có ô chọn hãng).
- Lệnh giao: danh sách với chip trạng thái (chưa giao / đang giao / đã giao đủ, cờ quá hạn), form tạo với nhiều dòng chi tiết (loại, cỡ, số lượng), trang chi tiết hiện `delivered` / `remaining` từng dòng.
- Xong khi: tạo được container và lệnh có nhiều dòng; trạng thái hiển thị đúng với dữ liệu seed.

### F4. Gate in và gate out (nghiệp vụ chính)
- Gate in: chọn container, phân loại A/B/C, tình trạng (hư hỏng thì bắt buộc mô tả), phương tiện, giờ vào (mặc định bây giờ), vị trí: chọn block, nếu block thật thì chọn bay/row/tier, block ảo thì ẩn các ô đó. Bay lẻ chỉ cho container 20ft, bay chẵn chỉ cho 40ft (lọc ở giao diện cho dễ dùng, backend vẫn kiểm tra).
- Gate out: chọn container đang trong bãi (`InYard`), chọn lệnh giao của đúng hãng, phương tiện ra, giờ ra.
- Hiển thị mã lỗi 409/400 của backend (`Position.OverlapsNeighbour`, `Tier.BelowEmpty`, `Stay.HasContainerAbove`, `ReleaseOrder.LineFull`, ...) bằng câu tiếng Việt dễ hiểu. Làm một bảng ánh xạ mã lỗi tại một chỗ.
- Xong khi: chạy được kịch bản gate in rồi gate out theo lệnh trên dữ liệu seed, các lỗi nghiệp vụ chính hiện đúng thông báo.

### F5. Sơ đồ bãi và cấu trúc bãi
- Sơ đồ: chọn block, vẽ lưới bay x row từ `GET /blocks/{id}/map`; mỗi ô là một chồng tier, container 40ft trải từ `fromBay` tới `toBay`; chip hư hỏng, tồn lâu (>= 10 ngày); block ảo hiện danh sách `unplaced`. Giao diện chỉ vẽ, không tự tính luật (đã thống nhất, luật nằm ở backend).
- Cấu trúc bãi: tạo/sửa/xóa block, form `layout` (fromBay, toBay, rowCount, maxTier), sửa `maxTier` của row, xóa bay/row/block có xác nhận và hiện lỗi 409 khi còn container.
- Bấm vào ô trống trên sơ đồ mở form gate in với vị trí điền sẵn (nối F4 và F5).
- Xong khi: sơ đồ khớp dữ liệu seed 3 block; thu hẹp bãi bị chặn đúng khi còn container.

### F6. Lịch sử lưu bãi và báo cáo
- Lịch sử: bảng `container-stays`, bộ lọc tồn bãi / đã ra.
- Báo cáo sản lượng: chọn khoảng ngày, bảng (ngày, hãng, nhập, xuất).
- Báo cáo tồn bãi: chọn ngày (mặc định hôm nay), bảng theo hãng với `recent`, `longStay`, `total`; có thể thêm thanh tiến độ tỉ lệ tồn lâu theo Design.md.
- Xong khi: số liệu hai báo cáo khớp với Postman/SQL cho cùng dữ liệu.

### F7. Hoàn thiện và nộp
- Trạng thái rỗng, đang tải, lỗi mạng ở mọi danh sách; kiểm tra bàn phím và tương phản.
- Test: Vitest cho `AuthService`, interceptor, hàm check digit, bảng ánh xạ lỗi, và mỗi trang chính một test render + gửi form.
- README: cách chạy FE + BE + DB (BR-NF-04), không hardcode secret.
- Chạy `sh scripts/check.sh` sạch.

## Thứ tự và phụ thuộc

F0 -> F1 -> F2 -> F3 -> F4 -> F5 -> F6 -> F7. F6 chỉ cần F1 nên có thể làm song song với F4/F5 nếu muốn. Nếu thiếu thời gian, mức tối thiểu để đạt BR-NF-03 là F0 đến F4, cộng báo cáo tồn bãi của F6.

## Quy tắc chung mọi giai đoạn

- Đọc `web/Design.md` trước khi viết UI; chỉ dùng token (`bg-surface`, `rounded-card`, ...), không màu hex, không giá trị tùy ý.
- Standalone component, `signal`, `@if` / `@for`, `inject()`, `OnPush`; không `any`, không `console.log`; hàm tối đa 60 dòng, độ phức tạp tối đa 10.
- Cấu trúc thư mục: `core/` (auth, api), `shared/ui`, `features/<tên>/` (trang + service riêng của tính năng), route lazy-load.
- Không tắt rule lint để qua check.
