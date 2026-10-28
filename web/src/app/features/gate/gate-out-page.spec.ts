import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { click, pick, search, type } from '../../../testing/dom';
import { GateOutPage } from './gate-out-page';

const API = 'http://localhost:5049/api';
const container = (id: string, number: string, lineOperatorId = 'o1') => ({
  id,
  number,
  lineOperatorId,
  lineOperatorCode: lineOperatorId === 'o1' ? 'CSQ' : 'MSK',
  containerTypeId: 't1',
  containerTypeCode: '20GP',
  isoCode: '22G1',
  sizeFeet: 20,
  maxWeightKg: 30480,
  tareWeightKg: 2200,
  manufactureDate: '2024-03-09',
});
const order = (id: string, orderNo: string, patch: object = {}) => ({
  id,
  lineOperatorId: 'o1',
  customerId: 'cu1',
  orderNo,
  issuedDate: '2026-10-01',
  expiryDate: '2026-12-31',
  exportVessel: 'MV Sun',
  isExpired: false,
  status: 'NotDelivered',
  items: [{ containerTypeId: 't1', sizeFeet: 20, quantity: 2, delivered: 0, remaining: 2 }],
  ...patch,
});
/** What the server sends for openOnly: orders still valid and with room. */
const OPEN_ORDERS = [order('r1', 'RO-1'), order('r2', 'RO-2', { lineOperatorId: 'o2' })];
const C1 = container('c1', 'CSQU3054383');

function paged<T>(items: T[]) {
  return { items, totalCount: items.length, page: 1, pageSize: 100, totalPages: 1 };
}

function providers(query: Record<string, string> = {}) {
  return [
    provideHttpClient(),
    provideHttpClientTesting(),
    {
      provide: ActivatedRoute,
      useValue: { snapshot: { queryParamMap: convertToParamMap(query) } },
    },
  ];
}

