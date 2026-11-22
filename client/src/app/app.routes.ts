import { Routes } from '@angular/router';

import { authGuard, guestGuard } from './auth/auth.guards';
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
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./pages/home/home').then((m) => m.Home),
  },
  { path: '**', redirectTo: '' },
];
