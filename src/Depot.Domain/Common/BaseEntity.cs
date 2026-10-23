namespace Depot.Domain.Common;

/// <summary>Base class of every entity: all tables use a Guid primary key (see ERD_DEPOT.md).</summary>
public abstract class BaseEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
}
