import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { BillingService, InvoiceDto, InvoiceLineDto } from '../../../core/services/billing.service';
import { ToastService } from '../../../core/services/toast.service';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';
import { DialogComponent } from '../../../shared/components/dialog/dialog.component';
import { ButtonComponent } from '../../../shared/components/button/button.component';

@Component({
  selector: 'app-parent-invoices',
  standalone: true,
  imports: [CommonModule, FormsModule, BadgeComponent, DialogComponent, ButtonComponent],
  template: `
    <div class="max-w-md mx-auto space-y-6 pb-24">
      <!-- Title Header -->
      <section class="space-y-1">
        <h1 class="text-2xl font-extrabold text-text-main tracking-tight">Tuition & Invoices</h1>
        <p class="text-xs text-text-muted">Review tuition statements and settle balances online.</p>
      </section>

      <!-- Account Balance Summary Card -->
      <div class="bg-surface-card rounded-card border border-border p-5 shadow-card space-y-4">
        <div class="flex items-center justify-between text-xs text-text-muted">
          <span class="font-semibold uppercase tracking-wider">Family Balance Due</span>
          <span class="px-2 py-0.5 rounded-full text-xs font-bold"
            [class.bg-danger-soft]="totalBalance > 0"
            [class.text-danger]="totalBalance > 0"
            [class.bg-success-soft]="totalBalance === 0"
            [class.text-success]="totalBalance === 0"
          >
            {{ totalBalance > 0 ? 'Payment Required' : 'All Settled' }}
          </span>
        </div>

        <div>
          <span class="text-3xl font-black text-text-main font-mono tabular-nums" [class.text-danger]="totalBalance > 0">
            ETB {{ totalBalance | number:'1.2-2' }}
          </span>
          <p class="text-xs text-text-muted mt-0.5">Across {{ pendingInvoices.length }} open billing statement(s)</p>
        </div>
      </div>

      <!-- Filter tabs -->
      <div class="flex items-center gap-2 border-b border-border pb-2 text-xs">
        <button
          type="button"
          (click)="filter = 'all'"
          [class]="filter === 'all' ? 'bg-primary-soft text-primary font-bold' : 'text-text-muted hover:text-text-main'"
          class="px-3 py-1.5 rounded-lg transition"
        >
          All ({{ invoices.length }})
        </button>
        <button
          type="button"
          (click)="filter = 'pending'"
          [class]="filter === 'pending' ? 'bg-primary-soft text-primary font-bold' : 'text-text-muted hover:text-text-main'"
          class="px-3 py-1.5 rounded-lg transition"
        >
          Pending ({{ pendingInvoices.length }})
        </button>
        <button
          type="button"
          (click)="filter = 'paid'"
          [class]="filter === 'paid' ? 'bg-primary-soft text-primary font-bold' : 'text-text-muted hover:text-text-main'"
          class="px-3 py-1.5 rounded-lg transition"
        >
          Settled ({{ paidInvoices.length }})
        </button>
      </div>

      <!-- Loading State -->
      <div *ngIf="loading" class="py-16 text-center text-text-muted">
        <div class="animate-spin inline-block w-8 h-8 border-3 border-primary border-t-transparent rounded-full mb-3"></div>
        <p class="text-xs font-medium">Loading billing statements...</p>
      </div>

      <!-- Invoices List -->
      <div *ngIf="!loading && displayedInvoices.length === 0" class="p-8 text-center text-text-muted text-xs bg-surface-card rounded-card border border-border">
        No statements found in this category.
      </div>

      <div *ngIf="!loading && displayedInvoices.length > 0" class="space-y-4">
        <article
          *ngFor="let inv of displayedInvoices"
          class="bg-surface-card rounded-card border border-border p-5 shadow-card space-y-4"
        >
          <!-- Invoice Header -->
          <div class="flex items-start justify-between gap-3">
            <div>
              <span class="font-mono text-xs font-bold text-text-main">{{ inv.invoiceNumber }}</span>
              <p class="text-xs font-semibold text-text-main mt-0.5">Child: {{ inv.childName }}</p>
            </div>
            <app-badge [status]="inv.status" [text]="inv.status"></app-badge>
          </div>

          <!-- Metadata Box -->
          <div class="p-3 rounded-lg bg-slate-50 border border-border text-xs space-y-1.5">
            <div class="flex justify-between text-text-muted">
              <span>Billing Period:</span>
              <span class="font-medium text-text-main">{{ inv.periodStart }} to {{ inv.periodEnd }}</span>
            </div>
            <div class="flex justify-between text-text-muted">
              <span>Due Date:</span>
              <span class="font-medium text-text-main">{{ inv.dueDate }}</span>
            </div>
            <div class="flex justify-between border-t border-border pt-1.5">
              <span class="text-text-muted">Total Invoiced:</span>
              <span class="font-bold text-text-main font-mono tabular-nums">ETB {{ inv.totalAmount | number:'1.2-2' }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-text-muted">Amount Paid:</span>
              <span class="font-semibold text-success font-mono tabular-nums">ETB {{ inv.amountPaid | number:'1.2-2' }}</span>
            </div>
            <div class="flex justify-between border-t border-border pt-1.5 text-sm font-bold">
              <span>Balance Due:</span>
              <span class="font-mono tabular-nums" [class.text-danger]="inv.balance > 0 && inv.status !== 'Void'" [class.text-success]="inv.balance === 0">
                ETB {{ inv.balance | number:'1.2-2' }}
              </span>
            </div>
          </div>

          <!-- Action buttons -->
          <div class="flex items-center gap-2 pt-1">
            <button
              (click)="viewInvoiceDetails(inv)"
              class="flex-1 h-9 rounded-input border border-border text-xs font-semibold text-text-main hover:bg-slate-50 transition"
            >
              View Breakdown
            </button>

            <button
              *ngIf="inv.balance > 0 && inv.status !== 'Void'"
              (click)="openPayOnlineModal(inv)"
              class="flex-1 h-9 rounded-input bg-primary text-white text-xs font-bold hover:bg-primary-hover shadow-sm transition"
            >
              Pay Online &rarr;
            </button>
          </div>
        </article>
      </div>

      <!-- PAY ONLINE MODAL -->
      <app-dialog
        [(isOpen)]="payModalOpen"
        title="Online Tuition Payment"
      >
        <div *ngIf="selectedInvoice" class="space-y-4 text-xs">
          <div class="p-3 rounded-lg bg-slate-50 border border-border space-y-1">
            <div class="flex justify-between">
              <span class="text-text-muted">Invoice #:</span>
              <span class="font-mono font-bold">{{ selectedInvoice.invoiceNumber }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-text-muted">Child:</span>
              <span class="font-semibold">{{ selectedInvoice.childName }}</span>
            </div>
            <div class="flex justify-between text-sm font-bold pt-1 border-t border-border">
              <span>Current Balance:</span>
              <span class="text-danger font-mono tabular-nums">ETB {{ selectedInvoice.balance | number:'1.2-2' }}</span>
            </div>
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">Payment Amount (ETB) <span class="text-danger">*</span></label>
            <input
              type="number"
              [(ngModel)]="payAmount"
              step="0.01"
              min="1"
              [max]="selectedInvoice.balance"
              class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs text-text-main font-bold"
            />
            <p *ngIf="getFieldError('amount')" class="text-danger text-xs mt-1 font-semibold">
              {{ getFieldError('amount') }}
            </p>
            <p *ngIf="payAmount > selectedInvoice.balance" class="text-danger text-xs mt-1 font-semibold">
              Payment amount cannot exceed the balance (ETB {{ selectedInvoice.balance | number:'1.2-2' }}).
            </p>
          </div>

          <div class="p-3 rounded-lg bg-emerald-50 border border-emerald-200 text-emerald-800 text-xs flex items-center gap-2">
            <svg class="w-4 h-4 text-emerald-600 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 15v2m-6 4h12a2 2 0 002-2v-6a2 2 0 00-2-2H6a2 2 0 00-2 2v6a2 2 0 002 2zm10-10V7a4 4 0 00-8 0v4h8z" />
            </svg>
            <span>Simulated Telebirr / CBE Birr Payment Gateway: Clicking below simulates instant electronic payment clearance.</span>
          </div>
        </div>

        <div dialog-actions class="flex justify-end gap-2">
          <app-button variant="secondary" (click)="payModalOpen = false">Cancel</app-button>
          <app-button variant="primary" [loading]="paying" (click)="onConfirmOnlinePayment()">Simulate Payment Authorization</app-button>
        </div>
      </app-dialog>

      <!-- BREAKDOWN DETAILS MODAL -->
      <app-dialog
        [(isOpen)]="detailModalOpen"
        title="Invoice Breakdown"
      >
        <div *ngIf="detailedInvoice" class="space-y-4 text-xs">
          <div class="font-mono font-bold text-sm text-text-main border-b border-border pb-2 flex justify-between items-center">
            <span>{{ detailedInvoice.invoiceNumber }}</span>
            <app-badge [status]="detailedInvoice.status" [text]="detailedInvoice.status"></app-badge>
          </div>

          <div>
            <span class="font-semibold text-text-muted uppercase text-xs block mb-2">Itemized Tuition & Fees</span>
            <div class="divide-y divide-border border border-border rounded-lg overflow-hidden">
              <div *ngFor="let line of detailedInvoice.lines" class="p-2.5 flex justify-between items-center bg-slate-50/50">
                <div>
                  <p class="font-medium text-text-main">{{ line.description }}</p>
                  <span class="text-xs text-text-muted">{{ line.lineType }} • Qty: {{ line.quantity }}</span>
                </div>
                <span class="font-bold text-text-main font-mono tabular-nums">ETB {{ line.lineTotal | number:'1.2-2' }}</span>
              </div>
            </div>
          </div>

          <div *ngIf="detailedInvoice.payments && detailedInvoice.payments.length > 0">
            <span class="font-semibold text-text-muted uppercase text-xs block mb-2">Payment Receipts</span>
            <div class="divide-y divide-border border border-border rounded-lg overflow-hidden">
              <div *ngFor="let p of detailedInvoice.payments" class="p-2.5 flex justify-between items-center bg-emerald-50/30">
                <div>
                  <p class="font-medium text-text-main">{{ p.method }} ({{ p.paidAt | slice:0:10 }})</p>
                  <span class="text-xs text-text-muted font-mono">Ref: {{ p.reference || 'N/A' }}</span>
                </div>
                <span class="font-bold text-success font-mono tabular-nums">ETB {{ p.amount | number:'1.2-2' }}</span>
              </div>
            </div>
          </div>
        </div>

        <div dialog-actions class="flex justify-end">
          <app-button variant="primary" (click)="detailModalOpen = false">Close</app-button>
        </div>
      </app-dialog>
    </div>
  `
})
export class ParentInvoicesComponent implements OnInit {
  private billingService = inject(BillingService);
  private toast = inject(ToastService);

