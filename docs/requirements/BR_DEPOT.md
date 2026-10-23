# Business Requirements (BR) - Hệ thống quản lý Depot

Nguồn: `SNP_HE_THONG_QUAN_LY_DEPOT.md` (viết tắt **YC**) và `SSC-Dev-Developer-New-011026-1020-100.md` (viết tắt **ĐỀ**).

## 1. Mục đích và phạm vi

Hệ thống giúp nhân viên depot quản lý **container rỗng** của các hãng khai thác: ghi nhận container vào bãi (gate in), xếp vào vị trí trong bãi, cho container ra khỏi bãi (gate out) theo lệnh giao của hãng, và thống kê.

**Trong phạm vi**
- Quản lý bãi theo Block, Bay, Row, Tier và block ảo.
- Quản lý thông tin container và danh mục hãng khai thác.
- Gate in, gate out.
- Lệnh giao container (do hãng khai thác phát, nhân viên depot nhập vào).
- Hai báo cáo: sản lượng nhập/xuất theo hãng theo ngày; tồn bãi theo hãng theo thời gian tồn.

**Ngoài phạm vi**
- Container có hàng, hàng hóa, seal, vận đơn, booking, hải quan.
- Tính phí lưu bãi, nâng hạ, sửa chữa.
- Phân quyền theo vai trò.
- Hãng khai thác tự điều chuyển container sang nơi khác (không có lệnh giao cho khách hàng).
- Theo dõi container sau khi ra khỏi cổng.
- Gợi ý vị trí tự động.

## 2. Thuật ngữ

| Thuật ngữ | Ý nghĩa |
|---|---|
| Depot | Bãi giữ container rỗng giùm hãng khai thác |
| Hãng khai thác (line operator) | Chủ của container; phát lệnh giao cho depot |
| Khách hàng | Bên đến lấy container rỗng theo lệnh (MST và tên) |
| Gate in | Ghi nhận container rỗng vào depot, mở một lần lưu bãi |
| Gate out | Ghi nhận container ra khỏi depot, đóng lần lưu bãi |
| Lần lưu bãi | Khoảng thời gian một container ở trong bãi, từ gate in đến gate out (tài liệu gọi là "vòng đời") |
| Lệnh giao container | Lệnh của hãng khai thác cho phép depot giao container rỗng cho khách hàng |
| Tồn bãi | Container đã gate in nhưng chưa gate out |
| Thời gian tồn | Số ngày lịch từ ngày gate in đến ngày báo cáo (xem BR-RP-04) |

## 3. Tác nhân

| Tác nhân | Việc làm |
|---|---|
| Nhân viên depot | Nhập lệnh giao, ghi gate in, ghi gate out, quản lý bãi, xem báo cáo |

Hãng khai thác và khách hàng không dùng hệ thống; họ chỉ xuất hiện như dữ liệu (xem A-13).

## 4. Giả định đã chọn

Các điểm tài liệu không nói rõ. Mỗi giả định được chọn theo hướng đơn giản nhất và có thể đổi sau.

