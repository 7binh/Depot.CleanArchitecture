import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { API_URL } from '../api/api-config';
import { AuthService, TokenResponse } from './auth.service';

const TOKENS: TokenResponse = {
  accessToken: 'access',
  expiresAt: '2026-10-27T10:15:00Z',
  refreshToken: 'refresh',
};

describe('AuthService', () => {
  let http: HttpTestingController;
  let service: AuthService;

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'login', children: [] }]),
        { provide: API_URL, useValue: 'http://api.test' },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    service = TestBed.inject(AuthService);
  });

  afterEach(() => http.verify());

  it('is signed out at the start', () => {
    expect(service.isAuthenticated()).toBe(false);
  });

  it('stores the tokens after a successful login', () => {
    service.login('a@b.vn', 'secret').subscribe();

    const request = http.expectOne('http://api.test/api/auth/login');
    expect(request.request.body).toEqual({ email: 'a@b.vn', password: 'secret' });
    request.flush(TOKENS);

    expect(service.isAuthenticated()).toBe(true);
    expect(sessionStorage.getItem('depot.tokens')).toContain('refresh');
  });

  it('exposes the email found in the access token', () => {
    const payload = btoa(JSON.stringify({ email: 'a@b.vn' })).replaceAll('=', '');
    service.login('a@b.vn', 'secret').subscribe();
    http
      .expectOne('http://api.test/api/auth/login')
      .flush({ ...TOKENS, accessToken: `h.${payload}.s` });

    expect(service.email()).toBe('a@b.vn');
  });

  it('stays signed out when the login fails', () => {
    service.login('a@b.vn', 'wrong').subscribe({ error: () => undefined });

    http
      .expectOne('http://api.test/api/auth/login')
      .flush({ detail: 'no' }, { status: 401, statusText: 'Unauthorized' });

    expect(service.isAuthenticated()).toBe(false);
  });

  it('clears the tokens and revokes the refresh token on logout', () => {
    service.login('a@b.vn', 'secret').subscribe();
    http.expectOne('http://api.test/api/auth/login').flush(TOKENS);

    service.logout();

    const request = http.expectOne('http://api.test/api/auth/logout');
    expect(request.request.body).toEqual({ refreshToken: 'refresh' });
    request.flush(null, { status: 204, statusText: 'No Content' });
    expect(service.isAuthenticated()).toBe(false);
    expect(sessionStorage.getItem('depot.tokens')).toBeNull();
  });
});
