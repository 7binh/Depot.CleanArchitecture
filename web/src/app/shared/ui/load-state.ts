import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { LoadStatus } from '../../core/api/list-loader';
import { Button } from './button';
import { ErrorBanner } from './error-banner';
import { Icon } from './icon';
import { Skeleton } from './skeleton';

/** Shows skeleton rows while loading, an error with retry when it failed, else the projected content. */
@Component({
  selector: 'app-load-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Button, ErrorBanner, Icon, Skeleton],
  template: `
    @switch (status()) {
      @case ('loading') {
        <div class="flex flex-col gap-1" aria-busy="true">
          @for (n of placeholders; track n) {
            <div appSkeleton class="h-9 w-full"></div>
          }
        </div>
      }
      @case ('failed') {
        <div class="flex flex-col items-start gap-4">
          <app-error-banner class="w-full">{{ failure() }}</app-error-banner>
          <button appButton type="button" (click)="retry.emit()">
            <app-icon name="refresh" [size]="16" />
            Thử lại
          </button>
        </div>
      }
      @case ('ready') {
        <ng-content />
      }
    }
  `,
})
export class LoadState {
  readonly status = input.required<LoadStatus>();
  readonly failure = input('');
  readonly retry = output();

  protected readonly placeholders = [1, 2, 3, 4];
}
