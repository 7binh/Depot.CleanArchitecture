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

    /// <summary>Tenant mặc định cho Depot Tân Cảng Cát Lái.</summary>
    public static TenantInfo Default => new()
    {
        Id = "catlai-depot",
        Name = "Tân Cảng Cát Lái",
        IsActive = true
    };
}
