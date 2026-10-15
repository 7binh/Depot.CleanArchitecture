namespace Depot.CleanArchitecture.Infrastructure.Persistence;

using System.Reflection;
using Depot.CleanArchitecture.Application.Abstractions.Tenancy;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Entities;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// Database Seeder nạp toàn diện dữ liệu thực tế cho Depot Tân Cảng Cát Lái:
/// - YardBlocks (Block A cont khô, Block B cont lạnh Reefer, Block C thiết bị chuyên dụng, Block MR ảo, Block WASH ảo)
/// - YardSlots (87 ô bãi vật lý với vị trí Bay, Row, Tier chuẩn xác)
/// - Containers (18+ containers chuẩn quốc tế ISO 6346 Modulo 11 từ MAEU, ONE, MSC, CMA, EMC, HMM)
/// - ContainerVisits (Đang lưu bãi, xếp tầng thực tế, mới GatedIn, đã GatedOut)
/// - ConditionSurveys & StayHolds (Giám định vỏ, cảnh báo hư hỏng và khóa giữ hải quan/kỹ thuật)
/// - MovementEvents (Nhật ký di chuyển chi tiết: Hạ bãi, Đảo chuyển bãi, Chuyển bãi sửa chữa)
/// - DeliveryOrders (Lệnh giao nhận vỏ hàng DO)
/// Đảm bảo Idempotent, chống xung đột đa luồng InMemory bằng Semaphore, và hỗ trợ force refresh.
/// </summary>
public static class AppDbSeeder
{
    private static readonly SemaphoreSlim Semaphore = new(1, 1);

    private static readonly Guid BlockAId = Guid.Parse("a0000000-0000-0000-0000-000000000001");
    private static readonly Guid BlockBId = Guid.Parse("b0000000-0000-0000-0000-000000000002");
    private static readonly Guid BlockCId = Guid.Parse("c0000000-0000-0000-0000-000000000003");
    private static readonly Guid BlockMrId = Guid.Parse("d0000000-0000-0000-0000-000000000004");
    private static readonly Guid BlockWashId = Guid.Parse("e0000000-0000-0000-0000-000000000005");

