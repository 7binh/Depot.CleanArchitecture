# HỆ THỐNG QUẢN LÝ DEPOT

Một công ty con thuộc Tổng công ty Tân cảng Sài Gòn thực muốn xây dựng **hệ thống quản lý depot** giúp điều hành và quản lý các hoạt động giao nhận sắp xếp container tại depot.

Cụ thể các chức năng mong muốn như sau:

## 1.Quản lý bãi container

Tùy theo mỗi depot có cách quy hoạch bãi riêng, bãi container được quy hoạch theo mô hình xếp chồng Block, bay, row, tier.

- **Block:** Khối container nhiều container được sắp xếp theo hàng, và xếp chồng lên nhau tại một khu vực cụ thể trong bãi container. Xếp theo chiều dài - bay, xếp theo hàng - row, xếp chồng lên nhau - tier.
- **Bay:** Đây là không gian theo chiều dài theo block (bãi chứa container) mỗi "bay" có thể chứa một hoặc nhiều hàng container theo chiều ngang. Bay lẻ xếp container 20 feet, bay chẵn chứa container 40 feet, 2 bay lẻ tương đương 1 bay chẵn.
- **Row** là hàng container theo chiều rộng (hướng từ trước ra sau) trong một "bay".
- **Tier:** Là các tầng container xếp chồng lên nhau theo chiều thẳng đứng trong mỗi "row". Mỗi "tier" là một tầng riêng biệt trong cùng một "row", có thể xếp chồng từ 2 đến nhiều tầng tùy thuộc vào khả năng của bãi container.

Người dùng có thể mở rộng, thu hẹp bãi tùy theo nhu cầu.

Ngoài các Block được quản lý theo Bay, Row, Tier người dùng có thể định nghĩa các block ảo (không quản lý theo bay, row, tier) để dùng điều hành trong một số trường hợp đặc biệt.

## 2.Quản lý thông tin container

Quản lý thông tin container bao gồm: Số Container / Container Number, Loại Container / Container Type, Mã ISO / ISO Code, Kích Thước Container / Container Size, Trọng Lượng Tối Đa / Maximum Weight, Trọng Lượng Vỏ Container / Tare Weight, Ngày Sản Xuất / Date of Manufacture, Chủ Sở Hữu Container / Container Owner, Tình Trạng Container / Container Condition.

## 3.Quản lý nhập/xuất container

Vòng đời container tính từ lúc container vào bãi cho đến khi container ra khỏi bãi. Thông tin 1 vòng đời container bao gồm:

- Hãng khai thác container (line operator).
- Quản lý vị trí container: Ghi nhận vị trí theo Block, Bay, Row, Tier.
- Phân loại container: A, B, C,...
- Tình trạng container: Bình thường hay có hư hỏng, móp, méo, cong,... gì không?
- Ghi nhận phương tiện vận chuyển container vào depot (In) và phương tiện xuất container ra khỏi depot (Out).
- Vị trí hiện tại của container trong bãi.

Ngoài ra, depot không tự ý giao container ra ngoài phải có lệnh của hãng vận chuyển (line operator), mỗi lệnh giao container bao gồm: Khách hàng (MST và tên khách hàng) Số lệnh, hạn lệnh (ngày cuối cùng được phép giao container rỗng ra ngoài), chuyến tàu xuất (chuyến tàu xuất container này khỏi khỏi Việt nam), và số lượng container mỗi loại.

---

# Phụ lục I: Cấu Trúc của Số Container

Số container thường được cấu thành từ bốn phần chính:

## 1. Mã Chỉ Định Chủ Sở Hữu (Owner Code)

- **Mô tả:** Hai chữ cái đầu tiên của số container, đại diện cho công ty hoặc tổ chức sở hữu container. Mã này được cấp theo tiêu chuẩn của Tổ chức Hàng hải Quốc tế (IMO), giúp phân biệt giữa các công ty vận tải khác nhau.
- **Ví dụ:**
  - **CMA:** CMA CGM
  - **MSC:** Mediterranean Shipping Company
  - **HMM:** Hyundai Merchant Marine

## 2. Mã Loại (Type Code)