  invoices: InvoiceDto[] = [];
  loading = true;
  filter: 'all' | 'pending' | 'paid' = 'all';

  payModalOpen = false;
  paying = false;
  selectedInvoice: InvoiceDto | null = null;
  payAmount = 0;
  payErrors: Record<string, string[]> = {};

  getFieldError(field: string): string | null {
    if (!this.payErrors) return null;
    const lower = field.toLowerCase();
    for (const key of Object.keys(this.payErrors)) {
      if (key.toLowerCase() === lower) {
        return this.payErrors[key][0];
      }
    }
    return null;
  }

  detailModalOpen = false;
  detailedInvoice: InvoiceDto | null = null;

  get pendingInvoices(): InvoiceDto[] {
    return this.invoices.filter(i => i.status !== 'Paid' && i.status !== 'Void' && i.balance > 0);
  }

  get paidInvoices(): InvoiceDto[] {
    return this.invoices.filter(i => i.status === 'Paid');
  }

  get totalBalance(): number {
    return this.pendingInvoices.reduce((sum, i) => sum + i.balance, 0);
  }

  get displayedInvoices(): InvoiceDto[] {
    if (this.filter === 'pending') return this.pendingInvoices;
    if (this.filter === 'paid') return this.paidInvoices;
    return this.invoices;
  }