function setup(query: Record<string, string> = {}) {
  TestBed.configureTestingModule({ providers: providers(query) });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(GateOutPage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  http.expectOne((r) => r.url === `${API}/release-orders/search`).flush(paged(OPEN_ORDERS));
  http.expectOne(`${API}/line-operators`).flush([{ id: 'o1', code: 'CSQ', name: 'Cosco' }]);
  http.expectOne(`${API}/container-types`).flush([{ id: 't1', code: '20GP', name: '20 feet' }]);
  fixture.detectChanges();
  return { http, fixture, element };
}

function containerSearch(page: ReturnType<typeof setup>) {
  return page.http.expectOne((r) => r.url === `${API}/containers/search`);
}

/** Picks an order, which makes the page ask for the containers in the yard of its operator. */
function chooseOrder(page: ReturnType<typeof setup>, orderNo: string, containers: object[] = [C1]) {
  pick(page.fixture, 'Lệnh giao', orderNo, orderNo);
  const request = containerSearch(page);
  request.flush(paged(containers));
  page.fixture.detectChanges();
  return request.request;
}

function fill(page: ReturnType<typeof setup>): void {
  chooseOrder(page, 'RO-1');
  pick(page.fixture, 'Container trong bãi', '3054', 'CSQU3054383');
  type(page.fixture, 'Biển số', '51C-123.45');
  click(page.fixture, 'Xác nhận Gate Out');
}

beforeEach(() => vi.useFakeTimers());
afterEach(() => vi.useRealTimers());

describe('GateOutPage load', () => {
  it('asks for one page of open orders and shows the form at once', () => {
    const { http, element } = setup();
    expect(element.querySelector('form')).not.toBeNull();
    http.expectNone((r) => r.url === `${API}/containers/search`);
    http.expectNone(`${API}/containers`);
    http.expectNone((r) => r.url === `${API}/container-stays`);
  });

  it('asks only for orders that can still deliver', () => {
    TestBed.configureTestingModule({ providers: providers() });
    const http = TestBed.inject(HttpTestingController);
    TestBed.createComponent(GateOutPage).detectChanges();
    const { params } = http.expectOne((r) => r.url === `${API}/release-orders/search`).request;
    expect(params.get('openOnly')).toBe('true');
    expect(params.get('pageSize')).toBe('100');
  });
});

describe('GateOutPage order from the address', () => {
  function orderInput(element: HTMLElement): HTMLInputElement {
    return element.querySelectorAll('app-search-select input')[0] as HTMLInputElement;
  }

  it('chooses the order named in the address and asks for its containers', () => {
    const page = setup({ order: 'r1' });
    page.fixture.detectChanges();
    const request = containerSearch(page).request;
    expect(request.params.get('lineOperatorId')).toBe('o1');
    page.fixture.detectChanges();
    expect(orderInput(page.element).value).toBe('RO-1');
    expect(page.element.textContent).toContain('hãng CSQ, tàu MV Sun');
  });

  it('leaves the order empty when it is not among the open ones', () => {
    const page = setup({ order: 'gone' });
    page.fixture.detectChanges();
    page.http.expectNone((r) => r.url === `${API}/containers/search`);
    expect(orderInput(page.element).value).toBe('');
  });

  it('chooses nothing when the address names no order', () => {
    const page = setup();
    page.fixture.detectChanges();
    expect(orderInput(page.element).value).toBe('');
  });
});

describe('GateOutPage choices', () => {
  it('suggests the open orders', () => {
    const { fixture } = setup();
    expect(search(fixture, 'Lệnh giao', 'RO')).toEqual(['RO-1 MV Sun', 'RO-2 MV Sun']);
  });

  it('finds an order by vessel name', () => {
    const { fixture } = setup();
    expect(search(fixture, 'Lệnh giao', 'sun')).toEqual(['RO-1 MV Sun', 'RO-2 MV Sun']);
  });

  it('asks for an order first', () => {
    const { fixture, element } = setup();
    search(fixture, 'Container trong bãi', '3054');
    expect(element.textContent).toContain('Chọn lệnh giao trước.');
  });

  it('asks for the containers in the yard of the order operator', () => {
    const page = setup();
    const request = chooseOrder(page, 'RO-1');
    expect(request.params.get('lineOperatorId')).toBe('o1');
    expect(request.params.get('inYard')).toBe('true');
    expect(page.element.textContent).toContain('hãng CSQ, tàu MV Sun');
  });

  it('suggests only the containers that fit a line of the order', () => {
    const page = setup();
    const otherSize = { ...container('c3', 'CSQU3054999'), sizeFeet: 40 };
    chooseOrder(page, 'RO-1', [C1, otherSize]);
    expect(search(page.fixture, 'Container trong bãi', 'CSQU')).toEqual([
      'CSQU3054383 CSQ · 20GP · 20 ft',
    ]);
  });

  it('says so when no container in the yard fits', () => {
    const page = setup();
    chooseOrder(page, 'RO-1', []);
    expect(search(page.fixture, 'Container trong bãi', 'MSKU')).toEqual([]);
    expect(page.element.textContent).toContain('Không có container nào trong bãi khớp lệnh này.');
  });

  it('asks the server again once typing pauses', () => {
    const page = setup();
    chooseOrder(page, 'RO-1');
    search(page.fixture, 'Container trong bãi', '3054');
    page.http.expectNone((r) => r.url === `${API}/containers/search`);
    vi.advanceTimersByTime(300);
    expect(containerSearch(page).request.params.get('number')).toBe('3054');
  });

  it('clears the container when the order changes', () => {
    const page = setup();
    chooseOrder(page, 'RO-1');
    pick(page.fixture, 'Container trong bãi', '3054', 'CSQU3054383');
    chooseOrder(page, 'RO-2', []);
    const input = page.element.querySelectorAll('app-search-select input')[1] as HTMLInputElement;
    expect(input.value).toBe('');
  });
});

describe('GateOutPage container profile', () => {
  it('accepts a full number typed without picking and shows its profile', () => {
    const page = setup();
    chooseOrder(page, 'RO-1');
    search(page.fixture, 'Container trong bãi', 'csqu3054383');
    const input = page.element.querySelectorAll('app-search-select input')[1] as HTMLInputElement;
    expect(input.value).toBe('CSQU3054383');
    expect(page.element.textContent).toContain('hãng CSQ, loại 20GP, cỡ 20 ft, ISO 22G1');
  });

  it('picks the container when the server answers with exactly the number typed', () => {
    const page = setup();
    chooseOrder(page, 'RO-1', []);
    search(page.fixture, 'Container trong bãi', 'CSQU3054383');
    vi.advanceTimersByTime(300);
    containerSearch(page).flush(paged([C1]));
    page.fixture.detectChanges();
    expect(page.element.textContent).toContain('hãng CSQ, loại 20GP, cỡ 20 ft, ISO 22G1');
  });
});

describe('GateOutPage submit', () => {
  it('sends the gate out and says the container left', () => {
    const page = setup();
    fill(page);
    const request = page.http.expectOne(`${API}/gate-out`);
    expect(request.request.body).toMatchObject({
      containerId: 'c1',
      releaseOrderId: 'r1',
      outVehicleType: 'Truck',
      outVehicleNumber: '51C-123.45',
    });
    request.flush({ id: 's1', containerId: 'c1' });
    page.fixture.detectChanges();
    expect(page.element.textContent).toContain('Đã gate out container CSQU3054383');
    page.http.expectOne((r) => r.url === `${API}/release-orders/search`);
  });

  it('does not call the API while the form is invalid', () => {
    const { http, fixture, element } = setup();
    click(fixture, 'Xác nhận Gate Out');
    http.expectNone(`${API}/gate-out`);
    expect(element.textContent).toContain('Chọn container đang trong bãi.');
  });

  it('shows a full line under the order field', () => {
    const page = setup();
    fill(page);
    page.http
      .expectOne(`${API}/gate-out`)
      .flush({ title: 'ReleaseOrder.LineFull' }, { status: 409, statusText: 'Conflict' });
    page.fixture.detectChanges();
    expect(page.element.textContent).toContain('đã giao đủ số lượng');
  });

  it('shows a container still under another under the container field', () => {
    const page = setup();
    fill(page);
    page.http
      .expectOne(`${API}/gate-out`)
      .flush({ title: 'Stay.HasContainerAbove' }, { status: 409, statusText: 'Conflict' });
    page.fixture.detectChanges();
    expect(page.element.textContent).toContain('Còn container xếp phía trên');
  });
});
