import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BillingService, InvoiceDto, GenerateInvoicesResultDto } from '../../../core/services/billing.service';
import { ToastService } from '../../../core/services/toast.service';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';
import { DialogComponent } from '../../../shared/components/dialog/dialog.component';
import { ButtonComponent } from '../../../shared/components/button/button.component';

@Component({
  selector: 'app-admin-invoices',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, BadgeComponent, DialogComponent, ButtonComponent],
  template: `
    <div class="space-y-6 pb-12">
      <!-- Page Header & Action Bar -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-text-main tracking-tight">Billing & Invoices</h1>
          <p class="text-xs text-text-muted mt-0.5">Manage tuition invoices, payment reconciliation, and ledger balances.</p>
        </div>
        <div class="flex items-center gap-3">
          <button
            (click)="openGenerateModal()"
            class="px-4 py-2 rounded-input bg-primary text-white text-xs font-bold hover:bg-primary-hover transition shadow-sm flex items-center gap-1.5"
          >
            <span>+</span>
            <span>Generate Invoices</span>
          </button>
        </div>
      </div>

      <!-- Filters Bar -->
      <div class="bg-surface-card rounded-card border border-border p-4 shadow-card flex flex-col sm:flex-row items-center gap-4 justify-between">
        <div class="flex flex-col sm:flex-row items-center gap-3 w-full sm:w-auto flex-1">
          <div class="relative w-full sm:w-72">
            <input
              type="text"
              [(ngModel)]="search"
              (keyup.enter)="loadInvoices()"
              aria-label="Search invoices"
              placeholder="Search invoice # or child..."
              class="w-full h-9 pl-8 pr-3 rounded-input border border-border bg-white text-xs text-text-main focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
            />
            <svg class="w-4 h-4 absolute left-2.5 top-1/2 -translate-y-1/2 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
            </svg>
          </div>

          <select
            [(ngModel)]="statusFilter"
            (change)="loadInvoices()"
            aria-label="Filter by invoice status"
            class="w-full sm:w-44 h-9 px-3 rounded-input border border-border bg-white text-xs text-text-main focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
          >
            <option value="">All statuses</option>
            <option value="Issued">Issued</option>
            <option value="PartiallyPaid">Partially Paid</option>
            <option value="Paid">Paid</option>
            <option value="Void">Void</option>
          </select>
        </div>

        <button
          (click)="loadInvoices()"
          class="px-3 py-1.5 rounded-input border border-border bg-white text-xs font-semibold text-text-main hover:bg-slate-50 transition shadow-sm"
        >
          Refresh
        </button>
      </div>

      <!-- Invoices Table -->
      <div class="bg-surface-card rounded-card border border-border shadow-card overflow-hidden">
        <div *ngIf="loading" class="py-16 text-center text-text-muted">
          <div class="animate-spin inline-block w-8 h-8 border-3 border-primary border-t-transparent rounded-full mb-3"></div>
          <p class="text-xs font-medium">Loading invoices ledger...</p>
        </div>

        <div *ngIf="!loading && invoices.length === 0" class="py-16 text-center text-text-muted text-xs">
          No invoices found matching current criteria. Click "Generate Invoices" to create billing records.
        </div>

        <div *ngIf="!loading && invoices.length > 0" class="overflow-x-auto">
          <table class="w-full text-left border-collapse text-xs">
            <thead>
              <tr class="bg-slate-50 border-b border-border text-text-muted uppercase tracking-wider">
                <th class="p-3.5 font-semibold">Invoice #</th>
                <th class="p-3.5 font-semibold">Child</th>
                <th class="p-3.5 font-semibold">Payer / Guardian</th>
                <th class="p-3.5 font-semibold">Period</th>
                <th class="p-3.5 font-semibold">Due Date</th>
                <th class="p-3.5 font-semibold text-right">Total</th>
                <th class="p-3.5 font-semibold text-right">Paid</th>
                <th class="p-3.5 font-semibold text-right">Balance</th>
                <th class="p-3.5 font-semibold text-center">Status</th>
                <th class="p-3.5 font-semibold text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-border">
              <tr *ngFor="let inv of invoices" class="hover:bg-slate-50/60 transition">
                <td class="p-3.5 font-mono font-bold text-text-main">
                  <a [routerLink]="['/admin/invoices', inv.id]" class="hover:text-primary hover:underline">
                    {{ inv.invoiceNumber }}
                  </a>
                </td>
                <td class="p-3.5 font-medium text-text-main">
                  {{ inv.childName }}
                </td>
                <td class="p-3.5 text-text-muted">
                  {{ inv.guardianName }}
                </td>
                <td class="p-3.5 text-text-muted">
                  {{ inv.periodStart | slice:0:7 }}
                </td>
                <td class="p-3.5 text-text-muted">
                  {{ inv.dueDate }}
                </td>
                <td class="p-3.5 font-semibold text-text-main text-right font-mono tabular-nums">
                  ETB {{ inv.totalAmount | number:'1.2-2' }}
                </td>
                <td class="p-3.5 text-success font-medium text-right font-mono tabular-nums">
                  ETB {{ inv.amountPaid | number:'1.2-2' }}
                </td>
                <td class="p-3.5 font-bold text-right font-mono tabular-nums" [class.text-danger]="inv.balance > 0 && inv.status !== 'Void'" [class.text-text-muted]="inv.balance === 0 || inv.status === 'Void'">
                  ETB {{ inv.balance | number:'1.2-2' }}
                </td>
                <td class="p-3.5 text-center">
                  <app-badge [status]="inv.status" [text]="inv.status"></app-badge>
                </td>
                <td class="p-3.5 text-right">
                  <a
                    [routerLink]="['/admin/invoices', inv.id]"
                    class="px-2.5 py-1 rounded-input border border-border text-xs font-semibold text-text-main hover:bg-slate-100 transition"
                  >
                    View
                  </a>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Pagination -->
        <div *ngIf="totalPages > 1" class="p-4 border-t border-border flex items-center justify-between text-xs text-text-muted">
          <span>Page {{ page }} of {{ totalPages }} ({{ totalCount }} invoices)</span>
          <div class="flex items-center gap-2">
            <button
              (click)="changePage(page - 1)"
              [disabled]="page <= 1"
              class="px-3 py-1 rounded border border-border bg-white disabled:opacity-40"
            >
              Previous
            </button>
            <button
              (click)="changePage(page + 1)"
              [disabled]="page >= totalPages"
              class="px-3 py-1 rounded border border-border bg-white disabled:opacity-40"
            >
              Next
            </button>
          </div>
        </div>
      </div>

      <!-- GENERATE INVOICES MODAL -->
      <app-dialog
        [(isOpen)]="generateModalOpen"
        title="Generate Monthly Invoices"
      >
        <div class="space-y-4 text-xs">
          <p class="text-text-muted">
            Generate monthly tuition invoices for all currently enrolled children with active fee plans.
          </p>

          <div>
            <label class="block font-semibold text-text-main mb-1">Billing Period Start <span class="text-danger">*</span></label>
            <input
              type="date"
              [(ngModel)]="genForm.periodStart"
              class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs text-text-main"
            />
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">Billing Period End <span class="text-danger">*</span></label>
            <input
              type="date"
              [(ngModel)]="genForm.periodEnd"
              class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs text-text-main"
            />
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">Payment Due Date <span class="text-danger">*</span></label>
            <input
              type="date"
              [(ngModel)]="genForm.dueDate"
              class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs text-text-main"
            />
          </div>
        </div>

        <div dialog-actions class="flex justify-end gap-2">
          <app-button variant="secondary" (click)="generateModalOpen = false">Cancel</app-button>
          <app-button variant="primary" [loading]="generating" (click)="onGenerateInvoices()">Run Invoice Batch</app-button>
        </div>
      </app-dialog>

      <!-- GENERATION RESULT MODAL -->
      <app-dialog
        [(isOpen)]="resultModalOpen"
        title="Invoices Generation Complete"
      >
        <div *ngIf="genResult" class="space-y-3 text-xs">
          <div class="p-3 rounded-lg bg-success-soft text-success font-medium flex items-center gap-1.5">
            <svg class="w-4 h-4 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
            </svg>
            <span>Successfully created <strong>{{ genResult.generatedCount }}</strong> invoice(s) totalling <strong>ETB {{ genResult.totalAmount | number:'1.2-2' }}</strong>.</span>
          </div>
          <div *ngIf="genResult.skippedCount > 0" class="text-text-muted">
            Skipped {{ genResult.skippedCount }} children (already invoiced or no active fee plan).
          </div>
          <div *ngIf="genResult.errors && genResult.errors.length > 0" class="p-3 rounded-lg bg-danger-soft text-danger space-y-1">
            <p class="font-bold">Errors encountered:</p>
            <ul class="list-disc pl-4">
              <li *ngFor="let err of genResult.errors">{{ err }}</li>
            </ul>
          </div>
        </div>

        <div dialog-actions class="flex justify-end">
          <app-button variant="primary" (click)="resultModalOpen = false">Close</app-button>
        </div>
      </app-dialog>
    </div>
  `
})
export class AdminInvoicesComponent implements OnInit {
  private billingService = inject(BillingService);
  private toast = inject(ToastService);

