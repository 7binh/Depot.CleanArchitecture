# ERD - Hệ thống quản lý Depot

Nguồn: `BR_DEPOT.md` (mã BR-xx, A-xx). Chưa có entity trong `src/`, nên ERD này là thiết kế đề xuất từ BR.

```mermaid
erDiagram
    LINE_OPERATOR ||--o{ CONTAINER : "sở hữu (A-15)"
    LINE_OPERATOR ||--o{ RELEASE_ORDER : "phát lệnh"
    CUSTOMER ||--o{ RELEASE_ORDER : "nhận container"
    CONTAINER_TYPE ||--o{ CONTAINER : "phân loại"
    CONTAINER_TYPE ||--o{ RELEASE_ORDER_ITEM : "yêu cầu"
    RELEASE_ORDER ||--|{ RELEASE_ORDER_ITEM : "gồm dòng"
    CONTAINER ||--o{ CONTAINER_STAY : "mỗi lần lưu bãi (A-14)"
    RELEASE_ORDER_ITEM |o--o{ CONTAINER_STAY : "gate out theo dòng"
    BLOCK ||--o{ BAY : "gồm"
    BAY ||--o{ YARD_ROW : "gồm"
    BLOCK ||--o{ CONTAINER_STAY : "chứa"
    YARD_ROW |o--o{ CONTAINER_STAY : "vị trí (row, tier)"

    LINE_OPERATOR {
        guid Id PK
        string Code UK "3 chữ owner code"
        string Name
    }
    CUSTOMER {
        guid Id PK
        string TaxCode UK "10 hoặc 13 số"
        string Name
    }
    CONTAINER_TYPE {
        guid Id PK
        string Code UK "Dry, Reefer, OpenTop, FlatRack, Bunker, Ventilated, Specialized"
        string Name
    }
    CONTAINER {
        guid Id PK
        string Number UK "11 ký tự ISO 6346"
        guid LineOperatorId FK
        guid ContainerTypeId FK
        string IsoCode
        int SizeFeet "20 hoặc 40"
        decimal MaxWeightKg
        decimal TareWeightKg
        date ManufactureDate
    }
    BLOCK {
        guid Id PK
        string Code UK
        string Name
        bool IsVirtual "block ảo: không có bay, row, tier"
    }
    BAY {
        guid Id PK
        guid BlockId FK
        int BayNo "lẻ = 20ft, chẵn = 40ft"
    }
    YARD_ROW {
        guid Id PK
        guid BayId FK
        int RowNo
        int MaxTier "tối thiểu 2"
    }
    RELEASE_ORDER {
        guid Id PK
        guid LineOperatorId FK
        guid CustomerId FK
        string OrderNo "duy nhất theo hãng"
        date IssuedDate
        date ExpiryDate "không trước IssuedDate"
        string ExportVessel "chuyến tàu xuất"
    }
    RELEASE_ORDER_ITEM {
        guid Id PK
        guid ReleaseOrderId FK
        guid ContainerTypeId FK
        int SizeFeet
        int Quantity "lớn hơn 0"
    }
    CONTAINER_STAY {
        guid Id PK
        guid ContainerId FK
        datetime GateInAt
        string Grade "A, B, C"
        string Condition "Normal hoặc Damaged"
        string DamageNote "bắt buộc khi Damaged"
        string InVehicleType "Truck, Barge, Other"
        string InVehicleNo
        guid BlockId FK
        guid RowId FK "null nếu block ảo"
        int Tier "null nếu block ảo"
        datetime GateOutAt "null = đang tồn bãi"
        guid ReleaseOrderItemId FK "null đến khi gate out"
        string OutVehicleType
        string OutVehicleNo
    }
```

## Ghi chú thiết kế

- **Lần lưu bãi** (`CONTAINER_STAY`) gộp gate in và gate out: `GateOutAt IS NULL` là tồn bãi (BR-GI-03, BR-RP-02). Một container có nhiều stay (BR-GI-04), nhưng chỉ một stay đang mở.
- **Phương tiện** (A-12) là value object nhúng vào stay (`InVehicle*`, `OutVehicle*`), không tách bảng.
- **Vị trí**: block thường dùng `RowId` + `Tier`; block ảo để trống hai cột này (BR-YD-10, BR-YD-11). Ràng buộc "một vị trí một container" là unique index trên (`RowId`, `Tier`) với stay đang mở (BR-YD-05).
- **Bay lẻ/chẵn**: `BayNo` lẻ nhận 20ft, chẵn nhận 40ft; container 40ft ở bay chẵn chiếm hai bay lẻ liền kề (BR-YD-03, 04). Kiểm tra bằng rule, không thể hiện bằng khóa.
- **Gate out** gắn `ReleaseOrderItemId`; lệnh suy ra qua dòng. Kiểm tra hãng lệnh trùng hãng container (BR-GO-06), loại và kích thước khớp dòng (BR-GO-07), còn số lượng (BR-RO-06), chưa quá hạn (BR-RO-05) đều thuộc rule ở Application.
- **Trạng thái lệnh** (chưa giao, đang giao, đã giao đủ, quá hạn) là giá trị suy ra từ số stay đã gate out theo từng dòng và `ExpiryDate`, không lưu cột.
- **Unique index**: `CONTAINER(Number)`, `RELEASE_ORDER(LineOperatorId, OrderNo)` (A-10), `LINE_OPERATOR(Code)`, `CUSTOMER(TaxCode)`.
- **Unique index có điều kiện** (chỉ tính stay đang mở, `GateOutAt IS NULL`):
  - `CONTAINER_STAY(ContainerId)`: mỗi container chỉ một stay đang mở (BR-GI-03).
  - `CONTAINER_STAY(RowId, Tier)`: mỗi vị trí chỉ một container (BR-YD-05).
- **Unique index cấu trúc**: `BAY(BlockId, BayNo)`, `YARD_ROW(BayId, RowNo)`, `RELEASE_ORDER_ITEM(ReleaseOrderId, ContainerTypeId, SizeFeet)`.
- **Một depot**: hệ thống chỉ quản lý một depot nên không có bảng `DEPOT`. Nếu cần nhiều depot thì thêm `DEPOT` và `DepotId` vào `BLOCK`.
- **Xóa bãi**: Row đã từng có stay thì không cho xóa, để giữ lịch sử vị trí (BR-YD-08, BR-YD-09). Kiểm tra bằng rule ở Application.
- `Condition` chỉ nằm ở stay (ghi nhận lúc gate in), không lặp ở `CONTAINER`.
