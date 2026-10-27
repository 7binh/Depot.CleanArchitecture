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
    { bayNo: 1, sizeFeet: 20, rows: [{ rowNo: 1, maxTier: 2, tiers: [] }] },
    {
      bayNo: 2,
      sizeFeet: 40,
      rows: [{ rowNo: 1, maxTier: 2, tiers: [{ tier: 1, fromBay: 1, toBay: 3, container: BOX }] }],
    },
    { bayNo: 3, sizeFeet: 20, rows: [{ rowNo: 1, maxTier: 2, tiers: [] }] },
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
  it('draws tier 1 of the first block as soon as the blocks arrive', () => {
    const { http, fixture, element } = setup();
    http.expectOne(`${API}/blocks/b1/map`).flush(REAL_MAP);
    fixture.detectChanges();
    expect(element.textContent).toContain('Bay 1');
    expect(element.textContent).toContain('Bay 3');
    expect(element.textContent).not.toContain('Bay 2');
    expect(element.textContent).toContain('Row 1');
    expect(element.textContent).toContain('CSQU3054383');
    expect(element.textContent).toContain('CSQ, 12 ngày');
    expect(element.textContent).toContain('Tồn lâu');
  });

  it('shows the free places of another tier and says a container is below', () => {
    const { http, fixture, element } = setup();
    http.expectOne(`${API}/blocks/b1/map`).flush(REAL_MAP);
    fixture.detectChanges();
    expect(element.querySelector('[title$="ở tầng dưới"]')).toBeNull();
    const tabs = [...element.querySelectorAll('app-segmented button')] as HTMLButtonElement[];
    expect(tabs.map((tab) => tab.textContent?.trim())).toEqual(['Tầng 1', 'Tầng 2']);
    tabs[1].click();
    fixture.detectChanges();
    expect(element.textContent).not.toContain('CSQU3054383');
    expect(element.textContent).toContain('Trống');
    expect(element.querySelector('[title="1 container ở tầng dưới"]')).not.toBeNull();
  });

  it('says a container stands above a place of tier 1', () => {
    const { http, fixture, element } = setup();
    http.expectOne(`${API}/blocks/b1/map`).flush({
      ...REAL_MAP,
      bays: [
        {
          bayNo: 1,
          sizeFeet: 20,
          rows: [
            { rowNo: 1, maxTier: 2, tiers: [{ tier: 2, fromBay: 1, toBay: 1, container: BOX }] },
          ],
        },
      ],
    });
    fixture.detectChanges();
    expect(element.querySelector('[title="1 container ở tầng trên"]')).not.toBeNull();
  });
});

function drawn() {
  const view = setup();
  view.http.expectOne(`${API}/blocks/b1/map`).flush(REAL_MAP);
  view.fixture.detectChanges();
  const boxes = [...view.element.querySelectorAll('button')] as HTMLButtonElement[];
  const cell = boxes.find((button) => button.textContent?.includes('CSQU3054383'));
  return { ...view, cell: cell as HTMLButtonElement };
}

describe('YardMapPage container detail', () => {
  it('opens a card with the container and its place when its cell is clicked', () => {
    const { fixture, element, cell } = drawn();
    expect(element.querySelector('app-container-detail')).toBeNull();
    cell.click();
    fixture.detectChanges();
    const card = element.querySelector('app-container-detail') as HTMLElement;
    expect(card.textContent).toContain('CSQU3054383');
    expect(card.textContent).toContain('Tồn lâu');
    expect(card.textContent).toContain('12 ngày');
    expect(cell.getAttribute('aria-pressed')).toBe('true');
  });

  it('does not let a free place be clicked', () => {
    const { fixture, element } = drawn();
    const tabs = [...element.querySelectorAll('app-segmented button')] as HTMLButtonElement[];
    tabs[1].click();
    fixture.detectChanges();
    const free = [...element.querySelectorAll('button')].filter((b) =>
      b.textContent?.includes('Trống'),
    );
    expect(free.length).toBeGreaterThan(0);
    expect(free.every((button) => button.disabled)).toBe(true);
  });
});

describe('YardMapPage closing the drawer', () => {
  it('closes the card from its close button and when the tier changes', () => {
    const { fixture, element, cell } = drawn();
    cell.click();
    fixture.detectChanges();
    (element.querySelector('[aria-label="Đóng"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(element.querySelector('app-container-detail')).toBeNull();
    cell.click();
    fixture.detectChanges();
    const tabs = [...element.querySelectorAll('app-segmented button')] as HTMLButtonElement[];
    tabs[1].click();
    fixture.detectChanges();
    expect(element.querySelector('app-container-detail')).toBeNull();
  });

  it('closes the drawer when the dimmed area behind it is clicked', () => {
    const { fixture, element, cell } = drawn();
    cell.click();
    fixture.detectChanges();
    (element.querySelector('[aria-label="Đóng chi tiết"]') as HTMLButtonElement).click();
    fixture.detectChanges();
    expect(element.querySelector('app-container-detail')).toBeNull();
  });

  it('closes the drawer on Escape', () => {
    const { fixture, element, cell } = drawn();
    cell.click();
    fixture.detectChanges();
    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape' }));
    fixture.detectChanges();
    expect(element.querySelector('app-container-detail')).toBeNull();
  });
});

describe('YardMapPage other blocks', () => {
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
