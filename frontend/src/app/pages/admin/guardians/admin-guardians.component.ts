import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { GuardiansService, GuardianDto, CreateGuardianRequest, UpdateGuardianRequest } from '../../../core/services/guardians.service';
import { ToastService } from '../../../core/services/toast.service';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { DialogComponent } from '../../../shared/components/dialog/dialog.component';
import { AvatarComponent } from '../../../shared/components/avatar/avatar.component';
import { PhotoUploaderComponent } from '../../../shared/components/photo-uploader/photo-uploader.component';

@Component({
  selector: 'app-admin-guardians',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent, DialogComponent, AvatarComponent, PhotoUploaderComponent],
  template: `
    <div class="space-y-6">
      <!-- Header -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold tracking-tight text-text-main">Guardians Directory</h1>
          <p class="text-sm text-text-muted mt-0.5">Manage parents, authorized guardians, emergency contacts, and parent portal logins</p>
        </div>
        <div class="flex items-center gap-3">
          <app-button variant="primary" (click)="openCreateModal()">
            + Add Guardian
          </app-button>
        </div>
      </div>

      <!-- Quick Metrics Overview -->
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
          <span class="text-xs font-semibold text-text-muted">Total Guardians</span>
          <p class="text-2xl font-bold text-text-main mt-1">{{ guardians.length }}</p>
        </div>
        <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
          <span class="text-xs font-semibold text-text-muted">Active Portals</span>
          <p class="text-2xl font-bold text-success mt-1">{{ activePortalCount }}</p>
        </div>
        <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
          <span class="text-xs font-semibold text-text-muted">No Portal Account</span>
          <p class="text-2xl font-bold text-text-muted mt-1">{{ noPortalCount }}</p>
        </div>
        <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
          <span class="text-xs font-semibold text-text-muted">Linked Children</span>
          <p class="text-2xl font-bold text-primary mt-1">{{ totalLinkedChildren }}</p>
        </div>
      </div>

      <!-- Search & Filters -->
      <div class="flex flex-col sm:flex-row gap-3 bg-surface-card p-4 rounded-card border border-border">
        <div class="relative flex-1">
          <input
            type="text"
            [(ngModel)]="searchQuery"
            (ngModelChange)="onSearch()"
            aria-label="Search guardians"
            placeholder="Search by name, phone, or email..."
            class="w-full h-9 pl-9 pr-4 rounded-input border border-border bg-white text-xs text-text-main placeholder:text-text-muted focus:outline-none focus:border-primary"
          />
          <svg class="w-4 h-4 absolute left-3 top-2.5 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
          </svg>
        </div>
        <select
          [(ngModel)]="statusFilter"
          aria-label="Filter by status"
          class="h-9 px-3 rounded-input border border-border bg-white text-xs text-text-main focus:outline-none focus:border-primary"
        >
          <option value="all">All Statuses</option>
          <option value="hasAccount">Active Portal</option>
          <option value="noAccount">No Portal Account</option>
        </select>
      </div>

      <!-- Loading / Empty / Table States -->
      <div *ngIf="loading" class="p-12 text-center text-text-muted text-sm">
        Loading guardians...
      </div>

      <div *ngIf="!loading && filteredGuardians.length === 0" class="p-12 text-center bg-surface-card rounded-card border border-border">
        <p class="text-sm font-semibold text-text-main">No guardians found</p>
        <p class="text-xs text-text-muted mt-1">Try adjusting your search criteria or add a new guardian.</p>
      </div>

      <div *ngIf="!loading && filteredGuardians.length > 0" class="bg-surface-card rounded-card border border-border shadow-card overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-left border-collapse text-xs">
            <thead>
              <tr class="bg-slate-50 border-b border-border text-text-muted uppercase tracking-wider">
                <th class="p-3.5 font-semibold">Guardian Name</th>
                <th class="p-3.5 font-semibold">Phone</th>
                <th class="p-3.5 font-semibold">Email</th>
                <th class="p-3.5 font-semibold">Linked Children</th>
                <th class="p-3.5 font-semibold text-center">Portal Account</th>
                <th class="p-3.5 font-semibold text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-border">
              <tr *ngFor="let g of filteredGuardians" class="hover:bg-slate-50/50 transition">
                <td class="p-3.5 font-medium text-text-main">
                  <div class="flex items-center gap-2.5">
                    <app-avatar
                      [kind]="'guardians'"
                      [id]="g.id"
                      [name]="g.firstName + ' ' + g.lastName"
                      [hasPhoto]="g.hasPhoto ?? false"
                      [photoUpdatedAt]="g.photoUpdatedAt"
                      [size]="32"
                    ></app-avatar>
                    <span>{{ g.firstName }} {{ g.lastName }}</span>
                  </div>
                </td>
                <td class="p-3.5 font-mono text-text-muted">{{ g.phone }}</td>
                <td class="p-3.5 text-text-muted">{{ g.email || '—' }}</td>
                <td class="p-3.5">
                  <div *ngIf="g.children?.length === 0" class="text-text-muted italic">None</div>
                  <div *ngIf="g.children && g.children.length > 0" class="flex flex-wrap gap-1">
                    <span
                      *ngFor="let c of g.children"
                      class="px-2 py-0.5 rounded-full bg-slate-100 text-xs text-text-main border border-border/50"
                      [title]="c.relationship + (c.canPickUp ? ' • Authorized Pickup' : '') + (c.isPayer ? ' • Payer' : '')"
                    >
                      {{ c.childName }} ({{ c.relationship }})
                      <svg *ngIf="c.canPickUp" class="w-3 h-3 text-success inline shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                      </svg>
                    </span>
                  </div>
                </td>
                <td class="p-3.5 text-center">
                  <span
                    *ngIf="g.hasAccount"
                    class="px-2.5 py-0.5 rounded-full bg-success-soft text-success text-xs font-bold"
                  >
                    Active
                  </span>
                  <span
                    *ngIf="!g.hasAccount"
                    class="px-2.5 py-0.5 rounded-full bg-slate-100 text-text-muted text-xs font-medium"
                  >
                    No Portal
                  </span>
                </td>
                <td class="p-3.5 text-right">
                  <div class="flex items-center justify-end gap-2">
                    <button
                      *ngIf="!g.hasAccount && g.email"
                      (click)="createAccount(g)"
                      class="px-2 py-1 rounded border border-primary/30 text-primary hover:bg-primary-soft text-xs font-medium transition"
                    >
                      Create Login
                    </button>
                    <button
                      *ngIf="g.hasAccount"
                      (click)="resetPassword(g)"
                      class="px-2 py-1 rounded border border-border text-text-muted hover:bg-slate-100 text-xs transition"
                    >
                      Reset Pwd
                    </button>
                    <button
                      (click)="openEditModal(g)"
                      class="px-2.5 py-1 rounded border border-border text-text-main hover:bg-slate-100 font-semibold transition"
                    >
                      Edit
                    </button>
                    <button
                      (click)="deleteGuardian(g)"
                      class="px-2 py-1 rounded border border-border text-danger hover:bg-danger-soft text-xs transition font-medium"
                    >
                      Delete
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- CREATE / EDIT MODAL -->
      <app-dialog
        [(isOpen)]="modalOpen"
        [title]="isEditing ? 'Edit Guardian' : 'Add New Guardian'"
      >
        <div class="space-y-4 text-xs">
          <!-- Photo Uploader (Edit Mode) -->
          <div *ngIf="isEditing && editingId">
            <app-photo-uploader
              [kind]="'guardians'"
              [id]="editingId"
              [name]="form.firstName + ' ' + form.lastName"
              [hasPhoto]="editingGuardianHasPhoto"
              [photoUpdatedAt]="editingGuardianPhotoUpdatedAt"
              [avatarSize]="64"
              (photoChanged)="onPhotoChanged($event)"
            ></app-photo-uploader>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block font-semibold text-text-main mb-1">First Name <span class="text-danger">*</span></label>
              <input
                type="text"
                [(ngModel)]="form.firstName"
                placeholder="e.g. Abebe"
                class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
              />
            </div>
            <div>
              <label class="block font-semibold text-text-main mb-1">Last Name <span class="text-danger">*</span></label>
              <input
                type="text"
                [(ngModel)]="form.lastName"
                placeholder="e.g. Tadesse"
                class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
              />
            </div>
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">Phone Number <span class="text-danger">*</span></label>
            <input
              type="tel"
              [(ngModel)]="form.phone"
              placeholder="e.g. +251911223344"
              class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
            />
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">Email Address</label>
            <input
              type="email"
              [(ngModel)]="form.email"
              placeholder="e.g. abebe@example.com (Required for portal login)"
              class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
            />
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">Residential Address</label>
            <input
              type="text"
              [(ngModel)]="form.address"
              placeholder="e.g. Bole Sub-city, Woreda 03, House 124"
              class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
            />
          </div>

          <div *ngIf="!isEditing" class="pt-1">
            <label class="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                [(ngModel)]="createPortalAccount"
                class="rounded border-border text-primary focus:ring-primary"
              />
              <span class="text-text-main font-medium">Create Parent Portal login credentials immediately</span>
            </label>
          </div>
        </div>

        <div dialog-actions class="flex justify-end gap-2">
          <app-button variant="secondary" (click)="modalOpen = false">Cancel</app-button>
          <app-button variant="primary" [loading]="saving" (click)="onSave()">Save Guardian</app-button>
        </div>
      </app-dialog>

      <!-- TEMPORARY PASSWORD DIALOG -->
      <app-dialog
        [(isOpen)]="tempPasswordModalOpen"
        title="Parent Portal Credentials Created"
      >
        <div class="space-y-4 text-xs">
          <p class="text-text-main font-medium">
            Account created successfully for <strong class="text-primary">{{ tempPasswordEmail }}</strong>.
          </p>

          <div class="p-3.5 bg-amber-50 border border-amber-200 rounded-lg text-amber-900 space-y-2">
            <span class="font-bold text-xs uppercase tracking-wide block">One-Time Temporary Password</span>
            <div class="flex items-center gap-2">
              <div class="flex-1 font-mono text-base font-bold select-all bg-white px-3 py-2 rounded border border-amber-300 text-center tracking-wider text-text-main overflow-x-auto">
                {{ showTempPassword ? tempPassword : '••••••••••••' }}
              </div>
              <button
                type="button"
                (click)="showTempPassword = !showTempPassword"
                class="px-2.5 py-2 bg-white hover:bg-amber-100 text-amber-900 border border-amber-300 rounded font-semibold text-xs flex items-center justify-center transition-colors cursor-pointer shrink-0 min-h-[40px] focus:outline-none focus:ring-2 focus:ring-amber-500"
                [attr.aria-label]="showTempPassword ? 'Hide temporary password' : 'Show temporary password'"
              >
                @if (showTempPassword) {
                  <svg class="w-4 h-4 text-amber-700" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l18 18" />
                  </svg>
                } @else {
                  <svg class="w-4 h-4 text-amber-700" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                  </svg>
                }
              </button>
              <button
                type="button"
                (click)="copyPassword(tempPassword)"
                class="px-3.5 py-2 bg-amber-600 hover:bg-amber-700 text-white rounded font-semibold text-xs flex items-center gap-1.5 transition-colors cursor-pointer shrink-0 min-h-[40px] focus:outline-none focus:ring-2 focus:ring-amber-500"
                aria-label="Copy temporary password to clipboard"
              >
                @if (copiedPassword) {
                  <svg class="w-4 h-4 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                  </svg>
                  <span>Copied!</span>
                } @else {
                  <svg class="w-4 h-4 text-white" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-6 12h8a2 2 0 002-2v-8a2 2 0 00-2-2h-8a2 2 0 00-2 2v8a2 2 0 002 2z" />
                  </svg>
                  <span>Copy</span>
                }
              </button>
            </div>
            <p class="text-xs text-amber-700 mt-1 flex items-center gap-1.5">
              <svg class="w-3.5 h-3.5 shrink-0 text-amber-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
              </svg>
              <span>Share this temporary password securely with the guardian. They will be prompted to choose a new password upon first login.</span>
            </p>
          </div>
        </div>

        <div dialog-actions class="flex justify-end">
          <app-button variant="primary" (click)="tempPasswordModalOpen = false">Done</app-button>
        </div>
      </app-dialog>
    </div>
  `
})
export class AdminGuardiansComponent implements OnInit {
  private guardiansService = inject(GuardiansService);
  private toast = inject(ToastService);

