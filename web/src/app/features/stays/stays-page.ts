import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { debounceTime, map, switchMap, tap } from 'rxjs';
import { ListLoader } from '../../core/api/list-loader';
import { Paged } from '../../core/api/paged';
import { Chip } from '../../shared/ui/chip';
import { EmptyState } from '../../shared/ui/empty-state';
import { LoadState } from '../../shared/ui/load-state';
import { PageHeader } from '../../shared/ui/page-header';
import { Pagination } from '../../shared/ui/pagination';
import { Segmented, SegmentOption } from '../../shared/ui/segmented';
import { TextField } from '../../shared/ui/text-field';
import { YardService } from '../yard/yard.service';
import { loadPlaceIndex } from './place-index';
import { StayRow, toStayRows } from './stay-rows';
import { InYardFilter, StayListItem, StayService } from './stay.service';

const SCOPES: Record<string, InYardFilter> = { all: null, inYard: true, left: false };
const PAGE_SIZE = 20;
/** Typing waits this long for a pause before it asks the API again. */
const FILTER_DELAY_MS = 300;

@Component({
  selector: 'app-stays-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Chip, EmptyState, LoadState, PageHeader, Pagination, Segmented, TextField],
  templateUrl: './stays-page.html',
  host: { class: 'flex flex-col gap-6' },
})
export class StaysPage implements OnInit {
  private readonly stays = inject(StayService);
  private readonly yard = inject(YardService);

  protected readonly scope = signal('all');
  protected readonly scopeOptions: SegmentOption[] = [
    { value: 'all', label: 'Tất cả' },
    { value: 'inYard', label: 'Đang trong bãi' },
    { value: 'left', label: 'Đã ra' },
  ];
  protected readonly searchFilter = new FormControl('', { nonNullable: true });
  protected readonly page = signal(1);
  /** The page the list shows: totals for the pager. Null until the first answer. */
  protected readonly paging = signal<Paged<StayListItem> | null>(null);
  /** Whether the answer on screen was filtered, which decides what an empty list says. */
  protected readonly filtered = signal(false);

  protected readonly list = new ListLoader<StayRow>(() => {
    const number = this.searchFilter.value;
    const inYard = SCOPES[this.scope()];
    return this.stays.search(number, inYard, this.page(), PAGE_SIZE).pipe(
      tap((paged) => {
        this.paging.set(paged);
        this.filtered.set(number.trim() !== '' || inYard !== null);
      }),
      switchMap((paged) =>
        loadPlaceIndex(this.yard, paged.items).pipe(
          map((places) => toStayRows(paged.items, places)),
        ),
      ),
    );
  });
  protected readonly headings = [
    'Container',
    'Giờ vào',
    'Giờ ra',
    'Vị trí',
    'Tình trạng',
    'Trạng thái',
  ];

  constructor() {
    this.searchFilter.valueChanges
      .pipe(debounceTime(FILTER_DELAY_MS), takeUntilDestroyed())
      .subscribe(() => this.goTo(1));
  }

  ngOnInit(): void {
    this.list.load();
  }

  protected choose(scope: string): void {
    this.scope.set(scope);
    this.goTo(1);
  }

  protected goTo(page: number): void {
    this.page.set(page);
    this.list.load(true);
  }
}
