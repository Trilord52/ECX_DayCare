import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-card',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div [class]="'bg-surface-card rounded-card border border-border shadow-card overflow-hidden ' + customClass">
      <div *ngIf="title || subtitle" class="px-6 py-4 border-b border-border flex items-center justify-between">
        <div>
          <h3 *ngIf="title" class="text-base font-semibold text-text-main">{{ title }}</h3>
          <p *ngIf="subtitle" class="text-xs text-text-muted mt-0.5">{{ subtitle }}</p>
        </div>
        <div class="flex items-center space-x-2">
          <ng-content select="[card-actions]"></ng-content>
        </div>
      </div>
      <div [class]="padding ? 'p-6' : ''">
        <ng-content></ng-content>
      </div>
      <div *ngIf="hasFooter" class="px-6 py-3 bg-slate-50 border-t border-border flex items-center justify-between">
        <ng-content select="[card-footer]"></ng-content>
      </div>
    </div>
  `
})
export class CardComponent {
  @Input() title = '';
  @Input() subtitle = '';
  @Input() padding = true;
  @Input() hasFooter = false;
  @Input() customClass = '';
}