  invoices: InvoiceDto[] = [];
  loading = true;
  search = '';
  statusFilter = '';
  page = 1;
  pageSize = 20;
  totalCount = 0;
  totalPages = 1;

  generateModalOpen = false;
  generating = false;
  genResult: GenerateInvoicesResultDto | null = null;
  resultModalOpen = false;

  genForm = {
    periodStart: '',
    periodEnd: '',
    dueDate: ''
  };

  ngOnInit(): void {
    this.setDefaultDates();
    this.loadInvoices();
  }

  setDefaultDates(): void {
    const now = new Date();
    const y = now.getFullYear();
    const m = now.getMonth();
    const start = new Date(y, m, 1);
    const end = new Date(y, m + 1, 0);
    const due = new Date(y, m, 5);

    this.genForm.periodStart = start.toISOString().split('T')[0];
    this.genForm.periodEnd = end.toISOString().split('T')[0];
    this.genForm.dueDate = due.toISOString().split('T')[0];
  }

  loadInvoices(): void {
    this.loading = true;
    this.billingService.getInvoices({
      search: this.search || undefined,
      status: this.statusFilter || undefined,
      page: this.page,
      pageSize: this.pageSize
    }).subscribe({
      next: (res) => {
        this.invoices = res.items;
        this.totalCount = res.totalCount;
        this.totalPages = res.totalPages;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toast.error('Failed to load invoices');
      }
    });
  }

  changePage(newPage: number): void {
    this.page = newPage;
    this.loadInvoices();
  }

  openGenerateModal(): void {
    this.setDefaultDates();
    this.generateModalOpen = true;
  }

  onGenerateInvoices(): void {
    if (!this.genForm.periodStart || !this.genForm.periodEnd || !this.genForm.dueDate) {
      this.toast.warning('Please fill in all dates');
      return;
    }

    this.generating = true;
    this.billingService.generateInvoices(this.genForm).subscribe({
      next: (res) => {
        this.generating = false;
        this.generateModalOpen = false;
        this.genResult = res;
        this.resultModalOpen = true;
        this.loadInvoices();
      },
      error: (err) => {
        this.generating = false;
        const msg = err.error?.detail || err.error?.title || 'Invoice generation failed';
        this.toast.error(msg);
      }
    });
  }
}
