import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { ListLoader } from '../../../core/api/list-loader';
import { Card } from '../../../shared/ui/card';
import { DataTable, TableColumn } from '../../../shared/ui/data-table';
import { EmptyState } from '../../../shared/ui/empty-state';
import { LoadState } from '../../../shared/ui/load-state';
import { PageHeader } from '../../../shared/ui/page-header';
import { ContainerType, ContainerTypeService } from './container-type.service';

@Component({
  selector: 'app-container-types-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Card, DataTable, EmptyState, LoadState, PageHeader],
  templateUrl: './container-types-page.html',
  host: { class: 'flex flex-col gap-6' },
})
export class ContainerTypesPage implements OnInit {
  private readonly service = inject(ContainerTypeService);

  protected readonly list = new ListLoader<ContainerType>(() => this.service.list());
  protected readonly columns: TableColumn<ContainerType>[] = [
    { key: 'code', label: 'Mã loại' },
    { key: 'name', label: 'Tên loại container' },
  ];

  ngOnInit(): void {
    this.list.load();
  }
}
