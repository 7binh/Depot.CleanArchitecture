import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { Router } from '@angular/router';
import { map, tap } from 'rxjs';
import { ListLoader } from '../../core/api/list-loader';
import { Card } from '../../shared/ui/card';
import { Chip } from '../../shared/ui/chip';
import { EmptyState } from '../../shared/ui/empty-state';
import { Icon } from '../../shared/ui/icon';
import { LoadState } from '../../shared/ui/load-state';
import { PageHeader } from '../../shared/ui/page-header';
import { Segmented } from '../../shared/ui/segmented';
import { SelectField } from '../../shared/ui/select-field';
import { GridCell, tierCount, toTierGrid } from './block-grid';
import { ContainerDetail } from './container-detail';
import { Block, BlockMap, YardService } from './yard.service';

/** Fixed widths of the row label column and of a bay column, so a long number never stretches the map. */
const LABEL_WIDTH = '3.5rem';
const BAY_WIDTH = '8.75rem';

@Component({
  selector: 'app-yard-map-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Card,
    Chip,
    ContainerDetail,
    EmptyState,
    Icon,
    LoadState,
    PageHeader,
    Segmented,
    SelectField,
  ],
  templateUrl: './yard-map-page.html',
  host: { class: 'flex flex-col gap-6' },
})
export class YardMapPage implements OnInit {
  private readonly yard = inject(YardService);
  private readonly router = inject(Router);

  protected readonly blockControl = new FormControl('', { nonNullable: true });
  private readonly blockId = toSignal(this.blockControl.valueChanges, { initialValue: '' });

  protected readonly blocks = new ListLoader<Block>(() =>
    this.yard.blocks().pipe(tap((blocks) => this.blockControl.setValue(blocks.at(0)?.id ?? ''))),
  );
  /** A loader holding the one map of the chosen block, so the map gets the same loading and retry as the lists. */
  protected readonly mapLoader = new ListLoader<BlockMap>(() =>
    this.yard.map(this.blockId()).pipe(
      tap(() => {
        this.tier.set('1');
        this.selectedId.set(null);
      }),
      map((blockMap) => [blockMap]),
    ),
  );

  /** The tier on show, as text because the segmented control holds text. */
  protected readonly tier = signal('1');

  protected readonly blockOptions = computed(() =>
    this.blocks
      .items()
      .map((block) => ({ value: block.id, label: `${block.code} - ${block.name}` })),
  );
  protected readonly current = computed(() => this.mapLoader.items()[0]);
  protected readonly tierOptions = computed(() => {
    const map = this.current();
    const count = map ? tierCount(map) : 0;
    return Array.from({ length: count }, (_, i) => ({
      value: `${i + 1}`,
      label: `Tầng ${i + 1}`,
    }));
  });
  protected readonly grid = computed(() => {
    const map = this.current();
    return map ? toTierGrid(map, Number(this.tier())) : undefined;
  });
  protected readonly template = computed(
    () => `${LABEL_WIDTH} repeat(${this.grid()?.bays.length ?? 0}, ${BAY_WIDTH})`,
  );

  /** The container whose card is open; the card shows only while that container is on the tier on view. */
  private readonly selectedId = signal<string | null>(null);
  protected readonly selected = computed(() => {
    const id = this.selectedId();
    const cells = this.grid()?.rows.flatMap((row) => row.cells) ?? [];
    return cells.find((cell) => id !== null && cell.container?.stayId === id);
  });

  constructor() {
    this.blockControl.valueChanges.pipe(takeUntilDestroyed()).subscribe((id) => {
      if (id) {
        this.mapLoader.load();
      }
    });
  }

  protected isSelected(cell: GridCell): boolean {
    return cell.container !== null && cell.container.stayId === this.selectedId();
  }

  /** A container opens its card; a free place opens Gate In with that place filled in. */
  protected select(cell: GridCell): void {
    if (cell.container) {
      this.selectedId.set(cell.container.stayId);
      return;
    }
    void this.router.navigate(['/gate-in'], {
      queryParams: {
        block: this.current()?.blockId,
        bay: cell.bayNo,
        row: cell.rowNo,
        tier: cell.tier,
      },
    });
  }

  protected close(): void {
    this.selectedId.set(null);
  }

  ngOnInit(): void {
    this.blocks.load();
  }
}
