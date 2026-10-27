import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ReactiveFormsModule } from '@angular/forms';
import { catchError, forkJoin, map, of, switchMap } from 'rxjs';
import { errorCode, toMessage } from '../../core/api/api-error';
import { ListLoader } from '../../core/api/list-loader';
import { Button } from '../../shared/ui/button';
import { Card } from '../../shared/ui/card';
import { ErrorBanner } from '../../shared/ui/error-banner';
import { Icon } from '../../shared/ui/icon';
import { LoadState } from '../../shared/ui/load-state';
import { PageHeader } from '../../shared/ui/page-header';
import { SelectField, SelectOption } from '../../shared/ui/select-field';
import { TextField } from '../../shared/ui/text-field';
import { ContainerTypeService } from '../catalogs/container-types/container-type.service';
import { LineOperatorService } from '../catalogs/line-operators/line-operator.service';
import { ContainerRow, toContainerRows } from '../containers/container-rows';
import { Container, ContainerService } from '../containers/container.service';
import { CreateContainerDialog } from '../containers/create-container-dialog';
import { Bay, Block, YardRow, YardService, baySize } from '../yard/yard.service';
import {
  CONDITION_OPTIONS,
  GATE_IN_MESSAGES,
  GRADE_OPTIONS,
  GateInForm,
  VEHICLE_OPTIONS,
  buildGateInForm,
  nowLocal,
} from './gate-in-form';
import { GateService, NewGateIn, StayCondition, StayGrade, VehicleType } from './gate.service';

type Field = keyof GateInForm['controls'];

/** Which field an API error belongs under; errors not listed here go to the banner. */
const FIELD_OF_ERROR: Record<string, Field> = {
  'GateIn.ContainerNotFound': 'number',
  'Container.AlreadyInYard': 'number',
  'GateInAt.InFuture': 'gateInAt',
  'DamageNote.Required': 'damageNote',
  'GateIn.BlockNotFound': 'blockId',
  'GateIn.RowNotFound': 'rowId',
  'Position.RowAndTierRequired': 'rowId',
  'Position.Inconsistent': 'rowId',
  'Position.Occupied': 'tier',
  'Position.OverlapsNeighbour': 'tier',
  'Tier.OutOfRange': 'tier',
  'Tier.BelowEmpty': 'tier',
  'Bay.SizeMismatch': 'bayId',
  'Bay.NeighboursMissing': 'bayId',
};

/** Case-insensitive match on the typed number, spaces ignored. */
function findByNumber(rows: ContainerRow[], typed: string): ContainerRow | undefined {
  const wanted = typed.replace(/\s/g, '').toUpperCase();
  return wanted === '' ? undefined : rows.find((row) => row.number.toUpperCase() === wanted);
}

@Component({
  selector: 'app-gate-in-page',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    Card,
    ReactiveFormsModule,
    Button,
    CreateContainerDialog,
    ErrorBanner,
    Icon,
    LoadState,
    PageHeader,
    SelectField,
    TextField,
  ],
  templateUrl: './gate-in-page.html',
  host: { class: 'flex flex-col gap-6' },
})
export class GateInPage implements OnInit {
  private readonly containerService = inject(ContainerService);
  private readonly operators = inject(LineOperatorService);
  private readonly types = inject(ContainerTypeService);
  private readonly yard = inject(YardService);
  private readonly gate = inject(GateService);

  protected readonly containers = new ListLoader<ContainerRow>(() =>
    forkJoin([this.containerService.list(), this.operators.list(), this.types.list()]).pipe(
      map(([containers, operators, types]) => toContainerRows(containers, operators, types)),
    ),
  );
  protected readonly blocks = new ListLoader<Block>(() => this.yard.blocks());

  protected readonly form = buildGateInForm({
    hasContainer: (number) => findByNumber(this.containers.items(), number) !== undefined,
    isRealBlock: (id) => this.isRealBlock(id),
  });
  protected readonly messages = GATE_IN_MESSAGES;
  protected readonly gradeOptions = GRADE_OPTIONS;
  protected readonly conditionOptions = CONDITION_OPTIONS;
  protected readonly vehicleOptions = VEHICLE_OPTIONS;

  private readonly typedNumber = toSignal(this.form.controls.number.valueChanges, {
    initialValue: '',
  });
  private readonly condition = toSignal(this.form.controls.condition.valueChanges, {
    initialValue: 'Normal',
  });
  private readonly blockId = toSignal(this.form.controls.blockId.valueChanges, {
    initialValue: '',
  });
  private readonly bayId = toSignal(this.form.controls.bayId.valueChanges, { initialValue: '' });
  private readonly rowId = toSignal(this.form.controls.rowId.valueChanges, { initialValue: '' });

