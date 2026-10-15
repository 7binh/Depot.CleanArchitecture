namespace Depot.CleanArchitecture.Application.Features.Yard.GetPendingPlacements;

using Depot.CleanArchitecture.Application.Abstractions.Data;
using Depot.CleanArchitecture.Application.Abstractions.Messaging;
using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Enums;
using Microsoft.EntityFrameworkCore;

public sealed class GetPendingPlacementsQueryHandler(IAppDbContext dbContext)
    : IQueryHandler<GetPendingPlacementsQuery, Result<IReadOnlyList<PendingPlacementDto>>>
{
    public async Task<Result<IReadOnlyList<PendingPlacementDto>>> HandleAsync(
        GetPendingPlacementsQuery query,
        CancellationToken cancellationToken = default)
    {
        var rawItems = await (
            from v in dbContext.ContainerVisits.AsNoTracking()
            join c in dbContext.Containers.AsNoTracking() on v.ContainerId equals c.Id
            where v.Status == ContainerVisitStatus.GatedIn
               && v.CurrentBlockId == null
               && v.CurrentSlotId == null
            orderby v.GateInDate ascending, v.Id ascending
            select new
            {
                VisitId = v.Id,
                ContainerId = c.Id,
                ContainerNumber = c.Number,
                LineOperator = v.LineOperator,
                Size = c.Size,
                Type = c.Type,
                IsoCode = c.IsoCode,
                InSurveyGrade = v.InSurveyGrade,
                GateInDate = v.GateInDate,
                TractorNo = v.InVehicle.TractorNo,
                TrailerNo = v.InVehicle.TrailerNo,
                DriverName = v.InVehicle.DriverName,
                DriverPhone = v.InVehicle.DriverPhone,
                DamageNotes = v.InDamageNotes
            }
        ).ToListAsync(cancellationToken);

        var now = DateTime.UtcNow;

        var dtos = rawItems.Select(x =>
        {
            var isReefer = x.Type == ContainerType.Reefer ||
                           (!string.IsNullOrEmpty(x.IsoCode) &&
                            (x.IsoCode.StartsWith("45R", StringComparison.OrdinalIgnoreCase) ||
                             x.IsoCode.StartsWith("22R", StringComparison.OrdinalIgnoreCase)));

            var waitingMinutes = (int)Math.Max(0, (now - x.GateInDate).TotalMinutes);

            return new PendingPlacementDto(
                VisitId: x.VisitId,
                ContainerId: x.ContainerId,
                ContainerNumber: x.ContainerNumber.Value,
                LineOperator: x.LineOperator,
                Size: x.Size,
                Type: x.Type.ToString(),
                IsoCode: x.IsoCode,
                Grade: x.InSurveyGrade.ToString(),
                GradeDescription: GetGradeDescription(x.InSurveyGrade),
                IsReefer: isReefer,
                GateInDate: x.GateInDate,
                WaitingMinutes: waitingMinutes,
                TractorNo: x.TractorNo,
                TrailerNo: x.TrailerNo,
                DriverName: x.DriverName,
                DriverPhone: x.DriverPhone,
                DamageNotes: x.DamageNotes);
        }).ToList();

        return Result.Success<IReadOnlyList<PendingPlacementDto>>(dtos);
    }

    private static string GetGradeDescription(ContainerGrade grade) => grade switch
    {
        ContainerGrade.GradeA => "Grade A (Đạt chuẩn đóng hàng xuất khẩu)",
        ContainerGrade.GradeB => "Grade B (Đóng hàng bách hóa tổng hợp)",
        ContainerGrade.GradeC => "Grade C (Đóng hàng thô, phế liệu)",
        ContainerGrade.GradeD_Damaged => "Grade D (Hư hỏng, cần sửa chữa M&R)",
        ContainerGrade.GradeE_Scrap => "Grade E (Phế thải, chờ thanh lý)",
        _ => grade.ToString()
    };
}
