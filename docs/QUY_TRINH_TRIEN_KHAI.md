# Quy trình triển khai dự án Depot (từ A đến Z)

Nguồn yêu cầu: `requirements/BR_DEPOT.md`, `internship/SSC-Dev-Developer-New-011026-1020-100.md`.
Nguyên tắc: **hiểu nghiệp vụ → thiết kế → dựng nền → code từ trong ra ngoài → kiểm thử → đóng gói**. Không viết code trước khi có thiết kế dữ liệu.

Ký hiệu: `[x]` đã xong, `[ ]` chưa làm.

## Các quyết định đã chốt

| Quyết định | Nội dung | Lý do |
|---|---|---|
| Hướng dữ liệu | **Code-first**: entity ở Domain, cấu hình EF Core ở Infrastructure, bảng sinh bằng migration | Hệ thống mới, chuẩn Clean Architecture; lịch sử DB nằm trong Git |
| Docker | Chỉ để chạy SQL Server (và sau này cả app), không phải DB-first | Môi trường giống nhau trên mọi máy |
| SQL viết tay | Chỉ cho dữ liệu mẫu (`database/seed`) và SQL thống kê (`database/queries`) | Đề yêu cầu viết SQL thống kê |
| Số depot | Một depot | Đơn giản; ERD không có bảng `DEPOT` |
| Đăng nhập, phân quyền | Không làm | Ngoài phạm vi (BR mục 1) |
| Use case | Không vẽ sơ đồ UML; use case là class ở tầng Application | Đề không yêu cầu; chỉ có một tác nhân |
| Ưu tiên | Làm kỹ **Gate in** và **Gate out** (đề bắt buộc API và unit test); phần còn lại làm kiểu CRUD đơn giản | Hai use case này chứa hầu hết rule |
| Cần hỏi mentor | Có chấp nhận 3-Tier thay cho 4 tầng Clean Architecture đầy đủ không | Đề ghi 3-Tier ở mức bắt buộc, Clean Architecture đầy đủ chưa bắt buộc |

## Giai đoạn 1: Phân tích nghiệp vụ

- [x] Đọc hai tài liệu của mentor, phát hiện chỗ mâu thuẫn (owner code 3 chữ, serial 6 số, check digit dư 10, mốc 0-10 và >= 10).
- [x] Viết `requirements/BR_DEPOT.md`: phạm vi, thuật ngữ, giả định A-xx, yêu cầu BR-xx, công thức check digit.
- [x] Chốt các quyết định ở bảng trên.

Sản phẩm: BR. Mọi bước sau tham chiếu mã BR.

## Giai đoạn 2: Thiết kế dữ liệu

- [x] Vẽ ERD (`requirements/ERD_DEPOT.md`) từ BR.
- [x] Rà soát ERD với BR, không phát hiện lỗi nghiệp vụ.
- [x] Bổ sung vào mục "Ghi chú thiết kế" của ERD:
  - Filtered unique index: một stay đang mở cho mỗi container; một container cho mỗi (`RowId`, `Tier`) của stay đang mở.
  - Unique `BAY(BlockId, BayNo)`, `YARD_ROW(BayId, RowNo)`, `RELEASE_ORDER_ITEM(ReleaseOrderId, ContainerTypeId, SizeFeet)`.
  - Giả định một depot.
  - Rule: cấm xóa Row đã từng có stay.

Sản phẩm: ERD. Đây là thứ mentor mở ra đầu tiên.

## Giai đoạn 3: Môi trường và cơ sở dữ liệu

- [x] `docker-compose.yml` chạy SQL Server. Mật khẩu đặt trong `.env`, không commit.
- [x] Tạo thư mục `database/seed` và `database/queries`.
- [x] Kiểm tra kết nối được vào SQL Server từ công cụ quản trị.

## Giai đoạn 4: Nền tảng dùng chung

Những thứ mọi chức năng đều dùng, làm trước khi làm chức năng đầu tiên.

- [ ] Khung solution theo `AGENTS.md` (Domain, Application, Infrastructure, Api, Tests) và cổng chất lượng `scripts/check.sh`.
- [ ] Result/Error pattern và `IRule` + `RuleChecker` ở Domain.
- [ ] Khung frontend (Angular, Tailwind) và `web/Design.md`.

## Giai đoạn 5: Backend theo lát cắt dọc, từ trong ra ngoài