  protected readonly found = computed(() =>
    findByNumber(this.containers.items(), this.typedNumber()),
  );
  protected readonly typedSomething = computed(() => this.typedNumber().trim() !== '');
  protected readonly damaged = computed(() => this.condition() === 'Damaged');
  protected readonly positioned = computed(() => this.isRealBlock(this.blockId()));

  private readonly bays = toSignal(
    this.form.controls.blockId.valueChanges.pipe(
      switchMap((id) =>
        this.isRealBlock(id) ? this.yard.bays(id).pipe(catchError(() => of([] as Bay[]))) : of([]),
      ),
    ),
    { initialValue: [] as Bay[] },
  );
  private readonly rows = toSignal(
    this.form.controls.bayId.valueChanges.pipe(
      switchMap((bayId) =>
        bayId
          ? this.yard
              .rows(this.form.controls.blockId.value, bayId)
              .pipe(catchError(() => of([] as YardRow[])))
          : of([]),
      ),
    ),
    { initialValue: [] as YardRow[] },
  );

  protected readonly blockOptions = computed<SelectOption[]>(() =>
    this.blocks.items().map((b) => ({ value: b.id, label: `${b.code} - ${b.name}` })),
  );
  protected readonly bayOptions = computed<SelectOption[]>(() => {
    const size = this.found()?.sizeFeet;
    return [...this.bays()]
      .filter((bay) => size === undefined || baySize(bay.bayNo) === size)
      .sort((a, b) => a.bayNo - b.bayNo)
      .map((bay) => ({ value: bay.id, label: `Bay ${bay.bayNo} (${baySize(bay.bayNo)} ft)` }));
  });
  protected readonly rowOptions = computed<SelectOption[]>(() =>
    [...this.rows()]
      .sort((a, b) => a.rowNo - b.rowNo)
      .map((row) => ({ value: row.id, label: `Row ${row.rowNo}` })),
  );
  protected readonly tierOptions = computed<SelectOption[]>(() => {
    const maxTier = this.rows().find((row) => row.id === this.rowId())?.maxTier ?? 0;
    return Array.from({ length: maxTier }, (_, i) => ({
      value: `${i + 1}`,
      label: `Tầng ${i + 1}`,
    }));
  });

  protected readonly declaring = signal(false);
  protected readonly notice = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly done = signal<string | null>(null);

  constructor() {
    const { controls } = this.form;
    controls.condition.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe(() => controls.damageNote.updateValueAndValidity());
    controls.blockId.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      controls.bayId.setValue('');
      controls.bayId.updateValueAndValidity();
    });
    controls.bayId.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      controls.rowId.setValue('');
      controls.rowId.updateValueAndValidity();
    });
    controls.rowId.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      controls.tier.setValue('');
      controls.tier.updateValueAndValidity();
    });
  }

  ngOnInit(): void {
    this.containers.load();
    this.blocks.load();
  }

  /** A container declared inside the Gate In flow: reload the list, then pick it. */
  protected declared(container: Container): void {
    this.containers.load();
    this.form.controls.number.setValue(container.number);
  }

  protected submit(): void {
    this.notice.set(null);
    this.done.set(null);
    const container = this.found();
    if (this.form.invalid || !container) {
      this.form.markAllAsTouched();
      return;
    }
    this.submitting.set(true);
    this.gate.gateIn(this.toRequest(container.id)).subscribe({
      next: () => this.succeed(container.number),
      error: (error: unknown) => this.fail(error),
    });
  }

  private isRealBlock(id: string): boolean {
    const block = this.blocks.items().find((b) => b.id === id);
    return block !== undefined && !block.isVirtual;
  }

  private toRequest(containerId: string): NewGateIn {
    const v = this.form.getRawValue();
    const positioned = this.isRealBlock(v.blockId);
    return {
      containerId,
      gateInAt: new Date(v.gateInAt).toISOString(),
      grade: v.grade as StayGrade,
      condition: v.condition as StayCondition,
      damageNote: v.condition === 'Damaged' ? v.damageNote.trim() : null,
      inVehicleType: v.vehicleType as VehicleType,
      inVehicleNumber: v.vehicleNumber.trim(),
      blockId: v.blockId,
      rowId: positioned ? v.rowId : null,
      tier: positioned ? Number(v.tier) : null,
    };
  }

  private succeed(number: string): void {
    this.submitting.set(false);
    this.done.set(number);
    this.form.reset();
    this.form.controls.gateInAt.setValue(nowLocal());
  }

  private fail(error: unknown): void {
    this.submitting.set(false);
    const code = errorCode(error);
    const field = code ? FIELD_OF_ERROR[code] : undefined;
    if (code && field) {
      const control = this.form.controls[field];
      control.setErrors({ [code]: true });
      control.markAsTouched();
      return;
    }
    this.notice.set(toMessage(error));
  }
}
