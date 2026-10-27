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
