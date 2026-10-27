import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { choose, click, type } from '../../../testing/dom';
import { GateOutPage } from './gate-out-page';

const API = 'http://localhost:5049/api';
const container = (id: string, number: string, lineOperatorId: string) => ({
  id,
  number,
  lineOperatorId,
  containerTypeId: 't1',
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
const ORDERS = [
  order('r1', 'RO-1'),
  order('r2', 'RO-2', { lineOperatorId: 'o2' }),
  order('r3', 'RO-3', { isExpired: true }),
  order('r4', 'RO-4', {
    items: [{ containerTypeId: 't1', sizeFeet: 20, quantity: 1, delivered: 1, remaining: 0 }],
  }),
];

function setup() {
  TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(GateOutPage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  http
    .expectOne(`${API}/containers`)
    .flush([container('c1', 'CSQU3054383', 'o1'), container('c2', 'MSKU0000000', 'o2')]);
  http.expectOne(`${API}/line-operators`).flush([{ id: 'o1', code: 'CSQ', name: 'Cosco' }]);
  http.expectOne(`${API}/container-types`).flush([{ id: 't1', code: '20GP', name: '20 feet' }]);
  http
    .expectOne((r) => r.url === `${API}/container-stays`)
    .flush([{ id: 's1', containerId: 'c1' }]);
  http.expectOne(`${API}/release-orders`).flush(ORDERS);
  fixture.detectChanges();
  return { http, fixture, element };
}

function optionValues(element: HTMLElement, label: string): string[] {
  const field = [...element.querySelectorAll('app-select-field')].find((f) =>
    f.textContent?.includes(label),
  );
  return [...(field?.querySelectorAll('option') ?? [])].map((o) => o.value).filter(Boolean);
}

function fill(page: ReturnType<typeof setup>): void {
  choose(page.fixture, 'Container trong bãi', 'c1');
  choose(page.fixture, 'Lệnh giao', 'r1');
  type(page.fixture, 'Biển số', '51C-123.45');
  click(page.fixture, 'Xác nhận Gate Out');
}

describe('GateOutPage choices', () => {
  it('lists only the containers that are in the yard', () => {
    const { element } = setup();
    expect(optionValues(element, 'Container trong bãi')).toEqual(['c1']);
  });

  it('offers only valid orders of the same operator with room left', () => {
    const { fixture, element } = setup();
    choose(fixture, 'Container trong bãi', 'c1');
    expect(optionValues(element, 'Lệnh giao')).toEqual(['r1']);
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
    page.http.expectOne((r) => r.url === `${API}/container-stays`);
    page.http.expectOne(`${API}/release-orders`);
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
