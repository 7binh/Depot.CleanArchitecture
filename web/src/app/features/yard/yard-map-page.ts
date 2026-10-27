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
import { tierCount, toTierGrid } from './block-grid';
import { Block, BlockMap, YardService } from './yard.service';

/** Width of the row label column and the least width of a bay column. */
const LABEL_WIDTH = '5rem';
const BAY_WIDTH = 'minmax(7rem, 1fr)';

@Component({
  selector: 'app-yard-map-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Card, Chip, EmptyState, Icon, LoadState, PageHeader, Segmented, SelectField],
  templateUrl: './yard-map-page.html',
  host: { class: 'flex flex-col gap-6' },
})
export class YardMapPage implements OnInit {
  private readonly yard = inject(YardService);

  protected readonly blockControl = new FormControl('', { nonNullable: true });
  private readonly blockId = toSignal(this.blockControl.valueChanges, { initialValue: '' });

  protected readonly blocks = new ListLoader<Block>(() =>
    this.yard.blocks().pipe(tap((blocks) => this.blockControl.setValue(blocks.at(0)?.id ?? ''))),
  );
  /** A loader holding the one map of the chosen block, so the map gets the same loading and retry as the lists. */
  protected readonly mapLoader = new ListLoader<BlockMap>(() =>
    this.yard.map(this.blockId()).pipe(
      tap(() => this.tier.set('1')),
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

  constructor() {
    this.blockControl.valueChanges.pipe(takeUntilDestroyed()).subscribe((id) => {
      if (id) {
        this.mapLoader.load();
      }
    });
  }

  ngOnInit(): void {
    this.blocks.load();
  }
}