  guardians: GuardianDto[] = [];
  loading = true;
  searchQuery = '';
  statusFilter: 'all' | 'hasAccount' | 'noAccount' = 'all';

  modalOpen = false;
  isEditing = false;
  editingId: string | null = null;
  editingGuardianHasPhoto = false;
  editingGuardianPhotoUpdatedAt: string | null = null;
  saving = false;
  createPortalAccount = false;

  form: {
    firstName: string;
    lastName: string;
    phone: string;
    email: string;
    address: string;
  } = {
    firstName: '',
    lastName: '',
    phone: '',
    email: '',
    address: ''
  };

  tempPasswordModalOpen = false;
  tempPassword = '';
  tempPasswordEmail = '';
  copiedPassword = false;
  showTempPassword = true;

  get activePortalCount(): number {
    return this.guardians.filter(g => g.hasAccount).length;
  }

  get noPortalCount(): number {
    return this.guardians.filter(g => !g.hasAccount).length;
  }

  get totalLinkedChildren(): number {
    const childIds = new Set<string>();
    this.guardians.forEach(g => (g.children || []).forEach(c => childIds.add(c.childId)));
    return childIds.size;
  }

  get filteredGuardians(): GuardianDto[] {
    return this.guardians.filter(g => {
      if (this.statusFilter === 'hasAccount' && !g.hasAccount) return false;
      if (this.statusFilter === 'noAccount' && g.hasAccount) return false;

      if (this.searchQuery.trim()) {
        const q = this.searchQuery.toLowerCase();
        const nameMatch = `${g.firstName} ${g.lastName}`.toLowerCase().includes(q);
        const phoneMatch = g.phone.toLowerCase().includes(q);
        const emailMatch = (g.email || '').toLowerCase().includes(q);
        const childMatch = (g.children || []).some(c => c.childName.toLowerCase().includes(q));
        return nameMatch || phoneMatch || emailMatch || childMatch;
      }
      return true;
    });
  }

