import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { DashboardService, AdminDashboardDto } from '../../../core/services/dashboard.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-admin-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <div class="space-y-6 pb-12">
      <!-- Page Header -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-text-main tracking-tight">Admin Operations Dashboard</h1>
          <p class="text-xs text-text-muted mt-0.5">Real-time facility overview, statutory ratios, and financial status.</p>
        </div>
        <div class="flex items-center gap-2 self-start sm:self-auto">
          <span class="inline-flex items-center gap-1.5 text-xs font-semibold px-3 py-1.5 rounded-full bg-slate-100 text-text-main border border-border">
            <svg class="w-3.5 h-3.5 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
            {{ todayFormatted }}
          </span>
          <button
            (click)="loadDashboard()"
            class="p-2 rounded-input border border-border bg-white hover:bg-slate-50 text-text-muted hover:text-text-main transition shadow-sm"
            title="Refresh dashboard"
          >
            <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
            </svg>
          </button>
        </div>
      </div>

      <!-- CRITICAL RATIO WARNING BANNER (BR-13) -->
      <div
        *ngIf="dashboard && overRatioCount > 0"
        class="p-4 rounded-card bg-danger-soft border-2 border-danger text-danger flex items-start sm:items-center justify-between gap-4 animate-pulse shadow-sm"
      >
        <div class="flex items-center gap-3">
          <svg class="w-6 h-6 shrink-0 text-danger" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
          <div>
            <h3 class="text-sm font-bold">STATUTORY RATIO BREACH ALERT</h3>
            <p class="text-xs mt-0.5 text-danger font-medium">
              {{ overRatioCount }} classroom(s) currently exceed statutory staff-to-child ratio limits! Immediate staff reassignment required.
            </p>
          </div>
        </div>
        <a
          routerLink="/admin/attendance"
          class="shrink-0 px-3 py-1.5 rounded-input bg-danger text-white text-xs font-bold hover:bg-red-700 transition"
        >
          View Attendance Board &rarr;
        </a>
      </div>

      <div *ngIf="loading" class="py-16 text-center text-text-muted">
        <div class="animate-spin inline-block w-8 h-8 border-3 border-primary border-t-transparent rounded-full mb-3"></div>
        <p class="text-sm font-medium">Loading operations data...</p>
      </div>

      <div *ngIf="!loading && dashboard" class="space-y-6">
        <!-- ROW 1: 4 Stat Cards -->
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <!-- Card 1: Children Enrolled -->
          <div class="bg-surface-card rounded-card p-5 border border-border shadow-card flex flex-col justify-between">
            <div class="flex items-center justify-between text-text-muted">
              <span class="text-xs font-semibold uppercase tracking-wider">Enrolled Children</span>
              <svg class="w-5 h-5 text-primary" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 20h5v-2a3 3 0 00-5.356-1.857M17 20H7m10 0v-2c0-.656-.126-1.283-.356-1.857M7 20H2v-2a3 3 0 015.356-1.857M7 20v-2c0-.656.126-1.283.356-1.857m0 0a5.002 5.002 0 019.288 0M15 7a3 3 0 11-6 0 3 3 0 016 0zm6 3a2 2 0 11-4 0 2 2 0 014 0zM7 10a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
            </div>
            <div class="mt-4 flex items-baseline justify-between">
              <span class="text-3xl font-extrabold text-text-main">{{ dashboard.enrolledChildren }}</span>
              <a routerLink="/admin/children" class="text-xs font-medium text-primary hover:underline">View all</a>
            </div>
          </div>

          <!-- Card 2: Present Today -->
          <div class="bg-surface-card rounded-card p-5 border border-border shadow-card flex flex-col justify-between">
            <div class="flex items-center justify-between text-text-muted">
              <span class="text-xs font-semibold uppercase tracking-wider">Present Today</span>
              <svg class="w-5 h-5 text-success" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12l2 2 4-4m6 2a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <div class="mt-4 flex flex-col">
              <span class="text-3xl font-extrabold text-success">{{ dashboard.presentToday }}</span>
              <span class="text-xs text-text-muted mt-1">Currently in daycare facility</span>
            </div>
          </div>

          <!-- Card 3: Absent -->
          <div class="bg-surface-card rounded-card p-5 border border-border shadow-card flex flex-col justify-between">
            <div class="flex items-center justify-between text-text-muted">
              <span class="text-xs font-semibold uppercase tracking-wider">Excused Absent</span>
              <svg class="w-5 h-5 text-danger" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
              </svg>
            </div>
            <div class="mt-4 flex flex-col">
              <span class="text-3xl font-extrabold text-danger">{{ dashboard.absentToday }}</span>
              <span class="text-xs text-text-muted mt-1">Reported by parents/guardians</span>
            </div>
          </div>

          <!-- Card 4: Not Arrived Yet -->
          <div class="bg-surface-card rounded-card p-5 border border-border shadow-card flex flex-col justify-between">
            <div class="flex items-center justify-between text-text-muted">
              <span class="text-xs font-semibold uppercase tracking-wider">Not Arrived Yet</span>
              <svg class="w-5 h-5 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
            </div>
            <div class="mt-4 flex flex-col">
              <span class="text-3xl font-extrabold text-text-main">{{ dashboard.notArrivedToday }}</span>
              <span class="text-xs text-text-muted mt-1">Pending check-in today</span>
            </div>
          </div>
        </div>

        <!-- ROW 2: Classroom Statutory Ratios Table -->
        <div class="bg-surface-card rounded-card border border-border shadow-card overflow-hidden">
          <div class="p-5 border-b border-border flex items-center justify-between">
            <div>
              <h2 class="text-base font-bold text-text-main">Classroom Supervision & Statutory Ratios</h2>
              <p class="text-xs text-text-muted mt-0.5">Live ratios compared against legal statutory ceilings</p>
            </div>
            <a routerLink="/admin/attendance" class="text-xs font-semibold text-primary hover:underline flex items-center gap-1">
              Attendance Roster &rarr;
            </a>
          </div>

          <div class="overflow-x-auto">
            <table class="w-full text-left border-collapse text-xs">
              <thead>
                <tr class="bg-slate-50 border-b border-border text-text-muted uppercase tracking-wider">
                  <th class="p-4 font-semibold">Classroom</th>
                  <th class="p-4 font-semibold">Children Present</th>
                  <th class="p-4 font-semibold">Staff Assigned</th>
                  <th class="p-4 font-semibold">Statutory Ratio</th>
                  <th class="p-4 font-semibold">Max Capacity</th>
                  <th class="p-4 font-semibold text-right">Ratio Status</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-border">
                <tr
                  *ngFor="let room of dashboard.ratios"
                  [class.bg-danger-soft]="room.isOverLimit"
                  class="hover:bg-slate-50/60 transition"
                >
                  <td class="p-4 font-bold text-text-main flex items-center gap-2">
                    <span
                      class="w-2.5 h-2.5 rounded-full"
                      [class.bg-danger]="room.isOverLimit"
                      [class.bg-success]="!room.isOverLimit"
                    ></span>
                    {{ room.classroomName }}
                  </td>
                  <td class="p-4 font-semibold" [class.text-danger]="room.isOverLimit">
                    {{ room.presentCount }} present
                  </td>
                  <td class="p-4 font-medium text-text-main">
                    {{ room.staffCount }} staff
                  </td>
                  <td class="p-4 text-text-muted">
                    1:{{ room.maxChildrenPerStaff }} (Max allowed: {{ room.allowedCount }})
                  </td>
                  <td class="p-4 text-text-muted">
                    {{ room.capacity || room.allowedCount }}
                  </td>
                  <td class="p-4 text-right">
                    <span
                      *ngIf="room.isOverLimit"
                      class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-danger text-white font-bold text-xs shadow-sm animate-pulse"
                    >
                      <svg class="w-3.5 h-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                      </svg>
                      Over Limit
                    </span>
                    <span
                      *ngIf="!room.isOverLimit"
                      class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full bg-success-soft text-success font-semibold text-xs"
                    >
                      <svg class="w-3.5 h-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                      </svg>
                      Compliant
                    </span>
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <!-- ROW 3: Monthly Billing & Attendance Trend -->
        <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
          <!-- Billing Summary Card -->
          <div class="bg-surface-card rounded-card border border-border p-5 shadow-card flex flex-col justify-between">
            <div>
              <div class="flex items-center justify-between pb-3 border-b border-border">
                <div>
                  <h2 class="text-sm font-bold text-text-main">Financial & Billing Overview</h2>
                  <p class="text-xs text-text-muted">Current month tuition and ledger balances</p>
                </div>
                <a routerLink="/admin/invoices" class="text-xs font-semibold text-primary hover:underline">
                  Invoices &rarr;
                </a>
              </div>

              <div class="grid grid-cols-3 gap-3 my-5">
                <div class="p-3 rounded-lg bg-slate-50 border border-border">
                  <span class="text-xs font-semibold text-text-muted uppercase">Invoiced</span>
                  <p class="text-base font-bold text-text-main mt-1 font-mono tabular-nums">
                    ETB {{ dashboard.billing.invoiced | number:'1.2-2' }}
                  </p>
                  <span class="text-xs text-text-muted">Current period</span>
                </div>

                <div class="p-3 rounded-lg bg-success-soft/30 border border-success/20">
                  <span class="text-xs font-semibold text-success uppercase">Collected</span>
                  <p class="text-base font-bold text-success mt-1 font-mono tabular-nums">
                    ETB {{ dashboard.billing.collected | number:'1.2-2' }}
                  </p>
                  <span class="text-xs text-success">Received this month</span>
                </div>

                <div class="p-3 rounded-lg bg-danger-soft/30 border border-danger/20">
                  <span class="text-xs font-semibold text-danger uppercase">Outstanding (all months)</span>
                  <p class="text-base font-bold text-danger mt-1 font-mono tabular-nums">
                    ETB {{ dashboard.billing.outstanding | number:'1.2-2' }}
                  </p>
                  <span class="text-xs text-danger">Unpaid balance across all billing cycles ({{ dashboard.billing.overdueCount }} overdue)</span>
                </div>
              </div>

              <!-- Collection Rate Progress -->
              <div>
                <div *ngIf="dashboard.billing.invoiced > 0">
                  <div class="flex justify-between items-center text-xs mb-1.5">
                    <span class="font-semibold text-text-muted">Collection Rate</span>
                    <span class="font-bold text-primary">{{ collectionRate }}% collected</span>
                  </div>
                  <div class="w-full bg-slate-100 rounded-full h-2.5 overflow-hidden">
                    <div
                      class="bg-primary h-2.5 rounded-full transition-all duration-500"
                      [style.width.%]="collectionRate"
                    ></div>
                  </div>
                </div>
                <div *ngIf="dashboard.billing.invoiced <= 0" class="text-xs text-text-muted italic py-1">
                  No invoices issued this period
                </div>
              </div>
            </div>

            <div class="pt-4 border-t border-border mt-4 flex items-center justify-between text-xs text-text-muted">
              <span>Overdue Invoices: <strong class="text-danger">{{ dashboard.billing.overdueCount }}</strong></span>
              <a routerLink="/admin/invoices" class="text-primary font-semibold hover:underline">Generate Invoices &rarr;</a>
            </div>
          </div>

          <!-- Attendance Trend Card -->
          <div class="bg-surface-card rounded-card border border-border p-5 shadow-card flex flex-col justify-between">
            <div>
              <div class="flex items-center justify-between pb-3 border-b border-border">
                <div>
                  <h2 class="text-sm font-bold text-text-main">Recent Attendance History</h2>
                  <p class="text-xs text-text-muted">Daily present counts across the previous days</p>
                </div>
                <span class="text-xs text-text-muted">Trend</span>
              </div>

              <div *ngIf="dashboard.attendanceTrend.length === 0" class="py-10 text-center text-text-muted text-xs">
                No past trend records available.
              </div>

              <div *ngIf="dashboard.attendanceTrend.length > 0" class="space-y-3 my-4">
                <div
                  *ngFor="let item of dashboard.attendanceTrend"
                  class="flex items-center justify-between p-2.5 rounded-lg bg-slate-50 border border-border text-xs"
                >
                  <span class="font-medium text-text-main">{{ item.date }}</span>
                  <div class="flex items-center gap-3">
                    <span class="font-bold text-primary">{{ item.present }} children present</span>
                    <div class="w-24 bg-slate-200 rounded-full h-2 overflow-hidden">
                      <div
                        class="bg-primary h-2 rounded-full"
                        [style.width.%]="(item.present / dashboard.enrolledChildren) * 100"
                      ></div>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div class="pt-4 border-t border-border mt-2 flex items-center justify-between text-xs text-text-muted">
              <span>Facility Capacity: <strong>36 total seats</strong></span>
              <a routerLink="/admin/attendance" class="text-primary font-semibold hover:underline">Open Attendance Board</a>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class AdminDashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private toast = inject(ToastService);

  dashboard: AdminDashboardDto | null = null;
  loading = true;
  todayFormatted = new Date().toLocaleDateString('en-US', {
    weekday: 'short',
    year: 'numeric',
    month: 'short',
    day: 'numeric'
  });

  get overRatioCount(): number {
    return this.dashboard?.ratios.filter(r => r.isOverLimit).length || 0;
  }

  get collectionRate(): number {
    if (!this.dashboard || this.dashboard.billing.invoiced <= 0) return 0;
    const rate = (this.dashboard.billing.collected / this.dashboard.billing.invoiced) * 100;
    return Math.min(100, Math.round(rate * 10) / 10);
  }

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    this.loading = true;
    this.dashboardService.getAdminDashboard().subscribe({
      next: (res) => {
        this.dashboard = res;
        this.loading = false;
      },
      error: (err) => {
        this.loading = false;
        this.toast.error('Failed to load dashboard data');
      }
    });
  }
}
