using Depot.Domain.Catalogs;
using Depot.Domain.Common;
using Depot.Domain.Common.Rules;
using Depot.Domain.Containers.Rules;

namespace Depot.Domain.Containers;

public sealed class Container : BaseEntity
{
    public const int NumberLength = 11;
    public const int IsoCodeLength = 4;
    public const int SizeSmallFeet = 20;
    public const int SizeLargeFeet = 40;

    // Only for EF Core: it cannot bind the constructor below because of the `spec` parameter.
    private Container()
    {
    }

    private Container(string number, Guid lineOperatorId, Guid containerTypeId, string isoCode, ContainerSpec spec)
    {
        Number = number;
        LineOperatorId = lineOperatorId;
        ContainerTypeId = containerTypeId;
        IsoCode = isoCode;
        SizeFeet = spec.SizeFeet;
        MaxWeightKg = spec.MaxWeightKg;
        TareWeightKg = spec.TareWeightKg;
        ManufactureDate = spec.ManufactureDate;
    }

    public string Number { get; private set; } = string.Empty;

    public Guid LineOperatorId { get; private set; }

    public Guid ContainerTypeId { get; private set; }

    public string IsoCode { get; private set; } = string.Empty;

    public int SizeFeet { get; private set; }

    public decimal MaxWeightKg { get; private set; }

    public decimal TareWeightKg { get; private set; }

    public DateOnly ManufactureDate { get; private set; }

    /// <summary>
    /// The line operator is passed in whole because the owner code of the number must match its code (BR-CT-09);
    /// the caller supplies <paramref name="today"/> so the domain never reads the clock.
    /// </summary>
    public static Result<Container> Create(
        string? number,
        LineOperator lineOperator,
        Guid containerTypeId,
        ContainerSpec spec,
        DateOnly today)
    {
        var cleanNumber = number?.Trim().ToUpperInvariant();
        var cleanIsoCode = spec.IsoCode?.Trim().ToUpperInvariant();

        var check = BusinessRuleValidator.Check(
            new NotEmptyRule(cleanNumber, "Number"),
            new ContainerNumberFormatRule(cleanNumber),
            new ContainerNumberCheckDigitRule(cleanNumber),
            new OwnerCodeMatchesRule(cleanNumber, lineOperator.Code),
            new NotEmptyRule(cleanIsoCode, "IsoCode"),
            new IsoCodeRule(cleanIsoCode),
            new SizeFeetRule(spec.SizeFeet),
            new PositiveWeightRule(spec.MaxWeightKg, "MaxWeightKg"),
            new PositiveWeightRule(spec.TareWeightKg, "TareWeightKg"),
            new TareNotAboveMaxRule(spec.TareWeightKg, spec.MaxWeightKg),
            new ManufactureDateRule(spec.ManufactureDate, today));

        if (check.IsFailure)
        {
            return Result.Failure<Container>(check.Error);
        }

        return Result.Success(new Container(cleanNumber!, lineOperator.Id, containerTypeId, cleanIsoCode!, spec));
    }
}
