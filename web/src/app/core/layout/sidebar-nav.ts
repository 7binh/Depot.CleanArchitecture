import { ChangeDetectionStrategy, Component, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { Icon } from '../../shared/ui/icon';
import { NAV_GROUPS } from './nav';

const BASE =
  'flex h-10 items-center gap-2 rounded-control px-2.5 text-body transition-colors duration-150 ease-out sm:h-7.5 ' +
  'focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand motion-reduce:transition-none';
const IDLE = `${BASE} text-ink-2 hover:bg-control/50 hover:text-ink`;
const ACTIVE = `${BASE} bg-nav-active font-medium text-ink`;

@Component({
  selector: 'app-sidebar-nav',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [RouterLink, RouterLinkActive, Icon],
  templateUrl: './sidebar-nav.html',
})
export class SidebarNav {
  /** Emits when a link is followed, so the mobile drawer can close. */
  readonly navigated = output();

  protected readonly groups = NAV_GROUPS;
  protected readonly idle = IDLE;
  protected readonly active = ACTIVE;
}
