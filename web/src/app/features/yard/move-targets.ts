import { GridBay } from './block-grid';
import { BayMap, BlockMap, MapContainer } from './yard.service';

/** `free` can be chosen, `occupied` holds a container, `blocked` cannot take the container being moved. */
export type MoveCellState = 'free' | 'occupied' | 'blocked';

export interface MoveCell {
  key: string;
  bayNo: number;
  rowNo: number;
  rowId: string;
  tier: number;
  /** 1-based grid column. */
  column: number;
  state: MoveCellState;
  /** The container standing here when the place is occupied. */
  container: MapContainer | null;
  /** Why a blocked place cannot be chosen. */
  reason: string | null;
}

export interface MoveRow {
  rowNo: number;
  cells: MoveCell[];
}

export interface MoveGrid {
  bays: GridBay[];
  rows: MoveRow[];
}

const BLOCKED_HERE = 'Vị trí hiện tại của container';
const BLOCKED_BELOW = 'Tầng dưới còn trống';
const BLOCKED_NEIGHBOUR = 'Container ở bay bên cạnh đang chiếm chỗ';
const BLOCKED_NO_NEIGHBOUR = 'Thiếu bay lẻ bên cạnh';

/** Where the container being moved stands now, so that place is not offered as a target. */
interface Origin {
  bayNo: number;
  rowNo: number;
  tier: number;
}

/**
 * Lays one tier of a block out for choosing where `mover` goes. The columns are the bays that take its size
 * (odd for 20ft, even for 40ft), and each place is marked with the same rules the server applies (BR-YD-03 to
 * BR-YD-06), counted without the container being moved. The server stays the judge: this only avoids offering
 * places that are certain to be refused.
 */
export function toMoveGrid(map: BlockMap, tier: number, mover: MapContainer): MoveGrid {
  const { bays, origin } = withoutMover(map.bays, mover);
  const columns = bays.filter((bay) => bay.sizeFeet === mover.sizeFeet);
  const gridBays = columns.map((bay, index) => ({ bayNo: bay.bayNo, column: index + 1 }));
  const rowNos = [...new Set(columns.flatMap((bay) => bay.rows.map((row) => row.rowNo)))].sort(
    (a, b) => a - b,
  );
  return {
    bays: gridBays,
    rows: rowNos
      .map((rowNo) => ({
        rowNo,
        cells: columns.flatMap((bay) => {
          const column = gridBays.find((candidate) => candidate.bayNo === bay.bayNo)?.column ?? 1;
          return cellsOf(bays, bay, rowNo, tier, column, mover.sizeFeet, origin);
        }),
      }))
      // A row too low for this tier has no place to show.
      .filter((row) => row.cells.length > 0),
  };
}

function withoutMover(
  bays: BayMap[],
  mover: MapContainer,
): { bays: BayMap[]; origin: Origin | null } {
  let origin: Origin | null = null;
  const others = bays.map((bay) => ({
    ...bay,
    rows: bay.rows.map((row) => {
      const mine = row.tiers.find((t) => t.container.stayId === mover.stayId);
      if (mine) {
        origin = { bayNo: bay.bayNo, rowNo: row.rowNo, tier: mine.tier };
      }
      return { ...row, tiers: row.tiers.filter((t) => t.container.stayId !== mover.stayId) };
    }),
  }));
  return { bays: others, origin };
}

function cellsOf(
  bays: BayMap[],
  bay: BayMap,
  rowNo: number,
  tier: number,
  column: number,
  sizeFeet: number,
  origin: Origin | null,
): MoveCell[] {
  const row = bay.rows.find((candidate) => candidate.rowNo === rowNo);
  if (!row || tier > row.maxTier) {
    return [];
  }
  const base = {
    key: `${bay.bayNo}-${rowNo}-${tier}`,
    bayNo: bay.bayNo,
    rowNo,
    rowId: row.rowId,
    tier,
    column,
  };
  const here = origin?.bayNo === bay.bayNo && origin.rowNo === rowNo && origin.tier === tier;
  if (here) {
    return [{ ...base, state: 'blocked', container: null, reason: BLOCKED_HERE }];
  }
  const verdict =
    sizeFeet === 20
      ? verdictForTwenty(bays, bay, rowNo, tier)
      : verdictForForty(bays, bay, rowNo, tier);
  return [{ ...base, container: verdict.container, ...verdict.place }];
}

interface Verdict {
  container: MapContainer | null;
  place: { state: MoveCellState; reason: string | null };
}

/** The container that covers `bayNo` on the floor at this row and tier, if any (a 40ft one covers three bays). */
function coveringAt(
  bays: BayMap[],
  rowNo: number,
  tier: number,
  bayNo: number,
): MapContainer | null {
  for (const bay of bays) {
    const row = bay.rows.find((candidate) => candidate.rowNo === rowNo);
    const found = row?.tiers.find((t) => t.tier === tier && t.fromBay <= bayNo && bayNo <= t.toBay);
    if (found) {
      return found.container;
    }
  }
  return null;
}

/** A 20ft container takes a place of its own bay and never rests on a 40ft one, so only its own row counts below. */
function verdictForTwenty(bays: BayMap[], bay: BayMap, rowNo: number, tier: number): Verdict {
  const covering = coveringAt(bays, rowNo, tier, bay.bayNo);
  if (covering) {
    return { container: covering, place: { state: 'occupied', reason: null } };
  }
  const row = bay.rows.find((candidate) => candidate.rowNo === rowNo);
  const supported = tier === 1 || (row?.tiers.some((t) => t.tier === tier - 1) ?? false);
  return { container: null, place: placeOf(supported, BLOCKED_BELOW) };
}

/** A 40ft container takes its even bay and the odd bays beside it; it may rest on a 40ft or on two 20ft ones. */
function verdictForForty(bays: BayMap[], bay: BayMap, rowNo: number, tier: number): Verdict {
  const own = bay.rows
    .find((candidate) => candidate.rowNo === rowNo)
    ?.tiers.find((t) => t.tier === tier);
  if (own) {
    return { container: own.container, place: { state: 'occupied', reason: null } };
  }
  const before = bay.bayNo - 1;
  const after = bay.bayNo + 1;
  if (!bays.some((b) => b.bayNo === before) || !bays.some((b) => b.bayNo === after)) {
    return { container: null, place: placeOf(false, BLOCKED_NO_NEIGHBOUR) };
  }
  if (coveringAt(bays, rowNo, tier, before) || coveringAt(bays, rowNo, tier, after)) {
    return { container: null, place: placeOf(false, BLOCKED_NEIGHBOUR) };
  }
  return {
    container: null,
    place: placeOf(isFortySupported(bays, bay, rowNo, tier), BLOCKED_BELOW),
  };
}

function isFortySupported(bays: BayMap[], bay: BayMap, rowNo: number, tier: number): boolean {
  if (tier === 1) {
    return true;
  }
  const below = tier - 1;
  const onFortyBelow = bay.rows.find((r) => r.rowNo === rowNo)?.tiers.some((t) => t.tier === below);
  const onTwoTwenty =
    coveringAt(bays, rowNo, below, bay.bayNo - 1) !== null &&
    coveringAt(bays, rowNo, below, bay.bayNo + 1) !== null;
  return Boolean(onFortyBelow) || onTwoTwenty;
}

function placeOf(allowed: boolean, reason: string): Verdict['place'] {
  return allowed ? { state: 'free', reason: null } : { state: 'blocked', reason };
}
