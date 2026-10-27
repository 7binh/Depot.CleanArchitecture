import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { choose, click, type } from '../../../testing/dom';
import { CreateReleaseOrderPage } from './create-release-order-page';

const API = 'http://localhost:5049/api';

function setup() {
  TestBed.configureTestingModule({
    providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
  });
  const http = TestBed.inject(HttpTestingController);
  const navigate = vi.spyOn(TestBed.inject(Router), 'navigate').mockResolvedValue(true);
  const fixture = TestBed.createComponent(CreateReleaseOrderPage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  http.expectOne(`${API}/line-operators`).flush([{ id: 'o1', code: 'CSQ', name: 'Cosco' }]);
  http.expectOne(`${API}/customers`).flush([{ id: 'cu1', taxCode: '0123456789', name: 'Acme' }]);
  http.expectOne(`${API}/container-types`).flush([{ id: 't1', code: '20GP', name: '20 feet' }]);
  fixture.detectChanges();
  return { http, fixture, element, navigate };
}

function fillHeader(page: ReturnType<typeof setup>): void {
  choose(page.fixture, 'Hãng khai thác', 'o1');
  choose(page.fixture, 'Khách hàng', 'cu1');
  type(page.fixture, 'Số lệnh', ' RO-1 ');
  type(page.fixture, 'Tàu xuất', 'MV Sun');
  type(page.fixture, 'Ngày cấp', '2026-10-01');
  type(page.fixture, 'Hạn lệnh', '2026-12-31');
}

function fillLine(page: ReturnType<typeof setup>, n: number, size: string, qty: string): void {
  choose(page.fixture, `Loại (dòng ${n})`, 't1');
  choose(page.fixture, `Cỡ (dòng ${n})`, size);
  type(page.fixture, `Số lượng (dòng ${n})`, qty);
}

function fillValid(page: ReturnType<typeof setup>): void {
  fillHeader(page);
  fillLine(page, 1, '20', '5');
  click(page.fixture, 'Tạo lệnh giao');
}

describe('CreateReleaseOrderPage form', () => {
  it('sends the order with its lines and opens it', () => {
    const page = setup();
    fillValid(page);
    const request = page.http.expectOne(`${API}/release-orders`);
    expect(request.request.body).toEqual({
      lineOperatorId: 'o1',
      customerId: 'cu1',
      orderNo: 'RO-1',
      issuedDate: '2026-10-01',
      expiryDate: '2026-12-31',
      exportVessel: 'MV Sun',
      lines: [{ containerTypeId: 't1', sizeFeet: 20, quantity: 5 }],
    });
    request.flush({ id: 'r9' });
    expect(page.navigate).toHaveBeenCalledWith(['/lenh-giao', 'r9']);
  });

  it('does not call the API while the form is invalid', () => {
    const { http, fixture, element } = setup();
    click(fixture, 'Tạo lệnh giao');
    http.expectNone(`${API}/release-orders`);
    expect(element.textContent).toContain('Nhập số lệnh giao.');
    expect(element.textContent).toContain('Chọn loại container.');
  });

  it('adds and removes lines but keeps at least one', () => {
    const { fixture, element } = setup();
    click(fixture, 'Thêm dòng');
    expect(element.textContent).toContain('Loại (dòng 2)');
    click(fixture, 'Thêm dòng');
    const remove = (n: number) =>
      (element.querySelector(`[aria-label="Xóa dòng ${n}"]`) as HTMLButtonElement).click();
    remove(3);
    remove(2);
    fixture.detectChanges();
    const only = element.querySelector('[aria-label="Xóa dòng 1"]') as HTMLButtonElement;
    expect(only.disabled).toBe(true);
    expect(element.textContent).not.toContain('Loại (dòng 2)');
  });

  it('warns about a repeated type and size and refuses to send', () => {
    const page = setup();
    fillHeader(page);
    fillLine(page, 1, '20', '5');
    click(page.fixture, 'Thêm dòng');
    fillLine(page, 2, '20', '3');
    expect(page.element.textContent).toContain('chỉ được xuất hiện một lần');
    click(page.fixture, 'Tạo lệnh giao');
    page.http.expectNone(`${API}/release-orders`);
  });
});

describe('CreateReleaseOrderPage validation', () => {
  it('rejects an expiry before the issue date and a quantity that is not whole', () => {
    const page = setup();
    fillHeader(page);
    type(page.fixture, 'Hạn lệnh', '2026-09-01');
    fillLine(page, 1, '20', '2,5');
    click(page.fixture, 'Tạo lệnh giao');
    expect(page.element.textContent).toContain('Hạn lệnh không được trước ngày cấp.');
    expect(page.element.textContent).toContain('Số lượng phải là số nguyên lớn hơn 0.');
    page.http.expectNone(`${API}/release-orders`);
  });

  it('shows a taken order number under the order number field', () => {
    const page = setup();
    fillValid(page);
    page.http
      .expectOne(`${API}/release-orders`)
      .flush({ title: 'ReleaseOrder.OrderNoExists' }, { status: 409, statusText: 'Conflict' });
    page.fixture.detectChanges();
    expect(page.element.textContent).toContain('Hãng này đã có lệnh giao với số lệnh đó.');
    expect(page.navigate).not.toHaveBeenCalled();
  });

  it('shows a lines error from the API under the lines', () => {
    const page = setup();
    fillValid(page);
    page.http
      .expectOne(`${API}/release-orders`)
      .flush({ title: 'Quantity.Invalid' }, { status: 400, statusText: 'Bad Request' });
    page.fixture.detectChanges();
    expect(page.element.textContent).toContain('Số lượng mỗi dòng phải lớn hơn 0.');
  });
});
