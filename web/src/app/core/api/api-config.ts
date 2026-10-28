import { InjectionToken, isDevMode } from '@angular/core';

/**
 * Where the API is. The dev server talks to the API on its own port; the production build uses the same origin
 * as the page, where the web server forwards `/api` to the API.
 */
export const API_URL = new InjectionToken<string>('API_URL', {
  providedIn: 'root',
  factory: () => (isDevMode() ? 'http://localhost:5049' : ''),
});
