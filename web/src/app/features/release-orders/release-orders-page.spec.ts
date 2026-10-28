import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { choose, click, type } from '../../../testing/dom';
import { ReleaseOrdersPage } from './release-orders-page';

const API = 'http://localhost:5049/api';
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
  items: [
    { containerTypeId: 't1', sizeFeet: 20, quantity: 2, delivered: 0, remaining: 2 },
    { containerTypeId: 't1', sizeFeet: 40, quantity: 3, delivered: 0, remaining: 3 },
  ],
  ...patch,
});
const ORDERS = [
  order('r1', 'RO-1'),
  order('r2', 'RO-2', { status: 'Delivering', exportVessel: 'MV Moon' }),
  order('r3', 'RO-3', { status: 'Delivered' }),
  order('r4', 'RO-4', { isExpired: true }),
];

function paged(items: unknown[], totalCount = items.length, page = 1) {
  return { items, totalCount, page, pageSize: 20, totalPages: Math.ceil(totalCount / 20) };
}

function searchRequest(http: HttpTestingController) {
  return http.expectOne((r) => r.url === `${API}/release-orders/search`);
}

function setup(orders: unknown[] = ORDERS, totalCount = orders.length) {
  TestBed.configureTestingModule({
    providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
  });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(ReleaseOrdersPage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  searchRequest(http).flush(paged(orders, totalCount));
  http.expectOne(`${API}/line-operators`).flush([{ id: 'o1', code: 'CSQ', name: 'Cosco' }]);
  http.expectOne(`${API}/customers`).flush([{ id: 'cu1', taxCode: '0123456789', name: 'Acme' }]);
  fixture.detectChanges();
  return { http, fixture, element };
}

function rows(element: HTMLElement): string[] {
  return [...element.querySelectorAll('tbody tr')].map((tr) =>
    (tr.textContent ?? '').replace(/\s+/g, ' ').trim(),
  );
}

describe('ReleaseOrdersPage list', () => {
  it('lists the orders with operator, customer, progress and status', () => {
    const { element } = setup();
    const first = rows(element)[0];
    for (const part of ['RO-1', 'MV Sun', 'CSQ', 'Acme', '31/12/2026', '01/10/2026', '0/5']) {
      expect(first).toContain(part);
    }
    expect(first).toContain('Chưa giao');
    expect(rows(element)[1]).toContain('Đang giao');
    expect(rows(element)[2]).toContain('Đã giao đủ');
  });

  it('shows an expired order that still had room as expired', () => {
    const { element } = setup();
    expect(rows(element)[3]).toContain('Quá hạn');
  });

  it('keeps a fully delivered order delivered after its expiry date', () => {
    const { element } = setup([order('r5', 'RO-5', { status: 'Delivered', isExpired: true })]);
    expect(rows(element)[0]).toContain('Đã giao đủ');
  });

  it('offers to add the first order when there are none', () => {
    const { element } = setup([]);
    expect(element.textContent).toContain('Chưa có lệnh giao nào.');
  });

  it('asks for the first page of 20 with no filter', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    const http = TestBed.inject(HttpTestingController);
    TestBed.createComponent(ReleaseOrdersPage).detectChanges();
    const { params } = searchRequest(http).request;
    expect(params.keys().sort()).toEqual(['page', 'pageSize']);
    expect(params.get('pageSize')).toBe('20');
  });
});

describe('ReleaseOrdersPage filters', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('sends the typed order number once typing pauses', () => {
    const { http, fixture } = setup();
    type(fixture, 'Tìm lệnh', 'ro-3');
    http.expectNone((r) => r.url === `${API}/release-orders/search`);
    vi.advanceTimersByTime(300);
    expect(searchRequest(http).request.params.get('orderNo')).toBe('ro-3');
  });

  it('sends the chosen status and shows the orders that come back', () => {
    const { http, fixture, element } = setup();
    choose(fixture, 'Trạng thái', 'Expired');
    vi.advanceTimersByTime(300);
    const request = searchRequest(http);
    expect(request.request.params.get('status')).toBe('Expired');
    request.flush(paged([ORDERS[3]]));
    fixture.detectChanges();
    expect(rows(element).length).toBe(1);
    expect(rows(element)[0]).toContain('RO-4');
  });

  it('says so when nothing matches the filter', () => {
    const { http, fixture, element } = setup();
    type(fixture, 'Tìm lệnh', 'zzz');
    vi.advanceTimersByTime(300);
    searchRequest(http).flush(paged([]));
    fixture.detectChanges();
    expect(element.textContent).toContain('Không có lệnh giao nào khớp bộ lọc.');
  });
});

describe('ReleaseOrdersPage paging', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('shows the range and the page count', () => {
    const { element } = setup(ORDERS, 45);
    expect(element.querySelector('nav')?.textContent).toContain('1–20 / 45');
    expect(element.querySelector('nav')?.textContent).toContain('1 / 3');
  });

  it('asks for the next page when Sau is clicked', () => {
    const { http, fixture, element } = setup(ORDERS, 45);
    click(fixture, 'Sau');
    const request = searchRequest(http);
    expect(request.request.params.get('page')).toBe('2');
    request.flush(paged(ORDERS, 45, 2));
    fixture.detectChanges();
    expect(element.querySelector('nav')?.textContent).toContain('21–40 / 45');
  });

  it('goes back to page 1 when a filter changes', () => {
    const { http, fixture } = setup(ORDERS, 45);
    click(fixture, 'Sau');
    searchRequest(http).flush(paged(ORDERS, 45, 2));
    fixture.detectChanges();

    choose(fixture, 'Trạng thái', 'Delivered');
    vi.advanceTimersByTime(300);
    expect(searchRequest(http).request.params.get('page')).toBe('1');
  });
});
