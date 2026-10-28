import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { type } from '../../../testing/dom';
import { today } from './report-dates';
import { YardStockPage } from './yard-stock-page';

const URL = 'http://localhost:5049/api/reports/yard-stock';
const ROWS = [
  { operatorCode: 'CSQ', operatorName: 'Cosco', recent: 6, longStay: 2, total: 8 },
  { operatorCode: 'MSK', operatorName: 'Maersk', recent: 3, longStay: 0, total: 3 },
];

function setup(rows = ROWS) {
  TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(YardStockPage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  http.expectOne((r) => r.url === URL && r.params.get('date') === today()).flush(rows);
  fixture.detectChanges();
  return { http, fixture, element };
}

function cells(row: Element): string[] {
  return [...row.querySelectorAll('td')].map((td) => (td.textContent ?? '').trim());
}

describe('YardStockPage', () => {
  it('asks for today first and lists each operator with its share of long stays', () => {
    const { element } = setup();
    const [first, second] = [...element.querySelectorAll('tbody tr')].map(cells);
    expect(first).toEqual(['CSQ - Cosco', '6', '2', '8', '25%']);
    expect(second).toEqual(['MSK - Maersk', '3', '0', '3', '0%']);
  });

  it('ends with the total of all operators', () => {
    const { element } = setup();
    const total = cells(element.querySelector('tfoot tr') as Element);
    expect(total).toEqual(['Tổng', '9', '2', '11', '18%']);
  });

  it('says so when no container was in the yard that day', () => {
    const { element } = setup([]);
    expect(element.textContent).toContain('Không có container nào trong bãi vào ngày này.');
  });

  it('asks again when another day is chosen', () => {
    const { http, fixture, element } = setup();
    type(fixture, 'Ngày', '2026-10-01');
    http.expectOne((r) => r.url === URL && r.params.get('date') === '2026-10-01').flush([ROWS[1]]);
    fixture.detectChanges();
    expect(element.querySelectorAll('tbody tr').length).toBe(1);
  });

  it('does not ask for a day that is still to come', () => {
    const { http, fixture, element } = setup();
    type(fixture, 'Ngày', '2999-01-01');
    http.expectNone(URL);
    expect(element.textContent).toContain('Ngày không được ở tương lai.');
  });
});
