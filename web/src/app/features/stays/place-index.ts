import { Observable, forkJoin, map, of, switchMap } from 'rxjs';
import { Block, YardService } from '../yard/yard.service';
import { StayRecord } from './stay.service';

export interface RowPlace {
  bayNo: number;
  rowNo: number;
}

/** What is needed to name a stay's place: its block, and the bay and row numbers of its row. */
export interface PlaceIndex {
  blocks: Map<string, Block>;
  rows: Map<string, RowPlace>;
}

function rowsOfBlock(yard: YardService, blockId: string): Observable<[string, RowPlace][]> {
  return yard.bays(blockId).pipe(
    switchMap((bays) =>
      bays.length === 0
        ? of([])
        : forkJoin(
            bays.map((bay) =>
              yard
                .rows(blockId, bay.id)
                .pipe(
                  map((rows) =>
                    rows.map((row): [string, RowPlace] => [
                      row.id,
                      { bayNo: bay.bayNo, rowNo: row.rowNo },
                    ]),
                  ),
                ),
            ),
          ),
    ),
    map((perBay) => perBay.flat()),
  );
}

/** Looks up the blocks, then the bays and rows only of the real blocks the stays used. */
export function loadPlaceIndex(yard: YardService, stays: StayRecord[]): Observable<PlaceIndex> {
  return yard.blocks().pipe(
    switchMap((blocks) => {
      const byId = new Map(blocks.map((b) => [b.id, b]));
      const wanted = new Set(stays.filter((s) => s.rowId).map((s) => s.blockId));
      const real = [...wanted].filter((id) => byId.get(id)?.isVirtual === false);
      if (real.length === 0) {
        return of({ blocks: byId, rows: new Map<string, RowPlace>() });
      }
      return forkJoin(real.map((id) => rowsOfBlock(yard, id))).pipe(
        map((perBlock) => ({ blocks: byId, rows: new Map(perBlock.flat()) })),
      );
    }),
  );
}
