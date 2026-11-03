import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { BillingService, InvoiceDto, RecordPaymentRequest, AddAdjustmentRequest, VoidInvoiceRequest } from '../../../core/services/billing.service';
import { ToastService } from '../../../core/services/toast.service';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';
import { DialogComponent } from '../../../shared/components/dialog/dialog.component';
import { ButtonComponent } from '../../../shared/components/button/button.component';

@Component({
  selector: 'app-invoice-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, BadgeComponent, DialogComponent, ButtonComponent],
  template: `
    <div *ngIf="invoice" class="space-y-6 max-w-5xl mx-auto pb-16">
      <!-- Breadcrumb -->
      <nav class="flex items-center gap-2 text-xs text-text-muted">
        <a routerLink="/admin/invoices" class="hover:text-primary transition-colors flex items-center gap-1">
          <span>&larr; Invoices</span>
        </a>
        <span>/</span>
        <span class="text-text-main font-semibold font-mono">{{ invoice.invoiceNumber }}</span>
      </nav>

      <!-- Invoice Title & Action Bar -->
      <div class="bg-surface-card rounded-card border border-border p-6 shadow-card flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div class="flex items-center gap-3">
            <h1 class="text-2xl font-bold font-mono text-text-main">{{ invoice.invoiceNumber }}</h1>
            <app-badge [status]="invoice.status" [text]="invoice.status"></app-badge>
          </div>
          <p class="text-xs text-text-muted mt-1">
            Child: <strong class="text-text-main">{{ invoice.childName }}</strong> • Billed to: <strong class="text-text-main">{{ invoice.guardianName }}</strong>
          </p>
        </div>

        <div class="flex items-center gap-2 flex-wrap">
          <button
            *ngIf="invoice.status !== 'Paid' && invoice.status !== 'Void'"
            (click)="openPaymentModal()"
            class="px-4 py-2 rounded-input bg-primary text-white text-xs font-bold hover:bg-primary-hover transition shadow-sm"
          >
            Record Payment
          </button>

          <button
            *ngIf="invoice.status !== 'Paid' && invoice.status !== 'Void'"
            (click)="openAdjustmentModal()"
            class="px-3 py-2 rounded-input border border-border bg-white text-xs font-semibold text-text-main hover:bg-slate-50 transition"
          >
            Add Adjustment
          </button>

          <button
            (click)="printInvoice()"
            class="inline-flex items-center gap-1.5 px-3 py-2 rounded-input border border-border bg-white text-xs font-semibold text-text-main hover:bg-slate-50 transition"
          >
            <svg class="w-3.5 h-3.5 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M17 17h2a2 2 0 002-2v-4a2 2 0 00-2-2H5a2 2 0 00-2 2v4a2 2 0 002 2h2m2 4h6a2 2 0 002-2v-4H7v4a2 2 0 002 2zm8-12V5a2 2 0 00-2-2H9a2 2 0 00-2 2v4h10z" />
            </svg>
            Print
          </button>

          <button
            *ngIf="invoice.status !== 'Void'"
            (click)="openVoidModal()"
            class="px-3 py-2 rounded-input border border-danger/30 text-danger text-xs font-semibold hover:bg-danger-soft transition"
          >
            Void Invoice
          </button>
        </div>
      </div>

      <!-- VOID NOTICE BANNER -->
      <div *ngIf="invoice.status === 'Void'" class="p-4 rounded-card bg-slate-100 border border-border text-text-muted text-xs flex items-center gap-2">
        <svg class="w-4 h-4 text-danger shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
        </svg>
        <span><strong class="text-danger">VOIDED INVOICE:</strong> This invoice was voided on {{ invoice.voidedAt }}. Reason: "{{ invoice.voidReason }}". Outstanding balance cancelled.</span>
      </div>

      <!-- Summary Info Cards Grid -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <!-- Payer Card -->
        <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
          <span class="text-xs font-semibold uppercase text-text-muted">Billed To (Guardian)</span>
          <p class="text-sm font-bold text-text-main mt-1">{{ invoice.guardianName }}</p>
          <a [routerLink]="['/admin/children', invoice.childId]" class="text-xs text-primary hover:underline mt-1 block">
            Child: {{ invoice.childName }} &rarr;
          </a>
        </div>

        <!-- Period & Dates Card -->
        <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
          <span class="text-xs font-semibold uppercase text-text-muted">Billing Cycle</span>
          <p class="text-xs font-medium text-text-main mt-1">
            Period: {{ invoice.periodStart }} to {{ invoice.periodEnd }}
          </p>
          <p class="text-xs text-text-muted mt-0.5">
            Issued: {{ invoice.issueDate }} • Due: <strong class="text-text-main">{{ invoice.dueDate }}</strong>
          </p>
        </div>

        <!-- Ledger Balance Card -->
        <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
          <span class="text-xs font-semibold uppercase text-text-muted">Balance Due</span>
          <p class="text-xl font-black mt-1 font-mono tabular-nums" [class.text-danger]="invoice.balance > 0 && invoice.status !== 'Void'" [class.text-success]="invoice.balance === 0">
            ETB {{ invoice.balance | number:'1.2-2' }}
          </p>
          <p class="text-xs text-text-muted mt-0.5 font-mono tabular-nums">
            Total: ETB {{ invoice.totalAmount | number:'1.2-2' }} • Paid: ETB {{ invoice.amountPaid | number:'1.2-2' }}
          </p>
        </div>
      </div>

      <!-- Line Items Section -->
      <div class="bg-surface-card rounded-card border border-border shadow-card overflow-hidden">
        <div class="p-4 border-b border-border">
          <h2 class="text-sm font-bold text-text-main">Invoice Line Items</h2>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full text-left border-collapse text-xs min-w-[500px]">
            <thead>
              <tr class="bg-slate-50 border-b border-border text-text-muted uppercase tracking-wider">
                <th class="p-3 font-semibold">Type</th>
                <th class="p-3 font-semibold">Description</th>
                <th class="p-3 font-semibold text-center">Qty</th>
                <th class="p-3 font-semibold text-right">Unit Price</th>
                <th class="p-3 font-semibold text-right">Total</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-border">
              <tr *ngFor="let line of invoice.lines" class="hover:bg-slate-50/50 transition">
                <td class="p-3 font-medium text-text-main">
                  <span class="px-2 py-0.5 rounded bg-slate-100 text-xs font-semibold">{{ line.lineType }}</span>
                </td>
                <td class="p-3 text-text-main">{{ line.description }}</td>
                <td class="p-3 text-center text-text-muted">{{ line.quantity }}</td>
                <td class="p-3 text-right text-text-muted font-mono tabular-nums">ETB {{ line.unitPrice | number:'1.2-2' }}</td>
                <td class="p-3 font-bold text-text-main text-right font-mono tabular-nums">ETB {{ line.lineTotal | number:'1.2-2' }}</td>
              </tr>
            </tbody>
            <tfoot>
              <tr class="bg-slate-50 border-t-2 border-border font-bold">
                <td colspan="4" class="p-3 text-right text-text-muted uppercase text-xs">Total Invoiced:</td>
                <td class="p-3 text-right text-text-main text-sm font-mono tabular-nums font-bold">ETB {{ invoice.totalAmount | number:'1.2-2' }}</td>
              </tr>
            </tfoot>
          </table>
        </div>
      </div>

      <!-- Payments History Section -->
      <div class="bg-surface-card rounded-card border border-border shadow-card overflow-hidden">
        <div class="p-4 border-b border-border flex items-center justify-between">
          <h2 class="text-sm font-bold text-text-main">Recorded Payments & Transactions</h2>
          <span class="text-xs text-text-muted">{{ invoice.payments?.length || 0 }} transaction(s)</span>
        </div>

        <div *ngIf="!invoice.payments || invoice.payments.length === 0" class="p-6 text-center text-text-muted text-xs">
          No payments recorded yet for this invoice.
        </div>

        <div *ngIf="invoice.payments && invoice.payments.length > 0" class="overflow-x-auto">
          <table class="w-full text-left border-collapse text-xs min-w-[550px]">
            <thead>
              <tr class="bg-slate-50 border-b border-border text-text-muted uppercase tracking-wider">
                <th class="p-3 font-semibold">Date</th>
                <th class="p-3 font-semibold">Method</th>
                <th class="p-3 font-semibold">Reference</th>
                <th class="p-3 font-semibold text-right">Recorded By</th>
                <th class="p-3 font-semibold text-right">Amount</th>
                <th class="p-3 font-semibold text-center">Status</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-border">
              <tr *ngFor="let p of invoice.payments" [class.opacity-50]="p.voidedAt" class="hover:bg-slate-50/50 transition">
                <td class="p-3 text-text-main">{{ p.paidAt | slice:0:10 }}</td>
                <td class="p-3 font-medium text-text-main">{{ p.method }}</td>
                <td class="p-3 font-mono text-text-muted">{{ p.reference || '—' }}</td>
                <td class="p-3 text-right text-text-muted">{{ p.recordedByName }}</td>
                <td class="p-3 font-bold text-success text-right font-mono tabular-nums">ETB {{ p.amount | number:'1.2-2' }}</td>
                <td class="p-3 text-center">
                  <span *ngIf="p.voidedAt" class="px-2 py-0.5 rounded bg-danger-soft text-danger text-xs font-bold">Voided</span>
                  <span *ngIf="!p.voidedAt" class="px-2 py-0.5 rounded bg-success-soft text-success text-xs font-semibold">Cleared</span>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- RECORD PAYMENT DIALOG -->
      <app-dialog
        [(isOpen)]="paymentModalOpen"
        title="Record Payment"
      >
        <div class="space-y-4 text-xs">
          <div class="p-3 rounded-lg bg-slate-50 border border-border">
            <div class="flex justify-between">
              <span class="text-text-muted">Total Invoice Balance:</span>
              <strong class="text-text-main font-bold">ETB {{ invoice.balance | number:'1.2-2' }}</strong>
            </div>
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">Payment Amount (ETB) <span class="text-danger">*</span></label>
            <input
              type="number"
              [(ngModel)]="paymentForm.amount"
              step="0.01"
              min="0.01"
              placeholder="e.g. 2500"
              class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs text-text-main"
            />
            <p *ngIf="getModalError(paymentErrors, 'amount')" class="text-xs text-danger mt-1 font-semibold">
              {{ getModalError(paymentErrors, 'amount') }}
            </p>
            <p *ngIf="paymentForm.amount > invoice.balance" class="text-danger font-semibold text-xs mt-1 flex items-center gap-1">
              <svg class="w-3.5 h-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
              <span>Warning: Amount exceeds current balance (ETB {{ invoice.balance | number:'1.2-2' }}). Overpayments are prohibited (BR-21).</span>
            </p>
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">Payment Method <span class="text-danger">*</span></label>
            <select
              [(ngModel)]="paymentForm.method"
              class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs text-text-main"
            >
              <option value="Cash">Cash</option>
              <option value="BankTransfer">Bank Transfer</option>
              <option value="Telebirr">Telebirr</option>
              <option value="Check">Check</option>
              <option value="Other">Other</option>
            </select>
            <p *ngIf="getModalError(paymentErrors, 'method')" class="text-xs text-danger mt-1 font-semibold">
              {{ getModalError(paymentErrors, 'method') }}
            </p>
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">Reference / Transaction Number</label>
            <input
              type="text"
              [(ngModel)]="paymentForm.reference"
              placeholder="e.g. CBE-TXN-984321"
              class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs text-text-main"
            />
            <p *ngIf="getModalError(paymentErrors, 'reference')" class="text-xs text-danger mt-1 font-semibold">
              {{ getModalError(paymentErrors, 'reference') }}
            </p>
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">Notes</label>
            <textarea
              [(ngModel)]="paymentForm.notes"
              rows="2"
              placeholder="Optional payment notes..."
              class="w-full p-2.5 rounded-input border border-border bg-white text-xs text-text-main"
            ></textarea>
            <p *ngIf="getModalError(paymentErrors, 'notes')" class="text-xs text-danger mt-1 font-semibold">
              {{ getModalError(paymentErrors, 'notes') }}
            </p>
          </div>
        </div>

        <div dialog-actions class="flex justify-end gap-2">
          <app-button variant="secondary" (click)="paymentModalOpen = false">Cancel</app-button>
          <app-button variant="primary" [loading]="recordingPayment" (click)="onRecordPayment()">Save Payment</app-button>
        </div>
      </app-dialog>

      <!-- ADD ADJUSTMENT DIALOG -->
      <app-dialog
        [(isOpen)]="adjustmentModalOpen"
        title="Add Invoice Adjustment"
      >
        <div class="space-y-4 text-xs">
          <div>
            <label class="block font-semibold text-text-main mb-1">Adjustment Type <span class="text-danger">*</span></label>
            <select
              [(ngModel)]="adjForm.lineType"
              class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs text-text-main"
            >
              <option value="Discount">Discount (negative or credit)</option>
              <option value="LateFee">Late Fee</option>
              <option value="Registration">Registration Fee</option>
              <option value="Adjustment">Custom Adjustment</option>
            </select>
            <p *ngIf="getModalError(adjErrors, 'lineType')" class="text-xs text-danger mt-1 font-semibold">
              {{ getModalError(adjErrors, 'lineType') }}
            </p>
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">Description <span class="text-danger">*</span></label>
            <input
              type="text"
              [(ngModel)]="adjForm.description"
              placeholder="e.g. Sibling discount 10%"
              class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs text-text-main"
            />
            <p *ngIf="getModalError(adjErrors, 'description')" class="text-xs text-danger mt-1 font-semibold">
              {{ getModalError(adjErrors, 'description') }}
            </p>
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">Amount (ETB) <span class="text-danger">*</span></label>
            <input
              type="number"
              [(ngModel)]="adjForm.amount"
              step="0.01"
              placeholder="e.g. -450.00 or 150.00"
              class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs text-text-main"
            />
            <p *ngIf="getModalError(adjErrors, 'amount')" class="text-xs text-danger mt-1 font-semibold">
              {{ getModalError(adjErrors, 'amount') }}
            </p>
          </div>
        </div>

        <div dialog-actions class="flex justify-end gap-2">
          <app-button variant="secondary" (click)="adjustmentModalOpen = false">Cancel</app-button>
          <app-button variant="primary" [loading]="addingAdjustment" (click)="onAddAdjustment()">Apply Adjustment</app-button>
        </div>
      </app-dialog>

      <!-- VOID INVOICE DIALOG -->
      <app-dialog
        [(isOpen)]="voidModalOpen"
        title="Void Invoice"
      >
        <div class="space-y-4 text-xs">
          <p class="text-danger font-medium">
            Are you sure you want to void this invoice? This will set outstanding balance to 0 and permanently lock the invoice from further payments.
          </p>

          <div>
            <label class="block font-semibold text-text-main mb-1">Reason for Voiding <span class="text-danger">*</span></label>
            <textarea
              [(ngModel)]="voidReason"
              rows="3"
              placeholder="e.g. Incorrect enrollment fee plan applied..."
              class="w-full p-2.5 rounded-input border border-border bg-white text-xs text-text-main"
            ></textarea>
            <p *ngIf="getModalError(voidErrors, 'reason')" class="text-xs text-danger mt-1 font-semibold">
              {{ getModalError(voidErrors, 'reason') }}
            </p>
          </div>
        </div>

        <div dialog-actions class="flex justify-end gap-2">
          <app-button variant="secondary" (click)="voidModalOpen = false">Cancel</app-button>
          <app-button variant="danger" [loading]="voiding" (click)="onVoidInvoice()">Confirm Void</app-button>
        </div>
      </app-dialog>
    </div>
  `
})
export class InvoiceDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private billingService = inject(BillingService);
  private toast = inject(ToastService);

  invoice: InvoiceDto | null = null;
  invoiceId = '';

  paymentModalOpen = false;
  recordingPayment = false;
  paymentErrors: Record<string, string[]> = {};
  paymentForm: RecordPaymentRequest = {
    amount: 0,
    method: 'Cash',
    reference: '',
    notes: ''
  };

  adjustmentModalOpen = false;
  addingAdjustment = false;
  adjErrors: Record<string, string[]> = {};
  adjForm: AddAdjustmentRequest = {
    lineType: 'Discount',
    description: '',
    amount: 0
  };

  voidModalOpen = false;
  voiding = false;
  voidErrors: Record<string, string[]> = {};
  voidReason = '';

  getModalError(errors: Record<string, string[]>, field: string): string | null {
    if (!errors) return null;
    const lower = field.toLowerCase();
    for (const key of Object.keys(errors)) {
      if (key.toLowerCase() === lower) {
        return errors[key][0];
      }
    }
    return null;
  }

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.invoiceId = id;
      this.loadInvoice();
    }
  }

  loadInvoice(): void {
    this.billingService.getInvoice(this.invoiceId).subscribe({
      next: (res) => (this.invoice = res),
      error: () => this.toast.error('Failed to load invoice details')
    });
  }

  openPaymentModal(): void {
    if (!this.invoice) return;
    this.paymentErrors = {};
    this.paymentForm = {
      amount: this.invoice.balance,
      method: 'Cash',
      reference: '',
      notes: ''
    };
    this.paymentModalOpen = true;
  }

  openAdjustmentModal(): void {
    this.adjErrors = {};
    this.adjForm = {
      lineType: 'Discount',
      description: '',
      amount: 0
    };
    this.adjustmentModalOpen = true;
  }

  openVoidModal(): void {
    this.voidErrors = {};
    this.voidReason = '';
    this.voidModalOpen = true;
  }

  onRecordPayment(): void {
    if (!this.invoice || this.paymentForm.amount <= 0) {
      this.toast.warning('Please enter a valid payment amount');
      return;
    }

    this.recordingPayment = true;
    this.billingService.recordPayment(this.invoice.id, this.paymentForm).subscribe({
      next: () => {
        this.recordingPayment = false;
        this.paymentModalOpen = false;
        this.toast.success('Payment recorded successfully');
        this.loadInvoice();
      },
      error: (err) => {
        this.recordingPayment = false;
        this.paymentErrors = err.error?.errors || {};
        const msg = err.error?.detail || err.error?.title || 'Failed to record payment';
        this.toast.error(msg);
      }
    });
  }

  onAddAdjustment(): void {
    if (!this.invoice || !this.adjForm.description || this.adjForm.amount === 0) {
      this.toast.warning('Please fill in adjustment description and amount');
      return;
    }

    this.addingAdjustment = true;
    this.billingService.addAdjustment(this.invoice.id, this.adjForm).subscribe({
      next: () => {
        this.addingAdjustment = false;
        this.adjustmentModalOpen = false;
        this.toast.success('Adjustment added successfully');
        this.loadInvoice();
      },
      error: (err) => {
        this.addingAdjustment = false;
        this.adjErrors = err.error?.errors || {};
        const msg = err.error?.detail || err.error?.title || 'Failed to add adjustment';
        this.toast.error(msg);
      }
    });
  }

  onVoidInvoice(): void {
    if (!this.invoice || !this.voidReason.trim()) {
      this.toast.warning('Please provide a reason for voiding');
      return;
    }

    this.voiding = true;
    this.billingService.voidInvoice(this.invoice.id, { reason: this.voidReason }).subscribe({
      next: () => {
        this.voiding = false;
        this.voidModalOpen = false;
        this.toast.success('Invoice has been voided');
        this.loadInvoice();
      },
      error: (err) => {
        this.voiding = false;
        this.voidErrors = err.error?.errors || {};
        const msg = err.error?.detail || err.error?.title || 'Failed to void invoice';
        this.toast.error(msg);
      }
    });
  }

  printInvoice(): void {
    window.print();
  }
}
