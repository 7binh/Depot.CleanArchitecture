import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export interface TableColumn<T> {
  /** Key of the row field shown in this column. */
  key: keyof T & string;
  label: string;
  /** Numbers and trailing values are right-aligned. */
  align?: 'start' | 'end';
}

/** Read-only table: the header names the columns, each row shows plain text per column. */
@Component({
  selector: 'app-data-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <table class="w-full border-separate border-spacing-0 text-left">
      <caption class="sr-only">
        {{
          caption()
        }}
      </caption>
      <thead>
        <tr>
          @for (column of columns(); track column.key) {
            <th
              scope="col"
              class="h-11 bg-surface-sunken px-4 text-small whitespace-nowrap font-normal text-ink-2 first:rounded-l-inset last:rounded-r-inset"
              [class.text-right]="column.align === 'end'"
            >
              {{ column.label }}
            </th>
          }
        </tr>
      </thead>
      <tbody>
        @for (row of rows(); track row.id) {
          <tr>
            @for (column of columns(); track column.key; let first = $first) {
              <td
                class="h-16 border-b border-line px-4 text-body whitespace-nowrap"
                [class.font-medium]="first"
                [class.text-ink]="first"
                [class.text-ink-2]="!first"
                [class.text-right]="column.align === 'end'"
              >
                {{ row[column.key] }}
              </td>
            }
          </tr>
        }
      </tbody>
    </table>
  `,
})
export class DataTable<T extends { id: string }> {
  readonly caption = input.required<string>();
  readonly columns = input.required<readonly TableColumn<T>[]>();
  readonly rows = input.required<readonly T[]>();
}
