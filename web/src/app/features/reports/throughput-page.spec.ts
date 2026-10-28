import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { type } from '../../../testing/dom';
import { daysAgo, today } from './report-dates';
import { ThroughputPage } from './throughput-page';

const URL = 'http://localhost:5049/api/reports/throughput';
const ROWS = [
  { date: '2026-10-26', operatorCode: 'CSQ', operatorName: 'Cosco', gateIns: 4, gateOuts: 1 },
  { date: '2026-10-27', operatorCode: 'MSK', operatorName: 'Maersk', gateIns: 2, gateOuts: 3 },
];

function setup(rows = ROWS) {
  TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(ThroughputPage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  http
    .expectOne(
      (r) => r.url === URL && r.params.get('from') === daysAgo(6) && r.params.get('to') === today(),
    )
    .flush(rows);
  fixture.detectChanges();
  return { http, fixture, element };
}

function cells(row: Element): string[] {
  return [...row.querySelectorAll('td')].map((td) => (td.textContent ?? '').trim());
}

describe('ThroughputPage', () => {
  it('asks for the last 7 days first and lists each day and operator', () => {
    const { element } = setup();
    const [first, second] = [...element.querySelectorAll('tbody tr')].map(cells);
    expect(first).toEqual(['26/10/2026', 'CSQ - Cosco', '4', '1']);
    expect(second).toEqual(['27/10/2026', 'MSK - Maersk', '2', '3']);
  });

  it('ends with the total of gate ins and gate outs', () => {
    const { element } = setup();
    expect(cells(element.querySelector('tfoot tr') as Element)).toEqual(['Tổng', '6', '4']);
  });

  it('says so when nothing moved in the range', () => {
    const { element } = setup([]);
    expect(element.textContent).toContain('Không có container nào vào hoặc ra');
  });

  it('asks again with the new range', () => {
    const { http, fixture, element } = setup();
    type(fixture, 'Từ ngày', '2026-10-01');
    http.expectOne((r) => r.url === URL && r.params.get('from') === '2026-10-01').flush([ROWS[0]]);
    fixture.detectChanges();
    expect(element.querySelectorAll('tbody tr').length).toBe(1);
  });

  it('reports a range that ends before it starts without asking the API', () => {
    const { http, fixture, element } = setup();
    type(fixture, 'Đến ngày', '2026-09-01');
    http.expectNone(URL);
    expect(element.textContent).toContain('Từ ngày không được sau đến ngày.');
  });

  it('does not ask for a day that is still to come', () => {
    const { http, fixture, element } = setup();
    type(fixture, 'Đến ngày', '2999-01-01');
    http.expectNone(URL);
    expect(element.textContent).toContain('Ngày không được ở tương lai.');
  });
});
