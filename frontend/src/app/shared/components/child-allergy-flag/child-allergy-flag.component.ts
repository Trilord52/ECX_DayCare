import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-child-allergy-flag',
  standalone: true,
  imports: [CommonModule],
  template: `
    <span
      *ngIf="allergies && allergies.trim().length > 0"
      [class]="containerClasses"
      role="status"
      aria-label="Allergy alert"
    >
      <svg class="w-3.5 h-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor" aria-hidden="true">
        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
      </svg>
      <span class="font-medium truncate">Allergy: {{ allergies }}</span>
    </span>
  `
})
export class ChildAllergyFlagComponent {
  @Input() allergies?: string | null = '';
  @Input() size: 'sm' | 'md' = 'sm';

  get containerClasses(): string {
    const base = 'inline-flex items-center gap-1.5 rounded-full font-medium bg-danger-soft text-danger border border-danger/30';
    if (this.size === 'md') {
      return `${base} px-3 py-1 text-xs`;
    }
    return `${base} px-2 py-0.5 text-xs`;
  }
}

