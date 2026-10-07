namespace Depot.CleanArchitecture.Domain.Common;

public abstract class BaseEntity
{
    //Định danh duy nhất bằng Guid cho tất cả các thực thể
    public Guid Id { get; protected set; } = Guid.NewGuid();
}