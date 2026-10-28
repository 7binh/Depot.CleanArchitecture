import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup } from '@angular/forms';
import { debounceTime, map, tap } from 'rxjs';
import { ListLoader } from '../../core/api/list-loader';
import { Paged } from '../../core/api/paged';
import { Button } from '../../shared/ui/button';
import { DataTable, TableColumn } from '../../shared/ui/data-table';
import { EmptyState } from '../../shared/ui/empty-state';
import { Icon } from '../../shared/ui/icon';
import { LoadState } from '../../shared/ui/load-state';
import { PageHeader } from '../../shared/ui/page-header';
import { Pagination } from '../../shared/ui/pagination';
import { SelectField, SelectOption } from '../../shared/ui/select-field';
import { TextField } from '../../shared/ui/text-field';
import { ContainerTypeService } from '../catalogs/container-types/container-type.service';
import { LineOperatorService } from '../catalogs/line-operators/line-operator.service';
import { ContainerTableRow, toContainerTableRows } from './container-rows';
import { ContainerFilter, ContainerListItem, ContainerService } from './container.service';
import { CreateContainerDialog } from './create-container-dialog';

const PAGE_SIZE = 20;
/** Typing waits this long for a pause before it asks the API again. */
const FILTER_DELAY_MS = 300;

function textControl(): FormControl<string> {
  return new FormControl('', { nonNullable: true });
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
    Pagination,
    SelectField,
    TextField,
  ],
  templateUrl: './containers-page.html',
  host: { class: 'flex flex-col gap-6' },
})
export class ContainersPage implements OnInit {
  private readonly containers = inject(ContainerService);

  protected readonly filters = new FormGroup({
    number: textControl(),
    lineOperatorId: textControl(),
    containerTypeId: textControl(),
    sizeFeet: textControl(),
    isoCode: textControl(),
  });
  protected readonly sizeOptions: SelectOption[] = [
    { value: '20', label: '20 ft' },
    { value: '40', label: '40 ft' },
  ];

  private readonly operatorService = inject(LineOperatorService);
  private readonly typeService = inject(ContainerTypeService);
  private readonly operators = new ListLoader(() => this.operatorService.list());
  private readonly types = new ListLoader(() => this.typeService.list());
  protected readonly operatorOptions = computed(() =>
    this.operators.items().map((o) => ({ value: o.id, label: o.code })),
  );
  protected readonly typeOptions = computed(() =>
    this.types.items().map((t) => ({ value: t.id, label: t.code })),
  );

  protected readonly page = signal(1);
  /** The page the list shows: totals for the pager. Null until the first answer. */
  protected readonly paging = signal<Paged<ContainerListItem> | null>(null);
  /** Whether the answer on screen was filtered, which decides what an empty list says. */
  protected readonly filtered = signal(false);
  protected readonly pageSize = PAGE_SIZE;

  protected readonly list = new ListLoader<ContainerTableRow>(() => {
    const filter: ContainerFilter = this.filters.getRawValue();
    return this.containers.search(filter, this.page(), PAGE_SIZE).pipe(
      tap((paged) => {
        this.paging.set(paged);
        this.filtered.set(Object.values(filter).some((value) => value.trim() !== ''));
      }),
      map((paged) => toContainerTableRows(paged.items)),
    );
  });
  protected readonly columns: TableColumn<ContainerTableRow>[] = [
    { key: 'number', label: 'Số container' },
    { key: 'lineOperator', label: 'Hãng' },
    { key: 'containerType', label: 'Loại' },
    { key: 'isoCode', label: 'Mã ISO' },
    { key: 'size', label: 'Cỡ' },
    { key: 'maxWeight', label: 'Tải tối đa (kg)', align: 'end' },
    { key: 'tareWeight', label: 'Vỏ (kg)', align: 'end' },
    { key: 'manufactureDate', label: 'Ngày sản xuất' },
  ];

  protected readonly creating = signal(false);

  constructor() {
    this.filters.valueChanges
      .pipe(debounceTime(FILTER_DELAY_MS), takeUntilDestroyed())
      .subscribe(() => this.goTo(1));
  }

  ngOnInit(): void {
    this.list.load();
    this.operators.load();
    this.types.load();
  }

  protected goTo(page: number): void {
    this.page.set(page);
    this.list.load(true);
  }
}
