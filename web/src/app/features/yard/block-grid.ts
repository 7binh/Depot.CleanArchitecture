import { ChipTone } from '../../shared/ui/chip';
import { BayMap, BlockMap, MapContainer, TierMap } from './yard.service';

/** An odd bay: the only kind of column the map has, because a 40ft container lies across two of them. */
export interface GridBay {
  bayNo: number;
  /** 1-based grid column. */
  column: number;
}

export interface CellFlag {
  label: string;
  tone: ChipTone;
}

/** One box of a row on the chosen tier: a container, or a free place when `container` is null. */
export interface GridCell {
  key: string;
  column: number;
  /** How many bay columns it covers: 2 for a 40ft container. */
  span: number;
  container: MapContainer | null;
  flag: CellFlag | null;
  /** Some other tier above / below this place holds a container. */
  hasAbove: boolean;
  hasBelow: boolean;
}

export interface GridRow {
  rowNo: number;
  cells: GridCell[];
}

export interface TierGrid {
  bays: GridBay[];
  rows: GridRow[];
}

/** A container placed on the floor of the odd-bay columns it covers. */
interface Placed {
  tier: number;
  column: number;
  span: number;
  container: MapContainer;
}

/** Damaged wins over long stay, so a cell never carries two chip colours. */
export function flagOf(container: MapContainer): CellFlag | null {
  if (container.isDamaged) {
    return { label: 'Hư hỏng', tone: 'danger' };
  }
  return container.isLongStay ? { label: 'Tồn lâu', tone: 'warning' } : null;
}

/** The number of tiers to offer: the tallest stack any row of the block allows. */
export function tierCount(map: BlockMap): number {
  const heights = map.bays.flatMap((bay) => bay.rows.map((row) => row.maxTier));
  return Math.max(0, ...heights);
}

/** Lays one tier of the block out on a grid: one column per odd bay, 40ft containers two columns wide. */
export function toTierGrid(map: BlockMap, tier: number): TierGrid {
  const bays = map.bays
    .filter((bay) => bay.sizeFeet === 20)
    .map((bay, index) => ({ bayNo: bay.bayNo, column: index + 1 }));
  const rowNos = [...new Set(map.bays.flatMap((bay) => bay.rows.map((row) => row.rowNo)))].sort(
    (a, b) => a - b,
  );
  return {
    bays,
    rows: rowNos.map((rowNo) => ({ rowNo, cells: cellsOf(rowNo, tier, map.bays, bays) })),
  };
}

function cellsOf(rowNo: number, tier: number, bays: BayMap[], columns: GridBay[]): GridCell[] {
  const placed = bays.flatMap((bay) =>
    (bay.rows.find((row) => row.rowNo === rowNo)?.tiers ?? []).map((t) => place(t, columns)),
  );
  const filled = placed
    .filter((item) => item.tier === tier)
    .map((item) => filledCell(item, placed));
  const taken = new Set(filled.flatMap(columnsOf));
  const free = bays
    .filter((bay) => bay.sizeFeet === 20)
    .flatMap((bay) => freeCell(bay, rowNo, tier, columns, taken, placed));
  return [...filled, ...free].sort((a, b) => a.column - b.column);
}

function place(tier: TierMap, columns: GridBay[]): Placed {
  const covered = columns.filter((bay) => bay.bayNo >= tier.fromBay && bay.bayNo <= tier.toBay);
  return {
    tier: tier.tier,
    column: covered.at(0)?.column ?? 1,
    span: Math.max(covered.length, 1),
    container: tier.container,
  };
}

function filledCell(item: Placed, placed: Placed[]): GridCell {
  return {
    key: `${item.container.stayId}`,
    column: item.column,
    span: item.span,
    container: item.container,
    flag: flagOf(item.container),
    ...neighbours(item.column, item.span, item.tier, placed),
  };
}

/** A free place exists only where the bay's row is tall enough for this tier and no container covers it. */
function freeCell(
  bay: BayMap,
  rowNo: number,
  tier: number,
  columns: GridBay[],
  taken: Set<number>,
  placed: Placed[],
): GridCell[] {
  const row = bay.rows.find((candidate) => candidate.rowNo === rowNo);
  const column = columns.find((candidate) => candidate.bayNo === bay.bayNo)?.column;
  if (!row || column === undefined || tier > row.maxTier || taken.has(column)) {
    return [];
  }
  return [
    {
      key: `free-${rowNo}-${column}`,
      column,
      span: 1,
      container: null,
      flag: null,
      ...neighbours(column, 1, tier, placed),
    },
  ];
}

function neighbours(column: number, span: number, tier: number, placed: Placed[]) {
  const here = placed.filter(
    (item) => item.column < column + span && item.column + item.span > column,
  );
  return {
    hasAbove: here.some((item) => item.tier > tier),
    hasBelow: here.some((item) => item.tier < tier),
  };
}

function columnsOf(cell: GridCell): number[] {
  return Array.from({ length: cell.span }, (_, i) => cell.column + i);
}
