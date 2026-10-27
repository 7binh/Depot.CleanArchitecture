import { toBlockGrid } from './block-grid';
import { BlockMap, MapContainer } from './yard.service';

function container(number: string, extra: Partial<MapContainer> = {}): MapContainer {
  return {
    stayId: number,
    number,
    sizeFeet: 20,
    operatorCode: 'CSQ',
    daysInYard: 2,
    isLongStay: false,
    isDamaged: false,
    ...extra,
  };
}

const FORTY = container('CSQU3054383', { sizeFeet: 40, isDamaged: true, isLongStay: true });
const TWENTY = container('MSCU1234565', { isLongStay: true });

const MAP: BlockMap = {
  blockId: 'b1',
  code: 'B1',
  name: 'Block 1',
  isVirtual: false,
  unplaced: [],
  bays: [
    {
      bayNo: 1,
      sizeFeet: 20,
      rows: [
        { rowNo: 1, maxTier: 2, tiers: [{ tier: 2, fromBay: 1, toBay: 1, container: TWENTY }] },
      ],
    },
    {
      bayNo: 2,
      sizeFeet: 40,
      rows: [
        { rowNo: 1, maxTier: 2, tiers: [{ tier: 1, fromBay: 1, toBay: 3, container: FORTY }] },
      ],
    },
    { bayNo: 3, sizeFeet: 20, rows: [{ rowNo: 1, maxTier: 2, tiers: [] }] },
  ],
};

describe('toBlockGrid', () => {
  it('gives each existing bay one column', () => {
    const grid = toBlockGrid(MAP);
    expect(grid.bays.map((b) => [b.bayNo, b.column])).toEqual([
      [1, 1],
      [2, 2],
      [3, 3],
    ]);
  });

  it('spreads a 40ft container over the bays it covers, with the top tier on the top line', () => {
    const [row] = toBlockGrid(MAP).rows;
    const forty = row.slots.find((s) => s.container === FORTY);
    const twenty = row.slots.find((s) => s.container === TWENTY);
    expect([forty?.line, forty?.column, forty?.span]).toEqual([2, 1, 3]);
    expect([twenty?.line, twenty?.column, twenty?.span]).toEqual([1, 1, 1]);
  });

  it('draws free places only where no container stands or covers', () => {
    const [row] = toBlockGrid(MAP).rows;
    const free = row.slots.filter((s) => !s.container).map((s) => [s.line, s.column]);
    expect(free).toEqual([
      [1, 2],
      [1, 3],
    ]);
  });

  it('flags damage before long stay so a cell has one chip colour', () => {
    const [row] = toBlockGrid(MAP).rows;
    const flagOfBox = (box: MapContainer) =>
      row.slots.find((s) => s.container === box)?.flag?.label;
    expect(flagOfBox(FORTY)).toBe('Hư hỏng');
    expect(flagOfBox(TWENTY)).toBe('Tồn lâu');
  });

  it('keeps the unplaced containers of a virtual block', () => {
    const grid = toBlockGrid({ ...MAP, isVirtual: true, bays: [], unplaced: [TWENTY] });
    expect(grid.rows).toEqual([]);
    expect(grid.unplaced).toEqual([TWENTY]);
  });
});
