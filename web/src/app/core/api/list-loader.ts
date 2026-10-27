import { DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Observable } from 'rxjs';
import { toMessage } from './api-error';

export type LoadStatus = 'loading' | 'ready' | 'failed';

/** Loads a list once and keeps its status, items and failure message as signals. Create it in a field initializer. */
export class ListLoader<T> {
  private readonly destroyRef = inject(DestroyRef);
  private readonly fetch: () => Observable<T[]>;

  readonly status = signal<LoadStatus>('loading');
  readonly items = signal<T[]>([]);
  readonly failure = signal('');

  constructor(fetch: () => Observable<T[]>) {
    this.fetch = fetch;
  }

  /** `background` keeps showing the current items instead of the loading state while the list refreshes. */
  load(background = false): void {
    if (!background) {
      this.status.set('loading');
    }
    this.fetch()
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (items) => {
          this.items.set(items);
          this.status.set('ready');
        },
        error: (error: unknown) => {
          this.failure.set(toMessage(error));
          this.status.set('failed');
        },
      });
  }
}
