import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { BadgeComponent } from '../../shared/components/badge/badge.component';

@Component({
  selector: 'app-staff-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, BadgeComponent],
  template: `
    <div class="min-h-screen bg-background flex flex-col">
      <!-- Tablet-optimized Header with >=44px touch targets -->
      <header class="h-16 bg-surface-card border-b border-border px-6 flex items-center justify-between sticky top-0 z-30 shadow-sm">
        <div class="flex items-center gap-6">
          <!-- Logo -->
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-lg bg-primary flex items-center justify-center text-white font-bold text-base">
              LS
            </div>
            <div>
              <span class="text-base font-bold text-text-main leading-tight block">Little Steps</span>
              <p class="text-xs text-text-muted">Staff Room</p>
            </div>
          </div>

          <!-- Navigation (touch targets min 44px) -->
          <nav class="flex items-center gap-2">
            <a
              routerLink="/staff/my-day"
              routerLinkActive="bg-primary text-white font-medium"
              ariaCurrentWhenActive="page"
              class="min-h-[44px] px-4 flex items-center gap-2 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-100 transition-colors"
            >
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              My Day
            </a>

            <a
              routerLink="/staff/announcements"
              routerLinkActive="bg-primary text-white font-medium"
              ariaCurrentWhenActive="page"
              class="min-h-[44px] px-4 flex items-center gap-2 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-100 transition-colors"
            >
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5.882V19.24a1.76 1.76 0 01-3.417.592l-2.147-6.15M18 13a3 3 0 100-6M5.436 13.683A4.001 4.001 0 017 6h1.832c4.1 0 7.625-1.234 9.168-3v14c-1.543-1.766-5.067-3-9.168-3H7a3.988 3.988 0 01-1.564-.317z" />
              </svg>
              Announcements
            </a>
          </nav>
        </div>

        <!-- User info and logout -->
        <div class="flex items-center gap-4">
          <div class="hidden sm:block text-right">
            <p class="text-sm font-semibold text-text-main leading-tight">{{ authService.currentUser()?.fullName || 'Staff Member' }}</p>
            <p class="text-xs text-text-muted">{{ authService.currentUser()?.email }}</p>
          </div>
          <app-badge variant="info" text="Staff"></app-badge>
          <button
            (click)="authService.logout()"
            title="Log out"
            aria-label="Log out"
            class="min-h-[44px] min-w-[44px] flex items-center justify-center text-text-muted hover:text-danger rounded-input hover:bg-red-50 transition-colors cursor-pointer"
          >
            <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
            </svg>
          </button>
        </div>
      </header>

      <!-- Main Outlet -->
      <main class="flex-1 p-6 max-w-7xl mx-auto w-full">
        <router-outlet></router-outlet>
      </main>
    </div>
  `
})
export class StaffShellComponent {
  public authService = inject(AuthService);
}
