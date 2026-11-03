import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ReportsService, AttendanceReportDto, BillingReportDto } from '../../../core/services/reports.service';
import { ClassroomService, ClassroomDto } from '../../../core/services/classroom.service';
import { ToastService } from '../../../core/services/toast.service';
import { ButtonComponent } from '../../../shared/components/button/button.component';

@Component({
  selector: 'app-admin-reports',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent],
  template: `
    <div class="space-y-6">
      <!-- Header -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold tracking-tight text-text-main">Operational Reports</h1>
          <p class="text-sm text-text-muted mt-0.5">Generate, audit, and export historical attendance records and financial billing ledgers</p>
        </div>
        <div class="flex items-center gap-2">
          <app-button variant="secondary" [loading]="exporting" (click)="onExportCsv()">
            <svg class="w-3.5 h-3.5 mr-1.5 inline shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
            </svg>
            Export CSV
          </app-button>
        </div>
      </div>

      <!-- Report Tabs -->
      <div class="flex border-b border-border text-xs font-semibold">
        <button
          (click)="activeTab = 'attendance'; loadReport()"
          [class.border-primary]="activeTab === 'attendance'"
          [class.text-primary]="activeTab === 'attendance'"
          [class.border-transparent]="activeTab !== 'attendance'"
          [class.text-text-muted]="activeTab !== 'attendance'"
          class="px-4 py-2.5 border-b-2 hover:text-text-main transition"
        >
          Attendance Report
        </button>
        <button
          (click)="activeTab = 'billing'; loadReport()"
          [class.border-primary]="activeTab === 'billing'"
          [class.text-primary]="activeTab === 'billing'"
          [class.border-transparent]="activeTab !== 'billing'"
          [class.text-text-muted]="activeTab !== 'billing'"
          class="px-4 py-2.5 border-b-2 hover:text-text-main transition"
        >
          Billing & Collection Report
        </button>
      </div>

      <!-- Filter Controls Bar -->
      <div class="flex flex-wrap items-center gap-3 bg-surface-card p-4 rounded-card border border-border text-xs">
        <div>
          <label class="block text-xs font-semibold text-text-muted mb-1">From Date</label>
          <input
            type="date"
            [(ngModel)]="dateFrom"
            (ngModelChange)="loadReport()"
            class="h-9 px-3 rounded-input border border-border bg-white text-xs text-text-main"
          />
        </div>

        <div>
          <label class="block text-xs font-semibold text-text-muted mb-1">To Date</label>
          <input
            type="date"
            [(ngModel)]="dateTo"
            (ngModelChange)="loadReport()"
            class="h-9 px-3 rounded-input border border-border bg-white text-xs text-text-main"
          />
        </div>

        <div *ngIf="activeTab === 'attendance'">
          <label class="block text-xs font-semibold text-text-muted mb-1">Classroom Filter</label>
          <select
            [(ngModel)]="selectedClassroom"
            (ngModelChange)="loadReport()"
            class="h-9 px-3 rounded-input border border-border bg-white text-xs text-text-main"
          >
            <option value="">All Classrooms</option>
            <option *ngFor="let room of classrooms" [value]="room.id">{{ room.name }}</option>
          </select>
        </div>
      </div>

      <!-- ATTENDANCE TAB CONTENT -->
      <div *ngIf="activeTab === 'attendance'" class="space-y-4">
        <!-- Metric Cards -->
        <div *ngIf="attendanceReport" class="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
            <span class="text-xs font-semibold text-text-muted">Total Present Days</span>
            <p class="text-2xl font-bold text-success mt-1">{{ attendanceReport.totalPresent }}</p>
          </div>
          <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
            <span class="text-xs font-semibold text-text-muted">Recorded Absences</span>
            <p class="text-2xl font-bold text-danger mt-1">{{ attendanceReport.totalAbsences }}</p>
          </div>
          <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
            <span class="text-xs font-semibold text-text-muted">Late Pickups</span>
            <p class="text-2xl font-bold text-amber-700 mt-1">{{ attendanceReport.totalLatePickups }}</p>
          </div>
          <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
            <span class="text-xs font-semibold text-text-muted">Total Late Minutes</span>
            <p class="text-2xl font-bold text-text-main mt-1">{{ attendanceReport.totalLateMinutes }} min</p>
          </div>
        </div>

        <div *ngIf="loading" class="p-12 text-center text-text-muted text-sm">
          Generating attendance report...
        </div>

        <div *ngIf="!loading && (!attendanceReport || attendanceReport.items.length === 0)" class="p-12 text-center bg-surface-card rounded-card border border-border">
          <p class="text-sm font-semibold text-text-main">No attendance records in selected date range</p>
        </div>

        <div *ngIf="!loading && attendanceReport && attendanceReport.items.length > 0" class="bg-surface-card rounded-card border border-border shadow-card overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-left border-collapse text-xs">
              <thead>
                <tr class="bg-slate-50 border-b border-border text-text-muted uppercase tracking-wider">
                  <th class="p-3 font-semibold">Date</th>
                  <th class="p-3 font-semibold">Child</th>
                  <th class="p-3 font-semibold">Classroom</th>
                  <th class="p-3 font-semibold">Status</th>
                  <th class="p-3 font-semibold">Check-In</th>
                  <th class="p-3 font-semibold">Check-Out</th>
                  <th class="p-3 font-semibold text-right">Late Minutes</th>
                  <th class="p-3 font-semibold">Absence Reason</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-border">
                <tr *ngFor="let item of attendanceReport.items" class="hover:bg-slate-50/50 transition">
                  <td class="p-3 font-mono text-text-main">{{ item.date }}</td>
                  <td class="p-3 font-bold text-text-main">{{ item.childName }}</td>
                  <td class="p-3 text-text-muted">{{ item.classroomName }}</td>
                  <td class="p-3">
                    <span
                      [class.text-success]="item.status === 'Present'"
                      [class.text-danger]="item.status === 'Absent'"
                      [class.text-text-muted]="item.status === 'CheckedOut'"
                      class="font-semibold"
                    >
                      {{ item.status }}
                    </span>
                  </td>
                  <td class="p-3 font-mono text-text-muted">{{ item.checkInAt ? (item.checkInAt | slice:11:16) : '—' }}</td>
                  <td class="p-3 font-mono text-text-muted">{{ item.checkOutAt ? (item.checkOutAt | slice:11:16) : '—' }}</td>
                  <td class="p-3 text-right font-mono" [class.text-danger]="item.lateMinutes > 0">
                    {{ item.lateMinutes > 0 ? (item.lateMinutes + ' min') : '—' }}
                  </td>
                  <td class="p-3 text-text-muted italic">{{ item.absenceReason || '—' }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>

      <!-- BILLING TAB CONTENT -->
      <div *ngIf="activeTab === 'billing'" class="space-y-4">
        <!-- Metric Cards -->
        <div *ngIf="billingReport" class="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
            <span class="text-xs font-semibold text-text-muted">Total Invoiced</span>
            <p class="text-xl font-bold text-text-main mt-1 font-mono tabular-nums">ETB {{ billingReport.totalBilled | number:'1.2-2' }}</p>
          </div>
          <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
            <span class="text-xs font-semibold text-text-muted">Collected Payments</span>
            <p class="text-xl font-bold text-success mt-1 font-mono tabular-nums">ETB {{ billingReport.totalCollected | number:'1.2-2' }}</p>
          </div>
          <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
            <span class="text-xs font-semibold text-text-muted">Outstanding Balance</span>
            <p class="text-xl font-bold text-danger mt-1 font-mono tabular-nums">ETB {{ billingReport.totalOutstanding | number:'1.2-2' }}</p>
          </div>
          <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
            <span class="text-xs font-semibold text-text-muted">Overdue Invoices</span>
            <p class="text-xl font-bold text-amber-700 mt-1">{{ billingReport.overdueInvoicesCount }}</p>
          </div>
        </div>

        <div *ngIf="loading" class="p-12 text-center text-text-muted text-sm">
          Generating billing report...
        </div>

        <div *ngIf="!loading && (!billingReport || billingReport.items.length === 0)" class="p-12 text-center bg-surface-card rounded-card border border-border">
          <p class="text-sm font-semibold text-text-main">No invoices recorded in selected period</p>
        </div>

        <div *ngIf="!loading && billingReport && billingReport.items.length > 0" class="bg-surface-card rounded-card border border-border shadow-card overflow-hidden">
          <div class="overflow-x-auto">
            <table class="w-full text-left border-collapse text-xs">
              <thead>
                <tr class="bg-slate-50 border-b border-border text-text-muted uppercase tracking-wider">
                  <th class="p-3 font-semibold">Invoice #</th>
                  <th class="p-3 font-semibold">Child</th>
                  <th class="p-3 font-semibold">Guardian / Payer</th>
                  <th class="p-3 font-semibold">Due Date</th>
                  <th class="p-3 font-semibold">Status</th>
                  <th class="p-3 font-semibold text-right">Invoiced</th>
                  <th class="p-3 font-semibold text-right">Paid</th>
                  <th class="p-3 font-semibold text-right">Balance Due</th>
                </tr>
              </thead>
              <tbody class="divide-y divide-border">
                <tr *ngFor="let item of billingReport.items" class="hover:bg-slate-50/50 transition">
                  <td class="p-3 font-mono font-bold text-text-main">{{ item.invoiceNumber }}</td>
                  <td class="p-3 font-medium text-text-main">{{ item.childName }}</td>
                  <td class="p-3 text-text-muted">{{ item.guardianName }}</td>
                  <td class="p-3 text-text-muted">{{ item.dueDate }}</td>
                  <td class="p-3">
                    <span
                      [class.bg-success-soft]="item.status === 'Paid'"
                      [class.text-success]="item.status === 'Paid'"
                      [class.bg-amber-100]="item.status === 'PartiallyPaid'"
                      [class.text-amber-800]="item.status === 'PartiallyPaid'"
                      [class.bg-blue-50]="item.status === 'Issued'"
                      [class.text-blue-700]="item.status === 'Issued'"
                      [class.bg-slate-100]="item.status === 'Void'"
                      [class.text-text-muted]="item.status === 'Void'"
                      class="px-2 py-0.5 rounded-full text-xs font-bold"
                    >
                      {{ item.status }}
                    </span>
                  </td>
                  <td class="p-3 text-right font-medium font-mono tabular-nums">ETB {{ item.totalAmount | number:'1.2-2' }}</td>
                  <td class="p-3 text-right text-success font-medium font-mono tabular-nums">ETB {{ item.amountPaid | number:'1.2-2' }}</td>
                  <td class="p-3 text-right font-bold font-mono tabular-nums" [class.text-danger]="item.balance > 0">
                    ETB {{ item.balance | number:'1.2-2' }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  `
})
export class AdminReportsComponent implements OnInit {
  private reportsService = inject(ReportsService);
  private classroomService = inject(ClassroomService);
  private toast = inject(ToastService);

