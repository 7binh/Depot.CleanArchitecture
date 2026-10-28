import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, provideRouter } from '@angular/router';
import { ReleaseOrderPage } from './release-order-page';

const API = 'http://localhost:5049/api';
const ORDER = {
  id: 'r1',
  lineOperatorId: 'o1',
  customerId: 'cu1',
  orderNo: 'RO-1',
  issuedDate: '2026-10-01',
  expiryDate: '2026-12-31',
  exportVessel: 'MV Sun',
  isExpired: false,
  status: 'Delivering',
  items: [
    { containerTypeId: 't1', sizeFeet: 20, quantity: 4, delivered: 1, remaining: 3 },
    { containerTypeId: 't1', sizeFeet: 40, quantity: 2, delivered: 2, remaining: 0 },
  ],
};

const DELIVERIES = [
  {
    stayId: 's2',
    containerId: 'c2',
    containerNumber: 'MSKU0000000',
    containerTypeId: 't1',
    sizeFeet: 40,
    gateOutAt: '2026-10-20T08:30:00+07:00',
    outVehicleType: 'Barge',
    outVehicleNumber: 'SL-01',
  },
  {
    stayId: 's1',
    containerId: 'c1',
    containerNumber: 'CSQU3054383',
    containerTypeId: 't1',
    sizeFeet: 20,
    gateOutAt: '2026-10-19T14:05:00+07:00',
    outVehicleType: 'Truck',
    outVehicleNumber: '51C-123.45',
  },
];

function setup(order: object = ORDER, deliveries: object[] = DELIVERIES) {
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      provideRouter([]),
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: new Map([['id', 'r1']]) } } },
    ],
  });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(ReleaseOrderPage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  http.expectOne(`${API}/release-orders/r1`).flush(order);
  http.expectOne(`${API}/line-operators`).flush([{ id: 'o1', code: 'CSQ', name: 'Cosco' }]);
  http.expectOne(`${API}/customers`).flush([{ id: 'cu1', taxCode: '0123456789', name: 'Acme' }]);
  http.expectOne(`${API}/container-types`).flush([{ id: 't1', code: '20GP', name: '20 feet' }]);
  http.expectOne(`${API}/release-orders/r1/deliveries`).flush(deliveries);
  fixture.detectChanges();
  return { http, fixture, element };
}

function rows(element: HTMLElement, table = 0): string[] {
  const body = element.querySelectorAll('tbody')[table];
  return [...(body?.querySelectorAll('tr') ?? [])].map((tr) =>
    (tr.textContent ?? '').replace(/\s+/g, ' ').trim(),
  );
}

describe('ReleaseOrderPage', () => {
  it('shows the order and what each line has delivered and has left', () => {
    const { element } = setup();
    expect(element.querySelector('h1')?.textContent).toContain('Lệnh giao RO-1');
    expect(element.textContent).toContain('CSQ - Cosco');
    expect(element.textContent).toContain('Acme');
    expect(element.textContent).toContain('Đang giao');
    expect(rows(element)).toEqual(['20GP 20 ft 4 1 3', '20GP 40 ft 2 2 0']);
  });

  it('draws each line progress', () => {
    const { element } = setup();
    const bars = [...element.querySelectorAll('[role="progressbar"]')];
    expect(bars.map((b) => b.getAttribute('aria-valuenow'))).toEqual(['25', '100']);
  });

  it('warns that an expired order cannot be used for Gate Out', () => {
    const { element } = setup({ ...ORDER, isExpired: true });
    expect(element.textContent).toContain('Lệnh đã quá hạn');
    expect(element.textContent).toContain('Quá hạn');
  });

  it('shows no warning for a fully delivered order past its date', () => {
    const { element } = setup({ ...ORDER, isExpired: true, status: 'Delivered' });
    expect(element.textContent).not.toContain('Lệnh đã quá hạn');
  });

  it('says the order was not found', () => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: new Map([['id', 'x']]) } } },
      ],
    });
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(ReleaseOrderPage);
    fixture.detectChanges();
    http.expectOne(`${API}/release-orders/x/deliveries`).flush([]);
    http
      .expectOne(`${API}/release-orders/x`)
      .flush({ title: 'ReleaseOrder.NotFound' }, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'Không tìm thấy lệnh giao.',
    );
  });
});

describe('ReleaseOrderPage deliveries', () => {
  it('lists the containers already delivered on the order, latest first', () => {
    const { element } = setup();
    expect(element.textContent).toContain('Container đã giao');
    expect(rows(element, 1)).toEqual([
      'MSKU0000000 20GP 40 ft 20/10/2026 08:30 Sà lan SL-01',
      'CSQU3054383 20GP 20 ft 19/10/2026 14:05 Xe tải 51C-123.45',
    ]);
  });

  it('says so when nothing has left on the order yet', () => {
    const { element } = setup(ORDER, []);
    expect(element.textContent).toContain('Chưa có container nào rời bãi theo lệnh này.');
    expect(element.querySelectorAll('tbody').length).toBe(1);
  });

  it('offers Gate Out with the order filled in while the order can still deliver', () => {
    const { element } = setup();
    const link = [...element.querySelectorAll('a')].find((a) =>
      a.textContent?.includes('Gate Out theo lệnh này'),
    );
    expect(link?.getAttribute('href')).toBe('/gate-out?order=r1');
  });

  it('offers no Gate Out for an expired or fully delivered order', () => {
    expect(setup({ ...ORDER, isExpired: true }).element.textContent).not.toContain(
      'Gate Out theo lệnh này',
    );
    TestBed.resetTestingModule();
    expect(setup({ ...ORDER, status: 'Delivered' }).element.textContent).not.toContain(
      'Gate Out theo lệnh này',
    );
  });
});
