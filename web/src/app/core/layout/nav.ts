import { IconName } from '../../shared/ui/icon';

export interface NavItem {
  label: string;
  path: string;
  icon: IconName;
  /** Active only on this exact URL, not on the URLs below it. */
  exact?: boolean;
}

export interface NavGroup {
  label: string;
  items: NavItem[];
}

/** Add an item here when its screen gets a route. */
export const NAV_GROUPS: NavGroup[] = [
  {
    label: 'Chung',
    items: [{ label: 'Trang chủ', path: '/', icon: 'home', exact: true }],
  },
];
