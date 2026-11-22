import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthService, LOGIN_PATH } from './auth.service';

export const authGuard: CanActivateFn = (_, state) =>
  inject(AuthService).user()
    ? true
    : inject(Router).createUrlTree([LOGIN_PATH], { queryParams: { returnUrl: state.url } });

export const guestGuard: CanActivateFn = () =>
  inject(AuthService).user() ? inject(Router).createUrlTree(['/']) : true;
