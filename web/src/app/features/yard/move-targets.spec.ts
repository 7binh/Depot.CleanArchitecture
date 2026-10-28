import { toMoveGrid } from './move-targets';
import { BayMap, BlockMap, MapContainer, TierMap } from './yard.service';

function container(number: string, sizeFeet = 20): MapContainer {
  return {
    stayId: number,
    number,
    sizeFeet,
    operatorCode: 'CSQ',
    daysInYard: 1,
    isLongStay: false,
    isDamaged: false,
  };
}

const MOVER_20 = container('MOVER000001');
const MOVER_40 = container('MOVER000002', 40);

/** What sits on the floor of the row: `fromBay` to `toBay` are the odd bays it covers. */
function entry(tier: number, fromBay: number, toBay: number, who: MapContainer): TierMap {
  return { tier, fromBay, toBay, container: who };
}

function bay(bayNo: number, tiers: TierMap[] = []): BayMap {
  return {
    bayNo,
    sizeFeet: bayNo % 2 === 1 ? 20 : 40,
    rows: [{ rowId: `row-${bayNo}`, rowNo: 1, maxTier: 3, tiers }],
  };
}

function blockOf(...bays: BayMap[]): BlockMap {
  return { blockId: 'b1', code: 'A', name: 'Block A', isVirtual: false, bays, unplaced: [] };
}

function cellOf(map: BlockMap, tier: number, mover: MapContainer, bayNo: number) {
  const grid = toMoveGrid(map, tier, mover);
  const found = grid.rows.flatMap((row) => row.cells).find((cell) => cell.bayNo === bayNo);
  if (!found) {
    throw new Error(`No cell for bay ${bayNo}`);
  }
  return found;
}

describe('toMoveGrid for a 20ft container', () => {
  it('has one column per odd bay and offers every free place on tier 1', () => {
    const map = blockOf(bay(1), bay(2), bay(3), bay(5));
    const grid = toMoveGrid(map, 1, MOVER_20);
    expect(grid.bays.map((b) => b.bayNo)).toEqual([1, 3, 5]);
    expect(grid.rows[0].cells.map((c) => c.state)).toEqual(['free', 'free', 'free']);
  });

  it('shows the container standing in a place, and the 40ft one covering both odd bays', () => {
    const forty = container('FORTY0000001', 40);
    const map = blockOf(bay(1), bay(2, [entry(1, 1, 3, forty)]), bay(3), bay(5));
    expect(cellOf(map, 1, MOVER_20, 1)).toMatchObject({ state: 'occupied', container: forty });
    expect(cellOf(map, 1, MOVER_20, 3)).toMatchObject({ state: 'occupied', container: forty });
    expect(cellOf(map, 1, MOVER_20, 5).state).toBe('free');
  });

  it('needs a 20ft container below a higher tier, never a 40ft one', () => {
    const twenty = container('TWENTY000001');
    const forty = container('FORTY0000001', 40);
    const map = blockOf(
      bay(1, [entry(1, 1, 1, twenty)]),
      bay(2, [entry(1, 1, 3, forty)]),
      bay(3),
      bay(5),
    );
    expect(cellOf(map, 2, MOVER_20, 1).state).toBe('free');
    expect(cellOf(map, 2, MOVER_20, 3)).toMatchObject({
      state: 'blocked',
      reason: 'Tầng dưới còn trống',
    });
    expect(cellOf(map, 2, MOVER_20, 5).state).toBe('blocked');
  });

  it('does not offer the place the container stands in, and ignores the container itself', () => {
    const map = blockOf(bay(1, [entry(1, 1, 1, MOVER_20)]), bay(3));
    expect(cellOf(map, 1, MOVER_20, 1)).toMatchObject({
      state: 'blocked',
      reason: 'Vị trí hiện tại của container',
    });
    expect(cellOf(map, 2, MOVER_20, 1).state).toBe('blocked');
  });

  it('leaves out the rows too low for the tier', () => {
    const map = blockOf(bay(1), bay(3));
    expect(toMoveGrid(map, 4, MOVER_20).rows).toEqual([]);
  });
});

describe('toMoveGrid for a 40ft container', () => {
  it('has one column per even bay and offers free ones on tier 1', () => {
    const map = blockOf(bay(1), bay(2), bay(3), bay(4), bay(5));
    const grid = toMoveGrid(map, 1, MOVER_40);
    expect(grid.bays.map((b) => b.bayNo)).toEqual([2, 4]);
    expect(grid.rows[0].cells.map((c) => c.state)).toEqual(['free', 'free']);
  });

  it('is blocked where a container in a bay beside it covers the floor', () => {
    const twenty = container('TWENTY000001');
    const map = blockOf(bay(1), bay(2), bay(3, [entry(1, 3, 3, twenty)]), bay(4), bay(5));
    expect(cellOf(map, 1, MOVER_40, 2)).toMatchObject({
      state: 'blocked',
      reason: 'Container ở bay bên cạnh đang chiếm chỗ',
    });
    expect(cellOf(map, 1, MOVER_40, 4).state).toBe('blocked');
  });

  it('is blocked when an odd bay beside it does not exist', () => {
    const map = blockOf(bay(1), bay(2), bay(3), bay(4));
    expect(cellOf(map, 1, MOVER_40, 4)).toMatchObject({
      state: 'blocked',
      reason: 'Thiếu bay lẻ bên cạnh',
    });
    expect(cellOf(map, 1, MOVER_40, 2).state).toBe('free');
  });

  it('may rest on two 20ft containers or on a 40ft one, but not on one 20ft container', () => {
    const a = container('TWENTY000001');
    const b = container('TWENTY000002');
    const both = blockOf(bay(1, [entry(1, 1, 1, a)]), bay(2), bay(3, [entry(1, 3, 3, b)]));
    const one = blockOf(bay(1, [entry(1, 1, 1, a)]), bay(2), bay(3));
    expect(cellOf(both, 2, MOVER_40, 2).state).toBe('free');
    expect(cellOf(one, 2, MOVER_40, 2)).toMatchObject({
      state: 'blocked',
      reason: 'Tầng dưới còn trống',
    });
    const onForty = blockOf(
      bay(1),
      bay(2, [entry(1, 1, 3, container('FORTY0000001', 40))]),
      bay(3),
    );
    expect(cellOf(onForty, 2, MOVER_40, 2).state).toBe('free');
  });

  it('shows another 40ft container standing in the place', () => {
    const forty = container('FORTY0000001', 40);
    const map = blockOf(bay(1), bay(2, [entry(1, 1, 3, forty)]), bay(3));
    expect(cellOf(map, 1, MOVER_40, 2)).toMatchObject({ state: 'occupied', container: forty });
  });
});
