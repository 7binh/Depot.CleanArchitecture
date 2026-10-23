import { Routes } from '@angular/router';
import { AuthLayout } from './features/auth/auth-layout';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'login' },
  {
    path: '',
    component: AuthLayout,
    children: [
      {
        path: 'login',
        title: 'Đăng nhập - Depot',
        loadComponent: () => import('./features/auth/login-page').then((m) => m.LoginPage),
      },
      {
        path: 'register',
        title: 'Tạo tài khoản - Depot',
        loadComponent: () => import('./features/auth/register-page').then((m) => m.RegisterPage),
      },
    ],
  },
  { path: '**', redirectTo: 'login' },
];
