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

    public bool IsReefer => Type == ContainerType.Reefer ||
                            (!string.IsNullOrEmpty(IsoCode) && (IsoCode.StartsWith("45R") || IsoCode.StartsWith("22R")));

    public static (decimal Tare, decimal MaxGross, string IsoCode) GetDefaultSpecs(int size, ContainerType type)
    {
        if (type == ContainerType.Reefer)
        {
            return size == 20
                ? (2900m, 30480m, "22R1")
                : (4400m, 32500m, "45R1");
        }

        return size switch
        {
            20 => (2200m, 30480m, "22G1"),
            45 => (4200m, 32500m, "45G1"),
            _ => (3800m, 32500m, "42G1")
        };
    }

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
