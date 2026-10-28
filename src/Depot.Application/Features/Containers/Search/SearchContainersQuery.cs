namespace Depot.Application.Features.Containers.Search;

/// <param name="Number">Part of the container number, any case.</param>
/// <param name="LineOperatorId">Keeps only containers of this line operator.</param>
/// <param name="ContainerTypeId">Keeps only containers of this type.</param>
/// <param name="SizeFeet">Keeps only containers of this size, 20 or 40.</param>
/// <param name="IsoCode">Part of the ISO code, any case.</param>
/// <param name="InYard">True keeps only containers now in the yard (a stay still open); false keeps the others.</param>
/// <param name="Page">Starts at 1; a smaller value is read as 1.</param>
/// <param name="PageSize">Kept between 1 and <see cref="MaxPageSize"/>.</param>
public sealed record SearchContainersQuery(
    string? Number = null,
    Guid? LineOperatorId = null,
    Guid? ContainerTypeId = null,
    int? SizeFeet = null,
    string? IsoCode = null,
    bool? InYard = null,
    int Page = 1,
    int PageSize = SearchContainersQuery.DefaultPageSize)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}