    public static async Task SeedAsync(
        AppDbContext context,
        bool force = false,
        CancellationToken cancellationToken = default)
    {
        await Semaphore.WaitAsync(cancellationToken);
        try
        {
            // 1. Kiểm tra nếu đã có dữ liệu YardBlock và không yêu cầu force -> Bỏ qua
            if (!force && await context.YardBlocks.AnyAsync(cancellationToken))
                return;

            var catLaiTenantId = TenantInfo.DefaultDepotCatLaiId;

            // 2. Dọn sạch dữ liệu cũ nếu database đang có dữ liệu rác/cũ hoặc khi force = true
            if (await context.YardBlocks.AnyAsync(cancellationToken))
            {
                context.MovementEvents.RemoveRange(context.MovementEvents);
                context.StayHolds.RemoveRange(context.StayHolds);
                context.ConditionSurveys.RemoveRange(context.ConditionSurveys);
                context.ContainerVisits.RemoveRange(context.ContainerVisits);
                context.YardSlots.RemoveRange(context.YardSlots);
                context.YardBlocks.RemoveRange(context.YardBlocks);
                context.Containers.RemoveRange(context.Containers);
                context.DeliveryOrders.RemoveRange(context.DeliveryOrders);
                await context.SaveChangesAsync(cancellationToken);
            }

            // =========================================================================
            // 3. KHỞI TẠO CÁC BLOCK BÃI (YARD BLOCKS)
            // =========================================================================
            var blockA = YardBlock.CreatePhysical(
                tenantId: catLaiTenantId,
                code: "A",
                name: "Block A - Bãi Cont Khô",
                maxBay: 41,
                maxRow: 6,
                maxTier: 4,
                hasReeferPower: false);
            SetEntityId(blockA, BlockAId);

            var slotsA = new Dictionary<string, YardSlot>();
            for (var bay = 1; bay <= 41; bay++)
            {
                for (var row = 1; row <= 6; row++)
                {
                    for (var tier = 1; tier <= 4; tier++)
                    {
                        var slot = YardSlot.Create(catLaiTenantId, BlockAId, bay, row, tier);
                        blockA.AddSlot(slot);
                        slotsA[$"{bay}_{row}_{tier}"] = slot;
                    }
                }
            }

            var blockB = YardBlock.CreatePhysical(
                tenantId: catLaiTenantId,
                code: "B",
                name: "Block B - Bãi Cont Lạnh (Reefer)",
                maxBay: 41,
                maxRow: 6,
                maxTier: 3,
                hasReeferPower: true);
            SetEntityId(blockB, BlockBId);

            var slotsB = new Dictionary<string, YardSlot>();
            for (var bay = 1; bay <= 41; bay++)
            {
                for (var row = 1; row <= 6; row++)
                {
                    for (var tier = 1; tier <= 3; tier++)
                    {
                        var slot = YardSlot.Create(catLaiTenantId, BlockBId, bay, row, tier);
                        blockB.AddSlot(slot);
                        slotsB[$"{bay}_{row}_{tier}"] = slot;
                    }
                }
            }

            var blockC = YardBlock.CreatePhysical(
                tenantId: catLaiTenantId,
                code: "C",
                name: "Block C - Bãi Vỏ & Thiết Bị Chuyên Dụng",
                maxBay: 41,
                maxRow: 6,
                maxTier: 3,
                hasReeferPower: false);
            SetEntityId(blockC, BlockCId);

            var slotsC = new Dictionary<string, YardSlot>();
            for (var bay = 1; bay <= 41; bay++)
            {
                for (var row = 1; row <= 6; row++)
                {
                    for (var tier = 1; tier <= 3; tier++)
                    {
                        var slot = YardSlot.Create(catLaiTenantId, BlockCId, bay, row, tier);
                        blockC.AddSlot(slot);
                        slotsC[$"{bay}_{row}_{tier}"] = slot;
                    }
                }
            }

            var blockMr = YardBlock.CreateVirtual(
                tenantId: catLaiTenantId,
                code: "MR",
                name: "Xưởng Giám Định & Sửa Chữa M&R",
                maxCapacity: 20);
            SetEntityId(blockMr, BlockMrId);

            var blockWash = YardBlock.CreateVirtual(
                tenantId: catLaiTenantId,
                code: "WASH",
                name: "Khu Vực Rửa & Vệ Sinh Vỏ Cont",
                maxCapacity: 15);
            SetEntityId(blockWash, BlockWashId);

            context.YardBlocks.AddRange(blockA, blockB, blockC, blockMr, blockWash);

            // =========================================================================
            // 4. KHỞI TẠO CONTAINERS ĐA DẠNG CHUẨN ISO 6346 MODULO 11
            // Lưu ý: CSQU3054383 và EMCU8717530 để trạng thái GatedOut để functional tests có thể GateIn mà không bị xung đột active visit!
            // =========================================================================
            var c01 = Container.Create(catLaiTenantId, CreateValidContainerNumber("MAU", "305438"), "MAEU", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
            var c02 = Container.Create(catLaiTenantId, CreateValidContainerNumber("MSK", "129485"), "MAEU", ContainerType.Dry, "22G1", 20, 2250m, 30480m, ContainerGrade.GradeA).Value!;
            var c03 = Container.Create(catLaiTenantId, CreateValidContainerNumber("CMA", "582103"), "CMA", ContainerType.Dry, "42G1", 40, 3800m, 32500m, ContainerGrade.GradeB).Value!;
            var c04 = Container.Create(catLaiTenantId, CreateValidContainerNumber("ONE", "284910"), "ONE", ContainerType.Dry, "42G1", 40, 3750m, 32500m, ContainerGrade.GradeA).Value!;
            var c05 = Container.Create(catLaiTenantId, CreateValidContainerNumber("EMC", "401928"), "EMC", ContainerType.Dry, "42G1", 40, 3900m, 32500m, ContainerGrade.GradeA).Value!;
            var c06 = Container.Create(catLaiTenantId, CreateValidContainerNumber("HMM", "918234"), "HMM", ContainerType.Dry, "22G1", 20, 2180m, 30480m, ContainerGrade.GradeB).Value!;
            var c07 = Container.Create(catLaiTenantId, CreateValidContainerNumber("MAU", "739102"), "MAEU", ContainerType.Dry, "42G1", 40, 3850m, 32500m, ContainerGrade.GradeC).Value!;
            var c08 = Container.Create(catLaiTenantId, CreateValidContainerNumber("MSC", "402918"), "MSC", ContainerType.Dry, "22G1", 20, 2300m, 30480m, ContainerGrade.GradeD_Damaged).Value!;

            var c09 = Container.Create(catLaiTenantId, ContainerNumber.Create("TCLU4582910").Value!, "ONE", ContainerType.Reefer, "45R1", 40, 4500m, 34000m, ContainerGrade.GradeB).Value!;
            var c10 = Container.Create(catLaiTenantId, CreateValidContainerNumber("MAU", "291048"), "MAEU", ContainerType.Reefer, "45R1", 40, 4600m, 34000m, ContainerGrade.GradeA).Value!;
            var c11 = Container.Create(catLaiTenantId, CreateValidContainerNumber("MSC", "839102"), "MSC", ContainerType.Reefer, "22R1", 20, 2900m, 30480m, ContainerGrade.GradeA).Value!;
            var c12 = Container.Create(catLaiTenantId, CreateValidContainerNumber("CMA", "310928"), "CMA", ContainerType.Reefer, "45R1", 40, 4550m, 34000m, ContainerGrade.GradeB).Value!;

            var c13 = Container.Create(catLaiTenantId, CreateValidContainerNumber("EMC", "761234"), "EMC", ContainerType.FlatRack, "42P1", 40, 5200m, 45000m, ContainerGrade.GradeB).Value!;
            var c14 = Container.Create(catLaiTenantId, CreateValidContainerNumber("ONE", "672109"), "ONE", ContainerType.OpenTop, "42U1", 40, 4100m, 34000m, ContainerGrade.GradeA).Value!;
            var c15 = Container.Create(catLaiTenantId, CreateValidContainerNumber("MSC", "901823"), "MSC", ContainerType.Tank, "22T1", 20, 3600m, 36000m, ContainerGrade.GradeA).Value!;

            var c16 = Container.Create(catLaiTenantId, CreateValidContainerNumber("MAU", "518293"), "MAEU", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
            var c17 = Container.Create(catLaiTenantId, CreateValidContainerNumber("CMA", "810293"), "CMA", ContainerType.Dry, "42G1", 40, 3800m, 32500m, ContainerGrade.GradeD_Damaged).Value!;

            // c18 & c19: Containers dùng trong các test GateIn / Relocate tests
            var c18 = Container.Create(catLaiTenantId, ContainerNumber.Create("CSQU3054383").Value!, "MAEU", ContainerType.Dry, "22G1", 20, 2200m, 30480m, ContainerGrade.GradeA).Value!;
            var c19 = Container.Create(catLaiTenantId, ContainerNumber.Create("EMCU8717530").Value!, "EMC", ContainerType.Dry, "42G1", 40, 3900m, 32500m, ContainerGrade.GradeA).Value!;

            context.Containers.AddRange(c01, c02, c03, c04, c05, c06, c07, c08, c09, c10, c11, c12, c13, c14, c15, c16, c17, c18, c19);

            // =========================================================================
            // 5. KHỞI TẠO LỆNH GIAO NHẬN (DELIVERY ORDERS)
            // =========================================================================
            var order1 = DeliveryOrder.Create(
                tenantId: catLaiTenantId,
                orderNumber: "DO-MAEU-20261001",
                lineOperator: "MAEU",
                customerName: "Công ty TNHH Tiếp vận Tân Cảng SNP",
                customerTaxCode: "0301234567",
                expirationDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
                vesselName: "MAERSK MC-KINNEY MOLLER",
                voyageNo: "2601W");
            order1.AddItem(20, ContainerType.Dry, ContainerGrade.GradeA, 5);

            var order2 = DeliveryOrder.Create(
                tenantId: catLaiTenantId,
                orderNumber: "DO-ONE-20261015",
                lineOperator: "ONE",
                customerName: "Tổng Công ty Tân Cảng Sài Gòn",
                customerTaxCode: "0300456789",
                expirationDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
                vesselName: "ONE HARBOR",
                voyageNo: "2608E");
            order2.AddItem(40, ContainerType.Reefer, ContainerGrade.GradeB, 2);

            var order3 = DeliveryOrder.Create(
                tenantId: catLaiTenantId,
                orderNumber: "DO-MSC-20261020",
                lineOperator: "MSC",
                customerName: "Công ty CP Transimex Logistics",
                customerTaxCode: "0300987654",
                expirationDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10)),
                vesselName: "MSC OSCAR",
                voyageNo: "2612S");
            order3.AddItem(20, ContainerType.Dry, ContainerGrade.GradeA, 4);
            order3.AddItem(40, ContainerType.Dry, ContainerGrade.GradeB, 3);

            context.DeliveryOrders.AddRange(order1, order2, order3);

            // =========================================================================
            // 6. KHỞI TẠO CÁC LƯỢT VIẾNG THĂM (CONTAINER VISITS) & XẾP BÃI THỰC TẾ
            // Trong EF Core, Owned Entity (InVehicle/OutVehicle) bắt buộc phải là instance độc lập cho mỗi entity
            // =========================================================================
            var now = DateTime.UtcNow;

            // V01: c01 tại Block A (Bay 1, Row 1, Tier 1 - Dwell 14 ngày -> Aging > 10d)
            var v01 = ContainerVisit.CreateGateIn(catLaiTenantId, c01.Id, c01.LineOperator, CreateVehicle("51C-882.34", "51R-123.45", "Nguyễn Văn Hùng", "0908123456"), c01.Grade, "Vỏ đạt chuẩn cấp hàng đóng gạo xuất khẩu");
            SetGateInDate(v01, now.AddDays(-14));
            var slotA_111 = slotsA["1_1_1"];
            slotA_111.Occupy(c01.Id, c01.Size);
            v01.AssignSlot(slotA_111.Id, BlockAId);

            // V02: c02 tại Block A (Bay 1, Row 1, Tier 2 - chồng lên c01)
            var v02 = ContainerVisit.CreateGateIn(catLaiTenantId, c02.Id, c02.LineOperator, CreateVehicle("50H-192.83", "51R-456.78", "Trần Quốc Toản", "0912345678"), c02.Grade, "Vỏ nguyên vẹn");
            SetGateInDate(v02, now.AddDays(-8));
            var slotA_112 = slotsA["1_1_2"];
            slotA_112.Occupy(c02.Id, c02.Size);
            v02.AssignSlot(slotA_112.Id, BlockAId);

            // V03: c03 tại Block A (Bay 6, Row 2, Tier 1 - Cont 40ft gộp Bay 05 & 07)
            var v03 = ContainerVisit.CreateGateIn(catLaiTenantId, c03.Id, c03.LineOperator, CreateVehicle("60C-341.29", "60R-789.01", "Lê Hoàng Nam", "0934567890"), c03.Grade, "Vỏ trầy xước nhẹ sàn gỗ");
            SetGateInDate(v03, now.AddDays(-6));
            var slotA_621 = slotsA["6_2_1"];
            slotA_621.Occupy(c03.Id, c03.Size);
            v03.AssignSlot(slotA_621.Id, BlockAId);

            // V04: c04 tại Block A (Bay 6, Row 1, Tier 2 - Cont 40ft chồng lên c05)
            var v04 = ContainerVisit.CreateGateIn(catLaiTenantId, c04.Id, c04.LineOperator, CreateVehicle("72C-562.10", "72R-234.56", "Phạm Văn Đức", "0978901234"), c04.Grade, "Sạch sẽ khô ráo");
            SetGateInDate(v04, now.AddDays(-4));
            var slotA_612 = slotsA["6_1_2"];
            slotA_612.Occupy(c04.Id, c04.Size);
            v04.AssignSlot(slotA_612.Id, BlockAId);

            // V05: c05 tại Block A (Bay 6, Row 1, Tier 1 - Cont 40ft gộp Bay 05 & 07)
            var v05 = ContainerVisit.CreateGateIn(catLaiTenantId, c05.Id, c05.LineOperator, CreateVehicle("51D-903.45", "51R-678.90", "Hoàng Minh Tuấn", "0987654321"), c05.Grade, "Vỏ tốt");
            SetGateInDate(v05, now.AddDays(-10));
            var slotA_611 = slotsA["6_1_1"];
            slotA_611.Occupy(c05.Id, c05.Size);
            v05.AssignSlot(slotA_611.Id, BlockAId);

            // V06: c06 tại Block A (Bay 1, Row 2, Tier 1 - Cont 20ft)
            var v06 = ContainerVisit.CreateGateIn(catLaiTenantId, c06.Id, c06.LineOperator, CreateVehicle("51C-678.12", "51R-345.67", "Đỗ Văn Long", "0918273645"), c06.Grade, "Vỏ đạt Grade B");
            SetGateInDate(v06, now.AddDays(-5));
            var slotA_121 = slotsA["1_2_1"];
            slotA_121.Occupy(c06.Id, c06.Size);
            v06.AssignSlot(slotA_121.Id, BlockAId);

            // V07: c07 tại Block A (Bay 10, Row 2, Tier 1 - Cont 40ft gộp Bay 09 & 11)
            var v07 = ContainerVisit.CreateGateIn(catLaiTenantId, c07.Id, c07.LineOperator, CreateVehicle("60C-341.29", "60R-789.01", "Lê Hoàng Nam", "0934567890"), c07.Grade, "Có vết ố dầu sàn cont");
            SetGateInDate(v07, now.AddDays(-3));
            var slotA_1021 = slotsA["10_2_1"];
            slotA_1021.Occupy(c07.Id, c07.Size);
            v07.AssignSlot(slotA_1021.Id, BlockAId);

            // V08: c08 tại Block A (Bay 3, Row 1, Tier 1 - Cont 20ft) - CÓ HƯ HỎNG & KHÓA GIỮ DAMAGE_HOLD
            var v08 = ContainerVisit.CreateGateIn(catLaiTenantId, c08.Id, c08.LineOperator, CreateVehicle("50H-192.83", "51R-456.78", "Trần Quốc Toản", "0912345678"), c08.Grade, "Trụ góc sau bên phải bị móp sâu 45mm");
            SetGateInDate(v08, now.AddDays(-2));
            var slotA_311 = slotsA["3_1_1"];
            slotA_311.Occupy(c08.Id, c08.Size);
            v08.AssignSlot(slotA_311.Id, BlockAId);
            var survey08 = ConditionSurvey.Create(
                catLaiTenantId,
                v08.Id,
                ContainerGrade.GradeD_Damaged,
                "DAMAGED",
                ["POST_DENT", "CORNER_BENT"],
                "Trụ góc sau móp méo sâu 45mm, viền đố đáy cong vênh do va chạm xe nâng",
                "/photos/cont_post_dent.jpg",
                "Vũ Hải Đăng - Giám định viên SNP M&R").Value!;
            v08.AddSurvey(survey08); // Tự động tạo DAMAGE_HOLD trên ContainerVisit

            // V09: c09 tại Block B (Bay 6, Row 1, Tier 1 - Cont 40ft Lạnh gộp Bay 05 & 07)
            var v09 = ContainerVisit.CreateGateIn(catLaiTenantId, c09.Id, c09.LineOperator, CreateVehicle("51D-903.45", "51R-678.90", "Hoàng Minh Tuấn", "0987654321"), c09.Grade, "Cắm điện bãi lạnh Cát Lái, duy trì nhiệt độ -18°C");
            SetGateInDate(v09, now.AddDays(-12));
            var slotB_611 = slotsB["6_1_1"];
            slotB_611.Occupy(c09.Id, c09.Size);
            v09.AssignSlot(slotB_611.Id, BlockBId);
            var survey09 = ConditionSurvey.Create(
                catLaiTenantId,
                v09.Id,
                ContainerGrade.GradeB,
                "PTI_PASSED",
                ["DIRTY_WASH"],
                "Đã cắm giàn điện lạnh SNP, máy Carrier hoạt động ổn định ở -18.2°C",
                "/photos/reefer_carrier_temp.jpg",
                "Lê Văn Lạnh - Đội Kỹ Thuật PTI").Value!;
            v09.AddSurvey(survey09);

            // V10: c10 tại Block B (Bay 6, Row 1, Tier 2 - Cont 40ft chồng lên c09)
            var v10 = ContainerVisit.CreateGateIn(catLaiTenantId, c10.Id, c10.LineOperator, CreateVehicle("51C-882.34", "51R-123.45", "Nguyễn Văn Hùng", "0908123456"), c10.Grade, "Cắm điện, nhiệt độ -20°C");
            SetGateInDate(v10, now.AddDays(-7));
            var slotB_612 = slotsB["6_1_2"];
            slotB_612.Occupy(c10.Id, c10.Size);
            v10.AssignSlot(slotB_612.Id, BlockBId);

            // V11: c11 tại Block B (Bay 1, Row 2, Tier 1 - Cont 20ft Lạnh)
            var v11 = ContainerVisit.CreateGateIn(catLaiTenantId, c11.Id, c11.LineOperator, CreateVehicle("50H-192.83", "51R-456.78", "Trần Quốc Toản", "0912345678"), c11.Grade, "Hàng hải sản đông lạnh");
            SetGateInDate(v11, now.AddDays(-3));
            var slotB_121 = slotsB["1_2_1"];
            slotB_121.Occupy(c11.Id, c11.Size);
            v11.AssignSlot(slotB_121.Id, BlockBId);

            // V12: c12 tại Block B (Bay 10, Row 1, Tier 1 - Cont 40ft Lạnh gộp Bay 09 & 11) - CUSTOMS_HOLD
            var v12 = ContainerVisit.CreateGateIn(catLaiTenantId, c12.Id, c12.LineOperator, CreateVehicle("72C-562.10", "72R-234.56", "Phạm Văn Đức", "0978901234"), c12.Grade, "Cắm điện bãi lạnh");
            SetGateInDate(v12, now.AddDays(-1));
            var slotB_1011 = slotsB["10_1_1"];
            slotB_1011.Occupy(c12.Id, c12.Size);
            v12.AssignSlot(slotB_1011.Id, BlockBId);
            v12.ApplyHold("CUSTOMS_HOLD", "Hải quan Khu vực 1 yêu cầu tạm giữ niêm phong kiểm hóa hàng đông lạnh", "Chi cục Hải quan Cát Lái");

            // V13: c13 tại Block C (Bay 6, Row 1, Tier 1 - Flat Rack 40ft gộp Bay 05 & 07)
            var v13 = ContainerVisit.CreateGateIn(catLaiTenantId, c13.Id, c13.LineOperator, CreateVehicle("60C-341.29", "60R-789.01", "Lê Hoàng Nam", "0934567890"), c13.Grade, "Container mặt bằng Flat Rack");
            SetGateInDate(v13, now.AddDays(-4));
            var slotC_611 = slotsC["6_1_1"];
            slotC_611.Occupy(c13.Id, c13.Size);
            v13.AssignSlot(slotC_611.Id, BlockCId);

            // V14: c14 tại Block C (Bay 10, Row 2, Tier 1 - Open Top 40ft gộp Bay 09 & 11)
            var v14 = ContainerVisit.CreateGateIn(catLaiTenantId, c14.Id, c14.LineOperator, CreateVehicle("51C-882.34", "51R-123.45", "Nguyễn Văn Hùng", "0908123456"), c14.Grade, "Container mở nóc có bạt phủ che");
            SetGateInDate(v14, now.AddDays(-5));
            var slotC_1021 = slotsC["10_2_1"];
            slotC_1021.Occupy(c14.Id, c14.Size);
            v14.AssignSlot(slotC_1021.Id, BlockCId);

            // V15: c15 tại Block C (Bay 1, Row 1, Tier 1 - Tank 20ft)
            var v15 = ContainerVisit.CreateGateIn(catLaiTenantId, c15.Id, c15.LineOperator, CreateVehicle("51D-903.45", "51R-678.90", "Hoàng Minh Tuấn", "0987654321"), c15.Grade, "Container bồn chứa hóa chất lỏng");
            SetGateInDate(v15, now.AddDays(-2));
            var slotC_111 = slotsC["1_1_1"];
            slotC_111.Occupy(c15.Id, c15.Size);
            v15.AssignSlot(slotC_111.Id, BlockCId);

            // V16: c16 - MỚI GATE-IN ĐANG ĐẬU TẠI BÃI ĐỆM CỔNG CHỜ CHỈ ĐỊNH Ô BÃI (GatedIn)
            var v16 = ContainerVisit.CreateGateIn(catLaiTenantId, c16.Id, c16.LineOperator, CreateVehicle("50H-192.83", "51R-456.78", "Trần Quốc Toản", "0912345678"), c16.Grade, "Mới tiếp nhận qua Cổng A Cát Lái");
            SetGateInDate(v16, now.AddHours(-2));

            // V17: c17 - CHUYỂN SANG BLOCK ẢO M&R (Khu sửa chữa) DO THỦNG VÁCH LỚN
            var v17 = ContainerVisit.CreateGateIn(catLaiTenantId, c17.Id, c17.LineOperator, CreateVehicle("60C-341.29", "60R-789.01", "Lê Hoàng Nam", "0934567890"), c17.Grade, "Vỏ thủng rách tôn");
            SetGateInDate(v17, now.AddDays(-3));
            v17.AssignVirtualBlock(BlockMrId);
            var survey17 = ConditionSurvey.Create(
                catLaiTenantId,
                v17.Id,
                ContainerGrade.GradeD_Damaged,
                "DAMAGED",
                ["HOLE_WALL", "FLOOR_GOUGE"],
                "Rách vách tôn bên hông trái 15x20cm, xước sàn gỗ",
                "/photos/cont_wall_hole.jpg",
                "Nguyễn Minh Trí - Kỹ thuật M&R").Value!;
            v17.AddSurvey(survey17);

            // V18 & V19: c18 (CSQU3054383) & c19 (EMCU8717530) ĐÃ XUẤT BÃI (Gate-Out hoàn tất)
            // Để lượt lưu bãi kết thúc, không còn active visit -> FunctionalTests có thể GateIn mà không lỗi!
            var v18 = ContainerVisit.CreateGateIn(catLaiTenantId, c18.Id, c18.LineOperator, CreateVehicle("51C-882.34", "51R-123.45", "Nguyễn Văn Hùng", "0908123456"), c18.Grade, "Cấp vỏ đóng hàng cà phê");
            SetGateInDate(v18, now.AddDays(-7));
            v18.AllocateToOrder(order1.Id);
            v18.GateOut(CreateVehicle("51C-998.88", "51R-888.99", "Ngô Đình Trọng", "0938889999"));
            SetGateOutDate(v18, now.AddDays(-1));
            var hold18Res = v18.ApplyHold("PAYMENT_HOLD", "Chờ xác nhận cược vỏ qua ePort", "Kế toán Thu ngân");
            if (hold18Res.IsSuccess)
            {
                v18.ReleaseHold(hold18Res.Value!.Id, "Khách hàng đã thanh toán trực tuyến qua cổng ePort", "Kế toán Thu ngân");
            }

            var v19 = ContainerVisit.CreateGateIn(catLaiTenantId, c19.Id, c19.LineOperator, CreateVehicle("51D-903.45", "51R-678.90", "Hoàng Minh Tuấn", "0987654321"), c19.Grade, "Hoàn tất chuyến giao hàng");
            SetGateInDate(v19, now.AddDays(-6));
            v19.GateOut(CreateVehicle("51C-998.88", "51R-888.99", "Ngô Đình Trọng", "0938889999"));
            SetGateOutDate(v19, now.AddDays(-2));

            context.ContainerVisits.AddRange(v01, v02, v03, v04, v05, v06, v07, v08, v09, v10, v11, v12, v13, v14, v15, v16, v17, v18, v19);

            // =========================================================================
            // 7. NHẬT KÝ DI CHUYỂN BÃI CHI TIẾT (MOVEMENT EVENTS)
            // =========================================================================
            var movements = new List<MovementEvent>
            {
                MovementEvent.Create(catLaiTenantId, v01.Id, "GATE_IN", null, null, "Cổng B Cát Lái", "Xe đầu kéo 51C-882.34 giao cont vào bãi"),
                MovementEvent.Create(catLaiTenantId, v01.Id, "PLACE_YARD", null, slotA_111.Id, "Xe Nâng RTG-01", "Hạ bãi vị trí chỉ định A-A01-01"),

                MovementEvent.Create(catLaiTenantId, v02.Id, "GATE_IN", null, null, "Cổng A Cát Lái", "Xe đầu kéo 50H-192.83 giao cont vào bãi"),
                MovementEvent.Create(catLaiTenantId, v02.Id, "PLACE_YARD", null, slotA_112.Id, "Xe Nâng RTG-01", "Hạ chồng lên cont tầng 2"),

                MovementEvent.Create(catLaiTenantId, v03.Id, "GATE_IN", null, null, "Cổng C Cát Lái", "Tiếp nhận vỏ rỗng"),
                MovementEvent.Create(catLaiTenantId, v03.Id, "PLACE_YARD", null, slotA_121.Id, "Xe Nâng Reach Stacker RS-02", "Hạ bãi ô A-B01-01"),

                MovementEvent.Create(catLaiTenantId, v08.Id, "GATE_IN", null, null, "Cổng B Cát Lái", "Tiếp nhận vỏ"),
                MovementEvent.Create(catLaiTenantId, v08.Id, "PLACE_YARD", null, slotA_311.Id, "Xe Nâng RTG-03", "Hạ bãi khu chờ giám định A-A03-01"),

                MovementEvent.Create(catLaiTenantId, v09.Id, "GATE_IN", null, null, "Cổng Lạnh Cát Lái", "Tiếp nhận cont lạnh cắm điện"),
                MovementEvent.Create(catLaiTenantId, v09.Id, "PLACE_YARD", null, slotB_611.Id, "Xe Nâng Kalmar RTG-05", "Hạ bãi giàn điện lạnh B-A06-01"),

                MovementEvent.Create(catLaiTenantId, v12.Id, "GATE_IN", null, null, "Cổng Lạnh Cát Lái", "Tiếp nhận cont lạnh"),
                MovementEvent.Create(catLaiTenantId, v12.Id, "PLACE_YARD", null, slotB_1011.Id, "Xe Nâng Kalmar RTG-05", "Hạ bãi ô B-A10-01"),
                MovementEvent.Create(catLaiTenantId, v12.Id, "CUSTOMS_HOLD", slotB_1011.Id, slotB_1011.Id, "Hải Quan Cát Lái", "Niêm phong kiểm hóa tại chỗ"),

                MovementEvent.Create(catLaiTenantId, v17.Id, "GATE_IN", null, null, "Cổng C Cát Lái", "Tiếp nhận cont"),
                MovementEvent.Create(catLaiTenantId, v17.Id, "MOVE_TO_VIRTUAL", null, null, "Xe Kéo Nội Bộ Cát Lái", "Chuyển sang Xưởng M&R để hàn vá vách tôn"),

                MovementEvent.Create(catLaiTenantId, v18.Id, "GATE_IN", null, null, "Cổng A Cát Lái", "Tiếp nhận vỏ"),
                MovementEvent.Create(catLaiTenantId, v18.Id, "PLACE_YARD", null, slotsA["3_2_1"].Id, "Xe Nâng RTG-01", "Hạ bãi"),
                MovementEvent.Create(catLaiTenantId, v18.Id, "RELOCATE_YARD", slotsA["3_2_1"].Id, null, "Xe Nâng RTG-01", "Bốc cont lên xe đầu kéo xuất bãi"),
                MovementEvent.Create(catLaiTenantId, v18.Id, "GATE_OUT", null, null, "Cổng D Xuất Bãi", "Xuất bãi theo lệnh DO-MAEU-20261001")
            };

            context.MovementEvents.AddRange(movements);

            await context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            Semaphore.Release();
        }
    }

