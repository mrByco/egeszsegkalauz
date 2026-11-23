import { Routes } from '@angular/router';

import { authGuard, guestGuard, permissionGuard } from './auth/auth.guards';
import { LOGIN_PATH, REGISTER_PATH } from './auth/auth.service';

export const routes: Routes = [
  {
    path: LOGIN_PATH.slice(1),
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/login/login').then((m) => m.Login),
  },
  {
    path: REGISTER_PATH.slice(1),
    canActivate: [guestGuard],
    loadComponent: () => import('./pages/register/register').then((m) => m.Register),
  },
  {
    path: 'pulzus',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/heart-rate/heart-rate').then((m) => m.HeartRate),
  },
  {
    path: 'integraciok',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/integrations/integrations').then((m) => m.Integrations),
  },
  {
    path: 'integraciok/:source/callback',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/oauth-callback/oauth-callback').then((m) => m.OAuthCallback),
  },
  {
    path: 'admin',
    canActivate: [authGuard],
    children: [
      {
        path: 'felhasznalok',
        canActivate: [permissionGuard('UsersView')],
        loadComponent: () => import('./pages/admin/users/users').then((m) => m.Users),
      },
      {
        path: 'szerepkorok',
        canActivate: [permissionGuard('RolesView')],
        loadComponent: () => import('./pages/admin/roles/roles').then((m) => m.Roles),
      },
      {
        path: 'szerepkorok/uj',
        canActivate: [permissionGuard('RolesEdit')],
        loadComponent: () => import('./pages/admin/role-edit/role-edit').then((m) => m.RoleEdit),
      },
      {
        path: 'szerepkorok/:nanoid',
        canActivate: [permissionGuard('RolesEdit')],
        loadComponent: () => import('./pages/admin/role-edit/role-edit').then((m) => m.RoleEdit),
      },
    ],
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/home/home').then((m) => m.Home),
  },
  { path: '**', redirectTo: '' },
];
