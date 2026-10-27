import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { map } from 'rxjs';
import { toMessage } from '../../core/api/api-error';
import { ListLoader, worstStatus } from '../../core/api/list-loader';
import { Button } from '../../shared/ui/button';
import { Chip } from '../../shared/ui/chip';
import { ConfirmDialog } from '../../shared/ui/confirm-dialog';
import { EmptyState } from '../../shared/ui/empty-state';
import { Icon } from '../../shared/ui/icon';
import { LoadState } from '../../shared/ui/load-state';
import { PageHeader } from '../../shared/ui/page-header';
import { Bay, Block, YardRow, YardService, baySize } from '../yard/yard.service';
import { LayoutDialog } from './layout-dialog';
import { RowTierDialog } from './row-tier-dialog';
import { LayoutResult, YardStructureService } from './yard-structure.service';

type Removal = { kind: 'bay'; bay: Bay } | { kind: 'row'; row: YardRow };

/** The bays of one real block and the rows of each: create them in bulk, change a row's tiers, remove them. */
@Component({
  selector: 'app-block-structure-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Button,
    Chip,
    ConfirmDialog,
    EmptyState,
    Icon,
    LayoutDialog,
    LoadState,
    PageHeader,
    RouterLink,
    RowTierDialog,
  ],
  templateUrl: './block-structure-page.html',
  host: { class: 'flex flex-col gap-6' },
})
export class BlockStructurePage implements OnInit {
  private readonly yard = inject(YardService);
  private readonly structure = inject(YardStructureService);
  protected readonly blockId = inject(ActivatedRoute).snapshot.paramMap.get('id') ?? '';

  private readonly found = new ListLoader<Block>(() =>
    this.yard.blocks().pipe(map((blocks) => blocks.filter((b) => b.id === this.blockId))),
  );
  protected readonly bays = new ListLoader<Bay>(() =>
    this.yard.bays(this.blockId).pipe(map((bays) => [...bays].sort((a, b) => a.bayNo - b.bayNo))),
  );
  protected readonly rows = new ListLoader<YardRow>(() =>
    this.yard
      .rows(this.blockId, this.expanded() ?? '')
      .pipe(map((rows) => [...rows].sort((a, b) => a.rowNo - b.rowNo))),
  );

  protected readonly block = computed(() => this.found.items().at(0));
  protected readonly status = computed(() =>
    worstStatus([this.found.status(), this.bays.status()]),
  );
  protected readonly failure = computed(() => this.found.failure() || this.bays.failure());
  protected readonly size = baySize;

  protected readonly expanded = signal<string | null>(null);
  protected readonly layouting = signal(false);
  protected readonly layoutDone = signal<string | null>(null);
  protected readonly tierRow = signal<YardRow | null>(null);
  protected readonly removal = signal<Removal | null>(null);
  protected readonly removeNotice = signal<string | null>(null);
  protected readonly removeBusy = signal(false);
  protected readonly removeMessage = computed(() => {
    const removal = this.removal();
    if (removal?.kind === 'bay') {
      return `Xóa bay ${removal.bay.bayNo} cùng các row của nó? Không thể hoàn tác.`;
    }
    return removal ? `Xóa row ${removal.row.rowNo}? Không thể hoàn tác.` : '';
  });

  ngOnInit(): void {
    this.found.load();
    this.bays.load();
  }

  protected toggle(bay: Bay): void {
    if (this.expanded() === bay.id) {
      this.expanded.set(null);
      return;
    }
    this.expanded.set(bay.id);
    this.rows.load();
  }

  protected built(result: LayoutResult): void {
    this.layoutDone.set(`Đã tạo ${result.baysCreated} bay và ${result.rowsCreated} row mới.`);
    this.refresh();
  }

  protected askRemove(removal: Removal): void {
    this.removeNotice.set(null);
    this.removal.set(removal);
  }

  protected remove(removal: Removal): void {
    this.removeBusy.set(true);
    const request =
      removal.kind === 'bay'
        ? this.structure.deleteBay(this.blockId, removal.bay.id)
        : this.structure.deleteRow(this.blockId, removal.row);
    request.subscribe({
      next: () => {
        this.removeBusy.set(false);
        this.removal.set(null);
        if (removal.kind === 'bay' && this.expanded() === removal.bay.id) {
          this.expanded.set(null);
        }
        this.refresh();
      },
      error: (error: unknown) => {
        this.removeBusy.set(false);
        this.removeNotice.set(toMessage(error));
      },
    });
  }

  protected refresh(): void {
    this.bays.load(true);
    if (this.expanded()) {
      this.rows.load(true);
    }
  }
}