  ngOnInit(): void {
    this.loadInvoices();
  }

  loadInvoices(): void {
    this.loading = true;
    this.billingService.getInvoices().subscribe({
      next: (res) => {
        this.invoices = res.items;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toast.error('Failed to load invoices');
      }
    });
  }

  viewInvoiceDetails(inv: InvoiceDto): void {
    this.billingService.getInvoice(inv.id).subscribe({
      next: (res) => {
        this.detailedInvoice = res;
        this.detailModalOpen = true;
      },
      error: () => this.toast.error('Failed to load invoice details')
    });
  }

  openPayOnlineModal(inv: InvoiceDto): void {
    this.selectedInvoice = inv;
    this.payAmount = inv.balance;
    this.payErrors = {};
    this.payModalOpen = true;
  }

  onConfirmOnlinePayment(): void {
    if (!this.selectedInvoice || this.payAmount <= 0) {
      this.toast.warning('Please enter a valid amount');
      return;
    }

    if (this.payAmount > this.selectedInvoice.balance) {
      this.toast.error('Amount cannot exceed open invoice balance');
      return;
    }

    this.paying = true;
    this.billingService.payOnline(this.selectedInvoice.id, this.payAmount).subscribe({
      next: () => {
        this.paying = false;
        this.payModalOpen = false;
        this.toast.success('Payment processed successfully via online gateway!');
        this.loadInvoices();
      },
      error: (err) => {
        this.paying = false;
        this.payErrors = err.error?.errors || {};
        const msg = err.error?.detail || err.error?.title || 'Online payment failed';
        this.toast.error(msg);
      }
    });
  }
}
