import { HttpErrorResponse, HttpInterceptorFn, HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';

import { AuthService } from './auth.service';

const AUTH_API = '/api/Auth/';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  return next(req).pipe(
    catchError((error: unknown) => {
      const sessionEnded =
        error instanceof HttpErrorResponse &&
        error.status === HttpStatusCode.Unauthorized &&
        !req.url.includes(AUTH_API);
      if (sessionEnded) {
        void auth.sessionExpired();
      }
      const permissionsChanged =
        error instanceof HttpErrorResponse && error.status === HttpStatusCode.Forbidden;
      if (permissionsChanged) {
        void auth.loadCurrentUser();
      }
      return throwError(() => error);
    }),
  );
};
