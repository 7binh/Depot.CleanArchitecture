namespace Depot.Application.Features.ContainerStays.Search;

/// <param name="Number">Part of the container number, any case.</param>
/// <param name="InYard">True keeps only stays still open; false keeps only stays that ended.</param>
/// <param name="Page">Starts at 1; a smaller value is read as 1.</param>
/// <param name="PageSize">Kept between 1 and <see cref="MaxPageSize"/>.</param>
public sealed record SearchContainerStaysQuery(
    string? Number = null,
    bool? InYard = null,
    int Page = 1,
    int PageSize = SearchContainerStaysQuery.DefaultPageSize)
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
}
