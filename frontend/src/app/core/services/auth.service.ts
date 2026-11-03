import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap, catchError, throwError, of } from 'rxjs';
import { environment } from '../../../environments/environment';
import { LoginRequest, LoginResponse, UserDto, UserRole } from '../models/auth.models';

const TOKEN_KEY = 'daycare_jwt_token';
const USER_KEY = 'daycare_user';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);

  private _token = signal<string | null>(this.getStoredToken());
  private _currentUser = signal<UserDto | null>(this.getStoredUser());

  public readonly token = this._token.asReadonly();
  public readonly currentUser = this._currentUser.asReadonly();
  public readonly isLoggedIn = computed(() => !!this._token());
  public readonly role = computed(() => this._currentUser()?.role ?? null);

  private getStoredToken(): string | null {
    return localStorage.getItem(TOKEN_KEY);
  }

  private getStoredUser(): UserDto | null {
    const raw = localStorage.getItem(USER_KEY);
    if (!raw) return null;
    try {
      return JSON.parse(raw) as UserDto;
    } catch {
      return null;
    }
  }

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiUrl}/auth/login`, credentials).pipe(
      tap(res => {
        this.setSession(res.accessToken, res.user);
      })
    );
  }

  loadCurrentUser(): Observable<UserDto | null> {
    if (!this._token()) {
      return of(null);
    }
    return this.http.get<UserDto>(`${environment.apiUrl}/auth/me`).pipe(
      tap(user => {
        this._currentUser.set(user);
        localStorage.setItem(USER_KEY, JSON.stringify(user));
      }),
      catchError(err => {
        if (err.status === 401) {
          this.logout();
        }
        return of(null);
      })
    );
  }

  changePassword(request: { currentPassword: string; newPassword: string }): Observable<any> {
    return this.http.post(`${environment.apiUrl}/auth/change-password`, request);
  }

  logout(redirect = true): void {
    localStorage.removeItem(TOKEN_KEY);
    localStorage.removeItem(USER_KEY);
    this._token.set(null);
    this._currentUser.set(null);
    if (redirect) {
      this.router.navigate(['/login']);
    }
  }

  private setSession(token: string, user: UserDto): void {
    localStorage.setItem(TOKEN_KEY, token);
    localStorage.setItem(USER_KEY, JSON.stringify(user));
    this._token.set(token);
    this._currentUser.set(user);
  }

  getDashboardRouteForRole(role: UserRole | string | null): string {
    switch (role) {
      case 'Admin':
        return '/admin/dashboard';
      case 'Staff':
        return '/staff/my-day';
      case 'Parent':
        return '/parent/home';
      default:
        return '/login';
    }
  }

  redirectToRoleDashboard(): void {
    const userRole = this.role();
    const destination = this.getDashboardRouteForRole(userRole);
    this.router.navigate([destination]);
  }
}
