import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { API_URL } from '../api/api-config';
import { AuthService } from './auth.service';

function withToken(request: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  return token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request;
}

/** Adds the access token to API calls and, on a 401, swaps the refresh token once and retries. */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const apiUrl = inject(API_URL);

  const isApiCall = request.url.startsWith(`${apiUrl}/api/`);
  const isAuthCall = request.url.startsWith(`${apiUrl}/api/auth/`);
  if (!isApiCall || isAuthCall) {
    return next(request);
  }

  return next(withToken(request, auth.accessToken())).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }
      return auth
        .refresh()
        .pipe(switchMap((tokens) => next(withToken(request, tokens.accessToken))));
    }),
  );
};
