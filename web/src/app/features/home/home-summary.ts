import { StockLine, toStockLines, totalOf } from '../reports/yard-stock-rows';
import { ThroughputRow, YardStockRow } from '../reports/report.service';

/** Gate movements of one day, all operators together. */
export interface DayMovement {
  /** `yyyy-MM-dd` */
  date: string;
  /** `dd/MM` */
  label: string;
  gateIns: number;
  gateOuts: number;
  total: number;
}

/** What the home page shows: the yard today, the last days of gate traffic and the orders still open. */
export interface HomeSummary {
  inYard: number;
  longStay: number;
  longStayPercent: number;
  /** The most recent day, which is today. */
  today: DayMovement;
  days: DayMovement[];
  openOrders: number;
  operators: StockLine[];
}

const NO_MOVEMENT: DayMovement = { date: '', label: '', gateIns: 0, gateOuts: 0, total: 0 };

/** One entry per date from `dates`, in that order, summing the operators of the report. */
export function toDayMovements(rows: ThroughputRow[], dates: string[]): DayMovement[] {
  return dates.map((date) => {
    const ofDay = rows.filter((r) => r.date === date);
    const gateIns = ofDay.reduce((sum, r) => sum + r.gateIns, 0);
    const gateOuts = ofDay.reduce((sum, r) => sum + r.gateOuts, 0);
    const label = `${date.slice(8)}/${date.slice(5, 7)}`;
    return { date, label, gateIns, gateOuts, total: gateIns + gateOuts };
  });
}

export function toHomeSummary(
  stock: YardStockRow[],
  days: DayMovement[],
  openOrders: number,
): HomeSummary {
  const operators = toStockLines(stock).sort((a, b) => b.total - a.total);
  const sum = totalOf(operators);
  return {
    inYard: sum.total,
    longStay: sum.longStay,
    longStayPercent: sum.longStayPercent,
    today: days.at(-1) ?? NO_MOVEMENT,
    days,
    openOrders,
    operators,
  };
}
