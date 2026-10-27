import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { click, clickInDialog, type } from '../../../testing/dom';
import { stubDialog } from '../../../testing/dialog';
import { BlockStructurePage } from './block-structure-page';

const API = 'http://localhost:5049/api/blocks';
const BLOCKS = [
  { id: 'b1', code: 'A', name: 'Khu A', isVirtual: false },
  { id: 'v1', code: 'V', name: 'Bãi ảo', isVirtual: true },
];
const BAYS = [
  { id: 'y2', blockId: 'b1', bayNo: 2 },
  { id: 'y1', blockId: 'b1', bayNo: 1 },
];
const ROWS = [{ id: 'r1', bayId: 'y1', rowNo: 1, maxTier: 3 }];
const CONFLICT = { status: 409, statusText: 'Conflict' };

function setup(blockId = 'b1') {
  stubDialog();
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      provideRouter([]),
      {
        provide: ActivatedRoute,
        useValue: { snapshot: { paramMap: convertToParamMap({ id: blockId }) } },
      },
    ],
  });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(BlockStructurePage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  http.expectOne(API).flush(BLOCKS);
  http.expectOne(`${API}/${blockId}/bays`).flush(BAYS);
  fixture.detectChanges();
  return { http, fixture, element };
}

function withRows() {
  const page = setup();
  click(page.fixture, 'Xem row');
  page.http.expectOne(`${API}/b1/bays/y1/rows`).flush(ROWS);
  page.fixture.detectChanges();
  return page;
}

describe('BlockStructurePage', () => {
  it('lists the bays in order with their size', () => {
    const { element } = setup();
    const text = element.textContent ?? '';
    expect(text.indexOf('Bay 1')).toBeLessThan(text.indexOf('Bay 2'));
    expect(text).toContain('20 ft');
    expect(text).toContain('40 ft');
  });

  it('shows the rows of a bay when it is opened', () => {
    const { element } = withRows();
    expect(element.textContent).toContain('Row 1');
    expect(element.textContent).toContain('Tối đa 3 tầng');
  });

  it('says a virtual block has no bays', () => {
    const { element } = setup('v1');
    expect(element.textContent).toContain('block ảo không có bay');
  });
});

describe('BlockStructurePage creating bays and rows', () => {
  function fill(page: ReturnType<typeof setup>, from: string, to: string) {
    click(page.fixture, 'Tạo bay và row');
    type(page.fixture, 'Từ bay', from);
    type(page.fixture, 'Đến bay', to);
    type(page.fixture, 'Số row mỗi bay', '2');
    clickInDialog(page.fixture, 'Tạo');
  }

  it('sends the range and says how many were created', () => {
    const page = setup();
    fill(page, '1', '4');
    const request = page.http.expectOne((r) => r.method === 'POST');
    expect(request.request.url).toBe(`${API}/b1/layout`);
    expect(request.request.body).toEqual({ fromBay: 1, toBay: 4, rowCount: 2, maxTier: 3 });
    request.flush({ baysCreated: 2, rowsCreated: 4 });
    page.fixture.detectChanges();
    page.http.expectOne(`${API}/b1/bays`).flush(BAYS);
    page.fixture.detectChanges();
    expect(page.element.textContent).toContain('Đã tạo 2 bay và 4 row mới.');
  });

  it('does not send a range that ends before it starts', () => {
    const page = setup();
    fill(page, '5', '3');
    page.http.expectNone((r) => r.method === 'POST');
    expect(page.element.querySelector('dialog')?.textContent).toContain(
      'Bay kết thúc không được nhỏ hơn bay bắt đầu.',
    );
  });
});

describe('BlockStructurePage changing and removing', () => {
  it('changes the tiers of a row', () => {
    const page = withRows();
    click(page.fixture, 'Đổi số tầng');
    type(page.fixture, 'Số tầng tối đa', '4');
    clickInDialog(page.fixture, 'Lưu');
    const request = page.http.expectOne((r) => r.method === 'PUT');
    expect(request.request.url).toBe(`${API}/b1/bays/y1/rows/r1`);
    expect(request.request.body).toEqual({ maxTier: 4 });
  });

  it('says why the tiers cannot go lower', () => {
    const page = withRows();
    click(page.fixture, 'Đổi số tầng');
    type(page.fixture, 'Số tầng tối đa', '2');
    clickInDialog(page.fixture, 'Lưu');
    page.http
      .expectOne((r) => r.method === 'PUT')
      .flush({ title: 'MaxTier.BelowOccupied' }, CONFLICT);
    page.fixture.detectChanges();
    expect(page.element.querySelector('dialog')?.textContent).toContain(
      'Đang có container ở tầng cao hơn',
    );
  });

  it('asks before deleting a row, then deletes and reloads', () => {
    const page = withRows();
    click(page.fixture, 'Xóa row');
    page.http.expectNone((r) => r.method === 'DELETE');
    clickInDialog(page.fixture, 'Xóa');
    page.http.expectOne((r) => r.method === 'DELETE').flush(null);
    page.fixture.detectChanges();
    page.http.expectOne(`${API}/b1/bays`).flush(BAYS);
    page.http.expectOne(`${API}/b1/bays/y1/rows`).flush([]);
    page.fixture.detectChanges();
    expect(page.element.querySelector('dialog')).toBeNull();
  });

  it('says why a bay with containers stays', () => {
    const { http, fixture, element } = setup();
    click(fixture, 'Xóa bay');
    clickInDialog(fixture, 'Xóa');
    http.expectOne((r) => r.method === 'DELETE').flush({ title: 'Bay.HasContainers' }, CONFLICT);
    fixture.detectChanges();
    expect(element.querySelector('dialog')?.textContent).toContain(
      'Bay đang có container, chưa xóa được.',
    );
  });
});
