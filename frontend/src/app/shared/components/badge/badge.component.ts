import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export type BadgeVariant = 'success' | 'warning' | 'danger' | 'info' | 'neutral' | 'primary';

@Component({
  selector: 'app-badge',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span [class]="computedClasses">
      <span *ngIf="dot" class="w-1.5 h-1.5 rounded-full mr-1.5" [class]="dotClass"></span>
      <ng-content></ng-content>
      {{ text }}
    </span>
  `
})
export class BadgeComponent {
  @Input() text = '';
  @Input() variant: BadgeVariant = 'neutral';
  @Input() status = '';
  @Input() dot = false;
  @Input() customClass = '';

  get resolvedVariant(): BadgeVariant {
    if (this.status) {
      switch (this.status) {
        case 'Present':
        case 'Paid':
        case 'Active':
          return 'success';
        case 'CheckedOut':
        case 'Issued':
          return 'info';
        case 'Absent':
        case 'Overdue':
        case 'overdue':
        case 'Serious':
          return 'danger';
        case 'PartiallyPaid':
        case 'Moderate':
          return 'warning';
        case 'NotArrived':
        case 'Void':
        case 'Minor':
        case 'Inactive':
          return 'neutral';
        default:
          return this.variant;
      }
    }
    return this.variant;
  }

  get computedClasses(): string {
    const base = 'inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium select-none';

    const variantClasses: Record<BadgeVariant, string> = {
      success: 'bg-success-soft text-green-800 font-medium',
      warning: 'bg-warning-soft text-amber-800 font-medium',
      danger: 'bg-danger-soft text-red-800 font-medium',
      info: 'bg-info-soft text-blue-800 font-medium',
      neutral: 'bg-slate-100 text-slate-800 font-medium',
      primary: 'bg-primary-soft text-teal-900 font-medium'
    };

    return `${base} ${variantClasses[this.resolvedVariant]} ${this.customClass}`;
  }

  get dotClass(): string {
    const dotClasses: Record<BadgeVariant, string> = {
      success: 'bg-green-700',
      warning: 'bg-amber-700',
      danger: 'bg-red-700',
      info: 'bg-blue-700',
      neutral: 'bg-slate-500',
      primary: 'bg-teal-700'
    };
    return dotClasses[this.resolvedVariant];
  }
}
