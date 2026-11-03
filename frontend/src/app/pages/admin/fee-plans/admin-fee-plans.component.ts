import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FeePlansService, FeePlanDto, CreateFeePlanRequest, UpdateFeePlanRequest } from '../../../core/services/fee-plans.service';
import { ToastService } from '../../../core/services/toast.service';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { DialogComponent } from '../../../shared/components/dialog/dialog.component';

@Component({
  selector: 'app-admin-fee-plans',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent, DialogComponent],
  template: `
    <div class="space-y-6">
      <!-- Header -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold tracking-tight text-text-main">Tuition Fee Plans</h1>
          <p class="text-sm text-text-muted mt-0.5">Manage recurring monthly fee schedules and billing rates applied to enrolled children</p>
        </div>
        <div class="flex items-center gap-3">
          <app-button variant="primary" (click)="openCreateModal()">
            + Add Fee Plan
          </app-button>
        </div>
      </div>

      <!-- Loading / Empty -->
      <div *ngIf="loading" class="p-12 text-center text-text-muted text-sm">
        Loading fee plans...
      </div>

      <div *ngIf="!loading && plans.length === 0" class="p-12 text-center bg-surface-card rounded-card border border-border">
        <p class="text-sm font-semibold text-text-main">No fee plans configured</p>
        <p class="text-xs text-text-muted mt-1">Create tuition packages (e.g. Full-time monthly, Part-time) to generate invoices.</p>
      </div>

      <!-- Fee Plans Table -->
      <div *ngIf="!loading && plans.length > 0" class="bg-surface-card rounded-card border border-border shadow-card overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-left border-collapse text-xs">
            <thead>
              <tr class="bg-slate-50 border-b border-border text-text-muted uppercase tracking-wider">
                <th class="p-3.5 font-semibold">Plan Name</th>
                <th class="p-3.5 font-semibold">Description</th>
                <th class="p-3.5 font-semibold text-right">Monthly Tuition (ETB)</th>
                <th class="p-3.5 font-semibold text-center">Status</th>
                <th class="p-3.5 font-semibold text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-border">
              <tr *ngFor="let p of plans" class="hover:bg-slate-50/50 transition">
                <td class="p-3.5 font-bold text-text-main">{{ p.name }}</td>
                <td class="p-3.5 text-text-muted">{{ p.description || 'Standard rate' }}</td>
                <td class="p-3.5 text-right font-bold text-base text-text-main font-mono tabular-nums">
                  ETB {{ p.monthlyAmount | number:'1.2-2' }}
                </td>
                <td class="p-3.5 text-center">
                  <span
                    [class.bg-success-soft]="p.isActive"
                    [class.text-success]="p.isActive"
                    [class.bg-slate-100]="!p.isActive"
                    [class.text-text-muted]="!p.isActive"
                    class="px-2.5 py-0.5 rounded-full text-xs font-bold"
                  >
                    {{ p.isActive ? 'Active' : 'Inactive' }}
                  </span>
                </td>
                <td class="p-3.5 text-right">
                  <button
                    (click)="openEditModal(p)"
                    class="px-2.5 py-1 rounded border border-border text-text-main hover:bg-slate-100 font-semibold transition"
                  >
                    Edit Plan
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- CREATE / EDIT MODAL -->
      <app-dialog
        [(isOpen)]="modalOpen"
        [title]="isEditing ? 'Edit Fee Plan' : 'Create New Fee Plan'"
      >
        <div class="space-y-4 text-xs">
          <div>
            <label class="block font-semibold text-text-main mb-1">Plan Name <span class="text-danger">*</span></label>
            <input
              type="text"
              [(ngModel)]="form.name"
              placeholder="e.g. Full-time monthly, Sibling rate"
              class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
            />
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">Monthly Tuition Amount (ETB) <span class="text-danger">*</span></label>
            <input
              type="number"
              [(ngModel)]="form.monthlyAmount"
              step="50"
              min="0"
              placeholder="e.g. 4500.00"
              class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs font-bold"
            />
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">Description</label>
            <textarea
              [(ngModel)]="form.description"
              rows="2"
              placeholder="e.g. Includes breakfast and afternoon snacks..."
              class="w-full p-2.5 rounded-input border border-border bg-white text-xs"
            ></textarea>
          </div>

          <div *ngIf="isEditing" class="pt-1">
            <label class="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                [(ngModel)]="form.isActive"
                class="rounded border-border text-primary focus:ring-primary"
              />
              <span class="text-text-main font-medium">Plan is active for new enrollees</span>
            </label>
          </div>
        </div>

        <div dialog-actions class="flex justify-end gap-2">
          <app-button variant="secondary" (click)="modalOpen = false">Cancel</app-button>
          <app-button variant="primary" [loading]="saving" (click)="onSave()">Save Plan</app-button>
        </div>
      </app-dialog>
    </div>
  `
})
export class AdminFeePlansComponent implements OnInit {
  private feePlansService = inject(FeePlansService);
  private toast = inject(ToastService);

  plans: FeePlanDto[] = [];
  loading = true;

  modalOpen = false;
  isEditing = false;
  editingId: string | null = null;
  saving = false;

  form: {
    name: string;
    description: string;
    monthlyAmount: number;
    isActive: boolean;
  } = {
    name: '',
    description: '',
    monthlyAmount: 0,
    isActive: true
  };

  ngOnInit(): void {
    this.loadPlans();
  }

  loadPlans(): void {
    this.loading = true;
    this.feePlansService.getFeePlans().subscribe({
      next: (res) => {
        this.plans = res;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toast.error('Failed to load fee plans');
      }
    });
  }

  openCreateModal(): void {
    this.isEditing = false;
    this.editingId = null;
    this.form = {
      name: '',
      description: '',
      monthlyAmount: 3500,
      isActive: true
    };
    this.modalOpen = true;
  }

  openEditModal(p: FeePlanDto): void {
    this.isEditing = true;
    this.editingId = p.id;
    this.form = {
      name: p.name,
      description: p.description || '',
      monthlyAmount: p.monthlyAmount,
      isActive: p.isActive
    };
    this.modalOpen = true;
  }

  onSave(): void {
    if (!this.form.name.trim() || this.form.monthlyAmount <= 0) {
      this.toast.warning('Plan name and a positive monthly amount are required');
      return;
    }

    this.saving = true;
    if (this.isEditing && this.editingId) {
      const req: UpdateFeePlanRequest = {
        name: this.form.name.trim(),
        description: this.form.description.trim() || null,
        monthlyAmount: this.form.monthlyAmount,
        isActive: this.form.isActive
      };

      this.feePlansService.updateFeePlan(this.editingId, req).subscribe({
        next: () => {
          this.saving = false;
          this.modalOpen = false;
          this.toast.success('Fee plan updated successfully');
          this.loadPlans();
        },
        error: (err) => {
          this.saving = false;
          this.toast.error(err.error?.detail || 'Failed to update fee plan');
        }
      });
    } else {
      const req: CreateFeePlanRequest = {
        name: this.form.name.trim(),
        description: this.form.description.trim() || null,
        monthlyAmount: this.form.monthlyAmount
      };

      this.feePlansService.createFeePlan(req).subscribe({
        next: () => {
          this.saving = false;
          this.modalOpen = false;
          this.toast.success('Fee plan created successfully');
          this.loadPlans();
        },
        error: (err) => {
          this.saving = false;
          this.toast.error(err.error?.detail || 'Failed to create fee plan');
        }
      });
    }
  }
}
