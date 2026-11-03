import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ToastService, ToastType } from '../../../core/services/toast.service';

@Component({
  selector: 'app-toast',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div
      aria-live="assertive"
      class="fixed bottom-4 right-4 z-50 flex flex-col gap-2 max-w-sm w-full pointer-events-none"
    >
      <div
        *ngFor="let toast of toastService.toasts()"
        class="pointer-events-auto flex items-start gap-3 p-4 rounded-card border shadow-lg transition-all duration-300"
        [class]="getToastClasses(toast.type)"
      >
        <!-- Icon -->
        <div class="flex-shrink-0 pt-0.5">
          <svg *ngIf="toast.type === 'success'" class="w-5 h-5 text-success" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
          </svg>
          <svg *ngIf="toast.type === 'danger'" class="w-5 h-5 text-danger" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
          <svg *ngIf="toast.type === 'warning'" class="w-5 h-5 text-warning" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
          </svg>
          <svg *ngIf="toast.type === 'info'" class="w-5 h-5 text-info" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
          </svg>
        </div>

        <!-- Content -->
        <div class="flex-1 min-w-0">
          <p *ngIf="toast.title" class="text-sm font-semibold text-text-main leading-tight">{{ toast.title }}</p>
          <p class="text-xs text-text-muted mt-0.5">{{ toast.message }}</p>
        </div>

        <!-- Dismiss button -->
        <button
          type="button"
          (click)="toastService.dismiss(toast.id)"
          aria-label="Dismiss notification"
          class="flex-shrink-0 text-text-muted hover:text-text-main p-1.5 rounded-input cursor-pointer"
        >
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>
    </div>
  `
})
export class ToastComponent {
  public toastService = inject(ToastService);

  getToastClasses(type: ToastType): string {
    const map: Record<ToastType, string> = {
      success: 'bg-white border-success/30 text-text-main',
      danger: 'bg-white border-danger/30 text-text-main',
      warning: 'bg-white border-warning/30 text-text-main',
      info: 'bg-white border-info/30 text-text-main'
    };
    return map[type];
  }
}
