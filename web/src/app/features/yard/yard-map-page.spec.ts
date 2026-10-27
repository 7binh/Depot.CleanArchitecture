import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { YardMapPage } from './yard-map-page';

const API = 'http://localhost:5049/api';
const BLOCKS = [
  { id: 'b1', code: 'B1', name: 'Block 1', isVirtual: false },
  { id: 'v1', code: 'V1', name: 'Bãi ảo', isVirtual: true },
];
const BOX = {
  stayId: 's1',
  number: 'CSQU3054383',
  sizeFeet: 40,
  operatorCode: 'CSQ',
  daysInYard: 12,
  isLongStay: true,
  isDamaged: false,
};
const REAL_MAP = {
  blockId: 'b1',
  code: 'B1',
  name: 'Block 1',
  isVirtual: false,
  unplaced: [],
  bays: [
    {
      bayNo: 2,
      sizeFeet: 40,
      rows: [{ rowNo: 1, maxTier: 2, tiers: [{ tier: 1, fromBay: 1, toBay: 3, container: BOX }] }],
    },
  ],
};
const VIRTUAL_MAP = {
  blockId: 'v1',
  code: 'V1',
  name: 'Bãi ảo',
  isVirtual: true,
  bays: [],
  unplaced: [BOX],
};

function setup() {
  TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
  const http = TestBed.inject(HttpTestingController);
  const fixture = TestBed.createComponent(YardMapPage);
  const element = fixture.nativeElement as HTMLElement;
  fixture.detectChanges();
  http.expectOne(`${API}/blocks`).flush(BLOCKS);
  fixture.detectChanges();
  return { http, fixture, element };
}

describe('YardMapPage', () => {
  it('draws the first block as soon as the blocks arrive', () => {
    const { http, fixture, element } = setup();
    http.expectOne(`${API}/blocks/b1/map`).flush(REAL_MAP);
    fixture.detectChanges();
    expect(element.textContent).toContain('Bay 2');
    expect(element.textContent).toContain('Row 1');
    expect(element.textContent).toContain('CSQU3054383');
    expect(element.textContent).toContain('CSQ, 12 ngày');
    expect(element.textContent).toContain('Tồn lâu');
  });

  it('lists the containers of a virtual block that have no position', () => {
    const { http, fixture, element } = setup();
    http.expectOne(`${API}/blocks/b1/map`).flush(REAL_MAP);
    const select = element.querySelector('select') as HTMLSelectElement;
    select.value = 'v1';
    select.dispatchEvent(new Event('change'));
    http.expectOne(`${API}/blocks/v1/map`).flush(VIRTUAL_MAP);
    fixture.detectChanges();
    expect(element.textContent).toContain('Chưa có vị trí');
    expect(element.textContent).toContain('CSQU3054383');
    expect(element.textContent).not.toContain('Row 1');
  });

  it('says so when the yard has no block', () => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    const http = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(YardMapPage);
    fixture.detectChanges();
    http.expectOne(`${API}/blocks`).flush([]);
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain(
      'Chưa có block nào trong bãi.',
    );
  });
});
