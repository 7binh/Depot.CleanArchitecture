namespace Depot.CleanArchitecture.Domain.Enums;

public enum ContainerGrade
{
    GradeA = 1,         // Loại 1: Hàng gạo, thực phẩm, hạt điều, điện tử
    GradeB = 2,         // Loại 2: Hàng bách hóa tổng hợp, máy móc đóng kiện
    GradeC = 3,         // Loại 3: Hàng thô, quặng, phế liệu, xơ dừa
    GradeD_Damaged = 4, // Hư hỏng: Cấm xuất bãi, chuyển xưởng M&R sửa chữa
    GradeE_Scrap = 5    // Phế thải: Biến dạng toàn phần, chờ thanh lý sắt vụn
}
