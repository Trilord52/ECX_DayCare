import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type ButtonVariant = 'primary' | 'secondary' | 'outline' | 'danger' | 'soft' | 'ghost';
export type ButtonSize = 'sm' | 'md' | 'lg';

@Component({
  selector: 'app-button',
  standalone: true,
  imports: [CommonModule],
  template: `
    <button
      [type]="type"
      [disabled]="disabled || loading"
      [class]="computedClasses"
    >
      <svg
        *ngIf="loading"
        class="animate-spin -ml-1 mr-2 h-4 w-4 text-current inline"
        xmlns="http://www.w3.org/2000/svg"
        fill="none"
        viewBox="0 0 24 24"
      >
        <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
        <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
      </svg>
      <ng-content></ng-content>
    </button>
  `
})
export class ButtonComponent {
  @Input() type: 'button' | 'submit' | 'reset' = 'button';
  @Input() variant: ButtonVariant = 'primary';
  @Input() size: ButtonSize = 'md';
  @Input() disabled = false;
  @Input() loading = false;
  @Input() customClass = '';

  get computedClasses(): string {
    const base = 'inline-flex items-center justify-center font-medium rounded-input transition-colors focus:outline-none focus:ring-2 focus:ring-offset-2 cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed select-none';

    const sizeClasses = {
      sm: 'px-3 py-1.5 text-xs min-h-[36px]',
      md: 'px-4 py-2 text-sm min-h-[40px]',
      lg: 'px-6 py-3 text-base min-h-[44px]'
    }[this.size];

    const variantClasses = {
      primary: 'bg-primary text-white hover:bg-primary-hover focus:ring-primary',
      secondary: 'bg-accent text-white hover:bg-accent-hover focus:ring-accent',
      outline: 'border border-border bg-white text-text-main hover:bg-surface-bg focus:ring-primary',
      danger: 'bg-danger text-white hover:bg-red-700 focus:ring-danger',
      soft: 'bg-primary-soft text-primary hover:bg-teal-100 focus:ring-primary',
      ghost: 'bg-transparent text-text-muted hover:text-text-main hover:bg-slate-100 focus:ring-slate-400'
    }[this.variant];

    return `${base} ${sizeClasses} ${variantClasses} ${this.customClass}`;
  }
}
