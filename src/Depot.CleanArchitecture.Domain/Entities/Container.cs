namespace Depot.CleanArchitecture.Domain.Entities;

using Depot.CleanArchitecture.Domain.Common;
using Depot.CleanArchitecture.Domain.Enums;
using Depot.CleanArchitecture.Domain.ValueObjects;

public class Container : AuditableEntity, ITenantEntity
{
    public Guid TenantId { get; set; }
    public ContainerNumber Number { get; private set; } = null!;
    public string LineOperator { get; private set; } = null!;
    public ContainerType Type { get; private set; }
    public string IsoCode { get; private set; } = null!;
    public int Size { get; private set; } // 20, 40, 45 feet
    public decimal TareWeight { get; private set; } // Trọng lượng vỏ cont
    public decimal MaxGrossWeight { get; private set; } // Tải trọng tối đa
    public decimal PayloadWeight => MaxGrossWeight - TareWeight; // Tải trọng hữu ích
    public ContainerGrade Grade { get; private set; }
    public string? DamageNotes { get; private set; }

    private Container() { } // Dành cho EF Core

    public static Result<Container> Create(
        Guid tenantId,
        ContainerNumber number,
        string lineOperator,
        ContainerType type,
        string isoCode,
        int size,
        decimal tareWeight,
        decimal maxGrossWeight,
        ContainerGrade grade)
    {
        if (maxGrossWeight <= tareWeight)
            return Result.Failure<Container>(
                Error.Validation("Container.InvalidWeight", "Tải trọng tối đa (Max Gross Weight) phải lớn hơn trọng lượng vỏ (Tare Weight)."));

        var container = new Container
        {
            TenantId = tenantId,
            Number = number,
            LineOperator = lineOperator.ToUpperInvariant(),
            Type = type,
            IsoCode = isoCode,
            Size = size,
            TareWeight = tareWeight,
            MaxGrossWeight = maxGrossWeight,
            Grade = grade
        };

        return Result.Success(container);
    }

    public void UpdateCondition(ContainerGrade newGrade, string? damageNotes)
    {
        Grade = newGrade;
        DamageNotes = damageNotes;
    }
}
