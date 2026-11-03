import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { AvatarComponent } from '../../../shared/components/avatar/avatar.component';

@Component({
  selector: 'app-parent-profile',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent, AvatarComponent],
  template: `
    <div class="space-y-6 max-w-xl mx-auto pb-12">
      <!-- Header -->
      <div>
        <h1 class="text-xl font-bold tracking-tight text-text-main">Account & Security</h1>
        <p class="text-xs text-text-muted mt-0.5">Manage your parent profile and security credentials</p>
      </div>

      <!-- Account Profile Card -->
      <article class="bg-surface-card rounded-card border border-border p-5 shadow-card space-y-4">
        <div class="flex items-center gap-4 border-b border-border pb-4">
          <app-avatar
            [kind]="'guardians'"
            [id]="user?.guardianId"
            [name]="user?.fullName || ''"
            [hasPhoto]="user?.hasPhoto ?? false"
            [photoUpdatedAt]="user?.photoUpdatedAt"
            [size]="80"
          ></app-avatar>
          <div>
            <h2 class="text-base font-bold text-text-main">{{ user?.fullName }}</h2>
            <p class="text-xs text-text-muted">{{ user?.email }}</p>
          </div>
        </div>

        <div class="grid grid-cols-2 gap-3 text-xs">
          <div class="p-2.5 bg-slate-50 rounded-lg border border-border">
            <span class="text-text-muted block text-xs">Account Type</span>
            <strong class="text-text-main font-semibold">{{ user?.role }} Portal</strong>
          </div>
          <div class="p-2.5 bg-slate-50 rounded-lg border border-border">
            <span class="text-text-muted block text-xs">Portal Status</span>
            <strong class="text-success font-semibold">Active & Verified</strong>
          </div>
        </div>
      </article>

      <!-- Change Password Card -->
      <article class="bg-surface-card rounded-card border border-border p-5 shadow-card space-y-4">
        <h2 class="text-sm font-bold text-text-main">Update Password</h2>

        <div class="space-y-3 text-xs">
          <div>
            <label class="block font-semibold text-text-main mb-1">Current Password <span class="text-danger">*</span></label>
            <div class="relative">
              <input
                [type]="showCurrentPassword ? 'text' : 'password'"
                [(ngModel)]="pwdForm.currentPassword"
                placeholder="Enter existing password"
                class="w-full h-9 pl-3 pr-10 rounded-input border border-border bg-white text-xs text-text-main focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
              />
              <button
                type="button"
                (click)="showCurrentPassword = !showCurrentPassword"
                [attr.aria-label]="showCurrentPassword ? 'Hide current password' : 'Show current password'"
                class="absolute inset-y-0 right-0 w-9 flex items-center justify-center text-text-muted hover:text-text-main cursor-pointer focus:outline-none"
              >
                <svg *ngIf="!showCurrentPassword" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                </svg>
                <svg *ngIf="showCurrentPassword" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l18 18" />
                </svg>
              </button>
            </div>
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">New Password <span class="text-danger">*</span></label>
            <div class="relative">
              <input
                [type]="showNewPassword ? 'text' : 'password'"
                [(ngModel)]="pwdForm.newPassword"
                placeholder="At least 6 characters with upper, lower, number, special"
                class="w-full h-9 pl-3 pr-10 rounded-input border border-border bg-white text-xs text-text-main focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
              />
              <button
                type="button"
                (click)="showNewPassword = !showNewPassword"
                [attr.aria-label]="showNewPassword ? 'Hide new password' : 'Show new password'"
                class="absolute inset-y-0 right-0 w-9 flex items-center justify-center text-text-muted hover:text-text-main cursor-pointer focus:outline-none"
              >
                <svg *ngIf="!showNewPassword" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                </svg>
                <svg *ngIf="showNewPassword" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l18 18" />
                </svg>
              </button>
            </div>
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">Confirm New Password <span class="text-danger">*</span></label>
            <div class="relative">
              <input
                [type]="showConfirmPassword ? 'text' : 'password'"
                [(ngModel)]="pwdForm.confirmPassword"
                placeholder="Repeat new password"
                class="w-full h-9 pl-3 pr-10 rounded-input border border-border bg-white text-xs text-text-main focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
              />
              <button
                type="button"
                (click)="showConfirmPassword = !showConfirmPassword"
                [attr.aria-label]="showConfirmPassword ? 'Hide confirm password' : 'Show confirm password'"
                class="absolute inset-y-0 right-0 w-9 flex items-center justify-center text-text-muted hover:text-text-main cursor-pointer focus:outline-none"
              >
                <svg *ngIf="!showConfirmPassword" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                </svg>
                <svg *ngIf="showConfirmPassword" class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l18 18" />
                </svg>
              </button>
            </div>
          </div>

          <div class="pt-2">
            <app-button variant="primary" [loading]="changingPassword" (click)="onChangePassword()">
              Update Password
            </app-button>
          </div>
        </div>
      </article>

      <!-- Sign Out Action -->
      <div class="pt-2">
        <button
          (click)="onSignOut()"
          class="w-full py-2.5 rounded-input border border-danger/30 text-danger hover:bg-danger-soft text-xs font-bold transition text-center"
        >
          Sign Out of Account
        </button>
      </div>
    </div>
  `
})
export class ParentProfileComponent {
  private auth = inject(AuthService);
  private toast = inject(ToastService);

  user = this.auth.currentUser();
  changingPassword = false;
  showCurrentPassword = false;
  showNewPassword = false;
  showConfirmPassword = false;

  pwdForm = {
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  };

  onChangePassword(): void {
    if (!this.pwdForm.currentPassword || !this.pwdForm.newPassword) {
      this.toast.warning('Please enter current and new password');
      return;
    }

    if (this.pwdForm.newPassword !== this.pwdForm.confirmPassword) {
      this.toast.warning('New password and confirmation do not match');
      return;
    }

    this.changingPassword = true;
    this.auth.changePassword({
      currentPassword: this.pwdForm.currentPassword,
      newPassword: this.pwdForm.newPassword
    }).subscribe({
      next: () => {
        this.changingPassword = false;
        this.pwdForm = { currentPassword: '', newPassword: '', confirmPassword: '' };
        this.toast.success('Password updated successfully');
      },
      error: (err) => {
        this.changingPassword = false;
        this.toast.error(err.error?.detail || err.error?.title || 'Failed to update password');
      }
    });
  }

  onSignOut(): void {
    this.auth.logout();
  }
}