  ngOnInit(): void {
    this.loadGuardians();
  }

  loadGuardians(): void {
    this.loading = true;
    this.guardiansService.getGuardians().subscribe({
      next: (res) => {
        this.guardians = res.items;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toast.error('Failed to load guardians');
      }
    });
  }

  onSearch(): void {
    // Handled via computed getter
  }

  openCreateModal(): void {
    this.isEditing = false;
    this.editingId = null;
    this.createPortalAccount = false;
    this.form = {
      firstName: '',
      lastName: '',
      phone: '',
      email: '',
      address: ''
    };
    this.modalOpen = true;
  }

  openEditModal(g: GuardianDto): void {
    this.isEditing = true;
    this.editingId = g.id;
    this.editingGuardianHasPhoto = g.hasPhoto ?? false;
    this.editingGuardianPhotoUpdatedAt = g.photoUpdatedAt ?? null;
    this.form = {
      firstName: g.firstName,
      lastName: g.lastName,
      phone: g.phone,
      email: g.email || '',
      address: g.address || ''
    };
    this.modalOpen = true;
  }

  onPhotoChanged(event: { hasPhoto: boolean; photoUpdatedAt: string | null }): void {
    this.editingGuardianHasPhoto = event.hasPhoto;
    this.editingGuardianPhotoUpdatedAt = event.photoUpdatedAt;
    if (this.editingId) {
      const found = this.guardians.find(x => x.id === this.editingId);
      if (found) {
        found.hasPhoto = event.hasPhoto;
        found.photoUpdatedAt = event.photoUpdatedAt;
      }
    }
  }

