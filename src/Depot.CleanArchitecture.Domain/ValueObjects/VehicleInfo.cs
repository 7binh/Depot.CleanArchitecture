namespace Depot.CleanArchitecture.Domain.ValueObjects;

using Depot.CleanArchitecture.Domain.Common;

public sealed class VehicleInfo : ValueObject
{
    public string TractorNo { get; private set; } = null!;  // Biển số xe đầu kéo (VD: 51C-123.45)
    public string TrailerNo { get; private set; } = null!;  // Biển số rơ-moóc (VD: 51R-678.90)
    public string DriverName { get; private set; } = null!; // Họ tên tài xế
    public string? DriverPhone { get; private set; }

    private VehicleInfo() { } // Dành cho EF Core

    private VehicleInfo(string tractorNo, string trailerNo, string driverName, string? driverPhone)
    {
        TractorNo = tractorNo;
        TrailerNo = trailerNo;
        DriverName = driverName;
        DriverPhone = driverPhone;
    }

    public static Result<VehicleInfo> Create(string tractorNo, string trailerNo, string driverName, string? driverPhone = null)
    {
        if (string.IsNullOrWhiteSpace(tractorNo) || string.IsNullOrWhiteSpace(trailerNo))
            return Result.Failure<VehicleInfo>(
                Error.Validation("Vehicle.InvalidLicense", "Biển số xe đầu kéo và rơ-moóc không được để trống."));

        if (string.IsNullOrWhiteSpace(driverName))
            return Result.Failure<VehicleInfo>(
                Error.Validation("Vehicle.InvalidDriver", "Họ tên tài xế không được để trống."));

        return Result.Success(new VehicleInfo(
            tractorNo.Trim().ToUpperInvariant(),
            trailerNo.Trim().ToUpperInvariant(),
            driverName.Trim(),
            driverPhone?.Trim()));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return TractorNo;
        yield return TrailerNo;
        yield return DriverName;
    }

    public override string ToString() => $"Tractor: {TractorNo}, Trailer: {TrailerNo}, Driver: {DriverName}";
}
