import { formatDate } from '../release-orders/release-order-status';
import { ThroughputRow } from './report.service';

/** A report row ready to show: the date as dd/MM/yyyy and the operator as one label. */
export interface ThroughputLine {
  id: string;
  date: string;
  operator: string;
  gateIns: number;
  gateOuts: number;
}

export function toThroughputLines(rows: ThroughputRow[]): ThroughputLine[] {
  return rows.map((r) => ({
    id: `${r.date}-${r.operatorCode}`,
    date: formatDate(r.date),
    operator: `${r.operatorCode} - ${r.operatorName}`,
    gateIns: r.gateIns,
    gateOuts: r.gateOuts,
  }));
}

/** The sums shown at the bottom of the table. */
export function throughputTotals(lines: ThroughputLine[]): { gateIns: number; gateOuts: number } {
  return {
    gateIns: lines.reduce((sum, l) => sum + l.gateIns, 0),
    gateOuts: lines.reduce((sum, l) => sum + l.gateOuts, 0),
  };
}
