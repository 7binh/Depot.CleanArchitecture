namespace Depot.CleanArchitecture.Domain.ValueObjects;

using Depot.CleanArchitecture.Domain.Common;

public sealed class ContainerNumber : ValueObject
{
    private static readonly Dictionary<char, int> CharMap = new()
    {
        {'A', 10}, {'B', 12}, {'C', 13}, {'D', 14}, {'E', 15}, {'F', 16}, {'G', 17},
        {'H', 18}, {'I', 19}, {'J', 20}, {'K', 21}, {'L', 23}, {'M', 24}, {'N', 25},
        {'O', 26}, {'P', 27}, {'Q', 28}, {'R', 29}, {'S', 30}, {'T', 31}, {'U', 32},
        {'V', 34}, {'W', 35}, {'X', 36}, {'Y', 37}, {'Z', 38}
    };

    public string Value { get; }
    public string OwnerCode => Value[..3];
    public char CategoryIdentifier => Value[3];
    public string SerialNumber => Value.Substring(4, 6);
    public char CheckDigit => Value[10];

    private ContainerNumber(string value) => Value = value;

    public static Result<ContainerNumber> Create(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return Result.Failure<ContainerNumber>(Error.Validation("Container.Empty", "Số container không được để trống."));

        var upper = raw.Trim().ToUpperInvariant();
        if (upper.Length != 11)
            return Result.Failure<ContainerNumber>(Error.Validation("Container.InvalidLength", "Số container phải có đúng 11 ký tự."));

        if (!ValidateModulo11(upper))
            return Result.Failure<ContainerNumber>(Error.Validation("Container.InvalidCheckDigit", $"Số container {upper} không đúng số kiểm tra Modulo 11 (ISO 6346)."));

        return Result.Success(new ContainerNumber(upper));
    }

    public static bool ValidateModulo11(string containerNumber)
    {
        if (containerNumber.Length != 11) return false;

        int sum = 0;
        for (int i = 0; i < 10; i++)
        {
            char c = containerNumber[i];
            int val;
            if (char.IsLetter(c))
            {
                if (!CharMap.TryGetValue(c, out val)) return false;
            }
            else if (char.IsDigit(c))
            {
                val = c - '0';
            }
            else
            {
                return false;
            }

            int weight = 1 << i; // 2^i
            sum += val * weight;
        }

        int remainder = sum % 11;
        char expectedCheckDigit = remainder == 10 ? 'X' : (char)('0' + remainder);

        return containerNumber[10] == expectedCheckDigit;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
