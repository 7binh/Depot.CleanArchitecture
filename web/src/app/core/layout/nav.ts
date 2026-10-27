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
  {
    label: 'Nghiệp vụ',
    items: [
      { label: 'Container', path: '/container', icon: 'box' },
      { label: 'Gate In', path: '/gate-in', icon: 'plus' },
    ],
  },
  {
    label: 'Danh mục',
    items: [
      { label: 'Hãng khai thác', path: '/danh-muc/hang-khai-thac', icon: 'ship' },
      { label: 'Loại container', path: '/danh-muc/loai-container', icon: 'layers' },
      { label: 'Khách hàng', path: '/danh-muc/khach-hang', icon: 'users' },
    ],
  },
];
