import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ContainerTypesPage } from './container-types-page';

const URL = 'http://localhost:5049/api/container-types';

describe('ContainerTypesPage', () => {
  let fixture: ComponentFixture<ContainerTypesPage>;
  let element: HTMLElement;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(ContainerTypesPage);
    element = fixture.nativeElement as HTMLElement;
    fixture.detectChanges();
  });

  afterEach(() => http.verify());

  it('lists the container types and has no add button', () => {
    http.expectOne(URL).flush([{ id: '1', code: '20GP', name: 'Container 20 feet' }]);
    fixture.detectChanges();
    expect(element.querySelector('td')?.textContent?.trim()).toBe('20GP');
    expect(element.textContent).not.toContain('Thêm');
  });

  it('says so when there are no types', () => {
    http.expectOne(URL).flush([]);
    fixture.detectChanges();
    expect(element.textContent).toContain('Chưa có loại container nào.');
  });

  it('shows the error and offers a retry', () => {
    http.expectOne(URL).flush(null, { status: 0, statusText: 'Unknown Error' });
    fixture.detectChanges();
    expect(element.querySelector('[role="alert"]')?.textContent).toContain(
      'Không kết nối được máy chủ',
    );
    expect(element.textContent).toContain('Thử lại');
  });
});
