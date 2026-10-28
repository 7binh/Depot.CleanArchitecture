import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { click, type } from '../../../testing/dom';
import { StaysPage } from './stays-page';

const API = 'http://localhost:5049/api';
const SEARCH_URL = `${API}/container-stays/search`;
const BLOCKS = [
  { id: 'b1', code: 'A', name: 'Khu A', isVirtual: false },
  { id: 'v1', code: 'V', name: 'Bãi ảo', isVirtual: true },
];
const STAYS = [
  {
    id: 's1',
    containerId: 'c1',
    containerNumber: 'CSQU3054383',
    lineOperatorCode: 'CSQ',
    gateInAt: '2026-10-20T08:30:00',
    condition: 'Normal',
    blockId: 'b1',
    rowId: 'r1',
    tier: 2,
    gateOutAt: null,
  },
  {
    id: 's2',
    containerId: 'c2',
    containerNumber: 'CSQU0000000',
    lineOperatorCode: 'CSQ',
    gateInAt: '2026-10-10T09:05:00',
    condition: 'Damaged',
    blockId: 'v1',
    rowId: null,
    tier: null,
    gateOutAt: '2026-10-15T16:40:00',
  },
];

function paged(items: unknown[], totalCount = items.length, page = 1) {
  return { items, totalCount, page, pageSize: 20, totalPages: Math.ceil(totalCount / 20) };
}

function searchRequest(http: HttpTestingController) {
  return http.expectOne((r) => r.url === SEARCH_URL);
}

/** Answers one load: the page of stays, then the blocks and the bays and rows they need. */
function answer(http: HttpTestingController, stays: unknown[], totalCount = stays.length) {
  searchRequest(http).flush(paged(stays, totalCount));
  answerPlaces(http, stays.length > 0);
}

function answerPlaces(http: HttpTestingController, withRows: boolean) {
  http.expectOne(`${API}/blocks`).flush(BLOCKS);
  if (withRows) {
    http.expectOne(`${API}/blocks/b1/bays`).flush([{ id: 'y1', blockId: 'b1', bayNo: 3 }]);
    http
      .expectOne(`${API}/blocks/b1/bays/y1/rows`)
      .flush([{ id: 'r1', bayId: 'y1', rowNo: 2, maxTier: 3 }]);
  }
}

function setup(stays: unknown[] = STAYS, totalCount = stays.length) {
  TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(StaysPage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  answer(http, stays, totalCount);
  fixture.detectChanges();
  return { http, fixture, element };
}

/** The text of each cell; a cell with several lines (container and operator) joins them with a space. */
function cellText(td: Element): string {
  const lines = [...td.querySelectorAll('p')].map((p) => (p.textContent ?? '').trim());
  return lines.length > 0 ? lines.join(' ') : (td.textContent ?? '').replace(/\s+/g, ' ').trim();
}

function rowTexts(element: HTMLElement): string[] {
  return [...element.querySelectorAll('tbody tr')].map((tr) =>
    [...tr.querySelectorAll('td')].map(cellText).join(' | '),
  );
}

describe('StaysPage list', () => {
  it('lists the stays in the order the API sends with the container, times, place and status', () => {
    const { element } = setup();
    const [first, second] = rowTexts(element);
    expect(first).toBe(
      'CSQU3054383 CSQ | 20/10/2026 08:30 | Chưa ra | A, bay 3, row 2, tầng 2 | Bình thường | Trong bãi',
    );
    expect(second).toBe(
      'CSQU0000000 CSQ | 10/10/2026 09:05 | 15/10/2026 16:40 | Bãi ảo | Hư hỏng | Đã ra',
    );
  });

  it('asks for the first page of 20 with no filter', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const http = TestBed.inject(HttpTestingController);
    TestBed.createComponent(StaysPage).detectChanges();
    const { params } = searchRequest(http).request;
    expect(params.keys().sort()).toEqual(['page', 'pageSize']);
    expect(params.get('pageSize')).toBe('20');
  });

  it('says so when there are no stays', () => {
    const { element } = setup([]);
    expect(element.textContent).toContain('Chưa có lần lưu bãi nào.');
  });
});

describe('StaysPage filters', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('asks the API again with inYard when a filter is chosen', () => {
    const { http, fixture, element } = setup();
    click(fixture, 'Đang trong bãi');
    const request = searchRequest(http);
    expect(request.request.params.get('inYard')).toBe('true');
    expect(request.request.params.get('page')).toBe('1');
    request.flush(paged([STAYS[0]]));
    answerPlaces(http, true);
    fixture.detectChanges();
    expect(rowTexts(element).length).toBe(1);
  });

  it('asks only for the stays that ended when Đã ra is chosen', () => {
    const { http, fixture, element } = setup();
    click(fixture, 'Đã ra');
    const request = searchRequest(http);
    expect(request.request.params.get('inYard')).toBe('false');
    request.flush(paged([STAYS[1]]));
    answerPlaces(http, false);
    fixture.detectChanges();
    expect(rowTexts(element).length).toBe(1);
    expect(rowTexts(element)[0]).toContain('Đã ra');
  });

  it('sends the typed number once typing pauses', () => {
    const { http, fixture } = setup();
    type(fixture, 'Tìm container', '0000');
    http.expectNone((r) => r.url === SEARCH_URL);
    vi.advanceTimersByTime(300);
    expect(searchRequest(http).request.params.get('number')).toBe('0000');
  });

  it('says so when a filtered search finds nothing', () => {
    const { http, fixture, element } = setup();
    type(fixture, 'Tìm container', 'ZZZ');
    vi.advanceTimersByTime(300);
    searchRequest(http).flush(paged([]));
    answerPlaces(http, false);
    fixture.detectChanges();
    expect(element.textContent).toContain('Không có lần lưu bãi nào khớp bộ lọc.');
  });
});

describe('StaysPage paging', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  it('shows the range and the page count', () => {
    const { element } = setup(STAYS, 45);
    expect(element.querySelector('nav')?.textContent).toContain('1–20 / 45');
    expect(element.querySelector('nav')?.textContent).toContain('1 / 3');
  });

  it('asks for the next page when Sau is clicked', () => {
    const { http, fixture, element } = setup(STAYS, 45);
    click(fixture, 'Sau');
    const request = searchRequest(http);
    expect(request.request.params.get('page')).toBe('2');
    request.flush(paged(STAYS, 45, 2));
    answerPlaces(http, true);
    fixture.detectChanges();
    expect(element.querySelector('nav')?.textContent).toContain('21–40 / 45');
  });

  it('goes back to page 1 when the scope changes', () => {
    const { http, fixture } = setup(STAYS, 45);
    click(fixture, 'Sau');
    searchRequest(http).flush(paged(STAYS, 45, 2));
    answerPlaces(http, true);
    fixture.detectChanges();

    click(fixture, 'Đã ra');
    expect(searchRequest(http).request.params.get('page')).toBe('1');
  });
});
