import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { BadgeComponent } from '../../shared/components/badge/badge.component';

@Component({
  selector: 'app-parent-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, BadgeComponent],
  template: `
    <div class="min-h-screen bg-background flex flex-col pb-20 sm:pb-0">
      <!-- Top header -->
      <header class="h-16 bg-surface-card border-b border-border px-4 sm:px-6 flex items-center justify-between sticky top-0 z-30 shadow-sm">
        <div class="flex items-center gap-3">
          <div class="w-9 h-9 rounded-lg bg-primary flex items-center justify-center text-white font-bold text-sm">
            LS
          </div>
          <div>
            <span class="text-sm font-bold text-text-main leading-tight block">Little Steps</span>
            <p class="text-xs text-text-muted">Parent Portal</p>
          </div>
        </div>

        <div class="flex items-center gap-3">
          <div class="hidden sm:block text-right">
            <p class="text-xs font-semibold text-text-main">{{ authService.currentUser()?.fullName || 'Parent' }}</p>
            <p class="text-xs text-text-muted">{{ authService.currentUser()?.email }}</p>
          </div>
          <app-badge variant="success" text="Parent"></app-badge>
          <button
            (click)="authService.logout()"
            title="Log out"
            aria-label="Log out"
            class="min-h-[44px] min-w-[44px] flex items-center justify-center text-text-muted hover:text-danger rounded-input hover:bg-red-50 transition-colors cursor-pointer"
          >
            <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
            </svg>
          </button>
        </div>
      </header>

      <!-- Desktop nav bar (for screens >= 640px) -->
      <nav class="hidden sm:flex bg-surface-card border-b border-border px-6 py-2 gap-2">
        <a
          routerLink="/parent/home"
          routerLinkActive="bg-primary-soft text-primary font-semibold"
          ariaCurrentWhenActive="page"
          class="px-4 py-2 rounded-input text-sm text-text-muted hover:text-text-main transition-colors"
        >
          Home
        </a>
        <a
          routerLink="/parent/incidents"
          routerLinkActive="bg-primary-soft text-primary font-semibold"
          ariaCurrentWhenActive="page"
          class="px-4 py-2 rounded-input text-sm text-text-muted hover:text-text-main transition-colors"
        >
          Incidents
        </a>
        <a
          routerLink="/parent/invoices"
          routerLinkActive="bg-primary-soft text-primary font-semibold"
          ariaCurrentWhenActive="page"
          class="px-4 py-2 rounded-input text-sm text-text-muted hover:text-text-main transition-colors"
        >
          Invoices
        </a>
        <a
          routerLink="/parent/announcements"
          routerLinkActive="bg-primary-soft text-primary font-semibold"
          ariaCurrentWhenActive="page"
          class="px-4 py-2 rounded-input text-sm text-text-muted hover:text-text-main transition-colors"
        >
          Announcements
        </a>
        <a
          routerLink="/parent/profile"
          routerLinkActive="bg-primary-soft text-primary font-semibold"
          ariaCurrentWhenActive="page"
          class="px-4 py-2 rounded-input text-sm text-text-muted hover:text-text-main transition-colors"
        >
          Profile
        </a>
      </nav>

      <!-- Main content -->
      <main class="flex-1 p-4 sm:p-6 max-w-4xl mx-auto w-full">
        <router-outlet></router-outlet>
      </main>

      <!-- Mobile Bottom Tab Bar (fixed at bottom on mobile screens, touch targets >= 44px) -->
      <nav class="sm:hidden fixed bottom-0 inset-x-0 bg-surface-card border-t border-border z-40 flex items-center justify-around h-16 shadow-lg">
        <a
          routerLink="/parent/home"
          routerLinkActive="text-primary font-semibold"
          ariaCurrentWhenActive="page"
          class="flex flex-col items-center justify-center flex-1 h-full text-text-muted hover:text-primary transition-colors text-xs min-h-[44px]"
        >
          <svg class="w-5 h-5 mb-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
          </svg>
          Home
        </a>

        <a
          routerLink="/parent/incidents"
          routerLinkActive="text-primary font-semibold"
          ariaCurrentWhenActive="page"
          class="flex flex-col items-center justify-center flex-1 h-full text-text-muted hover:text-primary transition-colors text-xs min-h-[44px]"
        >
          <svg class="w-5 h-5 mb-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
          Incidents
        </a>

        <a
          routerLink="/parent/invoices"
          routerLinkActive="text-primary font-semibold"
          ariaCurrentWhenActive="page"
          class="flex flex-col items-center justify-center flex-1 h-full text-text-muted hover:text-primary transition-colors text-xs min-h-[44px]"
        >
          <svg class="w-5 h-5 mb-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 14l6-6m-5.5.5h.01m4.99 5h.01M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16l3.5-2 3.5 2 3.5-2 3.5 2z" />
          </svg>
          Invoices
        </a>

        <a
          routerLink="/parent/announcements"
          routerLinkActive="text-primary font-semibold"
          ariaCurrentWhenActive="page"
          class="flex flex-col items-center justify-center flex-1 h-full text-text-muted hover:text-primary transition-colors text-xs min-h-[44px]"
        >
          <svg class="w-5 h-5 mb-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5.882V19.24a1.76 1.76 0 01-3.417.592l-2.147-6.15M18 13a3 3 0 100-6M5.436 13.683A4.001 4.001 0 017 6h1.832c4.1 0 7.625-1.234 9.168-3v14c-1.543-1.766-5.067-3-9.168-3H7a3.988 3.988 0 01-1.564-.317z" />
          </svg>
          News
        </a>

        <a
          routerLink="/parent/profile"
          routerLinkActive="text-primary font-semibold"
          ariaCurrentWhenActive="page"
          class="flex flex-col items-center justify-center flex-1 h-full text-text-muted hover:text-primary transition-colors text-xs min-h-[44px]"
        >
          <svg class="w-5 h-5 mb-0.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
          </svg>
          Profile
        </a>
      </nav>
    </div>
  `
})
export class ParentShellComponent {
  public authService = inject(AuthService);
}
