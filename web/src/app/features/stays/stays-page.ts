import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { forkJoin, map, switchMap } from 'rxjs';
import { ListLoader } from '../../core/api/list-loader';
import { Chip } from '../../shared/ui/chip';
import { EmptyState } from '../../shared/ui/empty-state';
import { LoadState } from '../../shared/ui/load-state';
import { PageHeader } from '../../shared/ui/page-header';
import { Segmented, SegmentOption } from '../../shared/ui/segmented';
import { TextField } from '../../shared/ui/text-field';
import { LineOperatorService } from '../catalogs/line-operators/line-operator.service';
import { ContainerService } from '../containers/container.service';
import { YardService } from '../yard/yard.service';
import { loadPlaceIndex } from './place-index';
import { StayRow, toStayRows } from './stay-rows';
import { InYardFilter, StayService } from './stay.service';

const SCOPES: Record<string, InYardFilter> = { all: null, inYard: true, left: false };

@Component({
  selector: 'app-stays-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Chip, EmptyState, LoadState, PageHeader, Segmented, TextField],
  templateUrl: './stays-page.html',
  host: { class: 'flex flex-col gap-6' },
})
export class StaysPage implements OnInit {
  private readonly stays = inject(StayService);
  private readonly containers = inject(ContainerService);
  private readonly operators = inject(LineOperatorService);
  private readonly yard = inject(YardService);

  protected readonly scope = signal('all');
  protected readonly scopeOptions: SegmentOption[] = [
    { value: 'all', label: 'Tất cả' },
    { value: 'inYard', label: 'Đang trong bãi' },
    { value: 'left', label: 'Đã ra' },
  ];
  protected readonly list = new ListLoader<StayRow>(() =>
    this.stays
      .list(SCOPES[this.scope()])
      .pipe(
        switchMap((stays) =>
          forkJoin([
            this.containers.list(),
            this.operators.list(),
            loadPlaceIndex(this.yard, stays),
          ]).pipe(
            map(([containers, operators, places]) =>
              toStayRows(stays, containers, operators, places),
            ),
          ),
        ),
      ),
  );
  protected readonly headings = [
    'Container',
    'Giờ vào',
    'Giờ ra',
    'Vị trí',
    'Tình trạng',
    'Trạng thái',
  ];

  protected readonly searchFilter = new FormControl('', { nonNullable: true });
  private readonly search = toSignal(this.searchFilter.valueChanges, { initialValue: '' });
  protected readonly visible = computed(() => {
    const wanted = this.search().trim().toLowerCase();
    return this.list.items().filter((row) => row.number.toLowerCase().includes(wanted));
  });

  ngOnInit(): void {
    this.list.load();
  }

  protected choose(scope: string): void {
    this.scope.set(scope);
    this.list.load();
  }
}
