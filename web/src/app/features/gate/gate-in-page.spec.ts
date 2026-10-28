import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { choose, click, type } from '../../../testing/dom';
import { stubDialog } from '../../../testing/dialog';
import { GateInPage } from './gate-in-page';

const API = 'http://localhost:5049/api';
const KNOWN = {
  id: 'c1',
  number: 'CSQU3054383',
  lineOperatorId: 'o1',
  lineOperatorCode: 'CSQ',
  containerTypeId: 't1',
  containerTypeCode: '20GP',
  isoCode: '22G1',
  sizeFeet: 20,
  maxWeightKg: 30480,
  tareWeightKg: 2200,
  manufactureDate: '2024-03-09',
};
const BLOCKS = [
  { id: 'b1', code: 'A', name: 'Block A', isVirtual: false },
  { id: 'b2', code: 'V', name: 'Bãi ảo', isVirtual: true },
];
const BAYS = [
  { id: 'y1', blockId: 'b1', bayNo: 1 },
  { id: 'y2', blockId: 'b1', bayNo: 2 },
  { id: 'y3', blockId: 'b1', bayNo: 3 },
];
const ROWS = [{ id: 'r1', bayId: 'y1', rowNo: 1, maxTier: 3 }];

function setup(query: Record<string, string> = {}) {
  stubDialog();
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      {
        provide: ActivatedRoute,
        useValue: { snapshot: { queryParamMap: convertToParamMap(query) } },
      },
    ],
  });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(GateInPage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  http.expectOne(`${API}/blocks`).flush(BLOCKS);
  fixture.detectChanges();
  return { http, fixture, element };
}

function lookupRequest(http: HttpTestingController) {
  return http.expectOne((r) => r.url === `${API}/containers/search`);
}

/** Types a number, waits for the pause, and answers the server's lookup with `found`. */
function lookUp(page: ReturnType<typeof setup>, number: string, found: object[] = [KNOWN]): void {
  type(page.fixture, 'Số container', number);
  vi.advanceTimersByTime(300);
  const request = lookupRequest(page.http);
  expect(request.request.params.get('number')).toBe(number.replace(/\s/g, '').toUpperCase());
  request.flush({ items: found, totalCount: found.length, page: 1, pageSize: 5, totalPages: 1 });
  page.fixture.detectChanges();
}

function optionValues(element: HTMLElement, label: string): string[] {
  const field = [...element.querySelectorAll('app-select-field')].find((f) =>
    f.textContent?.includes(label),
  );
  return [...(field?.querySelectorAll('option') ?? [])].map((o) => o.value).filter(Boolean);
}

function pickRealPosition(page: ReturnType<typeof setup>): void {
  choose(page.fixture, 'Block', 'b1');
  page.http.expectOne(`${API}/blocks/b1/bays`).flush(BAYS);
  page.fixture.detectChanges();
  choose(page.fixture, 'Bay', 'y1');
  page.http.expectOne(`${API}/blocks/b1/bays/y1/rows`).flush(ROWS);
  page.fixture.detectChanges();
  choose(page.fixture, 'Row', 'r1');
  choose(page.fixture, 'Tầng', '2');
}

function fillArrival(page: ReturnType<typeof setup>): void {
  lookUp(page, 'csqu3054383');
  type(page.fixture, 'Biển số', '51C-123.45');
}

beforeEach(() => vi.useFakeTimers());
afterEach(() => vi.useRealTimers());

describe('GateInPage load', () => {
  it('shows the form after the blocks only, without loading any container', () => {
    const { http, element } = setup();
    expect(element.querySelector('form')).not.toBeNull();
    http.expectNone((r) => r.url === `${API}/containers/search`);
    http.expectNone(`${API}/containers`);
    http.expectNone(`${API}/line-operators`);
    http.expectNone(`${API}/container-types`);
  });
});

describe('GateInPage lookup', () => {
  it('shows the profile of a known container for the clerk to check', () => {
    const page = setup();
    lookUp(page, 'csqu3054383');
    expect(page.element.textContent).toContain('hãng CSQ, loại 20GP, cỡ 20 ft, ISO 22G1');
  });

  it('waits for a pause in typing before it asks the server', () => {
    const { http, fixture } = setup();
    type(fixture, 'Số container', 'CSQU');
    http.expectNone((r) => r.url === `${API}/containers/search`);
    vi.advanceTimersByTime(299);
    http.expectNone((r) => r.url === `${API}/containers/search`);
    vi.advanceTimersByTime(1);
    expect(lookupRequest(http).request.params.get('number')).toBe('CSQU');
  });

  it('does not say there is no profile while the lookup is still running', () => {
    const { fixture, element } = setup();
    type(fixture, 'Số container', 'MSKU0000000');
    expect(element.textContent).not.toContain('Chưa có hồ sơ cho số container này.');
    expect(element.textContent).toContain('Đang kiểm tra số container.');
  });

  it('offers to declare a container that has no profile', () => {
    const page = setup();
    lookUp(page, 'MSKU0000000', []);
    expect(page.element.textContent).toContain('Chưa có hồ sơ cho số container này.');
    click(page.fixture, 'Khai báo container');
    expect(page.element.querySelector('dialog')).not.toBeNull();
  });

  it('does not take a longer number that only contains the one typed', () => {
    const page = setup();
    lookUp(page, 'CSQU305438', [KNOWN]);
    expect(page.element.textContent).toContain('Chưa có hồ sơ cho số container này.');
  });

  it('does not call the API for a number whose lookup has not finished', () => {
    const page = setup();
    type(page.fixture, 'Số container', 'CSQU3054383');
    type(page.fixture, 'Biển số', '51C-123.45');
    choose(page.fixture, 'Block', 'b2');
    click(page.fixture, 'Xác nhận Gate In');
    page.http.expectNone(`${API}/gate-in`);
    expect(page.element.textContent).toContain('Đang kiểm tra số container.');
  });
});