- **Mô tả:** Một chữ cái tiếp theo đại diện cho loại container. Mã này giúp nhận diện nhanh chóng loại hàng hóa mà container có thể chứa và tính năng của nó.
- **Ví dụ:**
  - **U:** Container khô (Dry Container)
  - **R:** Container lạnh (Reefer Container)
  - **S:** Container mở nắp (Open Top Container)
  - **F:** Container phẳng (Flat Rack Container)

## 3. Số Dãy (Serial Number)

- **Mô tả:** Bảy chữ số theo sau mã loại, đại diện cho một số duy nhất để nhận diện container trong số hàng triệu container khác. Số này thường được cấp theo thứ tự từ 000001 đến 9999999.
- **Ví dụ:**
  - **1234567:** Đây là số dãy của container.

## 4. Mã Kiểm Tra (Check Digit)

- **Mô tả:** Một chữ số cuối cùng được tính toán dựa trên các phần trước đó của số container để kiểm tra tính hợp lệ của số. Mã kiểm tra giúp đảm bảo rằng số container không bị nhập sai và có thể được xác nhận qua một thuật toán cụ thể.
- **Cách tính:** Mã kiểm tra thường được tính theo thuật toán Modulo 11, đảm bảo rằng chỉ những số hợp lệ mới được chấp nhận. Nếu phần dư là 0, mã kiểm tra là 0; nếu từ 1 đến 9, mã kiểm tra chính là phần dư; nếu là 10, mã kiểm tra sẽ là "X".

### Ví Dụ

- Một số container có thể có dạng như: **CMAU1234567**.
  - **CMA:** Mã chỉ định chủ sở hữu (CMA CGM).
  - **U:** Mã loại (Container khô).
  - **123456:** Số dãy duy nhất.
  - **7:** Mã kiểm tra (giả định, thực tế cần tính toán).

---

# Phụ lục II: Loại container

| Loại Container | Tên Tiếng Anh | Mô Tả | Đặc Điểm | Sử Dụng |
|---|---|---|---|---|
| **Container Khô** | Dry Container | Container phổ biến nhất, dùng để vận chuyển hàng hóa khô. | Không có hệ thống làm lạnh, chống nước và bụi, có cửa ra vào lớn. | Hàng hóa như quần áo, đồ điện tử, hàng tiêu dùng. |
| **Container Lạnh** | Reefer Container | Container thiết kế để vận chuyển hàng hóa nhạy cảm với nhiệt độ. | Hệ thống làm lạnh tích hợp, có thể điều chỉnh nhiệt độ từ -30°C đến +30°C. | Thực phẩm đông lạnh, trái cây, dược phẩm. |
| **Container Mở Nắp** | Open Top Container | Container không có mái, cho phép vận chuyển hàng hóa lớn và cồng kềnh. | Có thể phủ bằng tấm bạt, có các cột hỗ trợ để giữ hàng hóa an toàn. | Hàng hóa như máy móc, vật liệu xây dựng. |
| **Container Flat Rack** | Flat Rack Container | Container có nền phẳng với các cạnh bên để giữ hàng hóa cố định. | Không có tường hoặc mái, dễ dàng xếp dỡ hàng hóa. | Xe cộ, thiết bị xây dựng, sản phẩm chế tạo lớn. |
| **Container Bunker** | Bunker Container | Container đặc biệt để chứa hàng hóa nguy hiểm, chất lỏng hoặc hóa chất. | Trang bị các biện pháp an toàn đặc biệt, có thể có thiết kế chống cháy hoặc chống ăn mòn. | Hóa chất, nhiên liệu, hàng hóa nguy hiểm. |
| **Container Hở** | Ventilated Container | Container được thiết kế với các lỗ thông gió để thông khí cho hàng hóa nhạy cảm với độ ẩm. | Có thể điều chỉnh độ ẩm bên trong. | Rau củ, trái cây, sản phẩm nông nghiệp. |
| **Container Chuyên Dụng** | Specialized Container | Container thiết kế đặc biệt cho các mục đích cụ thể. | Có thể có tính năng cách nhiệt, cách âm, hoặc áp suất điều chỉnh. | Đồ điện tử nhạy cảm, hàng hóa y tế, hàng hóa quý giá. |
