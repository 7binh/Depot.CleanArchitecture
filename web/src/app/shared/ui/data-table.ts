import { ChangeDetectionStrategy, Component, input } from '@angular/core';

export interface TableColumn<T> {
  /** Key of the row field shown in this column. */
  key: keyof T & string;
  label: string;
  /** Numbers and trailing values are right-aligned. */
  align?: 'start' | 'end';
}

/**
 * Read-only table: the header names the columns, each row shows plain text per column.
 * Rows have no divider; one turns into a card on hover.
 */
@Component({
  selector: 'app-data-table',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <table class="w-full border-separate border-spacing-y-1 text-left">
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
              class="h-9 border-b border-line px-4 text-label whitespace-nowrap text-ink-3 uppercase"
              [class.text-right]="column.align === 'end'"
            >
              {{ column.label }}
            </th>
          }
        </tr>
      </thead>
      <tbody>
        @for (row of rows(); track row.id) {
          <tr class="group">
            @for (column of columns(); track column.key; let first = $first) {
              <td
                class="h-9 border-y border-transparent px-4 text-body whitespace-nowrap transition-colors duration-150 ease-out first:rounded-l-card first:border-l last:rounded-r-card last:border-r group-hover:border-line-card group-hover:bg-surface motion-reduce:transition-none"
                [class.font-medium]="first"
                [class.text-ink]="first"
                [class.text-ink-body]="!first"
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