  onSave(): void {
    if (!this.form.firstName.trim() || !this.form.lastName.trim() || !this.form.phone.trim()) {
      this.toast.warning('First name, last name, and phone are required');
      return;
    }

    this.saving = true;
    if (this.isEditing && this.editingId) {
      const req: UpdateGuardianRequest = {
        firstName: this.form.firstName,
        lastName: this.form.lastName,
        phone: this.form.phone,
        email: this.form.email.trim() || null,
        address: this.form.address.trim() || null
      };

      this.guardiansService.updateGuardian(this.editingId, req).subscribe({
        next: () => {
          this.saving = false;
          this.modalOpen = false;
          this.toast.success('Guardian updated successfully');
          this.loadGuardians();
        },
        error: (err) => {
          this.saving = false;
          this.toast.error(err.error?.detail || 'Failed to update guardian');
        }
      });
    } else {
      const req: CreateGuardianRequest = {
        firstName: this.form.firstName,
        lastName: this.form.lastName,
        phone: this.form.phone,
        email: this.form.email.trim() || null,
        address: this.form.address.trim() || null,
        createAccount: this.createPortalAccount
      };

      this.guardiansService.createGuardian(req).subscribe({
        next: (res) => {
          this.saving = false;
          this.modalOpen = false;
          this.toast.success('Guardian created successfully');
          this.loadGuardians();

          if (res.temporaryPassword) {
            this.tempPassword = res.temporaryPassword;
            this.tempPasswordEmail = this.form.email;
            this.showTempPassword = true;
            this.tempPasswordModalOpen = true;
          }
        },
        error: (err) => {
          this.saving = false;
          this.toast.error(err.error?.detail || 'Failed to create guardian');
        }
      });
    }
  }

