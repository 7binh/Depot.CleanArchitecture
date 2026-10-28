import { LineOperator } from '../catalogs/line-operators/line-operator.service';
import { Container } from '../containers/container.service';
import { PlaceIndex } from './place-index';
import { StayRecord } from './stay.service';

/** A stay with the container, operator and place worked out, ready to show in a table. */
export interface StayRow {
  id: string;
  number: string;
  operator: string;
  gateIn: string;
  gateOut: string;
  place: string;
  condition: string;
  isDamaged: boolean;
  inYard: boolean;
}

const PAD = (n: number): string => String(n).padStart(2, '0');

/** `2026-10-20T08:30:00+07:00` becomes `20/10/2026 08:30` in the viewer's time zone. */
export function formatDateTime(iso: string): string {
  const d = new Date(iso);
  return `${PAD(d.getDate())}/${PAD(d.getMonth() + 1)}/${d.getFullYear()} ${PAD(d.getHours())}:${PAD(d.getMinutes())}`;
}

/** The virtual block has no bay or row, so it is named by itself. */
function placeOf(stay: StayRecord, places: PlaceIndex): string {
  const block = places.blocks.get(stay.blockId);
  if (!block) {
    return '';
  }
  if (block.isVirtual) {
    return 'Bãi ảo';
  }
  const row = stay.rowId ? places.rows.get(stay.rowId) : undefined;
  const bayAndRow = row ? `, bay ${row.bayNo}, row ${row.rowNo}` : '';
  return `${block.code}${bayAndRow}, tầng ${stay.tier}`;
}

export function toStayRows(
  stays: StayRecord[],
  containers: Container[],
  operators: LineOperator[],
  places: PlaceIndex,
): StayRow[] {
  const containerById = new Map(containers.map((c) => [c.id, c]));
  const operatorCodes = new Map(operators.map((o) => [o.id, o.code]));
  return [...stays]
    .sort((a, b) => b.gateInAt.localeCompare(a.gateInAt))
    .map((stay) => {
      const container = containerById.get(stay.containerId);
      return {
        id: stay.id,
        number: container?.number ?? '',
        operator: (container && operatorCodes.get(container.lineOperatorId)) ?? '',
        gateIn: formatDateTime(stay.gateInAt),
        gateOut: stay.gateOutAt ? formatDateTime(stay.gateOutAt) : 'Chưa ra',
        place: placeOf(stay, places),
        condition: stay.condition === 'Damaged' ? 'Hư hỏng' : 'Bình thường',
        isDamaged: stay.condition === 'Damaged',
        inYard: stay.gateOutAt === null,
      };
    });
}
