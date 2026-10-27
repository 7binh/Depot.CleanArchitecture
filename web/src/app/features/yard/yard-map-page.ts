import { ChangeDetectionStrategy, Component, OnInit, computed, inject } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { map, tap } from 'rxjs';
import { ListLoader } from '../../core/api/list-loader';
import { Card } from '../../shared/ui/card';
import { Chip } from '../../shared/ui/chip';
import { EmptyState } from '../../shared/ui/empty-state';
import { LoadState } from '../../shared/ui/load-state';
import { PageHeader } from '../../shared/ui/page-header';
import { SelectField } from '../../shared/ui/select-field';
import { BlockGrid, toBlockGrid } from './block-grid';
import { Block, YardService } from './yard.service';

@Component({
  selector: 'app-yard-map-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Card, Chip, EmptyState, LoadState, PageHeader, SelectField],
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
  /** A loader holding the one grid of the chosen block, so the map gets the same loading and retry as the lists. */
  protected readonly grid = new ListLoader<BlockGrid>(() =>
    this.yard.map(this.blockId()).pipe(map((blockMap) => [toBlockGrid(blockMap)])),
  );

  protected readonly blockOptions = computed(() =>
    this.blocks
      .items()
      .map((block) => ({ value: block.id, label: `${block.code} - ${block.name}` })),
  );
  protected readonly current = computed(() => this.grid.items()[0]);
  protected readonly columns = computed(
    () => `repeat(${this.current()?.bays.length ?? 0}, minmax(7rem, 1fr))`,
  );

  constructor() {
    this.blockControl.valueChanges.pipe(takeUntilDestroyed()).subscribe((id) => {
      if (id) {
        this.grid.load();
      }
    });
  }

  ngOnInit(): void {
    this.blocks.load();
  }
}
