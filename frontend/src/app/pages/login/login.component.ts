import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { ButtonComponent } from '../../shared/components/button/button.component';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent],
  template: `
    <div class="min-h-screen bg-[#F0FDFA] flex items-center justify-center p-4">
      <div class="w-full max-w-[420px] bg-surface-card rounded-card border border-border shadow-card p-8">
        <!-- Logo and Header -->
        <div class="text-center mb-8">
          <div class="inline-flex items-center justify-center w-12 h-12 rounded-xl bg-primary text-white font-bold text-xl mb-3 shadow-sm">
            LS
          </div>
          <h1 class="text-2xl font-bold text-text-main">Little Steps Daycare</h1>
          <p class="text-sm text-text-muted mt-1">Sign in to your account</p>
        </div>

        <!-- Error alert -->
        <div
          *ngIf="errorMessage"
          id="login-error"
          role="alert"
          class="mb-6 p-3 rounded-input bg-danger-soft border border-danger/30 text-danger text-sm flex items-start gap-2"
        >
          <svg class="w-5 h-5 flex-shrink-0 mt-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
          <span>{{ errorMessage }}</span>
        </div>

        <!-- Login Form -->
        <form (ngSubmit)="onSubmit()" class="space-y-4">
          <div>
            <label for="email" class="block text-xs font-semibold text-text-main uppercase tracking-wider mb-1.5">
              Email Address
            </label>
            <input
              id="email"
              type="email"
              name="email"
              [(ngModel)]="email"
              required
              autocomplete="email"
              placeholder="you@daycare.local"
              [attr.aria-invalid]="errorMessage ? 'true' : null"
              [attr.aria-describedby]="errorMessage ? 'login-error' : null"
              class="w-full px-3.5 py-2.5 rounded-input border border-border bg-white text-text-main text-sm focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent transition-all placeholder:text-text-subtle"
            />
          </div>

          <div>
            <label for="password" class="block text-xs font-semibold text-text-main uppercase tracking-wider mb-1.5">
              Password
            </label>
            <div class="relative">
              <input
                id="password"
                [type]="showPassword ? 'text' : 'password'"
                name="password"
                [(ngModel)]="password"
                required
                autocomplete="current-password"
                placeholder="••••••••"
                [attr.aria-invalid]="errorMessage ? 'true' : null"
                [attr.aria-describedby]="errorMessage ? 'login-error' : null"
                class="w-full pl-3.5 pr-10 py-2.5 rounded-input border border-border bg-white text-text-main text-sm focus:outline-none focus:ring-2 focus:ring-primary focus:border-transparent transition-all placeholder:text-text-muted"
              />
              <button
                type="button"
                (click)="showPassword = !showPassword"
                [attr.aria-label]="showPassword ? 'Hide password' : 'Show password'"
                class="absolute inset-y-0 right-0 w-10 flex items-center justify-center text-text-muted hover:text-text-main cursor-pointer focus:outline-none"
              >
                <!-- Eye open -->
                <svg *ngIf="!showPassword" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                </svg>
                <!-- Eye closed -->
                <svg *ngIf="showPassword" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l18 18" />
                </svg>
              </button>
            </div>
          </div>

          <app-button
            type="submit"
            variant="primary"
            size="lg"
            [loading]="isLoading"
            customClass="w-full mt-2"
          >
            Sign In
          </app-button>
        </form>

        <!-- Demo accounts shortcut selector -->
        <div class="mt-8 pt-6 border-t border-border">
          <p class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-2 text-center">
            Demo Accounts Quick Fill
          </p>
          <div class="grid grid-cols-3 gap-2">
            <button
              type="button"
              (click)="quickFill('admin@daycare.local', 'Admin@12345')"
              class="px-2 py-1.5 text-xs font-medium rounded-input border border-border bg-slate-50 text-text-main hover:bg-primary-soft hover:text-primary transition-colors text-center"
            >
              Admin
            </button>
            <button
              type="button"
              (click)="quickFill('staff1@daycare.local', 'Staff@12345')"
              class="px-2 py-1.5 text-xs font-medium rounded-input border border-border bg-slate-50 text-text-main hover:bg-info-soft hover:text-info transition-colors text-center"
            >
              Staff
            </button>
            <button
              type="button"
              (click)="quickFill('parent1@daycare.local', 'Parent@12345')"
              class="px-2 py-1.5 text-xs font-medium rounded-input border border-border bg-slate-50 text-text-main hover:bg-success-soft hover:text-success transition-colors text-center"
            >
              Parent
            </button>
          </div>
        </div>
      </div>
    </div>
  `
})
export class LoginComponent {
  private authService = inject(AuthService);
  private toastService = inject(ToastService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  email = '';
  password = '';
  showPassword = false;
  isLoading = false;
  errorMessage = '';

  quickFill(email: string, pass: string): void {
    this.email = email;
    this.password = pass;
    this.errorMessage = '';
  }

  onSubmit(): void {
    if (!this.email || !this.password) {
      this.errorMessage = 'Please enter both email and password.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.authService.login({ email: this.email, password: this.password }).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.toastService.success(`Welcome back, ${res.user.fullName}!`);
        const targetRoute = this.authService.getDashboardRouteForRole(res.user.role);
        const returnUrl = this.route.snapshot.queryParams['returnUrl'];
        if (returnUrl && typeof returnUrl === 'string' && returnUrl.startsWith('/') && returnUrl !== '/login') {
          this.router.navigateByUrl(returnUrl);
        } else {
          this.router.navigate([targetRoute]);
        }
      },
      error: (err) => {
        this.isLoading = false;
        if (err.status === 401) {
          this.errorMessage = 'Invalid email or password.';
        } else if (err.error?.detail) {
          this.errorMessage = err.error.detail;
        } else {
          this.errorMessage = 'An unexpected error occurred. Please try again.';
        }
      }
    });
  }
}
