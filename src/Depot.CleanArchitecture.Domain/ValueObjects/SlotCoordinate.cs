namespace Depot.CleanArchitecture.Domain.ValueObjects;

using Depot.CleanArchitecture.Domain.Common;

public sealed class SlotCoordinate : ValueObject
{
    public int Bay { get; }
    public int Row { get; }
    public int Tier { get; }

    public bool IsOddBay => Bay % 2 != 0;   // Bay lẻ: cont 20ft
    public bool IsEvenBay => Bay % 2 == 0;  // Bay chẵn: cont 40ft

    private SlotCoordinate(int bay, int row, int tier)
    {
        Bay = bay;
        Row = row;
        Tier = tier;
    }

    public static Result<SlotCoordinate> Create(int bay, int row, int tier)
    {
        if (bay <= 0 || row <= 0 || tier <= 0)
            return Result.Failure<SlotCoordinate>(
                Error.Validation("Coordinate.Invalid", "Chỉ số Bay, Row, Tier phải là các số nguyên dương lớn hơn 0."));

        return Result.Success(new SlotCoordinate(bay, row, tier));
    }

    // Với Bay chẵn 40ft (VD: Bay 02) -> lấy 2 bay lẻ bị chiếm dụng (Bay 01, Bay 03)
    public (int PriorOddBay, int NextOddBay) GetOverlappingOddBays()
    {
        if (IsOddBay) throw new InvalidOperationException("Phương thức này chỉ áp dụng cho bay chẵn 40ft.");
        return (Bay - 1, Bay + 1);
    }

    // Với Bay lẻ 20ft (VD: Bay 01 hoặc Bay 03) -> lấy các bay chẵn lân cận có thể xung đột
    public IEnumerable<int> GetAdjacentEvenBays()
    {
        if (IsEvenBay) throw new InvalidOperationException("Phương thức này chỉ áp dụng cho bay lẻ 20ft.");
        if (Bay > 1) yield return Bay - 1;
        yield return Bay + 1;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Bay;
        yield return Row;
        yield return Tier;
    }

    public override string ToString() => $"Bay {Bay:D2} - Row {Row:D2} - Tier {Tier:D2}";
}
