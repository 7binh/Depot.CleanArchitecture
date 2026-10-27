import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { click, clickInDialog, type } from '../../../testing/dom';
import { stubDialog } from '../../../testing/dialog';
import { YardStructurePage } from './yard-structure-page';

const URL = 'http://localhost:5049/api/blocks';
const BLOCKS = [
  { id: 'b1', code: 'A', name: 'Khu A', isVirtual: false },
  { id: 'v1', code: 'V', name: 'Bãi ảo', isVirtual: true },
];
const CONFLICT = { status: 409, statusText: 'Conflict' };

function setup() {
  stubDialog();
  TestBed.configureTestingModule({
    providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
  });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(YardStructurePage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  http.expectOne(URL).flush(BLOCKS);
  fixture.detectChanges();
  return { http, fixture, element };
}

describe('YardStructurePage', () => {
  it('lists the blocks and links only the real one to its bays', () => {
    const { element } = setup();
    expect(element.textContent).toContain('Khu A');
    expect(element.textContent).toContain('Block thật');
    expect(element.textContent).toContain('Block ảo');
    expect(element.querySelectorAll('a[href="/cau-truc-bai/b1"]').length).toBe(1);
    expect(element.querySelectorAll('a[href="/cau-truc-bai/v1"]').length).toBe(0);
  });

  it('adds a block and reloads the list', () => {
    const { http, fixture, element } = setup();
    click(fixture, 'Thêm block');
    type(fixture, 'Mã block', 'B');
    type(fixture, 'Tên block', 'Khu B');
    clickInDialog(fixture, 'Lưu');
    const request = http.expectOne((r) => r.method === 'POST');
    expect(request.request.body).toEqual({ code: 'B', name: 'Khu B', isVirtual: false });
    request.flush({ id: 'b2', code: 'B', name: 'Khu B', isVirtual: false });
    fixture.detectChanges();
    http.expectOne(URL).flush(BLOCKS);
    fixture.detectChanges();
    expect(element.querySelector('dialog')).toBeNull();
  });

  it('shows a taken code under the field', () => {
    const { http, fixture, element } = setup();
    click(fixture, 'Thêm block');
    type(fixture, 'Mã block', 'A');
    type(fixture, 'Tên block', 'Khu khác');
    clickInDialog(fixture, 'Lưu');
    http.expectOne((r) => r.method === 'POST').flush({ title: 'Block.CodeExists' }, CONFLICT);
    fixture.detectChanges();
    expect(element.querySelector('dialog')?.textContent).toContain('Mã block này đã tồn tại.');
  });

  it('renames a block with its name filled in', () => {
    const { http, fixture, element } = setup();
    click(fixture, 'Đổi tên');
    const input = element.querySelector('dialog input') as HTMLInputElement;
    expect(input.value).toBe('Khu A');
    type(fixture, 'Tên block', 'Khu A mới');
    clickInDialog(fixture, 'Lưu');
    const request = http.expectOne((r) => r.method === 'PUT');
    expect(request.request.url).toBe(`${URL}/b1`);
    expect(request.request.body).toEqual({ name: 'Khu A mới' });
  });
});

describe('YardStructurePage removing a block', () => {
  it('asks first, then deletes and reloads', () => {
    const { http, fixture, element } = setup();
    click(fixture, 'Xóa');
    expect(element.querySelector('dialog')?.textContent).toContain('Xóa block A');
    http.expectNone((r) => r.method === 'DELETE');
    clickInDialog(fixture, 'Xóa');
    http.expectOne((r) => r.method === 'DELETE' && r.url === `${URL}/b1`).flush(null);
    fixture.detectChanges();
    http.expectOne(URL).flush([BLOCKS[1]]);
    fixture.detectChanges();
    expect(element.querySelector('dialog')).toBeNull();
    expect(element.textContent).not.toContain('Khu A');
  });

  it('says why a block that held containers stays', () => {
    const { http, fixture, element } = setup();
    click(fixture, 'Xóa');
    clickInDialog(fixture, 'Xóa');
    http.expectOne((r) => r.method === 'DELETE').flush({ title: 'Block.InUse' }, CONFLICT);
    fixture.detectChanges();
    expect(element.querySelector('dialog')?.textContent).toContain(
      'Block này đã từng chứa container nên không xóa được.',
    );
  });
});
