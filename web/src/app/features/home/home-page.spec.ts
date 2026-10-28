import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { daysAgo, today } from '../reports/report-dates';
import { toDayMovements, toHomeSummary } from './home-summary';
import { HomePage } from './home-page';

const API = 'http://localhost:5049/api';
const STOCK = [
  { operatorCode: 'MSK', operatorName: 'Maersk', recent: 3, longStay: 0, total: 3 },
  { operatorCode: 'CSQ', operatorName: 'Cosco', recent: 6, longStay: 2, total: 8 },
];
const THROUGHPUT = [
  { date: today(), operatorCode: 'CSQ', operatorName: 'Cosco', gateIns: 4, gateOuts: 1 },
  { date: today(), operatorCode: 'MSK', operatorName: 'Maersk', gateIns: 2, gateOuts: 0 },
  { date: daysAgo(1), operatorCode: 'CSQ', operatorName: 'Cosco', gateIns: 1, gateOuts: 3 },
];
const OPEN_ORDERS = { items: [], totalCount: 5, page: 1, pageSize: 1, totalPages: 5 };

function setup() {
  TestBed.configureTestingModule({
    providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
  });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(HomePage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  return { http, fixture, element };
}

function answer(view: ReturnType<typeof setup>) {
  const { http, fixture } = view;
  http
    .expectOne((r) => r.url === `${API}/reports/yard-stock` && r.params.get('date') === today())
    .flush(STOCK);
  http
    .expectOne(
      (r) =>
        r.url === `${API}/reports/throughput` &&
        r.params.get('from') === daysAgo(6) &&
        r.params.get('to') === today(),
    )
    .flush(THROUGHPUT);
  http
    .expectOne(
      (r) => r.url === `${API}/release-orders/search` && r.params.get('openOnly') === 'true',
    )
    .flush(OPEN_ORDERS);
  fixture.detectChanges();
}

describe('HomePage', () => {
  it('shows the yard, the long stays, today gate traffic and the open orders', () => {
    const view = setup();
    answer(view);
    const text = view.element.textContent ?? '';
    expect(text).toContain('Trong bãi');
    expect(text).toMatch(/Trong bãi\s*11\s*container/);
    expect(text).toMatch(/Tồn từ 10 ngày\s*2\s*18%/);
    expect(text).toMatch(/Gate In hôm nay\s*6/);
    expect(text).toMatch(/Gate Out hôm nay\s*1/);
    expect(text).toMatch(/Lệnh giao còn hiệu lực\s*5/);
  });

  it('lists the operators with the most containers first', () => {
    const view = setup();
    answer(view);
    const names = [...view.element.querySelectorAll('section ul li span.font-medium')].map((s) =>
      (s.textContent ?? '').trim(),
    );
    expect(names.slice(0, 2)).toEqual(['CSQ - Cosco', 'MSK - Maersk']);
  });

  it('draws seven days of traffic, the latest being today', () => {
    const view = setup();
    answer(view);
    const sparkline = view.element.querySelector('app-sparkline svg');
    expect(sparkline?.getAttribute('aria-label')).toContain('0, 0, 0, 0, 0, 4, 7');
  });

  it('links to the pages the work starts from', () => {
    const view = setup();
    answer(view);
    const links = [...view.element.querySelectorAll('a')].map((a) => a.getAttribute('href'));
    expect(links).toEqual(expect.arrayContaining(['/gate-in', '/gate-out', '/so-do-bai']));
  });

  it('says why when the numbers cannot be loaded and tries again on retry', () => {
    const view = setup();
    view.http
      .expectOne((r) => r.url === `${API}/reports/yard-stock`)
      .flush({ title: 'x' }, { status: 500, statusText: 'Server Error' });
    view.http.match(() => true);
    view.fixture.detectChanges();
    const retry = [...view.element.querySelectorAll('button')].find((b) =>
      b.textContent?.includes('Thử lại'),
    );
    expect(retry).toBeDefined();
    retry?.click();
    view.fixture.detectChanges();
    view.http.expectOne((r) => r.url === `${API}/reports/yard-stock`);
  });
});

describe('toHomeSummary', () => {
  it('has nothing for today when no day is given', () => {
    const summary = toHomeSummary([], [], 0);
    expect(summary).toMatchObject({ inYard: 0, longStayPercent: 0, openOrders: 0 });
    expect(summary.today.total).toBe(0);
  });

  it('sums the operators of one date and gives days without traffic as zero', () => {
    const days = toDayMovements(THROUGHPUT, [daysAgo(2), daysAgo(1), today()]);
    expect(days.map((d) => d.total)).toEqual([0, 4, 7]);
    expect(days[2]).toMatchObject({ gateIns: 6, gateOuts: 1 });
  });
});
