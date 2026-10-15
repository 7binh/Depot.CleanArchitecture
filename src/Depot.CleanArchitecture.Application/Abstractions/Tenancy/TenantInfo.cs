namespace Depot.CleanArchitecture.Application.Abstractions.Tenancy;

/// <summary>
/// Đại diện cho thông tin metadata của chi nhánh Depot (Tenant).
/// </summary>
public sealed record TenantInfo
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public string? ConnectionString { get; init; }
    public bool IsActive { get; init; } = true;

    public static readonly Guid DefaultDepotCatLaiId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    /// <summary>Tenant mặc định cho Depot Tân Cảng Cát Lái.</summary>
    public static TenantInfo Default => new()
    {
        Id = DefaultDepotCatLaiId.ToString(),
        Name = "Tân Cảng Cát Lái",
        IsActive = true
    };
}
