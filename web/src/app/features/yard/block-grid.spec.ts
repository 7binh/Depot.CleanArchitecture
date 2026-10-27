import { tierCount, toTierGrid } from './block-grid';
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

/** Bays 1, 2, 3 with one row of two tiers: a 20ft on tier 2 of bay 1, a 40ft on tier 1 across bays 1 to 3. */
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

function cells(tier: number) {
  return toTierGrid(MAP, tier).rows[0].cells;
}

describe('toTierGrid', () => {
  it('has one column per odd bay and none for even bays', () => {
    const { bays } = toTierGrid(MAP, 1);
    expect(bays.map((b) => [b.bayNo, b.column])).toEqual([
      [1, 1],
      [3, 2],
    ]);
  });

  it('draws a 40ft container as one box across two odd bays', () => {
    const [forty, ...rest] = cells(1);
    expect([forty.container, forty.column, forty.span]).toEqual([FORTY, 1, 2]);
    expect(rest).toEqual([]);
  });

  it('draws a free place for every odd bay that has no container on the chosen tier', () => {
    const [twenty, free] = cells(2);
    expect([twenty.container, twenty.column, twenty.span]).toEqual([TWENTY, 1, 1]);
    expect([free.container, free.column, free.span]).toEqual([null, 2, 1]);
  });

  it('names the bay, row and tier of every box, a 40ft one by its own even bay', () => {
    const [forty] = cells(1);
    const [twenty, free] = cells(2);
    expect([forty.bayNo, forty.rowNo, forty.tier]).toEqual([2, 1, 1]);
    expect([twenty.bayNo, twenty.rowNo, twenty.tier]).toEqual([1, 1, 2]);
    expect([free.bayNo, free.rowNo, free.tier]).toEqual([3, 1, 2]);
  });

  it('tells when a container stands above or below a place', () => {
    const [forty] = cells(1);
    const [twenty, free] = cells(2);
    expect([forty.above, forty.below]).toEqual([1, 0]);
    expect([twenty.above, twenty.below]).toEqual([0, 1]);
    expect([free.above, free.below]).toEqual([0, 1]);
  });

  it('counts a container once even when it covers two bays', () => {
    const [free] = cells(2).filter((cell) => !cell.container);
    const [forty] = cells(1);
    expect(free.below).toBe(1);
    expect(forty.span).toBe(2);
  });

  it('draws no place above the height a row allows', () => {
    expect(cells(3)).toEqual([]);
  });

  it('flags damage before long stay so a cell has one chip colour', () => {
    expect(cells(1)[0].flag?.label).toBe('Hư hỏng');
    expect(cells(2)[0].flag?.label).toBe('Tồn lâu');
  });

  it('offers as many tiers as the tallest row allows', () => {
    expect(tierCount(MAP)).toBe(2);
    expect(tierCount({ ...MAP, bays: [] })).toBe(0);
  });
});
