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
    lineOperatorCode: 'CSQ',
    containerTypeId: 't1',
    containerTypeCode: '20GP',
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
    lineOperatorCode: 'MSK',
    containerTypeId: 't2',
    containerTypeCode: '40HC',
    isoCode: '45G1',
    sizeFeet: 40,
    maxWeightKg: 32500,
    tareWeightKg: 3800,
    manufactureDate: '2023-01-02',
  },
];

function paged(items: typeof CONTAINERS, totalCount = items.length, page = 1) {
  return { items, totalCount, page, pageSize: 20, totalPages: Math.ceil(totalCount / 20) };
}

function searchRequest(http: HttpTestingController) {
  return http.expectOne((r) => r.url === `${API}/containers/search`);
}

function setup(items = CONTAINERS, totalCount = items.length) {
  stubDialog();
  TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(ContainersPage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  searchRequest(http).flush(paged(items, totalCount));
  http.expectOne(`${API}/line-operators`).flush(OPERATORS);
  http.expectOne(`${API}/container-types`).flush(TYPES);
  fixture.detectChanges();
  return { http, fixture, element };
}

function rowTexts(element: HTMLElement): string[] {
  return [...element.querySelectorAll('tbody tr')].map((tr) => tr.textContent ?? '');
}

describe('ContainersPage list', () => {
  it('shows each container with its operator and type codes', () => {
    const { element } = setup();
    const [first] = rowTexts(element);
    expect(first).toContain('CSQU3054383');
    expect(first).toContain('CSQ');
    expect(first).toContain('20GP');
    expect(first).toContain('20 ft');
    expect(first).toContain('30.480');
    expect(first).toContain('09/03/2024');
  });

  it('says so when there are no containers', () => {
    const { element } = setup([]);
    expect(element.textContent).toContain('Chưa có container nào.');
  });

  it('asks for the first page of 20 with no filter', () => {
    stubDialog();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const http = TestBed.inject(HttpTestingController);
    TestBed.createComponent(ContainersPage).detectChanges();
    const { params } = searchRequest(http).request;
    expect(params.keys().sort()).toEqual(['page', 'pageSize']);
    expect(params.get('page')).toBe('1');
    expect(params.get('pageSize')).toBe('20');
  });
});

describe('ContainersPage filters', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('asks the API for the chosen operator, type and size once typing pauses', () => {
    const { http, fixture, element } = setup();
    choose(fixture, 'Hãng', 'o2');
    choose(fixture, 'Loại', 't2');
    choose(fixture, 'Kích cỡ', '40');
    http.expectNone((r) => r.url === `${API}/containers/search`);

    vi.advanceTimersByTime(300);
    const request = searchRequest(http);
    expect(request.request.params.get('lineOperatorId')).toBe('o2');
    expect(request.request.params.get('containerTypeId')).toBe('t2');
    expect(request.request.params.get('sizeFeet')).toBe('40');
    request.flush(paged([CONTAINERS[1]]));
    fixture.detectChanges();
    expect(rowTexts(element).length).toBe(1);
    expect(rowTexts(element)[0]).toContain('MSKU0000000');
  });

  it('sends the typed number and ISO code', () => {
    const { http, fixture } = setup();
    type(fixture, 'Tìm số container', 'csq');
    type(fixture, 'Tìm mã ISO', '22g');
    vi.advanceTimersByTime(300);
    const { params } = searchRequest(http).request;
    expect(params.get('number')).toBe('csq');
    expect(params.get('isoCode')).toBe('22g');
  });

  it('says no container matches when a filtered search finds none', () => {
    const { http, fixture, element } = setup();
    choose(fixture, 'Hãng', 'o2');
    vi.advanceTimersByTime(300);
    searchRequest(http).flush(paged([]));
    fixture.detectChanges();
    expect(element.textContent).toContain('Không có container nào khớp bộ lọc.');
  });
});

describe('ContainersPage paging', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('shows the range and the page count', () => {
    const { element } = setup(CONTAINERS, 45);
    expect(element.querySelector('nav')?.textContent).toContain('1–20 / 45');
    expect(element.querySelector('nav')?.textContent).toContain('1 / 3');
  });

  it('disables Trước on the first page and Sau on the last', () => {
    const { fixture, element } = setup(CONTAINERS, 2);
    const buttons = [...element.querySelectorAll<HTMLButtonElement>('nav button')];
    expect(buttons.map((b) => b.disabled)).toEqual([true, true]);
    expect(fixture).toBeTruthy();
  });

  it('asks for the next page when Sau is clicked', () => {
    const { http, fixture, element } = setup(CONTAINERS, 45);
    click(fixture, 'Sau');
    const request = searchRequest(http);
    expect(request.request.params.get('page')).toBe('2');
    request.flush(paged(CONTAINERS, 45, 2));
    fixture.detectChanges();
    expect(element.querySelector('nav')?.textContent).toContain('21–40 / 45');
  });

  it('goes back to page 1 when a filter changes', () => {
    const { http, fixture } = setup(CONTAINERS, 45);
    click(fixture, 'Sau');
    searchRequest(http).flush(paged(CONTAINERS, 45, 2));
    fixture.detectChanges();

    choose(fixture, 'Hãng', 'o1');
    vi.advanceTimersByTime(300);
    expect(searchRequest(http).request.params.get('page')).toBe('1');
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
  it('sends the container, closes the dialog and reloads the list', () => {
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
    searchRequest(page.http);
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
