namespace Depot.CleanArchitecture.Domain.Entities;

using Depot.CleanArchitecture.Domain.Common;

public sealed class StayHold : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid VisitId { get; private set; }
    public string HoldCode { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public DateTime HeldAt { get; private set; }
    public string HeldBy { get; private set; } = string.Empty;
    public DateTime? ReleasedAt { get; private set; }
    public string? ReleasedBy { get; private set; }
    public string? ReleaseReason { get; private set; }
    public bool IsActive => ReleasedAt is null;

    private StayHold() { }

    public static Result<StayHold> Create(
        Guid tenantId,
        Guid visitId,
        string holdCode,
        string reason,
        string heldBy)
    {
        if (string.IsNullOrWhiteSpace(holdCode))
            return Result.Failure<StayHold>(new Error("Hold.CodeEmpty", "Mã khóa giữ không được để trống.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure<StayHold>(new Error("Hold.ReasonEmpty", "Lý do khóa giữ không được để trống.", ErrorType.Validation));

        var hold = new StayHold
        {
            TenantId = tenantId,
            VisitId = visitId,
            HoldCode = holdCode.Trim().ToUpperInvariant(),
            Reason = reason.Trim(),
            HeldAt = DateTime.UtcNow,
            HeldBy = heldBy.Trim()
        };

        return Result.Success(hold);
    }

    public Result Release(string releaseReason, string releasedBy)
    {
        if (!IsActive)
            return Result.Failure(new Error("Hold.AlreadyReleased", "Lệnh giữ này đã được giải tỏa trước đó.", ErrorType.Conflict));

        if (string.IsNullOrWhiteSpace(releaseReason))
            return Result.Failure(new Error("Hold.ReleaseReasonEmpty", "Lý do giải tỏa không được để trống.", ErrorType.Validation));

        ReleasedAt = DateTime.UtcNow;
        ReleasedBy = releasedBy.Trim();
        ReleaseReason = releaseReason.Trim();

        return Result.Success();
    }
}
