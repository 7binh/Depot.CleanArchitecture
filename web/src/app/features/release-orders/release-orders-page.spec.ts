import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { choose, type } from '../../../testing/dom';
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

function setup(orders: unknown[] = ORDERS) {
  TestBed.configureTestingModule({
    providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
  });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(ReleaseOrdersPage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  http.expectOne(`${API}/release-orders`).flush(orders);
  http.expectOne(`${API}/line-operators`).flush([{ id: 'o1', code: 'CSQ', name: 'Cosco' }]);
  http.expectOne(`${API}/customers`).flush([{ id: 'cu1', taxCode: '0123456789', name: 'Acme' }]);
  fixture.detectChanges();
  return { fixture, element };
}

function rows(element: HTMLElement): string[] {
  return [...element.querySelectorAll('tbody tr')].map((tr) =>
    (tr.textContent ?? '').replace(/\s+/g, ' ').trim(),
  );
}

describe('ReleaseOrdersPage', () => {
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

  it('finds an order by number or vessel', () => {
    const { fixture, element } = setup();
    type(fixture, 'Tìm lệnh', 'moon');
    expect(rows(element).length).toBe(1);
    type(fixture, 'Tìm lệnh', 'ro-3');
    expect(rows(element)[0]).toContain('RO-3');
  });

  it('filters by status', () => {
    const { fixture, element } = setup();
    choose(fixture, 'Trạng thái', 'Expired');
    expect(rows(element).length).toBe(1);
    expect(rows(element)[0]).toContain('RO-4');
  });

  it('says so when nothing matches the filter', () => {
    const { fixture, element } = setup();
    type(fixture, 'Tìm lệnh', 'zzz');
    expect(element.textContent).toContain('Không có lệnh giao nào khớp bộ lọc.');
  });

  it('offers to add the first order when there are none', () => {
    const { element } = setup([]);
    expect(element.textContent).toContain('Chưa có lệnh giao nào.');
  });
});
