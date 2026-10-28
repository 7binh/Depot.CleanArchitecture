import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { stubDialog } from '../../../testing/dialog';
import { YardMapPage } from './yard-map-page';

const API = 'http://localhost:5049/api';
const BLOCKS = [{ id: 'b1', code: 'B1', name: 'Block 1', isVirtual: false }];
const BOX = {
  stayId: 's20',
  number: 'CSQU3054383',
  sizeFeet: 20,
  operatorCode: 'CSQ',
  daysInYard: 3,
  isLongStay: false,
  isDamaged: false,
};
const OTHER = { ...BOX, stayId: 's21', number: 'MSKU0000000' };

/** Bay 1 holds the container (tier 1), bay 3 is empty, bay 5 holds another one with a third on top of it. */
const MAP = {
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
        {
          rowId: 'row-1',
          rowNo: 1,
          maxTier: 2,
          tiers: [{ tier: 1, fromBay: 1, toBay: 1, container: BOX }],
        },
      ],
    },
    { bayNo: 3, sizeFeet: 20, rows: [{ rowId: 'row-3', rowNo: 1, maxTier: 2, tiers: [] }] },
    {
      bayNo: 5,
      sizeFeet: 20,
      rows: [
        {
          rowId: 'row-5',
          rowNo: 1,
          maxTier: 2,
          tiers: [
            { tier: 1, fromBay: 5, toBay: 5, container: OTHER },
            {
              tier: 2,
              fromBay: 5,
              toBay: 5,
              container: { ...OTHER, stayId: 's22', number: 'TOP' },
            },
          ],
        },
      ],
    },
  ],
};

function mapRequest(http: HttpTestingController) {
  return http.expectOne(`${API}/blocks/b1/map`);
}

function setup() {
  stubDialog();
  TestBed.configureTestingModule({
    providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
  });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(YardMapPage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  http.expectOne(`${API}/blocks`).flush(BLOCKS);
  fixture.detectChanges();
  mapRequest(http).flush(MAP);
  fixture.detectChanges();
  return { http, fixture, element };
}

function buttonWith(element: HTMLElement, text: string): HTMLButtonElement | undefined {
  return [...element.querySelectorAll('button')].find((b) => b.textContent?.includes(text));
}

function openDialog() {
  const view = setup();
  buttonWith(view.element, 'CSQU3054383')?.click();
  view.fixture.detectChanges();
  buttonWith(view.element, 'Di chuyển')?.click();
  view.fixture.detectChanges();
  mapRequest(view.http).flush(MAP);
  view.fixture.detectChanges();
  return view;
}

describe('Moving a container from the drawer', () => {
  it('offers Di chuyển in the drawer and opens the dialog with the map of the block', () => {
    const { element } = openDialog();
    const dialog = element.querySelector('dialog') as HTMLDialogElement;
    expect(dialog.textContent).toContain('Di chuyển CSQU3054383');
    expect(dialog.textContent).toContain('B1, bay 1, row 1, tầng 1');
    expect(dialog.textContent).toContain('Chưa chọn');
  });

  it('offers only the free places that fit, and says so on the others', () => {
    const { element } = openDialog();
    const dialog = element.querySelector('dialog') as HTMLDialogElement;
    expect(dialog.querySelector('[aria-label="Chọn bay 3, row 1, tầng 1"]')).not.toBeNull();
    expect(dialog.querySelector('[aria-label="Chọn bay 1, row 1, tầng 1"]')).toBeNull();
    expect(dialog.querySelector('[aria-label="Chọn bay 5, row 1, tầng 1"]')).toBeNull();
    expect(dialog.textContent).toContain('MSKU0000000');
  });

  it('keeps Xác nhận di chuyển off until a place is chosen, then shows the new place', () => {
    const { fixture, element } = openDialog();
    const confirm = buttonWith(element, 'Xác nhận di chuyển') as HTMLButtonElement;
    expect(confirm.disabled).toBe(true);
    (element.querySelector('[aria-label="Chọn bay 3, row 1, tầng 1"]') as HTMLElement).click();
    fixture.detectChanges();
    expect(confirm.disabled).toBe(false);
    expect(element.querySelector('dialog')?.textContent).toContain('B1, bay 3, row 1, tầng 1');
  });

  it('sends the chosen place, then closes both and draws the map again', () => {
    const { http, fixture, element } = openDialog();
    (element.querySelector('[aria-label="Chọn bay 3, row 1, tầng 1"]') as HTMLElement).click();
    fixture.detectChanges();
    buttonWith(element, 'Xác nhận di chuyển')?.click();
    const put = http.expectOne(`${API}/container-stays/s20/position`);
    expect(put.request.method).toBe('PUT');
    expect(put.request.body).toEqual({ blockId: 'b1', rowId: 'row-3', tier: 1 });
    put.flush({});
    fixture.detectChanges();
    mapRequest(http).flush(MAP);
    fixture.detectChanges();
    expect(element.querySelector('dialog')).toBeNull();
    expect(element.querySelector('app-container-detail')).toBeNull();
  });

  it('keeps the dialog open and shows why when the server refuses', () => {
    const { http, fixture, element } = openDialog();
    (element.querySelector('[aria-label="Chọn bay 3, row 1, tầng 1"]') as HTMLElement).click();
    fixture.detectChanges();
    buttonWith(element, 'Xác nhận di chuyển')?.click();
    http
      .expectOne(`${API}/container-stays/s20/position`)
      .flush({ title: 'Position.Occupied' }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();
    expect(element.querySelector('dialog')?.textContent).toContain('Vị trí này đã có container.');
  });
});

describe('The drawer of a container with another one on top', () => {
  it('disables Di chuyển and says why', () => {
    const { fixture, element } = setup();
    buttonWith(element, 'MSKU0000000')?.click();
    fixture.detectChanges();
    const move = buttonWith(element, 'Di chuyển') as HTMLButtonElement;
    expect(move.disabled).toBe(true);
    expect(element.querySelector('app-container-detail')?.textContent).toContain(
      'Còn container xếp phía trên',
    );
  });
});
