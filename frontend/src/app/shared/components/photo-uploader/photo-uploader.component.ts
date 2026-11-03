import { Component, Input, Output, EventEmitter, inject, ViewChild, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { PhotoService, PhotoUploadResult } from '../../../core/services/photo.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { AvatarComponent } from '../avatar/avatar.component';
import { DialogComponent } from '../dialog/dialog.component';
import { ButtonComponent } from '../button/button.component';

@Component({
  selector: 'app-photo-uploader',
  standalone: true,
  imports: [CommonModule, AvatarComponent, DialogComponent, ButtonComponent],
  template: `
    <div class="flex flex-col sm:flex-row items-center sm:items-start gap-4 p-4 rounded-card bg-surface-card border border-border shadow-sm">
      <!-- Avatar Display -->
      <app-avatar
        [kind]="kind"
        [id]="id"
        [name]="name"
        [hasPhoto]="hasPhoto"
        [photoUpdatedAt]="photoUpdatedAt"
        [size]="avatarSize"
      ></app-avatar>

      <!-- Controls & Feedback -->
      <div class="flex-1 space-y-2 text-center sm:text-left">
        <div>
          <h4 class="text-sm font-bold text-text-main">
            {{ name ? name + '’s Photo' : 'Profile Photo' }}
          </h4>
          <p class="text-xs text-text-muted mt-0.5">
            Accepts JPEG, PNG, or WebP up to 2 MB. Square orientation is automatically formatted.
          </p>
        </div>

        <!-- In-flight or Error Alerts -->
        <div *ngIf="errorMessage" class="p-2.5 rounded-lg bg-danger-soft border border-danger/20 text-danger text-xs font-medium">
          {{ errorMessage }}
        </div>

        <!-- Action Buttons (Admin only) -->
        <div *ngIf="isAdmin" class="flex flex-wrap items-center justify-center sm:justify-start gap-2 pt-1">
          <input
            #fileInput
            type="file"
            accept="image/jpeg,image/png,image/webp,.jpg,.jpeg,.png,.webp"
            (change)="onFileSelected($event)"
            class="hidden"
            [disabled]="isBusy"
          />

          <app-button
            variant="outline"
            size="sm"
            [loading]="isUploading"
            [disabled]="isBusy"
            (click)="triggerFileInput()"
          >
            {{ hasPhoto ? 'Change photo' : 'Upload photo' }}
          </app-button>

          <app-button
            *ngIf="hasPhoto"
            variant="danger"
            size="sm"
            [disabled]="isBusy"
            (click)="showDeleteConfirm = true"
          >
            Remove photo
          </app-button>
        </div>

        <p *ngIf="!isAdmin" class="text-xs text-text-muted italic">
          Only administrators can update profile photos.
        </p>
      </div>
    </div>

    <!-- Confirmation Dialog for Removal -->
    <app-dialog
      [(isOpen)]="showDeleteConfirm"
      title="Remove profile photo?"
      maxWidth="sm"
    >
      <div class="space-y-3 text-xs text-text-main">
        <p>
          Are you sure you want to remove the profile photo for <strong>{{ name || 'this person' }}</strong>?
        </p>
        <p class="text-text-muted">
          This will delete the stored image file and revert to the initials avatar.
        </p>
      </div>

      <div dialog-actions class="flex justify-end gap-2">
        <app-button variant="outline" size="sm" (click)="showDeleteConfirm = false">
          Cancel
        </app-button>
        <app-button
          variant="danger"
          size="sm"
          [loading]="isDeleting"
          (click)="confirmDelete()"
        >
          Confirm Remove
        </app-button>
      </div>
    </app-dialog>
  `
})
export class PhotoUploaderComponent {
  private photoService = inject(PhotoService);
  private auth = inject(AuthService);
  private toast = inject(ToastService);

  @ViewChild('fileInput') fileInputRef!: ElementRef<HTMLInputElement>;

  @Input({ required: true }) kind!: 'children' | 'guardians' | 'staff' | string;
  @Input({ required: true }) id!: string;
  @Input() name: string = '';
  @Input() hasPhoto: boolean = false;
  @Input() photoUpdatedAt?: string | null = null;
  @Input() avatarSize: number = 80;

  @Output() photoChanged = new EventEmitter<{ hasPhoto: boolean; photoUpdatedAt: string | null }>();

  isUploading = false;
  isDeleting = false;
  showDeleteConfirm = false;
  errorMessage: string | null = null;

  get isBusy(): boolean {
    return this.isUploading || this.isDeleting;
  }

  get isAdmin(): boolean {
    return this.auth.role() === 'Admin';
  }

  triggerFileInput(): void {
    this.errorMessage = null;
    if (this.fileInputRef?.nativeElement) {
      this.fileInputRef.nativeElement.value = '';
      this.fileInputRef.nativeElement.click();
    }
  }

  async onFileSelected(event: Event): Promise<void> {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) return;

    const file = input.files[0];
    this.errorMessage = null;

    // Client-side MIME check
    const validMimes = ['image/jpeg', 'image/png', 'image/webp'];
    const validExts = ['.jpg', '.jpeg', '.png', '.webp'];
    const lowerName = file.name.toLowerCase();
    const hasValidExt = validExts.some(ext => lowerName.endsWith(ext));

    if (!validMimes.includes(file.type) && !hasValidExt) {
      this.errorMessage = 'Only JPEG, PNG, and WebP images are allowed.';
      return;
    }

    // Client-side 2 MB check
    const maxSizeBytes = 2 * 1024 * 1024;
    if (file.size > maxSizeBytes) {
      this.errorMessage = 'Image size must not exceed 2 MB.';
      return;
    }

    this.isUploading = true;

    try {
      const processedFile = await this.downscaleIfLarge(file);
      this.photoService.upload(this.kind, this.id, processedFile).subscribe({
        next: (result: PhotoUploadResult) => {
          this.isUploading = false;
          this.hasPhoto = result.hasPhoto;
          this.photoUpdatedAt = result.photoUpdatedAt;
          this.photoChanged.emit({
            hasPhoto: result.hasPhoto,
            photoUpdatedAt: result.photoUpdatedAt
          });
          this.toast.success('Photo updated successfully');
        },
        error: (err: HttpErrorResponse) => {
          this.isUploading = false;
          this.handleServerError(err);
        }
      });
    } catch {
      this.isUploading = false;
      this.errorMessage = 'Failed to process selected image.';
    }
  }

  confirmDelete(): void {
    this.isDeleting = true;
    this.errorMessage = null;

    this.photoService.delete(this.kind, this.id).subscribe({
      next: () => {
        this.isDeleting = false;
        this.showDeleteConfirm = false;
        this.hasPhoto = false;
        this.photoUpdatedAt = null;
        this.photoChanged.emit({
          hasPhoto: false,
          photoUpdatedAt: null
        });
        this.toast.success('Photo removed successfully');
      },
      error: (err: HttpErrorResponse) => {
        this.isDeleting = false;
        this.showDeleteConfirm = false;
        this.handleServerError(err);
      }
    });
  }

  private handleServerError(err: HttpErrorResponse): void {
    const errorBody = err.error;
    if (errorBody?.code) {
      switch (errorBody.code) {
        case 'PHOTO_TOO_LARGE':
          this.errorMessage = 'The image file exceeds the 2 MB size limit.';
          break;
        case 'PHOTO_INVALID_TYPE':
          this.errorMessage = 'Only JPEG, PNG, and WebP formats are supported.';
          break;
        case 'PHOTO_INVALID':
          this.errorMessage = 'Invalid or unreadable image file.';
          break;
        default:
          this.errorMessage = errorBody.detail || errorBody.title || `Server error (${errorBody.code})`;
          break;
      }
    } else if (err.status === 413 || err.status === 400) {
      this.errorMessage = 'File is too large to upload.';
    } else {
      this.errorMessage = errorBody?.detail || errorBody?.title || 'Failed to update photo.';
    }
    this.toast.error(this.errorMessage!);
  }

  private async downscaleIfLarge(file: File): Promise<File> {
    return new Promise<File>((resolve) => {
      // If browser doesn't support canvas or FileReader, fallback to original
      if (typeof window === 'undefined' || !window.FileReader) {
        resolve(file);
        return;
      }

      const reader = new FileReader();
      reader.onload = (e) => {
        const img = new Image();
        img.onload = () => {
          const maxDim = 1024;
          if (img.width <= maxDim && img.height <= maxDim) {
            resolve(file);
            return;
          }

          let width = img.width;
          let height = img.height;
          if (width > height) {
            if (width > maxDim) {
              height = Math.round((height * maxDim) / width);
              width = maxDim;
            }
          } else {
            if (height > maxDim) {
              width = Math.round((width * maxDim) / height);
              height = maxDim;
            }
          }

          const canvas = document.createElement('canvas');
          canvas.width = width;
          canvas.height = height;
          const ctx = canvas.getContext('2d');
          if (!ctx) {
            resolve(file);
            return;
          }

          ctx.drawImage(img, 0, 0, width, height);
          canvas.toBlob((blob) => {
            if (blob) {
              const resizedFile = new File([blob], file.name, {
                type: 'image/jpeg',
                lastModified: Date.now()
              });
              resolve(resizedFile);
            } else {
              resolve(file);
            }
          }, 'image/jpeg', 0.9);
        };
        img.onerror = () => resolve(file);
        img.src = e.target?.result as string;
      };
      reader.onerror = () => resolve(file);
      reader.readAsDataURL(file);
    });
  }
}
