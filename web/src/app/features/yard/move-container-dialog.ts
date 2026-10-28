import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { map, tap } from 'rxjs';
import { toMessage } from '../../core/api/api-error';
import { ListLoader } from '../../core/api/list-loader';
import { Button } from '../../shared/ui/button';
import { Dialog } from '../../shared/ui/dialog';
import { EmptyState } from '../../shared/ui/empty-state';
import { ErrorBanner } from '../../shared/ui/error-banner';
import { LoadState } from '../../shared/ui/load-state';
import { Segmented } from '../../shared/ui/segmented';
import { SelectField } from '../../shared/ui/select-field';
import { GridCell, tierCount } from './block-grid';
import { MoveCell, toMoveGrid } from './move-targets';
import { Block, BlockMap, MapContainer, MovePosition, YardService } from './yard.service';

/** Fixed widths of the row label column and of a bay column, so a long number never stretches the map. */
const LABEL_WIDTH = '3.5rem';
const BAY_WIDTH = '7rem';

/**
 * Moves a container to another place of the yard: the person picks the block and tier, then clicks a free
 * place on the map. The place is checked again by the server when the move is confirmed.
 */
@Component({
  selector: 'app-move-container-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, Dialog, EmptyState, ErrorBanner, LoadState, Segmented, SelectField],
  templateUrl: './move-container-dialog.html',
})
export class MoveContainerDialog implements OnInit {
  private readonly yard = inject(YardService);
  protected readonly dialog = viewChild.required(Dialog);

  /** The place the container is in now. */
  readonly cell = input.required<GridCell>();
  readonly blockId = input.required<string>();
  readonly blockCode = input.required<string>();
  readonly blocks = input.required<Block[]>();
  readonly closed = output();
  readonly moved = output();

  protected readonly blockControl = new FormControl('', { nonNullable: true });
  protected readonly tier = signal('1');
  protected readonly selected = signal<MoveCell | null>(null);
  protected readonly notice = signal<string | null>(null);
  protected readonly submitting = signal(false);

  /** The container being moved. */
  protected readonly container = computed<MapContainer>(() => {
    const container = this.cell().container;
    if (!container) {
      throw new Error('A free place has no container to move.');
    }
    return container;
  });

  protected readonly blockOptions = computed(() =>
    this.blocks().map((block) => ({ value: block.id, label: `${block.code} - ${block.name}` })),
  );
  /** A loader holding the one map of the chosen block, so the map gets the same loading and retry as the lists. */
  protected readonly mapLoader = new ListLoader<BlockMap>(() =>
    this.yard.map(this.blockControl.value).pipe(
      tap(() => this.reset('1')),
      map((blockMap) => [blockMap]),
    ),
  );
  protected readonly current = computed(() => this.mapLoader.items()[0]);
  protected readonly tierOptions = computed(() => {
    const blockMap = this.current();
    const count = blockMap ? tierCount(blockMap) : 0;
    return Array.from({ length: count }, (_, i) => ({ value: `${i + 1}`, label: `Tầng ${i + 1}` }));
  });
  protected readonly grid = computed(() => {
    const blockMap = this.current();
    return blockMap && !blockMap.isVirtual
      ? toMoveGrid(blockMap, Number(this.tier()), this.container())
      : undefined;
  });
  protected readonly template = computed(
    () => `${LABEL_WIDTH} repeat(${this.grid()?.bays.length ?? 0}, ${BAY_WIDTH})`,
  );

  protected readonly from = computed(() => {
    const cell = this.cell();
    return `${this.blockCode()}, bay ${cell.bayNo}, row ${cell.rowNo}, tầng ${cell.tier}`;
  });
  /** Where the container would go; null while nothing is chosen. A virtual block needs no place. */
  protected readonly destination = computed(() => {
    const blockMap = this.current();
    if (!blockMap) {
      return null;
    }
    if (blockMap.isVirtual) {
      return `${blockMap.code} (bãi ảo, không có bay, row, tầng)`;
    }
    const cell = this.selected();
    return cell
      ? `${blockMap.code}, bay ${cell.bayNo}, row ${cell.rowNo}, tầng ${cell.tier}`
      : null;
  });

  constructor() {
    this.blockControl.valueChanges.pipe(takeUntilDestroyed()).subscribe((id) => {
      if (id) {
        this.mapLoader.load();
      }
    });
  }

  ngOnInit(): void {
    this.blockControl.setValue(this.blockId());
  }

  protected isSelected(cell: MoveCell): boolean {
    return this.selected()?.key === cell.key;
  }

  protected choose(cell: MoveCell): void {
    this.notice.set(null);
    this.selected.set(cell);
  }

  protected chooseTier(tier: string): void {
    this.reset(tier);
  }

  protected confirm(): void {
    const position = this.position();
    if (!position) {
      return;
    }
    this.notice.set(null);
    this.submitting.set(true);
    this.yard.move(this.container().stayId, position).subscribe({
      next: () => {
        this.submitting.set(false);
        this.moved.emit();
      },
      error: (error: unknown) => {
        this.submitting.set(false);
        this.notice.set(toMessage(error));
      },
    });
  }

  private position(): MovePosition | null {
    const blockMap = this.current();
    if (!blockMap) {
      return null;
    }
    if (blockMap.isVirtual) {
      return { blockId: blockMap.blockId, rowId: null, tier: null };
    }
    const cell = this.selected();
    return cell ? { blockId: blockMap.blockId, rowId: cell.rowId, tier: cell.tier } : null;
  }

  private reset(tier: string): void {
    this.tier.set(tier);
    this.selected.set(null);
    this.notice.set(null);
  }
}
