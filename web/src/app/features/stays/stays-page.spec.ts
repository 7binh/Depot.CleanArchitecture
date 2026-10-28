import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { click, type } from '../../../testing/dom';
import { StaysPage } from './stays-page';

const API = 'http://localhost:5049/api';
const STAYS_URL = `${API}/container-stays`;
const OPERATORS = [{ id: 'o1', code: 'CSQ', name: 'Cosco' }];
const CONTAINERS = [
  { id: 'c1', number: 'CSQU3054383', lineOperatorId: 'o1' },
  { id: 'c2', number: 'CSQU0000000', lineOperatorId: 'o1' },
];
const BLOCKS = [
  { id: 'b1', code: 'A', name: 'Khu A', isVirtual: false },
  { id: 'v1', code: 'V', name: 'Bãi ảo', isVirtual: true },
];
const STAYS = [
  {
    id: 's1',
    containerId: 'c1',
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
    gateInAt: '2026-10-10T09:05:00',
    condition: 'Damaged',
    blockId: 'v1',
    rowId: null,
    tier: null,
    gateOutAt: '2026-10-15T16:40:00',
  },
];

function setup() {
  TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(StaysPage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  answer(http, STAYS);
  fixture.detectChanges();
  return { http, fixture, element };
}

/** Answers one load: the stays, then the lists and the bays and rows they need. */
function answer(http: HttpTestingController, stays: unknown[]) {
  http.expectOne((r) => r.url === STAYS_URL).flush(stays);
  http.expectOne(`${API}/containers`).flush(CONTAINERS);
  http.expectOne(`${API}/line-operators`).flush(OPERATORS);
  http.expectOne(`${API}/blocks`).flush(BLOCKS);
  if (stays.length > 0) {
    http.expectOne(`${API}/blocks/b1/bays`).flush([{ id: 'y1', blockId: 'b1', bayNo: 3 }]);
    http
      .expectOne(`${API}/blocks/b1/bays/y1/rows`)
      .flush([{ id: 'r1', bayId: 'y1', rowNo: 2, maxTier: 3 }]);
  }
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

describe('StaysPage', () => {
  it('lists the newest stay first with the container, times, place and status', () => {
    const { element } = setup();
    const [first, second] = rowTexts(element);
    expect(first).toBe(
      'CSQU3054383 CSQ | 20/10/2026 08:30 | Chưa ra | A, bay 3, row 2, tầng 2 | Bình thường | Trong bãi',
    );
    expect(second).toBe(
      'CSQU0000000 CSQ | 10/10/2026 09:05 | 15/10/2026 16:40 | Bãi ảo | Hư hỏng | Đã ra',
    );
  });

  it('asks the API again with inYard when a filter is chosen', () => {
    const { http, fixture, element } = setup();
    click(fixture, 'Đang trong bãi');
    const request = http.expectOne((r) => r.url === STAYS_URL);
    expect(request.request.params.get('inYard')).toBe('true');
    request.flush([STAYS[0]]);
    http.expectOne(`${API}/containers`).flush(CONTAINERS);
    http.expectOne(`${API}/line-operators`).flush(OPERATORS);
    http.expectOne(`${API}/blocks`).flush(BLOCKS);
    http.expectOne(`${API}/blocks/b1/bays`).flush([{ id: 'y1', blockId: 'b1', bayNo: 3 }]);
    http
      .expectOne(`${API}/blocks/b1/bays/y1/rows`)
      .flush([{ id: 'r1', bayId: 'y1', rowNo: 2, maxTier: 3 }]);
    fixture.detectChanges();
    expect(rowTexts(element).length).toBe(1);
  });

  it('finds a container by part of its number', () => {
    const { fixture, element } = setup();
    type(fixture, 'Tìm container', '0000');
    expect(rowTexts(element).length).toBe(1);
    expect(rowTexts(element)[0]).toContain('CSQU0000000');
  });

  it('says so when no number matches', () => {
    const { fixture, element } = setup();
    type(fixture, 'Tìm container', 'ZZZ');
    expect(element.textContent).toContain('Không có container nào khớp số đã nhập.');
  });

  it('says so when there are no stays', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(StaysPage);
    fixture.detectChanges();
    answer(http, []);
    fixture.detectChanges();
    expect(fixture.nativeElement.textContent).toContain('Chưa có lần lưu bãi nào.');
  });
});
