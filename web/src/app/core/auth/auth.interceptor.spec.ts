import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { API_URL } from '../api/api-config';
import { authInterceptor } from './auth.interceptor';
import { AuthService, TokenResponse } from './auth.service';

const OLD: TokenResponse = {
  accessToken: 'old',
  expiresAt: '2026-10-27T10:00:00Z',
  refreshToken: 'r1',
};
const NEW: TokenResponse = {
  accessToken: 'new',
  expiresAt: '2026-10-27T10:15:00Z',
  refreshToken: 'r2',
};
const UNAUTHORIZED = { status: 401, statusText: 'Unauthorized' };

function setup() {
  sessionStorage.clear();
  TestBed.configureTestingModule({
    providers: [
      provideHttpClient(withInterceptors([authInterceptor])),
      provideHttpClientTesting(),
      provideRouter([{ path: 'login', children: [] }]),
      { provide: API_URL, useValue: 'http://api.test' },
    ],
  });
  const http = TestBed.inject(HttpTestingController);
  const auth = TestBed.inject(AuthService);
  auth.login('a@b.vn', 'secret').subscribe();
  http.expectOne('http://api.test/api/auth/login').flush(OLD);
  return { http, auth, client: TestBed.inject(HttpClient) };
}

describe('authInterceptor headers', () => {
  it('adds the access token to API calls', () => {
    const { http, client } = setup();
    client.get('http://api.test/api/blocks').subscribe();

    const request = http.expectOne('http://api.test/api/blocks');
    expect(request.request.headers.get('Authorization')).toBe('Bearer old');
    request.flush([]);
    http.verify();
  });

  it('does not add a token to the auth endpoints', () => {
    const { http, client } = setup();
    client.post('http://api.test/api/auth/logout', {}).subscribe();

    const request = http.expectOne('http://api.test/api/auth/logout');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush(null);
    http.verify();
  });

  it('passes other errors through without refreshing', () => {
    const { http, client } = setup();
    let status = 0;
    client
      .get('http://api.test/api/blocks')
      .subscribe({ error: (e: { status: number }) => (status = e.status) });

    http
      .expectOne('http://api.test/api/blocks')
      .flush(null, { status: 409, statusText: 'Conflict' });

    expect(status).toBe(409);
    http.verify();
  });
});

describe('authInterceptor refresh', () => {
  it('refreshes once and retries the call after a 401', () => {
    const { http, client } = setup();
    let body: unknown;
    client.get('http://api.test/api/blocks').subscribe((value) => (body = value));

    http.expectOne('http://api.test/api/blocks').flush(null, UNAUTHORIZED);
    const refresh = http.expectOne('http://api.test/api/auth/refresh');
    expect(refresh.request.body).toEqual({ refreshToken: 'r1' });
    refresh.flush(NEW);

    const retry = http.expectOne('http://api.test/api/blocks');
    expect(retry.request.headers.get('Authorization')).toBe('Bearer new');
    retry.flush(['ok']);
    expect(body).toEqual(['ok']);
    http.verify();
  });

  it('shares one refresh between calls that fail together', () => {
    const { http, client } = setup();
    const results: unknown[] = [];
    client.get('http://api.test/api/blocks').subscribe((v) => results.push(v));
    client.get('http://api.test/api/containers').subscribe((v) => results.push(v));

    http.expectOne('http://api.test/api/blocks').flush(null, UNAUTHORIZED);
    http.expectOne('http://api.test/api/containers').flush(null, UNAUTHORIZED);
    http.expectOne('http://api.test/api/auth/refresh').flush(NEW);
    http.expectOne('http://api.test/api/blocks').flush(['b']);
    http.expectOne('http://api.test/api/containers').flush(['c']);

    expect(results).toEqual([['b'], ['c']]);
    http.verify();
  });

  it('signs the user out when the refresh fails', () => {
    const { http, auth, client } = setup();
    client.get('http://api.test/api/blocks').subscribe({ error: () => undefined });

    http.expectOne('http://api.test/api/blocks').flush(null, UNAUTHORIZED);
    http.expectOne('http://api.test/api/auth/refresh').flush(null, UNAUTHORIZED);

    expect(auth.isAuthenticated()).toBe(false);
    http.verify();
  });
});
