import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { toMessage } from '../../core/api/api-error';
import { ListLoader } from '../../core/api/list-loader';
import { Button } from '../../shared/ui/button';
import { Chip } from '../../shared/ui/chip';
import { ConfirmDialog } from '../../shared/ui/confirm-dialog';
import { EmptyState } from '../../shared/ui/empty-state';
import { Icon } from '../../shared/ui/icon';
import { LoadState } from '../../shared/ui/load-state';
import { PageHeader } from '../../shared/ui/page-header';
import { Block, YardService } from '../yard/yard.service';
import { BlockFormDialog } from './block-form-dialog';
import { YardStructureService } from './yard-structure.service';

/** One cell of a table row: no divider, the row turns into a card on hover (Design.md). */
export const CELL_CLASS =
  'h-12 border-y border-transparent px-4 text-body whitespace-nowrap first:rounded-l-card first:border-l last:rounded-r-card last:border-r group-hover:border-line-card group-hover:bg-surface';

@Component({
  selector: 'app-yard-structure-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    BlockFormDialog,
    Button,
    Chip,
    ConfirmDialog,
    EmptyState,
    Icon,
    LoadState,
    PageHeader,
    RouterLink,
  ],
  templateUrl: './yard-structure-page.html',
  host: { class: 'flex flex-col gap-6' },
})
export class YardStructurePage implements OnInit {
  private readonly yard = inject(YardService);
  private readonly structure = inject(YardStructureService);

  protected readonly list = new ListLoader<Block>(() => this.yard.blocks());
  protected readonly headings = ['Mã', 'Tên', 'Loại'];
  protected readonly cell = CELL_CLASS;

  protected readonly creating = signal(false);
  protected readonly renaming = signal<Block | null>(null);
  protected readonly removing = signal<Block | null>(null);
  protected readonly removeNotice = signal<string | null>(null);
  protected readonly removeBusy = signal(false);

  ngOnInit(): void {
    this.list.load();
  }

  protected askRemove(block: Block): void {
    this.removeNotice.set(null);
    this.removing.set(block);
  }

  protected remove(block: Block): void {
    this.removeBusy.set(true);
    this.structure.deleteBlock(block.id).subscribe({
      next: () => {
        this.removeBusy.set(false);
        this.removing.set(null);
        this.list.load(true);
      },
      error: (error: unknown) => {
        this.removeBusy.set(false);
        this.removeNotice.set(toMessage(error));
      },
    });
  }
}
