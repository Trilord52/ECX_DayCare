import { Component, Input, OnInit, OnChanges, SimpleChanges, inject, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PhotoService } from '../../../core/services/photo.service';

@Component({
  selector: 'app-avatar',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div
      [class]="containerClass"
      [style.width.px]="size"
      [style.height.px]="size"
      [style.min-width.px]="size"
      [style.min-height.px]="size"
      class="rounded-full overflow-hidden shrink-0 flex items-center justify-center bg-primary-soft text-primary font-bold select-none relative"
    >
      <img
        *ngIf="photoUrl && !imageFailed"
        [src]="photoUrl"
        [alt]="name || 'Profile photo'"
        (error)="onImageError()"
        class="w-full h-full object-cover rounded-full"
      />
      <span *ngIf="!photoUrl || imageFailed" [class]="textSizeClass">
        {{ computedInitials }}
      </span>
    </div>
  `
})
export class AvatarComponent implements OnInit, OnChanges {
  private photoService = inject(PhotoService);
  private cdr = inject(ChangeDetectorRef);

  @Input() kind: string = '';
  @Input() id?: string | null = null;
  @Input() hasPhoto: boolean = false;
  @Input() photoUpdatedAt?: string | null = null;
  @Input() size: number = 32;
  @Input() name: string = '';
  @Input() initials?: string = '';
  @Input() customClass: string = '';

  photoUrl: string | null = null;
  imageFailed: boolean = false;

  get containerClass(): string {
    return this.customClass || '';
  }

  get textSizeClass(): string {
    if (this.size <= 32) return 'text-xs';
    if (this.size <= 40) return 'text-xs font-semibold';
    if (this.size <= 48) return 'text-sm font-bold';
    if (this.size <= 64) return 'text-base font-bold';
    if (this.size <= 80) return 'text-xl font-bold';
    return 'text-2xl font-bold';
  }

  get computedInitials(): string {
    if (this.initials && this.initials.trim().length > 0) {
      return this.initials.trim().toUpperCase();
    }
    if (!this.name || !this.name.trim()) {
      return '?';
    }
    const parts = this.name.trim().split(/\s+/);
    if (parts.length === 1) {
      return parts[0].substring(0, Math.min(2, parts[0].length)).toUpperCase();
    }
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }

  ngOnInit(): void {
    this.resolvePhoto();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['hasPhoto'] || changes['id'] || changes['kind'] || changes['photoUpdatedAt']) {
      this.resolvePhoto();
    }
  }

  onImageError(): void {
    this.imageFailed = true;
    this.photoUrl = null;
    this.cdr.markForCheck();
  }

  private resolvePhoto(): void {
    this.imageFailed = false;
    if (!this.hasPhoto || !this.id || !this.kind) {
      this.photoUrl = null;
      return;
    }

    this.photoService.load(this.kind, this.id, this.photoUpdatedAt).subscribe({
      next: (url) => {
        this.photoUrl = url;
        this.cdr.markForCheck();
      },
      error: () => {
        this.photoUrl = null;
        this.cdr.markForCheck();
      }
    });
  }
}

