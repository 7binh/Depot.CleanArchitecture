import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { stubDialog } from '../../../testing/dialog';
import { DataTable, TableColumn } from './data-table';
import { Dialog } from './dialog';

@Component({
  imports: [Dialog],
  template: `
    @if (shown()) {
      <app-dialog title="Thêm mới" (closed)="onClosed()"><p>Nội dung</p></app-dialog>
    }
  `,
})
class DialogHost {
  readonly shown = signal(true);
  closedCount = 0;

  onClosed(): void {
    this.closedCount++;
    this.shown.set(false);
  }
}

interface Row {
  id: string;
  code: string;
  name: string;
}

@Component({
  imports: [DataTable],
  template: `<app-data-table caption="Hãng" [columns]="columns" [rows]="rows" />`,
})
class TableHost {
  readonly columns: TableColumn<Row>[] = [
    { key: 'code', label: 'Mã' },
    { key: 'name', label: 'Tên' },
  ];
  readonly rows: Row[] = [
    { id: '1', code: 'MSK', name: 'Maersk' },
    { id: '2', code: 'EMC', name: 'Evergreen' },
  ];
}

describe('Dialog', () => {
  beforeEach(() => stubDialog());

  function open() {
    const fixture = TestBed.createComponent(DialogHost);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    return { fixture, element, native: element.querySelector('dialog') as HTMLDialogElement };
  }

  it('opens as a modal with its title and projected content', () => {
    const { element, native } = open();
    expect(native.hasAttribute('open')).toBe(true);
    expect(native.getAttribute('aria-labelledby')).toBe(element.querySelector('h2')?.id);
    expect(element.querySelector('h2')?.textContent).toBe('Thêm mới');
    expect(element.textContent).toContain('Nội dung');
  });

  it('reports closed when the close button is used', () => {
    const { fixture, element } = open();
    element.querySelector<HTMLButtonElement>('button[aria-label="Đóng"]')?.click();
    fixture.detectChanges();
    expect(fixture.componentInstance.closedCount).toBe(1);
    expect(element.querySelector('dialog')).toBeNull();
  });

  it('closes when the dimmed area is clicked but not when the content is', () => {
    const { fixture, element, native } = open();
    element.querySelector('p')?.click();
    expect(fixture.componentInstance.closedCount).toBe(0);
    native.click();
    expect(fixture.componentInstance.closedCount).toBe(1);
  });
});

describe('DataTable', () => {
  it('shows a header per column and a row per item', () => {
    const fixture = TestBed.createComponent(TableHost);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;
    const headers = [...element.querySelectorAll('th')].map((th) => th.textContent?.trim());
    const cells = [...element.querySelectorAll('tbody tr:first-child td')].map((td) =>
      td.textContent?.trim(),
    );
    expect(headers).toEqual(['Mã', 'Tên']);
    expect(element.querySelectorAll('tbody tr').length).toBe(2);
    expect(cells).toEqual(['MSK', 'Maersk']);
    expect(element.querySelector('caption')?.textContent?.trim()).toBe('Hãng');
  });
});
