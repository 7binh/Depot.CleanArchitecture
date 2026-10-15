namespace Depot.CleanArchitecture.Domain.Entities;

using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Enums;

public sealed class ConditionSurvey : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public Guid VisitId { get; private set; }
    public ContainerGrade Grade { get; private set; }
    public string Condition { get; private set; } = string.Empty;
    public List<string> DamageCodes { get; private set; } = [];
    public string? Notes { get; private set; }
    public string? PhotoRefs { get; private set; }
    public DateTime InspectedAt { get; private set; }
    public string InspectedBy { get; private set; } = string.Empty;

    private ConditionSurvey() { }

    public static Result<ConditionSurvey> Create(
        Guid tenantId,
        Guid visitId,
        ContainerGrade grade,
        string condition,
        List<string> damageCodes,
        string? notes,
        string? photoRefs,
        string inspectedBy)
    {
        if (visitId == Guid.Empty)
            return Result.Failure<ConditionSurvey>(new Error("Inspection.VisitIdEmpty", "Mã lượt lưu bãi không được để trống.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(inspectedBy))
            return Result.Failure<ConditionSurvey>(new Error("Inspection.InspectorEmpty", "Tên người giám định không được để trống.", ErrorType.Validation));

        var survey = new ConditionSurvey
        {
            TenantId = tenantId,
            VisitId = visitId,
            Grade = grade,
            Condition = condition.Trim().ToUpperInvariant(),
            DamageCodes = damageCodes ?? [],
            Notes = notes?.Trim(),
            PhotoRefs = photoRefs?.Trim(),
            InspectedAt = DateTime.UtcNow,
            InspectedBy = inspectedBy.Trim()
        };

        return Result.Success(survey);
    }
}
