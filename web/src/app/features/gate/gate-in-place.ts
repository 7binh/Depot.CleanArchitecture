import { ParamMap } from '@angular/router';

/** A yard place handed over by the map: the block by id, the rest by the numbers the map shows. */
export interface PlaceToFill {
  blockId: string;
  bayNo: number;
  rowNo: number;
  tier: number;
}

function positive(value: string | null): number | null {
  const number = Number(value);
  return value !== null && Number.isInteger(number) && number > 0 ? number : null;
}

/** Reads `?block=&bay=&row=&tier=`; null when any part is missing or not a whole number. */
export function readPlace(params: ParamMap): PlaceToFill | null {
  const blockId = params.get('block');
  const bayNo = positive(params.get('bay'));
  const rowNo = positive(params.get('row'));
  const tier = positive(params.get('tier'));
  if (!blockId || bayNo === null || rowNo === null || tier === null) {
    return null;
  }
  return { blockId, bayNo, rowNo, tier };
}