Mỗi chức năng đi qua đủ các tầng theo thứ tự dưới đây rồi mới sang chức năng kế tiếp. Không làm "toàn bộ Domain" rồi "toàn bộ Infrastructure".

| Thứ tự | Tầng | Việc làm |
|---|---|---|
| 1 | Domain | Entity, rule nghiệp vụ, interface của quy tắc |
| 2 | Tests (Domain) | Unit test cho rule; test trước vì dễ nhất |
| 3 | Application | Use case, interface repository |
| 4 | Tests (Application) | Unit test use case (mock repository) |
| 5 | Infrastructure | Cấu hình EF Core, repository, **migration** |
| 6 | Api | Endpoint, `ToHttpResult()`, Swagger |
| 7 | Postman | Lưu bộ request mẫu |

Thứ tự chức năng:

1. **Danh mục:** hãng khai thác, loại container, khách hàng (CRUD đơn giản, để có dữ liệu cho các bước sau).
2. **Cấu trúc bãi:** Block, Bay, Row, Tier, block ảo (BR-YD).
3. **Container:** số container và check digit (BR-CT-02, BR-CT-03, mục 6.1 của BR).
4. **Lệnh giao** kèm các dòng chi tiết (BR-RO).
5. **Gate in** (BR-GI, BR-YD-03 đến 06). Làm kỹ, có test.
6. **Gate out** (BR-GO, BR-RO-05, BR-RO-06). Làm kỹ, có test.
7. **Báo cáo:** sản lượng theo ngày; tồn bãi theo thời gian tồn (BR-RP).

Dùng LINQ cho truy vấn ở bước 7 và để kiểm tra mục "LINQ" trong checklist bắt buộc.

## Giai đoạn 6: Dữ liệu mẫu và SQL thống kê

- [ ] `database/seed`: dữ liệu mẫu đủ cho hai nhóm tồn bãi (0-9 ngày, từ 10 ngày).
- [ ] `database/queries`: SQL tồn bãi theo hãng và SQL sản lượng nhập/xuất theo ngày, có JOIN.
- [ ] Đối chiếu kết quả SQL với kết quả báo cáo của API.

## Giai đoạn 7: Mockup giao diện

- [ ] Figma/Axure cho ba màn hình: quản lý nhập/xuất, thống kê sản lượng, thống kê tồn bãi.

Có thể làm song song với giai đoạn 5; phải xong trước giai đoạn 8.

## Giai đoạn 8: Frontend

Đọc `web/Design.md` trước khi viết giao diện.

- [ ] Service gọi API (Angular service + HttpClient).
- [ ] Màn hình quản lý nhập/xuất container (Gate in, Gate out).
- [ ] Màn hình thống kê sản lượng theo ngày.
- [ ] Màn hình thống kê tồn bãi theo thời gian tồn.

## Giai đoạn 9: Chất lượng và đóng gói

- [ ] Coverage unit test của API từ 80% trở lên (BR-NF-02).
- [ ] `sh scripts/check.sh` xanh.
- [ ] Dockerfile cho API và web; `docker-compose` chạy cả FE, BE, DB.
- [ ] Không hardcode secret; cấu hình qua biến môi trường.
- [ ] `README.md`: cách chạy end-to-end, cách chạy test, đường dẫn Swagger.
- [ ] Đối chiếu checklist bắt buộc của mentor (OOP, SOLID S và D, DI, Middleware, 3-Tier, EF Core CRUD, LINQ, REST, Swagger, Postman, SQL JOIN/PK/FK, Angular, Docker, OWASP).

## Giai đoạn 10: Chuẩn bị review với mentor

- [ ] Mở ERD và giải thích từng bảng vì sao tồn tại.
- [ ] Mở SQL thống kê và chạy trực tiếp trên DB.
- [ ] Trình bày Gate in/Gate out qua các tầng: rule nằm ở đâu, use case gọi gì, vì sao.
- [ ] Giải thích code-first, migration, dependency injection.
- [ ] Chuẩn bị trả lời: vì sao tách `CONTAINER` và `CONTAINER_STAY`; vì sao trạng thái lệnh không lưu cột; vì sao phương tiện không có bảng riêng.

## Phụ thuộc giữa các giai đoạn

```
1 Phân tích → 2 ERD → 3 Docker/DB ─┐
                          └→ 4 Nền tảng → 5 Backend ─→ 6 Seed + SQL ─┐
                                              └────→ 8 Frontend ─────┴→ 9 Đóng gói → 10 Review
                          7 Mockup (song song với 5) ─────────────────→ 8
```
