namespace Depot.Application.Features.Reports.YardStock;

/// <param name="ReportDate">Depot day the stock is counted on; empty means today.</param>
public sealed record YardStockQuery(DateOnly? ReportDate = null);
