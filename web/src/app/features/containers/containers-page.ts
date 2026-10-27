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
import { forkJoin, map } from 'rxjs';
import { ListLoader } from '../../core/api/list-loader';
import { Button } from '../../shared/ui/button';
import { DataTable, TableColumn } from '../../shared/ui/data-table';
import { EmptyState } from '../../shared/ui/empty-state';
import { Icon } from '../../shared/ui/icon';
import { LoadState } from '../../shared/ui/load-state';
import { PageHeader } from '../../shared/ui/page-header';
import { SelectField, SelectOption } from '../../shared/ui/select-field';
import { ContainerTypeService } from '../catalogs/container-types/container-type.service';
import { LineOperatorService } from '../catalogs/line-operators/line-operator.service';
import { ContainerRow, toContainerRows } from './container-rows';
import { ContainerService } from './container.service';
import { CreateContainerDialog } from './create-container-dialog';

/** The distinct id/label pairs found in the rows, sorted by label. */
function optionsFrom(
  rows: ContainerRow[],
  pick: (row: ContainerRow) => SelectOption,
): SelectOption[] {
  const unique = new Map(rows.map((row) => [pick(row).value, pick(row)]));
  return [...unique.values()].sort((a, b) => a.label.localeCompare(b.label));
}

@Component({
  selector: 'app-containers-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Button,
    CreateContainerDialog,
    DataTable,
    EmptyState,
    Icon,
    LoadState,
    PageHeader,
    SelectField,
  ],
  templateUrl: './containers-page.html',
  host: { class: 'flex flex-col gap-8' },
})
export class ContainersPage implements OnInit {
  private readonly containers = inject(ContainerService);
  private readonly operators = inject(LineOperatorService);
  private readonly types = inject(ContainerTypeService);

  protected readonly list = new ListLoader<ContainerRow>(() =>
    forkJoin([this.containers.list(), this.operators.list(), this.types.list()]).pipe(
      map(([containers, operators, types]) => toContainerRows(containers, operators, types)),
    ),
  );
  protected readonly columns: TableColumn<ContainerRow>[] = [
    { key: 'number', label: 'Số container' },
    { key: 'lineOperator', label: 'Hãng' },
    { key: 'containerType', label: 'Loại' },
    { key: 'isoCode', label: 'Mã ISO' },
    { key: 'size', label: 'Cỡ' },
    { key: 'maxWeight', label: 'Tải tối đa (kg)', align: 'end' },
    { key: 'tareWeight', label: 'Vỏ (kg)', align: 'end' },
    { key: 'manufactureDate', label: 'Ngày sản xuất' },
  ];

  protected readonly operatorFilter = new FormControl('', { nonNullable: true });
  protected readonly typeFilter = new FormControl('', { nonNullable: true });
  private readonly operatorId = toSignal(this.operatorFilter.valueChanges, { initialValue: '' });
  private readonly typeId = toSignal(this.typeFilter.valueChanges, { initialValue: '' });

  protected readonly operatorOptions = computed(() =>
    optionsFrom(this.list.items(), (r) => ({ value: r.lineOperatorId, label: r.lineOperator })),
  );
  protected readonly typeOptions = computed(() =>
    optionsFrom(this.list.items(), (r) => ({ value: r.containerTypeId, label: r.containerType })),
  );
  protected readonly visible = computed(() =>
    this.list
      .items()
      .filter((r) => !this.operatorId() || r.lineOperatorId === this.operatorId())
      .filter((r) => !this.typeId() || r.containerTypeId === this.typeId()),
  );

  protected readonly creating = signal(false);

  ngOnInit(): void {
    this.list.load();
  }
}