| Mã | Giả định | Lý do |
|---|---|---|
| A-01 | Depot chỉ nhận container rỗng | YC chỉ nói giao container rỗng; không có thông tin hàng hóa |
| A-02 | Mọi gate out đều theo lệnh giao; không có trường hợp hãng tự điều chuyển | YC: "không tự ý giao, phải có lệnh" |
| A-03 | Không tính phí | Không có trong YC và ĐỀ |
| A-04 | Số container theo ISO 6346: 3 chữ owner, 1 chữ category, 6 chữ số serial, 1 check digit | YC viết "hai chữ", "bảy chữ số", mâu thuẫn với ví dụ của chính nó |
| A-05 | Check digit: dư 10 thì bằng 0 | Chuẩn ISO 6346; YC viết "X" là sai |
| A-06 | Thời gian tồn tính theo ngày lịch; hai nhóm là 0 đến 9 ngày và từ 10 ngày trở lên | YC ghi "0-10" và ">= 10" bị trùng ở mốc 10 |
| A-07 | Hạng A/B/C là danh mục, không có logic nghiệp vụ | YC không nêu tiêu chí |
| A-08 | Gate in không cần lệnh giao | YC chỉ buộc lệnh ở chiều ra |
| A-09 | Hãng của lệnh phải trùng hãng khai thác của container | Thùng của hãng nào, lệnh phải của hãng đó |
| A-10 | Số lệnh duy nhất theo từng hãng | Hai hãng có thể trùng số |
| A-11 | Nhân viên tự chọn vị trí; hệ thống chỉ kiểm tra | Gợi ý tự động là tính năng thêm |
| A-12 | Phương tiện gồm loại (xe, sà lan, khác) và số hiệu | YC chỉ nói "phương tiện vận chuyển" |
| A-13 | Hãng khai thác và khách hàng chỉ là dữ liệu, không là người dùng | YC chỉ mô tả depot vận hành |
| A-14 | Container có thể vào bãi nhiều lần, mỗi lần là một lần lưu bãi | Container là tài sản dùng lại |
| A-15 | Mỗi container thuộc một hãng khai thác, xác định theo owner code | YC Phụ lục I |
| A-16 | Kích thước container gồm 20 feet và 40 feet | YC §1 |
| A-17 | Mã số thuế gồm 10 hoặc 13 chữ số; lệnh có trạng thái chưa giao, đang giao, đã giao đủ, quá hạn | Thông lệ MST Việt Nam; YC không nêu |
| A-18 | Ràng buộc "một vị trí một container", "tier trên cần tier dưới", "không thu hẹp khi còn container" là hệ quả của việc xếp chồng | YC chỉ mô tả cấu trúc, không viết thành quy tắc |

## 5. Yêu cầu nghiệp vụ

Cột **Loại**: `Gốc` = tài liệu nói thẳng; `A-xx` = dựa trên giả định ở mục 4.

### 5.1 Quản lý bãi (BR-YD)

| Mã | Nội dung | Nguồn | Loại |
|---|---|---|---|
| BR-YD-01 | Vị trí trong bãi xác định bởi Block, Bay, Row, Tier. | YC §1 | Gốc |
| BR-YD-02 | Người dùng được khai báo số Block, Bay, Row, Tier của từng depot. | YC §1 | Gốc |
| BR-YD-03 | Bay lẻ chỉ nhận container 20 feet; bay chẵn chỉ nhận container 40 feet. | YC §1 | Gốc |
| BR-YD-04 | Container 40 feet ở bay chẵn chiếm vị trí của hai bay lẻ liền kề; không đặt container 20 feet vào vị trí đó. | YC §1 | Gốc |
| BR-YD-05 | Mỗi vị trí chứa tối đa một container. | YC §1 | A-18 |
| BR-YD-06 | Tier chỉ được dùng khi tier bên dưới đã có container. | YC §1 | A-18 |
| BR-YD-07 | Số tier tối đa của mỗi Row do người dùng khai báo, tối thiểu 2. | YC §1 | Gốc |
| BR-YD-08 | Người dùng được thêm, sửa, xóa Block, Bay, Row, Tier để mở rộng hoặc thu hẹp bãi. | YC §1 | Gốc |
| BR-YD-09 | Hệ thống không cho thu hẹp bãi nếu vị trí bị bỏ còn container. | YC §1 | A-18 |
| BR-YD-10 | Người dùng được tạo block ảo; block ảo không quản lý Bay, Row, Tier. | YC §1 | Gốc |
| BR-YD-11 | Container trong block ảo vẫn tính là tồn bãi. | YC §1 | Gốc |

### 5.2 Thông tin container (BR-CT)

| Mã | Nội dung | Nguồn | Loại |
|---|---|---|---|
| BR-CT-01 | Hệ thống lưu cho mỗi container: số container, loại, mã ISO, kích thước, trọng lượng tối đa, trọng lượng vỏ, ngày sản xuất, chủ sở hữu, tình trạng. | YC §2 | Gốc |
| BR-CT-02 | Số container gồm 11 ký tự: 3 chữ owner code, 1 chữ category (U, J, Z), 6 chữ số serial, 1 chữ số check digit. | YC Phụ lục I | A-04 |
| BR-CT-03 | Hệ thống tính lại check digit theo mục 6.1 và từ chối số sai. | YC Phụ lục I | A-05 |
| BR-CT-04 | Số container phải duy nhất trong danh mục. | YC §2 | Gốc |
| BR-CT-05 | Loại container thuộc danh mục: Dry, Reefer, Open Top, Flat Rack, Bunker, Ventilated, Specialized. | YC Phụ lục II | Gốc |
| BR-CT-06 | Kích thước container thuộc 20 feet hoặc 40 feet. | YC §1 | A-16 |
| BR-CT-07 | Trọng lượng vỏ không lớn hơn trọng lượng tối đa; cả hai lớn hơn 0. | YC §2 | Gốc |
| BR-CT-08 | Hệ thống quản lý danh mục hãng khai thác: mã (3 chữ owner code), tên. | YC §3 | Gốc |
| BR-CT-09 | Owner code của container phải thuộc danh mục hãng khai thác; hãng của container được xác định theo owner code. | YC Phụ lục I | A-15 |

