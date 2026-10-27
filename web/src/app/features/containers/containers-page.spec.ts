import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { choose, click, type } from '../../../testing/dom';
import { stubDialog } from '../../../testing/dialog';
import { ContainersPage } from './containers-page';

const API = 'http://localhost:5049/api';
const OPERATORS = [
  { id: 'o1', code: 'CSQ', name: 'Cosco' },
  { id: 'o2', code: 'MSK', name: 'Maersk' },
];
const TYPES = [
  { id: 't1', code: '20GP', name: 'Container 20 feet' },
  { id: 't2', code: '40HC', name: 'Container 40 feet cao' },
];
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
  {
    id: 'c2',
    number: 'MSKU0000000',
    lineOperatorId: 'o2',
    containerTypeId: 't2',
    isoCode: '45G1',
    sizeFeet: 40,
    maxWeightKg: 32500,
    tareWeightKg: 3800,
    manufactureDate: '2023-01-02',
  },
];

function setup(containers = CONTAINERS) {
  stubDialog();
  TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(ContainersPage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  http.expectOne(`${API}/containers`).flush(containers);
  http.expectOne(`${API}/line-operators`).flush(OPERATORS);
  http.expectOne(`${API}/container-types`).flush(TYPES);
  fixture.detectChanges();
  return { http, fixture, element };
}

function rowTexts(element: HTMLElement): string[] {
  return [...element.querySelectorAll('tbody tr')].map((tr) => tr.textContent ?? '');
}

describe('ContainersPage list', () => {
  it('shows each container with its operator and type names', () => {
    const { element } = setup();
    const [first] = rowTexts(element);
    expect(first).toContain('CSQU3054383');
    expect(first).toContain('CSQ');
    expect(first).toContain('20GP');
    expect(first).toContain('20 ft');
    expect(first).toContain('30.480');
    expect(first).toContain('09/03/2024');
  });

  it('filters by line operator and by type', () => {
    const { fixture, element } = setup();
    choose(fixture, 'Hãng', 'o2');
    expect(rowTexts(element).length).toBe(1);
    expect(rowTexts(element)[0]).toContain('MSKU0000000');

    choose(fixture, 'Loại', 't1');
    expect(element.textContent).toContain('Không có container nào khớp bộ lọc.');
  });

  it('says so when there are no containers', () => {
    const { element } = setup([]);
    expect(element.textContent).toContain('Chưa có container nào.');
  });
});

function openDialog() {
  const page = setup();
  click(page.fixture, 'Thêm container');
  page.http.expectOne(`${API}/container-types`).flush(TYPES);
  page.fixture.detectChanges();
  return page;
}

function fill(page: ReturnType<typeof openDialog>, number: string, tare = '2200') {
  type(page.fixture, 'Số container', number);
  choose(page.fixture, 'Loại container', 't1');
  type(page.fixture, 'Mã ISO', '22G1');
  choose(page.fixture, 'Cỡ', '20');
  type(page.fixture, 'Tải trọng tối đa', '30480');
  type(page.fixture, 'Trọng lượng vỏ', tare);
  type(page.fixture, 'Ngày sản xuất', '2024-03-09');
  click(page.fixture, 'Lưu');
}

describe('ContainersPage validation', () => {
  it('reports a wrong check digit without calling the API', () => {
    const page = openDialog();
    fill(page, 'CSQU3054384');
    expect(page.element.querySelector('dialog')?.textContent).toContain('sai chữ số kiểm tra');
    page.http.expectNone(`${API}/containers`);
  });

  it('reports a tare weight above the maximum', () => {
    const page = openDialog();
    fill(page, 'CSQU3054383', '40000');
    expect(page.element.querySelector('dialog')?.textContent).toContain(
      'Trọng lượng vỏ không được lớn hơn',
    );
    page.http.expectNone(`${API}/containers`);
  });
});

describe('ContainersPage create', () => {
  it('sends the container, closes the dialog and reloads', () => {
    const page = openDialog();
    fill(page, 'csqu3054383');
    const request = page.http.expectOne((r) => r.method === 'POST');
    expect(request.request.body).toEqual({
      number: 'csqu3054383',
      containerTypeId: 't1',
      isoCode: '22G1',
      sizeFeet: 20,
      maxWeightKg: 30480,
      tareWeightKg: 2200,
      manufactureDate: '2024-03-09',
    });
    request.flush(CONTAINERS[0]);
    page.fixture.detectChanges();
    expect(page.element.querySelector('dialog')).toBeNull();
    page.http.expectOne(`${API}/containers`);
    page.http.expectOne(`${API}/line-operators`);
    page.http.expectOne(`${API}/container-types`);
  });

  it('shows a duplicate number under the number field', () => {
    const page = openDialog();
    fill(page, 'CSQU3054383');
    page.http
      .expectOne((r) => r.method === 'POST')
      .flush({ title: 'Container.NumberExists' }, { status: 409, statusText: 'Conflict' });
    page.fixture.detectChanges();
    expect(page.element.querySelector('dialog')?.textContent).toContain(
      'Số container này đã tồn tại.',
    );
  });

  it('shows an unknown owner under the number field', () => {
    const page = openDialog();
    fill(page, 'CSQU3054383');
    page.http
      .expectOne((r) => r.method === 'POST')
      .flush({ title: 'Container.OperatorNotFound' }, { status: 400, statusText: 'Bad Request' });
    page.fixture.detectChanges();
    expect(page.element.querySelector('dialog')?.textContent).toContain(
      'Ba chữ cái đầu không thuộc hãng khai thác nào.',
    );
  });
});
