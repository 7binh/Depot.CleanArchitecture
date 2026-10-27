import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, catchError, finalize, shareReplay, tap, throwError } from 'rxjs';
import { API_URL } from '../api/api-config';
import { emailFromToken } from './jwt';

export interface TokenResponse {
  accessToken: string;
  expiresAt: string;
  refreshToken: string;
}

const STORAGE_KEY = 'depot.tokens';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiUrl = inject(API_URL);

  private readonly tokens = signal<TokenResponse | null>(this.load());

  private refreshing: Observable<TokenResponse> | null = null;

  readonly isAuthenticated = computed(() => this.tokens() !== null);
  readonly accessToken = computed(() => this.tokens()?.accessToken ?? null);
  readonly email = computed(() => {
    const token = this.accessToken();
    return token ? emailFromToken(token) : null;
  });

  login(email: string, password: string): Observable<TokenResponse> {
    return this.http
      .post<TokenResponse>(`${this.apiUrl}/api/auth/login`, { email, password })
      .pipe(tap((response) => this.store(response)));
  }

  /** Swaps the refresh token for new tokens; callers that arrive meanwhile share the same request. */
  refresh(): Observable<TokenResponse> {
    if (this.refreshing) {
      return this.refreshing;
    }
    const refreshToken = this.tokens()?.refreshToken;
    if (!refreshToken) {
      this.endSession();
      return throwError(() => new Error('There is no refresh token.'));
    }
    this.refreshing = this.http
      .post<TokenResponse>(`${this.apiUrl}/api/auth/refresh`, { refreshToken })
      .pipe(
        tap((response) => this.store(response)),
        catchError((error: unknown) => {
          this.endSession();
          return throwError(() => error);
        }),
        finalize(() => (this.refreshing = null)),
        shareReplay({ bufferSize: 1, refCount: false }),
      );
    return this.refreshing;
  }

  logout(): void {
    const refreshToken = this.tokens()?.refreshToken;
    this.endSession();
    if (refreshToken) {
      this.http.post(`${this.apiUrl}/api/auth/logout`, { refreshToken }).subscribe({
        error: () => undefined,
      });
    }
  }

  private endSession(): void {
    this.store(null);
    void this.router.navigateByUrl('/login');
  }

  private store(value: TokenResponse | null): void {
    this.tokens.set(value);
    try {
      if (value) {
        sessionStorage.setItem(STORAGE_KEY, JSON.stringify(value));
      } else {
        sessionStorage.removeItem(STORAGE_KEY);
      }
    } catch {
      // Storage can be blocked; the session then lasts until the page reloads.
    }
  }

  private load(): TokenResponse | null {
    try {
      const raw = sessionStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as TokenResponse) : null;
    } catch {
      return null;
    }
  }
}
