import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { click, type } from '../../../../testing/dom';
import { stubDialog } from '../../../../testing/dialog';
import { LineOperatorsPage } from './line-operators-page';

const URL = 'http://localhost:5049/api/line-operators';
const MAERSK = { id: '1', code: 'MSK', name: 'Maersk Line' };

function setup() {
  stubDialog();
  TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(LineOperatorsPage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  const respondWith = (items: (typeof MAERSK)[]): void => {
    http.expectOne(URL).flush(items);
    fixture.detectChanges();
  };
  return { http, fixture, element, respondWith };
}

describe('LineOperatorsPage list', () => {
  it('shows placeholders while loading', () => {
    const { http, element } = setup();
    expect(element.querySelectorAll('[appSkeleton]').length).toBeGreaterThan(0);
    http.expectOne(URL);
  });

  it('lists the line operators', () => {
    const { element, respondWith } = setup();
    respondWith([MAERSK]);
    expect(element.querySelector('td')?.textContent?.trim()).toBe('MSK');
    expect(element.textContent).toContain('Maersk Line');
  });

  it('says so when the list is empty', () => {
    const { element, respondWith } = setup();
    respondWith([]);
    expect(element.textContent).toContain('Chưa có hãng khai thác nào.');
  });

  it('shows the error and loads again on retry', () => {
    const { http, fixture, element, respondWith } = setup();
    http.expectOne(URL).flush(null, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();
    expect(element.querySelector('[role="alert"]')?.textContent).toContain('Có lỗi xảy ra');

    click(fixture, 'Thử lại');
    respondWith([MAERSK]);
    expect(element.querySelector('[role="alert"]')).toBeNull();
    expect(element.querySelector('td')?.textContent?.trim()).toBe('MSK');
  });
});

describe('LineOperatorsPage create', () => {
  function openFilled(code: string) {
    const page = setup();
    page.respondWith([]);
    click(page.fixture, 'Thêm hãng khai thác');
    type(page.fixture, 'Mã hãng', code);
    type(page.fixture, 'Tên hãng', 'Maersk Line');
    click(page.fixture, 'Lưu');
    return page;
  }

  it('does not submit an invalid form', () => {
    const { http, element } = openFilled('AB');
    expect(element.textContent).toContain('Mã hãng gồm đúng 3 ký tự.');
    http.expectNone(URL);
  });

  it('creates a line operator, closes the dialog and reloads the list', () => {
    const { http, fixture, element, respondWith } = openFilled('MSK');
    const request = http.expectOne((r) => r.method === 'POST');
    expect(request.request.body).toEqual({ code: 'MSK', name: 'Maersk Line' });
    request.flush(MAERSK);
    fixture.detectChanges();
    expect(element.querySelector('dialog')).toBeNull();

    respondWith([MAERSK]);
    expect(element.querySelector('td')?.textContent?.trim()).toBe('MSK');
  });

  it('shows a duplicate code under the code field and keeps the dialog open', () => {
    const { http, fixture, element } = openFilled('MSK');
    const conflict = { title: 'LineOperator.CodeExists' };
    http
      .expectOne((r) => r.method === 'POST')
      .flush(conflict, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();
    expect(element.querySelector('dialog')?.textContent).toContain(
      'Mã hãng khai thác này đã tồn tại.',
    );
  });

  it('shows other create errors in a banner inside the dialog', () => {
    const { http, fixture, element } = openFilled('MSK');
    http
      .expectOne((r) => r.method === 'POST')
      .flush(null, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();
    expect(element.querySelector('dialog [role="alert"]')?.textContent).toContain('Có lỗi xảy ra');
  });
});
