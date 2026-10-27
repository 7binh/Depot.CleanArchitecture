import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { click, type } from '../../../../testing/dom';
import { stubDialog } from '../../../../testing/dialog';
import { CustomersPage } from './customers-page';

const URL = 'http://localhost:5049/api/customers';
const ABC = { id: '1', taxCode: '0312345678', name: 'Công ty ABC' };

function setup() {
  stubDialog();
  TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(CustomersPage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  http.expectOne(URL).flush([ABC]);
  fixture.detectChanges();
  return { http, fixture, element };
}

function fillAndSave(taxCode: string) {
  const page = setup();
  click(page.fixture, 'Thêm khách hàng');
  type(page.fixture, 'Mã số thuế', taxCode);
  type(page.fixture, 'Tên khách hàng', 'Công ty XYZ');
  click(page.fixture, 'Lưu');
  return page;
}

describe('CustomersPage', () => {
  it('lists the customers with their tax code', () => {
    const { element } = setup();
    expect(element.querySelector('td')?.textContent?.trim()).toBe('0312345678');
    expect(element.textContent).toContain('Công ty ABC');
  });

  it('rejects a tax code that is not 10 or 13 digits', () => {
    const { http, element } = fillAndSave('12345');
    expect(element.querySelector('dialog')?.textContent).toContain(
      'Mã số thuế gồm đúng 10 hoặc 13 chữ số.',
    );
    http.expectNone(URL);
  });

  it('creates a customer with a 13-digit tax code and reloads', () => {
    const { http, fixture, element } = fillAndSave('0312345678001');
    const request = http.expectOne((r) => r.method === 'POST');
    expect(request.request.body).toEqual({ taxCode: '0312345678001', name: 'Công ty XYZ' });
    request.flush({ id: '2', taxCode: '0312345678001', name: 'Công ty XYZ' });
    fixture.detectChanges();
    http.expectOne(URL).flush([]);
    fixture.detectChanges();
    expect(element.querySelector('dialog')).toBeNull();
  });

  it('shows a duplicate tax code under the field', () => {
    const { http, fixture, element } = fillAndSave('0312345678');
    const conflict = { title: 'Customer.TaxCodeExists' };
    http
      .expectOne((r) => r.method === 'POST')
      .flush(conflict, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();
    expect(element.querySelector('dialog')?.textContent).toContain(
      'Mã số thuế này đã có khách hàng sử dụng.',
    );
  });
});
