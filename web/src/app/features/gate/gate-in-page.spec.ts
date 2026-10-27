import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { choose, click, type } from '../../../testing/dom';
import { stubDialog } from '../../../testing/dialog';
import { GateInPage } from './gate-in-page';

const API = 'http://localhost:5049/api';
const CONTAINERS = [
  {
    id: 'c1',
    number: 'CSQU3054383',
    lineOperatorId: 'o1',
    containerTypeId: 't1',
    isoCode: '22G1',
    sizeFeet: 20,
    maxWeightKg: 30480,
    tareWeightKg: 2200,
    manufactureDate: '2024-03-09',
  },
];
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

function setup() {
  stubDialog();
  TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(GateInPage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  http.expectOne(`${API}/containers`).flush(CONTAINERS);
  http.expectOne(`${API}/line-operators`).flush([{ id: 'o1', code: 'CSQ', name: 'Cosco' }]);
  http.expectOne(`${API}/container-types`).flush([{ id: 't1', code: '20GP', name: '20 feet' }]);
  http.expectOne(`${API}/blocks`).flush(BLOCKS);
  fixture.detectChanges();
  return { http, fixture, element };
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
  type(page.fixture, 'Số container', 'csqu3054383');
  type(page.fixture, 'Biển số', '51C-123.45');
}

describe('GateInPage lookup', () => {
  it('shows the profile of a known container for the clerk to check', () => {
    const { fixture, element } = setup();
    type(fixture, 'Số container', 'csqu3054383');
    expect(element.textContent).toContain('hãng CSQ, loại 20GP, cỡ 20 ft, ISO 22G1');
  });

  it('offers to declare a container that has no profile', () => {
    const { fixture, element } = setup();
    type(fixture, 'Số container', 'MSKU0000000');
    expect(element.textContent).toContain('Chưa có hồ sơ cho số container này.');
    click(fixture, 'Khai báo container');
    expect(element.querySelector('dialog')).not.toBeNull();
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
    type(page.fixture, 'Số container', 'CSQU3054383');
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
