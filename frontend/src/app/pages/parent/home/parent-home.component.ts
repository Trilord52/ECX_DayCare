import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../../../core/services/auth.service';
import { ChildrenService, ChildListItemDto } from '../../../core/services/children.service';
import { DailyReportsService, DailyReportDetailDto } from '../../../core/services/daily-reports.service';
import { BillingService, InvoiceDto } from '../../../core/services/billing.service';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';
import { AvatarComponent } from '../../../shared/components/avatar/avatar.component';

interface ChildWithSummary extends ChildListItemDto {
  report?: DailyReportDetailDto | null;
}

@Component({
  selector: 'app-parent-home',
  standalone: true,
  imports: [CommonModule, RouterLink, BadgeComponent, AvatarComponent],
  template: `
    <div class="max-w-md mx-auto space-y-6 pb-20">
      <!-- Greeting Header -->
      <section class="space-y-1">
        <div class="flex items-center justify-between text-xs text-text-muted">
          <span class="font-semibold text-primary uppercase tracking-wide">Little Steps Daycare</span>
          <span class="flex items-center gap-1">
            <svg class="w-3.5 h-3.5 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
            <span>{{ todayFormatted }}</span>
          </span>
        </div>
        <h1 class="text-2xl font-extrabold text-text-main tracking-tight">
          Hello, {{ user()?.fullName?.split(' ')?.at(0) || 'Parent' }}
        </h1>
        <p class="text-xs text-text-muted">Here is today's overview and live activities for your children.</p>
      </section>

      <!-- Outstanding Tuition Alert Card (if any pending balance) -->
      <div *ngIf="unpaidInvoice" class="p-4 rounded-card bg-amber-50 border border-warning text-text-main shadow-sm space-y-2">
        <div class="flex items-start justify-between gap-2">
          <div class="flex items-center gap-2">
            <svg class="w-5 h-5 text-amber-700 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
            </svg>
            <div>
              <p class="text-xs font-bold text-amber-900">Tuition Invoice Balance</p>
              <p class="text-xs text-amber-800">
                Due: {{ unpaidInvoice.dueDate }} • Balance: <strong class="font-extrabold font-mono tabular-nums">ETB {{ unpaidInvoice.balance | number:'1.2-2' }}</strong>
              </p>
            </div>
          </div>
          <span class="px-2 py-0.5 rounded-full bg-amber-200 text-amber-900 text-xs font-bold">Action Needed</span>
        </div>
        <div class="flex justify-end pt-1">
          <a
            routerLink="/parent/invoices"
            class="px-3 py-1.5 rounded-input bg-warning text-white text-xs font-bold hover:bg-amber-700 transition"
          >
            Review & Pay Online &rarr;
          </a>
        </div>
      </div>

      <!-- Loading State -->
      <div *ngIf="loading" class="py-16 text-center text-text-muted">
        <div class="animate-spin inline-block w-8 h-8 border-3 border-primary border-t-transparent rounded-full mb-3"></div>
        <p class="text-xs font-medium">Loading family records...</p>
      </div>

      <!-- Children List Section -->
      <section *ngIf="!loading" class="space-y-4">
        <div class="flex items-center justify-between">
          <h2 class="text-sm font-bold uppercase tracking-wider text-text-muted">My Children</h2>
          <span class="text-xs text-text-muted">{{ children.length }} enrolled</span>
        </div>

        <div *ngIf="children.length === 0" class="p-8 text-center text-text-muted text-xs bg-surface-card rounded-card border border-border">
          No children currently enrolled under this account.
        </div>

        <!-- Child Cards -->
        <article
          *ngFor="let child of children"
          class="bg-surface-card rounded-card border border-border p-5 shadow-card space-y-4 hover:border-primary/40 transition"
        >
          <!-- Child Header -->
          <div class="flex items-start justify-between gap-3">
            <div class="flex items-center gap-3">
              <app-avatar
                [kind]="'children'"
                [id]="child.id"
                [name]="child.firstName + ' ' + child.lastName"
                [hasPhoto]="child.hasPhoto ?? false"
                [photoUpdatedAt]="child.photoUpdatedAt"
                [size]="48"
              ></app-avatar>
              <div>
                <h3 class="text-base font-bold text-text-main leading-tight">{{ child.firstName }} {{ child.lastName }}</h3>
                <p class="text-xs text-text-muted mt-0.5">{{ child.classroomName }}</p>
              </div>
            </div>

            <app-badge [status]="child.status" [text]="child.status"></app-badge>
          </div>

          <!-- Allergy Flag if present -->
          <div *ngIf="child.allergies" class="px-2.5 py-1 rounded bg-danger-soft text-danger text-xs font-semibold flex items-center gap-1.5">
            <svg class="w-3.5 h-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
            <span>Allergy alert: {{ child.allergies }}</span>
          </div>

          <!-- Today's Activity Summary Box -->
          <div class="p-3 rounded-lg bg-slate-50 border border-border text-xs space-y-1.5">
            <div class="flex items-center justify-between text-text-muted">
              <span class="font-semibold text-text-main">Today's Routine</span>
              <span *ngIf="child.report?.mood" class="font-medium text-primary">Mood: {{ child.report?.mood }}</span>
            </div>

            <p *ngIf="!child.report || !child.report.entries || child.report.entries.length === 0" class="text-text-muted text-xs">
              No log entries recorded yet today. Teachers will post updates as activities occur.
            </p>

            <div *ngIf="child.report && child.report.entries && child.report.entries.length > 0" class="space-y-1">
              <div class="flex items-center gap-2 text-text-main">
                <svg class="w-3.5 h-3.5 shrink-0 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span class="font-medium">
                  Latest: {{ child.report.entries[child.report.entries.length - 1].entryType }} at {{ child.report.entries[child.report.entries.length - 1].occurredAt | slice:11:16 }}
                </span>
              </div>
              <p class="text-xs text-text-muted pl-5 italic truncate">
                {{ child.report.entries[child.report.entries.length - 1].description || 'Routine completed' }}
              </p>
            </div>
          </div>

          <!-- Action Button -->
          <div class="pt-1">
            <a
              [routerLink]="['/parent/children', child.id]"
              class="w-full h-10 rounded-input bg-primary text-white text-xs font-bold hover:bg-primary-hover shadow-sm transition flex items-center justify-center gap-1.5"
            >
              <svg class="w-4 h-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.164 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
              </svg>
              <span>See Full Today Timeline</span>
            </a>
          </div>
        </article>
      </section>
    </div>
  `
})
export class ParentHomeComponent implements OnInit {
  private authService = inject(AuthService);
  private childrenService = inject(ChildrenService);
  private dailyReportsService = inject(DailyReportsService);
  private billingService = inject(BillingService);

