import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StaffService, StaffDto, CreateStaffRequest, UpdateStaffRequest } from '../../../core/services/staff.service';
import { ClassroomService, ClassroomDto } from '../../../core/services/classroom.service';
import { ToastService } from '../../../core/services/toast.service';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { DialogComponent } from '../../../shared/components/dialog/dialog.component';
import { AvatarComponent } from '../../../shared/components/avatar/avatar.component';
import { PhotoUploaderComponent } from '../../../shared/components/photo-uploader/photo-uploader.component';

@Component({
  selector: 'app-admin-staff',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent, DialogComponent, AvatarComponent, PhotoUploaderComponent],
  template: `
    <div class="space-y-6">
      <!-- Header -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold tracking-tight text-text-main">Staff Management</h1>
          <p class="text-sm text-text-muted mt-0.5">Manage teachers, assistants, job titles, and classroom assignments</p>
        </div>
        <div class="flex items-center gap-3">
          <app-button variant="primary" (click)="openCreateModal()">
            + Add Staff Member
          </app-button>
        </div>
      </div>

      <!-- Quick Stats -->
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
          <span class="text-xs font-semibold text-text-muted">Total Staff</span>
          <p class="text-2xl font-bold text-text-main mt-1">{{ staffList.length }}</p>
        </div>
        <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
          <span class="text-xs font-semibold text-text-muted">Active Staff</span>
          <p class="text-2xl font-bold text-success mt-1">{{ activeStaffCount }}</p>
        </div>
        <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
          <span class="text-xs font-semibold text-text-muted">Classrooms Covered</span>
          <p class="text-2xl font-bold text-primary mt-1">{{ classroomsCoveredCount }}</p>
        </div>
        <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
          <span class="text-xs font-semibold text-text-muted">Unassigned Staff</span>
          <p class="text-2xl font-bold text-text-muted mt-1">{{ unassignedStaffCount }}</p>
        </div>
      </div>

      <!-- Staff Table -->
      <div *ngIf="loading" class="p-12 text-center text-text-muted text-sm">
        Loading staff members...
      </div>

      <div *ngIf="!loading && staffList.length === 0" class="p-12 text-center bg-surface-card rounded-card border border-border">
        <p class="text-sm font-semibold text-text-main">No staff members found</p>
        <p class="text-xs text-text-muted mt-1">Add a teacher or caregiver to begin managing classroom rosters.</p>
      </div>

      <div *ngIf="!loading && staffList.length > 0" class="bg-surface-card rounded-card border border-border shadow-card overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-left border-collapse text-xs">
            <thead>
              <tr class="bg-slate-50 border-b border-border text-text-muted uppercase tracking-wider">
                <th class="p-3.5 font-semibold">Staff Member</th>
                <th class="p-3.5 font-semibold">Job Title</th>
                <th class="p-3.5 font-semibold">Assigned Classrooms</th>
                <th class="p-3.5 font-semibold">Contact</th>
                <th class="p-3.5 font-semibold">Hire Date</th>
                <th class="p-3.5 font-semibold text-center">Status</th>
                <th class="p-3.5 font-semibold text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-border">
              <tr *ngFor="let s of staffList" class="hover:bg-slate-50/50 transition">
                <td class="p-3.5 font-medium text-text-main">
                  <div class="flex items-center gap-2.5">
                    <app-avatar
                      [kind]="'staff'"
                      [id]="s.id"
                      [name]="s.firstName + ' ' + s.lastName"
                      [hasPhoto]="s.hasPhoto ?? false"
                      [photoUpdatedAt]="s.photoUpdatedAt"
                      [size]="32"
                    ></app-avatar>
                    <div>
                      <span class="font-bold">{{ s.firstName }} {{ s.lastName }}</span>
                      <p class="text-xs text-text-muted font-normal">{{ s.email }}</p>
                    </div>
                  </div>
                </td>
                <td class="p-3.5 text-text-main font-medium">{{ s.jobTitle }}</td>
                <td class="p-3.5">
                  <div *ngIf="s.classrooms?.length === 0" class="text-text-muted italic text-xs">Unassigned</div>
                  <div *ngIf="s.classrooms && s.classrooms.length > 0" class="flex flex-wrap gap-1">
                    <span
                      *ngFor="let c of s.classrooms"
                      class="px-2 py-0.5 rounded-full bg-primary-soft text-primary font-semibold text-xs"
                    >
                      {{ c.classroomName }}
                    </span>
                  </div>
                </td>
                <td class="p-3.5 font-mono text-text-muted">{{ s.phone }}</td>
                <td class="p-3.5 text-text-muted">{{ s.hireDate }}</td>
                <td class="p-3.5 text-center">
                  <span
                    [class.bg-success-soft]="s.isActive"
                    [class.text-success]="s.isActive"
                    [class.bg-slate-100]="!s.isActive"
                    [class.text-text-muted]="!s.isActive"
                    class="px-2.5 py-0.5 rounded-full text-xs font-bold"
                  >
                    {{ s.isActive ? 'Active' : 'Inactive' }}
                  </span>
                </td>
                <td class="p-3.5 text-right">
                  <div class="flex items-center justify-end gap-2">
                    <button
                      (click)="resetPassword(s)"
                      class="px-2 py-1 rounded border border-border text-text-muted hover:bg-slate-100 text-xs transition"
                    >
                      Reset Pwd
                    </button>
                    <button
                      (click)="openEditModal(s)"
                      class="px-2.5 py-1 rounded border border-border text-text-main hover:bg-slate-100 font-semibold transition"
                    >
                      Edit
                    </button>
                    <button
                      (click)="deleteStaff(s)"
                      class="px-2 py-1 rounded border border-border text-danger hover:bg-danger-soft text-xs transition font-medium"
                    >
                      Remove
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
        [title]="isEditing ? 'Edit Staff Member' : 'Add New Staff Member'"
      >
        <div class="space-y-4 text-xs">
          <!-- Photo Uploader (Edit Mode) -->
          <div *ngIf="isEditing && editingId">
            <app-photo-uploader
              [kind]="'staff'"
              [id]="editingId"
              [name]="form.firstName + ' ' + form.lastName"
              [hasPhoto]="editingStaffHasPhoto"
              [photoUpdatedAt]="editingStaffPhotoUpdatedAt"
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
                placeholder="e.g. Almaz"
                class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
              />
            </div>
            <div>
              <label class="block font-semibold text-text-main mb-1">Last Name <span class="text-danger">*</span></label>
              <input
                type="text"
                [(ngModel)]="form.lastName"
                placeholder="e.g. Bekele"
                class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
              />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block font-semibold text-text-main mb-1">Email (Login username) <span class="text-danger">*</span></label>
              <input
                type="email"
                [(ngModel)]="form.email"
                [disabled]="isEditing"
                placeholder="e.g. staff1@daycare.local"
                class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs disabled:opacity-50"
              />
            </div>
            <div>
              <label class="block font-semibold text-text-main mb-1">Phone Number <span class="text-danger">*</span></label>
              <input
                type="tel"
                [(ngModel)]="form.phone"
                placeholder="e.g. +251911334455"
                class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
              />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block font-semibold text-text-main mb-1">Job Title <span class="text-danger">*</span></label>
              <input
                type="text"
                [(ngModel)]="form.jobTitle"
                placeholder="e.g. Lead Teacher, Caregiver"
                class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
              />
            </div>
            <div>
              <label class="block font-semibold text-text-main mb-1">Hire Date <span class="text-danger">*</span></label>
              <input
                type="date"
                [(ngModel)]="form.hireDate"
                class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
              />
            </div>
          </div>

          <!-- Classroom Assignments -->
          <div>
            <label class="block font-semibold text-text-main mb-1.5">Assigned Classrooms</label>
            <div class="grid grid-cols-2 gap-2 border border-border rounded-lg p-2.5 bg-slate-50 max-h-36 overflow-y-auto">
              <label *ngFor="let room of allClassrooms" class="flex items-center gap-2 cursor-pointer p-1 hover:bg-white rounded">
                <input
                  type="checkbox"
                  [checked]="isClassroomSelected(room.id)"
                  (change)="toggleClassroom(room.id)"
                  class="rounded border-border text-primary focus:ring-primary"
                />
                <span class="text-text-main font-medium">{{ room.name }} (Cap: {{ room.capacity }})</span>
              </label>
            </div>
          </div>
        </div>

        <div dialog-actions class="flex justify-end gap-2">
          <app-button variant="secondary" (click)="modalOpen = false">Cancel</app-button>
          <app-button variant="primary" [loading]="saving" (click)="onSave()">Save Staff Member</app-button>
        </div>
      </app-dialog>

      <!-- TEMPORARY PASSWORD DIALOG -->
      <app-dialog
        [(isOpen)]="tempPasswordModalOpen"
        title="Staff Account Credentials Created"
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
              <span>Share this temporary password securely with the staff member.</span>
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
export class AdminStaffComponent implements OnInit {
  private staffService = inject(StaffService);
  private classroomService = inject(ClassroomService);
  private toast = inject(ToastService);

  staffList: StaffDto[] = [];
  allClassrooms: ClassroomDto[] = [];
  loading = true;

  modalOpen = false;
  isEditing = false;
  editingId: string | null = null;
  editingStaffHasPhoto = false;
  editingStaffPhotoUpdatedAt: string | null = null;
  saving = false;

  form: {
    firstName: string;
    lastName: string;
    email: string;
    phone: string;
    jobTitle: string;
    hireDate: string;
    classroomIds: string[];
  } = {
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    jobTitle: '',
    hireDate: new Date().toISOString().split('T')[0],
    classroomIds: []
  };

  tempPasswordModalOpen = false;
  tempPassword = '';
  tempPasswordEmail = '';
  copiedPassword = false;
  showTempPassword = true;

  get activeStaffCount(): number {
    return this.staffList.filter(s => s.isActive).length;
  }

  get classroomsCoveredCount(): number {
    const set = new Set<string>();
    this.staffList.forEach(s => (s.classrooms || []).forEach(c => set.add(c.classroomId)));
    return set.size;
  }

  get unassignedStaffCount(): number {
    return this.staffList.filter(s => !s.classrooms || s.classrooms.length === 0).length;
  }

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.loading = true;
    this.staffService.getStaff().subscribe({
      next: (res) => {
        this.staffList = res;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toast.error('Failed to load staff list');
      }
    });

    this.classroomService.getClassrooms().subscribe({
      next: (rooms) => (this.allClassrooms = rooms),
      error: () => {}
    });
  }

  isClassroomSelected(roomId: string): boolean {
    return this.form.classroomIds.includes(roomId);
  }

  toggleClassroom(roomId: string): void {
    if (this.isClassroomSelected(roomId)) {
      this.form.classroomIds = this.form.classroomIds.filter(id => id !== roomId);
    } else {
      this.form.classroomIds.push(roomId);
    }
  }

  openCreateModal(): void {
    this.isEditing = false;
    this.editingId = null;
    this.form = {
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      jobTitle: 'Teacher',
      hireDate: new Date().toISOString().split('T')[0],
      classroomIds: []
    };
    this.modalOpen = true;
  }

  openEditModal(s: StaffDto): void {
    this.isEditing = true;
    this.editingId = s.id;
    this.editingStaffHasPhoto = s.hasPhoto ?? false;
    this.editingStaffPhotoUpdatedAt = s.photoUpdatedAt ?? null;
    this.form = {
      firstName: s.firstName,
      lastName: s.lastName,
      email: s.email,
      phone: s.phone,
      jobTitle: s.jobTitle,
      hireDate: s.hireDate,
      classroomIds: (s.classrooms || []).map(c => c.classroomId)
    };
    this.modalOpen = true;
  }

  onPhotoChanged(event: { hasPhoto: boolean; photoUpdatedAt: string | null }): void {
    this.editingStaffHasPhoto = event.hasPhoto;
    this.editingStaffPhotoUpdatedAt = event.photoUpdatedAt;
    if (this.editingId) {
      const found = this.staffList.find(x => x.id === this.editingId);
      if (found) {
        found.hasPhoto = event.hasPhoto;
        found.photoUpdatedAt = event.photoUpdatedAt;
      }
    }
  }

  onSave(): void {
    if (!this.form.firstName.trim() || !this.form.lastName.trim() || !this.form.email.trim() || !this.form.phone.trim()) {
      this.toast.warning('First name, last name, email, and phone are required');
      return;
    }

    this.saving = true;
    if (this.isEditing && this.editingId) {
      const req: UpdateStaffRequest = {
        firstName: this.form.firstName,
        lastName: this.form.lastName,
        phone: this.form.phone,
        jobTitle: this.form.jobTitle,
        hireDate: this.form.hireDate,
        classroomIds: this.form.classroomIds
      };

      this.staffService.updateStaff(this.editingId, req).subscribe({
        next: () => {
          this.saving = false;
          this.modalOpen = false;
          this.toast.success('Staff member updated successfully');
          this.loadData();
        },
        error: (err) => {
          this.saving = false;
          this.toast.error(err.error?.detail || 'Failed to update staff member');
        }
      });
    } else {
      const req: CreateStaffRequest = {
        firstName: this.form.firstName,
        lastName: this.form.lastName,
        email: this.form.email,
        phone: this.form.phone,
        jobTitle: this.form.jobTitle,
        hireDate: this.form.hireDate,
        classroomIds: this.form.classroomIds
      };

      this.staffService.createStaff(req).subscribe({
        next: (res) => {
          this.saving = false;
          this.modalOpen = false;
          this.toast.success('Staff member created successfully');
          this.loadData();

          if (res.temporaryPassword) {
            this.tempPassword = res.temporaryPassword;
            this.tempPasswordEmail = this.form.email;
            this.showTempPassword = true;
            this.tempPasswordModalOpen = true;
          }
        },
        error: (err) => {
          this.saving = false;
          this.toast.error(err.error?.detail || 'Failed to create staff member');
        }
      });
    }
  }

  resetPassword(s: StaffDto): void {
    this.staffService.resetPassword(s.id).subscribe({
      next: (res) => {
        this.toast.success('Password reset successfully');
        if (res.temporaryPassword) {
          this.tempPassword = res.temporaryPassword;
          this.tempPasswordEmail = s.email;
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

  deleteStaff(s: StaffDto): void {
    if (!confirm(`Are you sure you want to deactivate and remove staff member ${s.firstName} ${s.lastName}? This will also revoke their login access.`)) return;

    this.staffService.deleteStaff(s.id).subscribe({
      next: () => {
        this.toast.success('Staff member removed successfully');
        this.loadData();
      },
      error: (err) => {
        this.toast.error(err.error?.detail || err.error?.title || 'Failed to remove staff');
      }
    });
  }
}