    /// <summary>
    /// Sinh số container chuẩn quốc tế ISO 6346 với thuật toán kiểm tra Modulo 11 chính xác.
    /// </summary>
    private static ContainerNumber CreateValidContainerNumber(string owner, string serial6)
    {
        var raw10 = $"{owner.Trim().ToUpperInvariant()}U{serial6.Trim()}";
        int sum = 0;
        for (int i = 0; i < 10; i++)
        {
            char c = raw10[i];
            int val = char.IsLetter(c) ? (c switch
            {
                'A' => 10, 'B' => 12, 'C' => 13, 'D' => 14, 'E' => 15, 'F' => 16, 'G' => 17,
                'H' => 18, 'I' => 19, 'J' => 20, 'K' => 21, 'L' => 23, 'M' => 24, 'N' => 25,
                'O' => 26, 'P' => 27, 'Q' => 28, 'R' => 29, 'S' => 30, 'T' => 31, 'U' => 32,
                'V' => 34, 'W' => 35, 'X' => 36, 'Y' => 37, 'Z' => 38, _ => 0
            }) : (c - '0');
            sum += val * (1 << i);
        }

        int remainder = sum % 11;
        char checkDigit = remainder == 10 ? '0' : (char)('0' + remainder);
        return ContainerNumber.Create($"{raw10}{checkDigit}").Value!;
    }

    private static void SetEntityId(BaseEntity entity, Guid id)
    {
        typeof(BaseEntity).GetProperty(nameof(BaseEntity.Id))?.SetValue(entity, id);
    }

    private static void SetGateInDate(ContainerVisit visit, DateTime date)
    {
        typeof(ContainerVisit).GetProperty(nameof(ContainerVisit.GateInDate))?.SetValue(visit, date);
    }

    private static void SetGateOutDate(ContainerVisit visit, DateTime date)
    {
        typeof(ContainerVisit).GetProperty(nameof(ContainerVisit.GateOutDate))?.SetValue(visit, date);
    }

    private static VehicleInfo CreateVehicle(string tractor, string trailer, string driver, string? phone = null)
    {
        return VehicleInfo.Create(tractor, trailer, driver, phone).Value!;
    }
}
