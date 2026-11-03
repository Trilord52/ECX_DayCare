import { Component, Input, Output, EventEmitter, HostListener, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { A11yModule } from '@angular/cdk/a11y';

@Component({
  selector: 'app-dialog',
  standalone: true,
  imports: [CommonModule, A11yModule],
  template: `
    <div
      *ngIf="isOpen"
      class="fixed inset-0 z-50 overflow-y-auto"
      aria-labelledby="modal-title"
      role="dialog"
      aria-modal="true"
    >
      <!-- Backdrop -->
      <div
        class="fixed inset-0 bg-slate-900/40 backdrop-blur-sm transition-opacity"
        (click)="onBackdropClick()"
      ></div>

      <!-- Dialog panel -->
      <div class="flex min-h-full items-center justify-center p-4 text-center sm:p-0">
        <div
          cdkTrapFocus
          [cdkTrapFocusAutoCapture]="true"
          class="relative transform overflow-hidden rounded-card bg-surface-card text-left shadow-xl transition-all w-full border border-border sm:my-8"
          [class]="maxWidthClass"
          (click)="$event.stopPropagation()"
        >
          <!-- Header -->
          <div class="px-6 py-4 border-b border-border flex items-center justify-between">
            <h3 class="text-lg font-semibold text-text-main" id="modal-title">
              {{ title }}
            </h3>
            <button
              type="button"
              (click)="close()"
              aria-label="Close dialog"
              class="text-text-muted hover:text-text-main p-1.5 rounded-input hover:bg-slate-100 transition-colors cursor-pointer focus:ring-2 focus:ring-primary focus:outline-none"
            >
              <svg class="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M6 18L18 6M6 6l12 12" />
              </svg>
            </button>
          </div>

          <!-- Body -->
          <div class="px-6 py-4">
            <ng-content></ng-content>
          </div>

          <!-- Footer -->
          <div *ngIf="showFooter" class="px-6 py-3 bg-slate-50 border-t border-border flex justify-end gap-3">
            <ng-content select="[dialog-actions]"></ng-content>
          </div>
        </div>
      </div>
    </div>
  `
})
export class DialogComponent implements OnChanges {
  @Input() isOpen = false;
  @Input() title = '';
  @Input() maxWidth: 'sm' | 'md' | 'lg' | 'xl' = 'md';
  @Input() closeOnBackdrop = true;
  @Input() showFooter = true;
  @Output() isOpenChange = new EventEmitter<boolean>();
  @Output() closed = new EventEmitter<void>();

  private openerElement: HTMLElement | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['isOpen']) {
      if (this.isOpen) {
        if (typeof document !== 'undefined') {
          this.openerElement = document.activeElement as HTMLElement;
        }
      } else {
        this.restoreOpenerFocus();
      }
    }
  }

  get maxWidthClass(): string {
    const classes = {
      sm: 'sm:max-w-sm',
      md: 'sm:max-w-md',
      lg: 'sm:max-w-lg',
      xl: 'sm:max-w-2xl'
    };
    return classes[this.maxWidth];
  }

  @HostListener('document:keydown.escape', ['$event'])
  onEscape(event: KeyboardEvent): void {
    if (this.isOpen) {
      event.preventDefault();
      this.close();
    }
  }

  onBackdropClick(): void {
    if (this.closeOnBackdrop) {
      this.close();
    }
  }

  close(): void {
    this.isOpen = false;
    this.isOpenChange.emit(false);
    this.closed.emit();
    this.restoreOpenerFocus();
  }

  private restoreOpenerFocus(): void {
    if (this.openerElement && typeof this.openerElement.focus === 'function') {
      const el = this.openerElement;
      this.openerElement = null;
      setTimeout(() => {
        el.focus();
      }, 0);
    }
  }
}
