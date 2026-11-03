import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { BadgeComponent } from '../../shared/components/badge/badge.component';

@Component({
  selector: 'app-admin-shell',
  standalone: true,
  imports: [CommonModule, RouterOutlet, RouterLink, RouterLinkActive, BadgeComponent],
  template: `
    <div class="min-h-screen bg-background flex">
      <!-- Desktop Sidebar (>= 1024px) -->
      <aside class="hidden lg:flex w-64 bg-surface-card border-r border-border flex-col fixed inset-y-0 z-30">
        <!-- Logo -->
        <div class="h-16 px-6 border-b border-border flex items-center gap-3">
          <div class="w-8 h-8 rounded-lg bg-primary flex items-center justify-center text-white font-bold text-sm">
            LS
          </div>
          <div>
            <span class="text-sm font-bold text-text-main leading-none block">Little Steps</span>
            <p class="text-xs text-text-muted mt-0.5">Daycare Admin</p>
          </div>
        </div>

        <!-- Navigation Links -->
        <nav class="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
          <a
            routerLink="/admin/dashboard"
            routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
            [routerLinkActiveOptions]="{ exact: true }"
            class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
          >
            <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
            </svg>
            <span>Dashboard</span>
          </a>

          <a
            routerLink="/admin/attendance"
            routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
            class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
          >
            <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
            </svg>
            <span>Attendance</span>
          </a>

          <a
            routerLink="/admin/children"
            routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
            class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
          >
            <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
            </svg>
            <span>Children</span>
          </a>

          <a
            routerLink="/admin/invoices"
            routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
            class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
          >
            <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 14l6-6m-5.5.5h.01m4.99 5h.01M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16l3.5-2 3.5 2 3.5-2 3.5 2zM10 8.5a.5.5 0 11-1 0 .5.5 0 011 0zm5 5a.5.5 0 11-1 0 .5.5 0 011 0z" />
            </svg>
            <span>Billing & Invoices</span>
          </a>

          <a
            routerLink="/admin/guardians"
            routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
            class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
          >
            <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
            </svg>
            <span>Guardians</span>
          </a>

          <a
            routerLink="/admin/staff"
            routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
            class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
          >
            <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
            </svg>
            <span>Staff</span>
          </a>

          <a
            routerLink="/admin/classrooms"
            routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
            class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
          >
            <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
            </svg>
            <span>Classrooms</span>
          </a>

          <a
            routerLink="/admin/incidents"
            routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
            class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
          >
            <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
            <span>Incidents</span>
          </a>

          <a
            routerLink="/admin/announcements"
            routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
            class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
          >
            <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5.882V19.24a1.76 1.76 0 01-3.417.592l-2.147-6.15M18 13a3 3 0 100-6M5.436 13.683A4.001 4.001 0 017 6h1.832c4.1 0 7.625-1.234 9.168-3v14c-1.543-1.766-5.067-3-9.168-3H7a3.988 3.988 0 01-1.564-.317z" />
            </svg>
            <span>Announcements</span>
          </a>

          <a
            routerLink="/admin/fee-plans"
            routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
            class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
          >
            <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
            </svg>
            <span>Fee Plans</span>
          </a>

          <a
            routerLink="/admin/reports"
            routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
            class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
          >
            <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
            <span>Reports</span>
          </a>
        </nav>

        <!-- Desktop User Section & Logout -->
        <div class="p-4 border-t border-border flex items-center justify-between">
          <div class="min-w-0 flex-1">
            <p class="text-xs font-semibold text-text-main truncate">{{ authService.currentUser()?.fullName || 'Admin User' }}</p>
            <p class="text-xs text-text-muted truncate">{{ authService.currentUser()?.email }}</p>
          </div>
          <button
            (click)="authService.logout()"
            title="Log out"
            aria-label="Log out"
            class="min-h-[44px] min-w-[44px] flex items-center justify-center text-text-muted hover:text-danger p-2 rounded-input hover:bg-red-50 transition-colors cursor-pointer"
          >
            <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
            </svg>
          </button>
        </div>
      </aside>

      <!-- Mobile Navigation Drawer Overlay (< 1024px) -->
      <div
        *ngIf="mobileMenuOpen"
        class="fixed inset-0 z-50 lg:hidden"
        role="dialog"
        aria-modal="true"
        aria-label="Mobile Administration Navigation"
      >
        <!-- Backdrop -->
        <div
          class="fixed inset-0 bg-slate-900/50 backdrop-blur-sm transition-opacity"
          (click)="mobileMenuOpen = false"
        ></div>

        <!-- Sliding Panel -->
        <div class="fixed inset-y-0 left-0 w-72 max-w-[85vw] bg-surface-card border-r border-border shadow-2xl flex flex-col z-50 animate-in slide-in-from-left duration-200">
          <!-- Drawer Header -->
          <div class="h-16 px-5 border-b border-border flex items-center justify-between">
            <div class="flex items-center gap-3">
              <div class="w-8 h-8 rounded-lg bg-primary flex items-center justify-center text-white font-bold text-sm">
                LS
              </div>
              <div>
                <span class="text-sm font-bold text-text-main leading-none block">Little Steps</span>
                <p class="text-xs text-text-muted mt-0.5">Admin Menu</p>
              </div>
            </div>
            <button
              type="button"
              (click)="mobileMenuOpen = false"
              aria-label="Close navigation menu"
              class="min-h-[44px] min-w-[44px] flex items-center justify-center p-2 text-text-muted hover:text-text-main rounded-input hover:bg-slate-100 cursor-pointer"
            >
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <!-- Mobile Nav Links -->
          <nav class="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
            <a
              routerLink="/admin/dashboard"
              (click)="mobileMenuOpen = false"
              routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
              [routerLinkActiveOptions]="{ exact: true }"
              class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
            >
              <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
              </svg>
              <span>Dashboard</span>
            </a>

            <a
              routerLink="/admin/attendance"
              (click)="mobileMenuOpen = false"
              routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
              class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
            >
              <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-6 9l2 2 4-4" />
              </svg>
              <span>Attendance</span>
            </a>

            <a
              routerLink="/admin/children"
              (click)="mobileMenuOpen = false"
              routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
              class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
            >
              <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
              </svg>
              <span>Children</span>
            </a>

            <a
              routerLink="/admin/invoices"
              (click)="mobileMenuOpen = false"
              routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
              class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
            >
              <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 14l6-6m-5.5.5h.01m4.99 5h.01M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16l3.5-2 3.5 2 3.5-2 3.5 2zM10 8.5a.5.5 0 11-1 0 .5.5 0 011 0zm5 5a.5.5 0 11-1 0 .5.5 0 011 0z" />
              </svg>
              <span>Billing & Invoices</span>
            </a>

            <a
              routerLink="/admin/guardians"
              (click)="mobileMenuOpen = false"
              routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
              class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
            >
              <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
              <span>Guardians</span>
            </a>

            <a
              routerLink="/admin/staff"
              (click)="mobileMenuOpen = false"
              routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
              class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
            >
              <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 13.255A23.931 23.931 0 0112 15c-3.183 0-6.22-.62-9-1.745M16 6V4a2 2 0 00-2-2h-4a2 2 0 00-2 2v2m4 6h.01M5 20h14a2 2 0 002-2V8a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z" />
              </svg>
              <span>Staff</span>
            </a>

            <a
              routerLink="/admin/classrooms"
              (click)="mobileMenuOpen = false"
              routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
              class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
            >
              <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
              </svg>
              <span>Classrooms</span>
            </a>

            <a
              routerLink="/admin/incidents"
              (click)="mobileMenuOpen = false"
              routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
              class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
            >
              <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
              <span>Incidents</span>
            </a>

            <a
              routerLink="/admin/announcements"
              (click)="mobileMenuOpen = false"
              routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
              class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
            >
              <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5.882V19.24a1.76 1.76 0 01-3.417.592l-2.147-6.15M18 13a3 3 0 100-6M5.436 13.683A4.001 4.001 0 017 6h1.832c4.1 0 7.625-1.234 9.168-3v14c-1.543-1.766-5.067-3-9.168-3H7a3.988 3.988 0 01-1.564-.317z" />
              </svg>
              <span>Announcements</span>
            </a>

            <a
              routerLink="/admin/fee-plans"
              (click)="mobileMenuOpen = false"
              routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
              class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
            >
              <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8c-1.657 0-3 .895-3 2s1.343 2 3 2 3 .895 3 2-1.343 2-3 2m0-8c1.11 0 2.08.402 2.599 1M12 8V7m0 1v8m0 0v1m0-1c-1.11 0-2.08-.402-2.599-1M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <span>Fee Plans</span>
            </a>

            <a
              routerLink="/admin/reports"
              (click)="mobileMenuOpen = false"
              routerLinkActive="bg-primary-soft text-primary font-semibold"
            ariaCurrentWhenActive="page"
              class="min-h-[44px] flex items-center gap-3 px-3 py-2.5 rounded-input text-sm text-text-muted hover:text-text-main hover:bg-slate-50 transition-colors"
            >
              <svg class="w-5 h-5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 17v-2m3 2v-4m3 4v-6m2 10H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              <span>Reports</span>
            </a>
          </nav>

          <!-- Mobile Drawer Footer & Logout -->
          <div class="p-4 border-t border-border flex items-center justify-between bg-slate-50">
            <div class="min-w-0 flex-1">
              <p class="text-xs font-semibold text-text-main truncate">{{ authService.currentUser()?.fullName || 'Admin User' }}</p>
              <p class="text-xs text-text-muted truncate">{{ authService.currentUser()?.email }}</p>
            </div>
            <button
              (click)="authService.logout(); mobileMenuOpen = false"
              title="Log out"
              aria-label="Log out"
              class="min-h-[44px] min-w-[44px] flex items-center justify-center text-text-muted hover:text-danger p-2 rounded-input hover:bg-red-50 transition-colors cursor-pointer"
            >
              <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 16l4-4m0 0l-4-4m4 4H7m6 4v1a3 3 0 01-3 3H6a3 3 0 01-3-3V7a3 3 0 013-3h4a3 3 0 013 3v1" />
              </svg>
            </button>
          </div>
        </div>
      </div>

      <!-- Main content area -->
      <div class="flex-1 flex flex-col lg:pl-64 pl-0 min-w-0">
        <!-- Top bar -->
        <header class="h-16 bg-surface-card border-b border-border px-4 sm:px-6 lg:px-8 flex items-center justify-between sticky top-0 z-20">
          <div class="flex items-center gap-3">
            <!-- Mobile Hamburger Button (< 1024px) -->
            <button
              type="button"
              (click)="mobileMenuOpen = true"
              aria-label="Open navigation menu"
              class="lg:hidden p-2 rounded-input text-text-muted hover:text-text-main hover:bg-slate-100 transition-colors cursor-pointer min-h-[44px] min-w-[44px] flex items-center justify-center -ml-2"
            >
              <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 6h16M4 12h16M4 18h16" />
              </svg>
            </button>

            <span class="text-xs font-semibold text-text-muted uppercase tracking-wider truncate">
              Little Steps Administration
            </span>
          </div>

          <div class="flex items-center gap-3">
            <app-badge variant="primary" text="Admin Portal"></app-badge>
          </div>
        </header>

        <!-- View outlet -->
        <main class="flex-1 p-4 sm:p-6 lg:p-8 min-w-0">
          <router-outlet></router-outlet>
        </main>
      </div>
    </div>
  `
})
export class AdminShellComponent {
  public authService = inject(AuthService);
  public mobileMenuOpen = false;
}