  activeTab: 'attendance' | 'billing' = 'attendance';
  classrooms: ClassroomDto[] = [];
  selectedClassroom = '';

  dateFrom = '2026-10-01';
  dateTo = '2026-10-31';

  loading = true;
  exporting = false;
  attendanceReport: AttendanceReportDto | null = null;
  billingReport: BillingReportDto | null = null;

  ngOnInit(): void {
    this.classroomService.getClassrooms().subscribe({
      next: (rooms) => (this.classrooms = rooms),
      error: () => {}
    });
    this.loadReport();
  }

  loadReport(): void {
    this.loading = true;
    if (this.activeTab === 'attendance') {
      this.reportsService.getAttendanceReport(this.dateFrom, this.dateTo, this.selectedClassroom || undefined).subscribe({
        next: (res) => {
          this.attendanceReport = res;
          this.loading = false;
        },
        error: () => {
          this.loading = false;
          this.toast.error('Failed to load attendance report');
        }
      });
    } else {
      this.reportsService.getBillingReport(this.dateFrom, this.dateTo).subscribe({
        next: (res) => {
          this.billingReport = res;
          this.loading = false;
        },
        error: () => {
          this.loading = false;
          this.toast.error('Failed to load billing report');
        }
      });
    }
  }

  onExportCsv(): void {
    this.exporting = true;
    const download = (blob: Blob, filename: string) => {
      this.exporting = false;
      const url = window.URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = filename;
      a.click();
      window.URL.revokeObjectURL(url);
      this.toast.success('Report CSV exported');
    };

    if (this.activeTab === 'attendance') {
      this.reportsService.downloadAttendanceCsv(this.dateFrom, this.dateTo, this.selectedClassroom || undefined).subscribe({
        next: (blob) => download(blob, `attendance-report-${this.dateFrom}-to-${this.dateTo}.csv`),
        error: () => {
          this.exporting = false;
          this.toast.error('Failed to export attendance CSV');
        }
      });
    } else {
      this.reportsService.downloadBillingCsv(this.dateFrom, this.dateTo).subscribe({
        next: (blob) => download(blob, `billing-report-${this.dateFrom}-to-${this.dateTo}.csv`),
        error: () => {
          this.exporting = false;
          this.toast.error('Failed to export billing CSV');
        }
      });
    }
  }
}