### 5.3 Nhập container - gate in (BR-GI)

| Mã | Nội dung | Nguồn | Loại |
|---|---|---|---|
| BR-GI-01 | Hệ thống cho ghi nhận container vào depot (gate in), mở một lần lưu bãi mới. | YC §3 | Gốc |
| BR-GI-02 | Mỗi gate in ghi nhận: container, hãng khai thác, vị trí, phân loại, tình trạng, phương tiện vào, thời điểm vào. | YC §3 | Gốc |
| BR-GI-03 | Hệ thống từ chối gate in container đang ở trong bãi. | YC §3 | Gốc |
| BR-GI-04 | Container có thể vào bãi nhiều lần; mỗi lần là một lần lưu bãi riêng, các lần cũ được giữ làm lịch sử. | YC §3 | A-14 |
| BR-GI-05 | Phân loại container thuộc danh mục A, B, C. | YC §3 | A-07 |
| BR-GI-06 | Tình trạng container gồm Bình thường hoặc Hư hỏng; khi hư hỏng phải ghi mô tả (móp, méo, cong...). | YC §3 | Gốc |
| BR-GI-07 | Vị trí gate in phải còn trống và hợp lệ theo BR-YD-03 đến BR-YD-06; block ảo không cần Bay, Row, Tier. | YC §1, §3 | A-11 |
| BR-GI-08 | Phương tiện vào gồm loại (xe, sà lan, khác) và số hiệu. | YC §3 | A-12 |
| BR-GI-09 | Gate in không yêu cầu lệnh giao. | YC §3 | A-08 |
| BR-GI-10 | Thời điểm vào không ở tương lai. | YC §3 | Gốc |

### 5.4 Xuất container - gate out (BR-GO)

| Mã | Nội dung | Nguồn | Loại |
|---|---|---|---|
| BR-GO-01 | Hệ thống cho ghi nhận container ra khỏi depot (gate out), đóng lần lưu bãi hiện tại. | YC §3 | Gốc |
| BR-GO-02 | Mỗi gate out ghi nhận: container, lệnh giao, phương tiện ra, thời điểm ra. | YC §3 | Gốc |
| BR-GO-03 | Chỉ container đang ở trong bãi mới được gate out. | YC §3 | Gốc |
| BR-GO-04 | Mỗi gate out phải gắn với một lệnh giao container. | YC §3 | Gốc |
| BR-GO-05 | Hệ thống từ chối gate out khi không có lệnh giao hợp lệ. | YC §3 | Gốc |
| BR-GO-06 | Hãng của lệnh giao phải trùng hãng khai thác của container. | YC §3 | A-09 |
| BR-GO-07 | Loại và kích thước container giao phải khớp một dòng chi tiết còn số lượng của lệnh. | YC §3 | Gốc |
| BR-GO-08 | Thời điểm ra không sớm hơn thời điểm vào. | YC §3 | Gốc |
| BR-GO-09 | Sau gate out, vị trí của container được giải phóng. | YC §3 | Gốc |
| BR-GO-10 | Phương tiện ra gồm loại (xe, sà lan, khác) và số hiệu. | YC §3 | A-12 |

### 5.5 Lệnh giao container (BR-RO)

