import { YardStockRow } from './report.service';

/** A report row ready to show: the operator as one label and the long-stay share as a whole percent. */
export interface StockLine {
  id: string;
  operator: string;
  recent: number;
  longStay: number;
  total: number;
  longStayPercent: number;
}

function percentOf(part: number, total: number): number {
  return total === 0 ? 0 : Math.round((part / total) * 100);
}

function line(id: string, operator: string, recent: number, longStay: number): StockLine {
  const total = recent + longStay;
  return { id, operator, recent, longStay, total, longStayPercent: percentOf(longStay, total) };
}

export function toStockLines(rows: YardStockRow[]): StockLine[] {
  return rows.map((r) =>
    line(r.operatorCode, `${r.operatorCode} - ${r.operatorName}`, r.recent, r.longStay),
  );
}

/** The sum of all lines, shown at the bottom of the table. */
export function totalOf(lines: StockLine[]): StockLine {
  const recent = lines.reduce((sum, l) => sum + l.recent, 0);
  const longStay = lines.reduce((sum, l) => sum + l.longStay, 0);
  return line('total', 'Tổng', recent, longStay);
}