  user = this.authService.currentUser;
  loading = true;
  todayFormatted = new Date().toLocaleDateString('en-US', {
    weekday: 'short',
    month: 'short',
    day: 'numeric'
  });

  children: ChildWithSummary[] = [];
  unpaidInvoice: InvoiceDto | null = null;

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.childrenService.getChildren().subscribe({
      next: (res) => {
        const items = res.items;
        if (items.length === 0) {
          this.children = [];
          this.loading = false;
          return;
        }

        // Fetch reports for all children in parallel
        const reportObservables = items.map(c =>
          this.dailyReportsService.getTodayReport(c.id).pipe(catchError(() => of(null)))
        );

        forkJoin(reportObservables).subscribe({
          next: (reports) => {
            this.children = items.map((child, idx) => ({
              ...child,
              report: reports[idx]
            }));
            this.loading = false;
          },
          error: () => {
            this.children = items;
            this.loading = false;
          }
        });
      },
      error: () => {
        this.loading = false;
      }
    });

    // Check for open invoices
    this.billingService.getInvoices().subscribe({
      next: (res) => {
        const unpaid = res.items.find(inv => inv.status !== 'Paid' && inv.status !== 'Void' && inv.balance > 0);
        this.unpaidInvoice = unpaid || null;
      }
    });
  }
}