| Mã | Nội dung | Nguồn | Loại |
|---|---|---|---|
| BR-RO-01 | Hệ thống cho nhập lệnh giao gồm: hãng, khách hàng (MST, tên), số lệnh, hạn lệnh, chuyến tàu xuất. | YC §3 | Gốc |
| BR-RO-02 | Mỗi lệnh có một hoặc nhiều dòng chi tiết gồm loại và kích thước container, số lượng lớn hơn 0. | YC §3 | Gốc |
| BR-RO-03 | Số lệnh duy nhất theo từng hãng. | YC §3 | A-10 |
| BR-RO-04 | Hạn lệnh là ngày cuối cùng được phép giao container rỗng ra ngoài; giao đúng ngày hạn vẫn hợp lệ. | YC §3 | Gốc |
| BR-RO-05 | Hệ thống từ chối gate out theo lệnh đã quá hạn. | YC §3 | Gốc |
| BR-RO-06 | Số container đã giao theo mỗi dòng không vượt số lượng ghi trên lệnh. | YC §3 | Gốc |
| BR-RO-07 | Hạn lệnh không trước ngày nhập lệnh. | YC §3 | Gốc |
| BR-RO-08 | Mã số thuế khách hàng gồm 10 hoặc 13 chữ số. | YC §3 | A-17 |
| BR-RO-09 | Lệnh hiển thị trạng thái: chưa giao, đang giao, đã giao đủ, quá hạn. | YC §3 | A-17 |

### 5.6 Báo cáo (BR-RP)

| Mã | Nội dung | Nguồn | Loại |
|---|---|---|---|
| BR-RP-01 | Thống kê sản lượng nhập (số lần gate in) và xuất (số lần gate out) của từng hãng theo ngày. | ĐỀ | Gốc |
| BR-RP-02 | Thống kê container đang tồn bãi theo từng hãng, theo thời gian tồn: 0 đến 9 ngày và từ 10 ngày trở lên (tồn lâu). | ĐỀ | A-06 |
| BR-RP-03 | Container đã gate out không tính vào báo cáo tồn bãi. | ĐỀ | Gốc |
| BR-RP-04 | Thời gian tồn là số ngày lịch từ ngày gate in đến ngày báo cáo. | ĐỀ | A-06 |

### 5.7 Yêu cầu phi chức năng (BR-NF)

| Mã | Nội dung | Nguồn | Loại |
|---|---|---|---|
| BR-NF-01 | Có API cho chức năng nhập/xuất container, có Swagger. | ĐỀ | Gốc |
| BR-NF-02 | Unit test của API đạt coverage từ 80%. | ĐỀ | Gốc |
| BR-NF-03 | Có màn hình quản lý nhập/xuất container. | ĐỀ | Gốc |
| BR-NF-04 | Dự án chạy end-to-end (FE, BE, DB), không hardcode secret, có README. | ĐỀ | Gốc |

## 6. Quy tắc tính toán

### 6.1 Check digit số container (ISO 6346)

1. Lấy 10 ký tự đầu (3 chữ owner, 1 chữ category, 6 chữ số serial).
2. Đổi mỗi ký tự thành giá trị: chữ số giữ nguyên; chữ cái theo bảng: A=10, B=12, C=13, D=14, E=15, F=16, G=17, H=18, I=19, J=20, K=21, L=23, M=24, N=25, O=26, P=27, Q=28, R=29, S=30, T=31, U=32, V=34, W=35, X=36, Y=37, Z=38 (bỏ qua các bội của 11).
3. Nhân giá trị ở vị trí i (từ 0) với 2^i.
4. Cộng các tích, chia 11 lấy dư.
5. Dư 10 thì check digit bằng 0; còn lại check digit bằng dư.

Ví dụ hợp lệ: `CSQU3054383` (tổng 6185, dư 3, khớp ký tự cuối).
Ví dụ trong tài liệu gốc `CMAU1234567` **không hợp lệ**: tổng 5493, dư 4, check digit đúng là 4 (`CMAU1234564`).

### 6.2 Thời gian tồn

Thời gian tồn = ngày báo cáo trừ ngày gate in, tính theo ngày lịch. Nhóm "mới" khi 0 đến 9 ngày, nhóm "tồn lâu" khi từ 10 ngày trở lên.

## 7. Khác biệt so với tài liệu gốc

| Điểm | Tài liệu gốc | BR này |
|---|---|---|
| Owner code | "Hai chữ cái đầu" | 3 chữ, theo ISO 6346 |
| Serial | "Bảy chữ số" | 6 chữ số, kèm 1 check digit |
| Check digit dư 10 | "X" | 0 |
| Ví dụ `CMAU1234567` | Hợp lệ | Không hợp lệ (check digit đúng là 4) |
| Mốc 0-10 và >= 10 | Trùng ở mốc 10 | 0 đến 9 ngày và từ 10 ngày trở lên |
