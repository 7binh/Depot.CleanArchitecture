import { ChipTone } from '../../shared/ui/chip';
import { BayMap, BlockMap, MapContainer, RowMap, TierMap } from './yard.service';

export interface GridBay {
  bayNo: number;
  sizeFeet: number;
  /** 1-based grid column of this bay. */
  column: number;
}

export interface SlotFlag {
  label: string;
  tone: ChipTone;
}

/** One box of a row: a container, or a free place when `container` is null. */
export interface GridSlot {
  key: string;
  /** 1-based grid line, counted from the top tier down. */
  line: number;
  column: number;
  span: number;
  container: MapContainer | null;
  flag: SlotFlag | null;
}

export interface GridRow {
  rowNo: number;
  slots: GridSlot[];
}

export interface BlockGrid {
  bays: GridBay[];
  rows: GridRow[];
  unplaced: MapContainer[];
}

interface RowInBay {
  bay: BayMap;
  row: RowMap;
}

/** Damaged wins over long stay, so a cell never carries two chip colours. */
export function flagOf(container: MapContainer): SlotFlag | null {
  if (container.isDamaged) {
    return { label: 'Hư hỏng', tone: 'danger' };
  }
  return container.isLongStay ? { label: 'Tồn lâu', tone: 'warning' } : null;
}

/** Lays the block out on a grid: one column per bay, one grid line per tier. Only draws what the API sent. */
export function toBlockGrid(map: BlockMap): BlockGrid {
  const bays = map.bays.map((bay, index) => ({
    bayNo: bay.bayNo,
    sizeFeet: bay.sizeFeet,
    column: index + 1,
  }));
  const rowNos = [...new Set(map.bays.flatMap((bay) => bay.rows.map((row) => row.rowNo)))].sort(
    (a, b) => a - b,
  );
  return {
    bays,
    rows: rowNos.map((rowNo) => toGridRow(rowNo, map.bays, bays)),
    unplaced: map.unplaced,
  };
}

function toGridRow(rowNo: number, bays: BayMap[], columns: GridBay[]): GridRow {
  const inBays = bays.flatMap((bay) => {
    const row = bay.rows.find((candidate) => candidate.rowNo === rowNo);
    return row ? [{ bay, row }] : [];
  });
  const tiers = Math.max(...inBays.map((item) => item.row.maxTier));
  const filled = inBays.flatMap(({ row }) =>
    row.tiers.map((tier) => filledSlot(tier, tiers, columns)),
  );
  const taken = new Set(
    filled.flatMap((slot) =>
      Array.from({ length: slot.span }, (_, i) => slotKey(slot.line, slot.column + i)),
    ),
  );
  const free = inBays.flatMap((item) => freeSlots(item, tiers, columns, taken));
  return { rowNo, slots: [...filled, ...free] };
}

function filledSlot(tier: TierMap, tiers: number, columns: GridBay[]): GridSlot {
  const covered = columns.filter((bay) => bay.bayNo >= tier.fromBay && bay.bayNo <= tier.toBay);
  const column = covered.at(0)?.column ?? 1;
  const line = tiers - tier.tier + 1;
  return {
    key: slotKey(line, column),
    line,
    column,
    span: Math.max(covered.length, 1),
    container: tier.container,
    flag: flagOf(tier.container),
  };
}

/** The free places of one bay, minus those a 40ft container above the neighbouring odd bays already covers. */
function freeSlots(
  { bay, row }: RowInBay,
  tiers: number,
  columns: GridBay[],
  taken: Set<string>,
): GridSlot[] {
  const column = columns.find((candidate) => candidate.bayNo === bay.bayNo)?.column ?? 1;
  const slots: GridSlot[] = [];
  for (let tier = 1; tier <= row.maxTier; tier++) {
    const line = tiers - tier + 1;
    if (!taken.has(slotKey(line, column))) {
      slots.push({
        key: slotKey(line, column),
        line,
        column,
        span: 1,
        container: null,
        flag: null,
      });
    }
  }
  return slots;
}

function slotKey(line: number, column: number): string {
  return `${line}:${column}`;
}