describe('GateInPage form', () => {
  it('asks for a damage description only when the container is damaged', () => {
    const { fixture, element } = setup();
    expect(element.textContent).not.toContain('Mô tả hư hỏng');
    choose(fixture, 'Tình trạng', 'Damaged');
    expect(element.textContent).toContain('Mô tả hư hỏng');
  });

  it('lists only the bays that fit the container size', () => {
    const page = setup();
    lookUp(page, 'CSQU3054383');
    choose(page.fixture, 'Block', 'b1');
    page.http.expectOne(`${API}/blocks/b1/bays`).flush(BAYS);
    page.fixture.detectChanges();
    expect(optionValues(page.element, 'Bay')).toEqual(['y1', 'y3']);
  });

  it('hides bay, row and tier for a virtual block', () => {
    const { fixture, element } = setup();
    choose(fixture, 'Block', 'b2');
    expect(element.textContent).not.toContain('Chọn bay');
  });

  it('does not call the API while the form is invalid', () => {
    const { http, fixture, element } = setup();
    click(fixture, 'Xác nhận Gate In');
    http.expectNone(`${API}/gate-in`);
    expect(element.textContent).toContain('Nhập số container.');
  });
});

describe('GateInPage place from the yard map', () => {
  const place = { block: 'b1', bay: '3', row: '1', tier: '2' };

  const rows = [{ ...ROWS[0], bayId: 'y3' }];

  it('fills block, bay, row and tier from the place', () => {
    const { http, fixture, element } = setup(place);
    http.expectOne(`${API}/blocks/b1/bays`).flush(BAYS);
    http.expectOne(`${API}/blocks/b1/bays/y3/rows`).flush(rows);
    http.expectOne(`${API}/blocks/b1/bays`).flush(BAYS);
    http.expectOne(`${API}/blocks/b1/bays/y3/rows`).flush(rows);
    fixture.detectChanges();
    expectSelected(element, 'Block', 'b1');
    expectSelected(element, 'Bay', 'y3');
    expectSelected(element, 'Row', 'r1');
    expectSelected(element, 'Tầng', '2');
  });

  it('leaves the position empty when the place has no matching bay', () => {
    const { http, fixture, element } = setup({ ...place, bay: '9' });
    http.expectOne(`${API}/blocks/b1/bays`).flush(BAYS);
    fixture.detectChanges();
    expectSelected(element, 'Block', '');
  });
});

function expectSelected(element: HTMLElement, label: string, value: string): void {
  const field = [...element.querySelectorAll('app-select-field')].find((f) =>
    f.textContent?.includes(label),
  );
  expect(field?.querySelector('select')?.value).toBe(value);
}

describe('GateInPage submit', () => {
  it('sends the position of a real block and says the container is in', () => {
    const page = setup();
    fillArrival(page);
    pickRealPosition(page);
    click(page.fixture, 'Xác nhận Gate In');
    const request = page.http.expectOne(`${API}/gate-in`);
    expect(request.request.body).toMatchObject({
      containerId: 'c1',
      grade: 'A',
      condition: 'Normal',
      damageNote: null,
      inVehicleType: 'Truck',
      inVehicleNumber: '51C-123.45',
      blockId: 'b1',
      rowId: 'r1',
      tier: 2,
    });
    request.flush({ id: 's1', containerId: 'c1' });
    page.fixture.detectChanges();
    expect(page.element.textContent).toContain('Đã gate in container CSQU3054383');
  });

  it('sends no row or tier for a virtual block', () => {
    const page = setup();
    fillArrival(page);
    choose(page.fixture, 'Block', 'b2');
    click(page.fixture, 'Xác nhận Gate In');
    const request = page.http.expectOne(`${API}/gate-in`);
    expect(request.request.body).toMatchObject({ blockId: 'b2', rowId: null, tier: null });
  });

  it('shows a position conflict under the tier field', () => {
    const page = setup();
    fillArrival(page);
    pickRealPosition(page);
    click(page.fixture, 'Xác nhận Gate In');
    page.http
      .expectOne(`${API}/gate-in`)
      .flush({ title: 'Position.Occupied' }, { status: 409, statusText: 'Conflict' });
    page.fixture.detectChanges();
    expect(page.element.textContent).toContain('Vị trí này đã có container.');
  });

  it('shows a container that is already in the yard under the number field', () => {
    const page = setup();
    fillArrival(page);
    choose(page.fixture, 'Block', 'b2');
    click(page.fixture, 'Xác nhận Gate In');
    page.http
      .expectOne(`${API}/gate-in`)
      .flush({ title: 'Container.AlreadyInYard' }, { status: 409, statusText: 'Conflict' });
    page.fixture.detectChanges();
    expect(page.element.textContent).toContain('đang ở trong bãi');
  });
});