  createAccount(g: GuardianDto): void {
    if (!g.email) {
      this.toast.warning('Guardian must have an email address to create an account');
      return;
    }

    this.guardiansService.createAccount(g.id, g.email).subscribe({
      next: (res) => {
        this.toast.success('Account created successfully');
        this.loadGuardians();
        if (res.temporaryPassword) {
          this.tempPassword = res.temporaryPassword;
          this.tempPasswordEmail = g.email!;
          this.showTempPassword = true;
          this.tempPasswordModalOpen = true;
        }
      },
      error: (err) => {
        this.toast.error(err.error?.detail || 'Failed to create portal account');
      }
    });
  }

  resetPassword(g: GuardianDto): void {
    this.guardiansService.resetPassword(g.id).subscribe({
      next: (res) => {
        this.toast.success('Password reset successfully');
        if (res.temporaryPassword) {
          this.tempPassword = res.temporaryPassword;
          this.tempPasswordEmail = g.email || g.firstName;
          this.showTempPassword = true;
          this.tempPasswordModalOpen = true;
        }
      },
      error: (err) => {
        this.toast.error(err.error?.detail || 'Failed to reset password');
      }
    });
  }

  copyPassword(password: string): void {
    if (!password) return;
    navigator.clipboard.writeText(password).then(() => {
      this.copiedPassword = true;
      this.toast.success('Temporary password copied to clipboard');
      setTimeout(() => {
        this.copiedPassword = false;
      }, 2000);
    }).catch(() => {
      this.toast.error('Could not copy password to clipboard');
    });
  }

  deleteGuardian(g: GuardianDto): void {
    if (!confirm(`Are you sure you want to delete guardian ${g.firstName} ${g.lastName}? This will also revoke any parent portal login.`)) return;

    this.guardiansService.deleteGuardian(g.id).subscribe({
      next: () => {
        this.toast.success('Guardian removed successfully');
        this.loadGuardians();
      },
      error: (err) => {
        this.toast.error(err.error?.detail || err.error?.title || 'Failed to delete guardian');
      }
    });
  }
}
