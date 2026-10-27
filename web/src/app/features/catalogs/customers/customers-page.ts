import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ListLoader } from '../../../core/api/list-loader';
import { Button } from '../../../shared/ui/button';
import { DataTable, TableColumn } from '../../../shared/ui/data-table';
import { EmptyState } from '../../../shared/ui/empty-state';
import { Icon } from '../../../shared/ui/icon';
import { LoadState } from '../../../shared/ui/load-state';
import { PageHeader } from '../../../shared/ui/page-header';
import { CreateCustomerDialog } from './create-customer-dialog';
import { Customer, CustomerService } from './customer.service';

@Component({
  selector: 'app-customers-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, CreateCustomerDialog, DataTable, EmptyState, Icon, LoadState, PageHeader],
  templateUrl: './customers-page.html',
  host: { class: 'flex flex-col gap-6' },
})
export class CustomersPage implements OnInit {
  private readonly service = inject(CustomerService);

  protected readonly list = new ListLoader<Customer>(() => this.service.list());
  protected readonly columns: TableColumn<Customer>[] = [
    { key: 'taxCode', label: 'Mã số thuế' },
    { key: 'name', label: 'Tên khách hàng' },
  ];
  protected readonly creating = signal(false);

  ngOnInit(): void {
    this.list.load();
  }
}
