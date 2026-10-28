import { LineOperator } from '../catalogs/line-operators/line-operator.service';
import { ContainerType } from '../catalogs/container-types/container-type.service';
import { Container, ContainerListItem } from './container.service';

/** A container with the operator and type names worked out, ready to show in a table. */
export interface ContainerRow {
  id: string;
  number: string;
  lineOperatorId: string;
  lineOperator: string;
  containerTypeId: string;
  containerType: string;
  isoCode: string;
  sizeFeet: number;
  size: string;
  maxWeight: string;
  tareWeight: string;
  manufactureDate: string;
}

/** A row of the paged container table: the API already sent the codes, so only the text is formatted. */
export interface ContainerTableRow {
  id: string;
  number: string;
  lineOperator: string;
  containerType: string;
  isoCode: string;
  size: string;
  maxWeight: string;
  tareWeight: string;
  manufactureDate: string;
}

const KG = new Intl.NumberFormat('vi-VN');

/** `2024-03-09` becomes `09/03/2024`. */
function formatDate(isoDate: string): string {
  const [year, month, day] = isoDate.split('-');
  return `${day}/${month}/${year}`;
}

export function toContainerRows(
  containers: Container[],
  operators: LineOperator[],
  types: ContainerType[],
): ContainerRow[] {
  const operatorCodes = new Map(operators.map((o) => [o.id, o.code]));
  const typeCodes = new Map(types.map((t) => [t.id, t.code]));
  return containers.map((c) => ({
    id: c.id,
    number: c.number,
    lineOperatorId: c.lineOperatorId,
    lineOperator: operatorCodes.get(c.lineOperatorId) ?? '',
    containerTypeId: c.containerTypeId,
    containerType: typeCodes.get(c.containerTypeId) ?? '',
    isoCode: c.isoCode,
    sizeFeet: c.sizeFeet,
    size: `${c.sizeFeet} ft`,
    maxWeight: KG.format(c.maxWeightKg),
    tareWeight: KG.format(c.tareWeightKg),
    manufactureDate: formatDate(c.manufactureDate),
  }));
}

export function toContainerTableRows(containers: ContainerListItem[]): ContainerTableRow[] {
  return containers.map((c) => ({
    id: c.id,
    number: c.number,
    lineOperator: c.lineOperatorCode,
    containerType: c.containerTypeCode,
    isoCode: c.isoCode,
    size: `${c.sizeFeet} ft`,
    maxWeight: KG.format(c.maxWeightKg),
    tareWeight: KG.format(c.tareWeightKg),
    manufactureDate: formatDate(c.manufactureDate),
  }));
}
