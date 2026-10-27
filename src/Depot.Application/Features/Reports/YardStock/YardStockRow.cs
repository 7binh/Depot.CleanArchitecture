namespace Depot.Application.Features.Reports.YardStock;

// Recent is 0 to 9 days in the yard, LongStay is 10 days or more (BR-RP-02).
public sealed record YardStockRow(string OperatorCode, string OperatorName, int Recent, int LongStay, int Total);
