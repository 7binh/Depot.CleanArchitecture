import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/auth/auth.guard';
import { AppShell } from './core/layout/app-shell';
import { AuthLayout } from './features/auth/auth-layout';

export const routes: Routes = [
  {
    path: '',
    component: AppShell,
    canActivate: [authGuard],
    children: [
      {
        path: '',
        pathMatch: 'full',
        title: 'Trang chủ - Depot',
        loadComponent: () => import('./features/home/home-page').then((m) => m.HomePage),
      },
      {
        path: 'container',
        title: 'Container - Depot',
        loadComponent: () =>
          import('./features/containers/containers-page').then((m) => m.ContainersPage),
      },
      {
        path: 'gate-in',
        title: 'Gate In - Depot',
        loadComponent: () => import('./features/gate/gate-in-page').then((m) => m.GateInPage),
      },
      {
        path: 'gate-out',
        title: 'Gate Out - Depot',
        loadComponent: () => import('./features/gate/gate-out-page').then((m) => m.GateOutPage),
      },
      {
        path: 'danh-muc/hang-khai-thac',
        title: 'Hãng khai thác - Depot',
        loadComponent: () =>
          import('./features/catalogs/line-operators/line-operators-page').then(
            (m) => m.LineOperatorsPage,
          ),
      },
      {
        path: 'danh-muc/loai-container',
        title: 'Loại container - Depot',
        loadComponent: () =>
          import('./features/catalogs/container-types/container-types-page').then(
            (m) => m.ContainerTypesPage,
          ),
      },
      {
        path: 'danh-muc/khach-hang',
        title: 'Khách hàng - Depot',
        loadComponent: () =>
          import('./features/catalogs/customers/customers-page').then((m) => m.CustomersPage),
      },
    ],
  },
  {
    path: '',
    component: AuthLayout,
    canActivate: [guestGuard],
    children: [
      {
        path: 'login',
        title: 'Đăng nhập - Depot',
        loadComponent: () => import('./features/auth/login-page').then((m) => m.LoginPage),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
