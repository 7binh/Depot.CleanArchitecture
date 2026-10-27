import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ListLoader } from '../../../core/api/list-loader';
import { Button } from '../../../shared/ui/button';
import { Card } from '../../../shared/ui/card';
import { DataTable, TableColumn } from '../../../shared/ui/data-table';
import { EmptyState } from '../../../shared/ui/empty-state';
import { Icon } from '../../../shared/ui/icon';
import { LoadState } from '../../../shared/ui/load-state';
import { PageHeader } from '../../../shared/ui/page-header';
import { CreateLineOperatorDialog } from './create-line-operator-dialog';
import { LineOperator, LineOperatorService } from './line-operator.service';

@Component({
  selector: 'app-line-operators-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Card,
    Button,
    CreateLineOperatorDialog,
    DataTable,
    EmptyState,
    Icon,
    LoadState,
    PageHeader,
  ],
  templateUrl: './line-operators-page.html',
  host: { class: 'flex flex-col gap-6' },
})
export class LineOperatorsPage implements OnInit {
  private readonly service = inject(LineOperatorService);

  protected readonly list = new ListLoader<LineOperator>(() => this.service.list());
  protected readonly columns: TableColumn<LineOperator>[] = [
    { key: 'code', label: 'Mã hãng' },
    { key: 'name', label: 'Tên hãng' },
  ];
  protected readonly creating = signal(false);

  ngOnInit(): void {
    this.list.load();
  }
}
