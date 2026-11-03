import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AnnouncementsService, AnnouncementDto, CreateAnnouncementRequest, UpdateAnnouncementRequest, AnnouncementAudience } from '../../../core/services/announcements.service';
import { ClassroomService, ClassroomDto } from '../../../core/services/classroom.service';
import { ToastService } from '../../../core/services/toast.service';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { DialogComponent } from '../../../shared/components/dialog/dialog.component';

@Component({
  selector: 'app-admin-announcements',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent, DialogComponent],
  template: `
    <div class="space-y-6">
      <!-- Header -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold tracking-tight text-text-main">Announcements & Notices</h1>
          <p class="text-sm text-text-muted mt-0.5">Publish bulletins, center events, and emergency notices to staff, parents, or specific classrooms</p>
        </div>
        <div class="flex items-center gap-3">
          <app-button variant="primary" (click)="openCreateModal()">
            + New Announcement
          </app-button>
        </div>
      </div>

      <!-- Loading / Empty -->
      <div *ngIf="loading" class="p-12 text-center text-text-muted text-sm">
        Loading announcements...
      </div>

      <div *ngIf="!loading && announcements.length === 0" class="p-12 text-center bg-surface-card rounded-card border border-border">
        <p class="text-sm font-semibold text-text-main">No announcements published</p>
        <p class="text-xs text-text-muted mt-1">Broadcast important reminders or upcoming center holidays to parents and staff.</p>
      </div>

      <!-- Announcements Feed -->
      <div *ngIf="!loading && announcements.length > 0" class="space-y-4">
        <article
          *ngFor="let a of announcements"
          class="bg-surface-card rounded-card border border-border p-5 shadow-card hover:border-slate-300 transition space-y-3"
        >
          <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
            <div class="flex items-center gap-3">
              <h2 class="text-base font-bold text-text-main">{{ a.title }}</h2>
              <span
                [class.bg-purple-100]="a.audience === 'All'"
                [class.text-purple-800]="a.audience === 'All'"
                [class.bg-emerald-100]="a.audience === 'Staff'"
                [class.text-emerald-800]="a.audience === 'Staff'"
                [class.bg-blue-100]="a.audience === 'Parents'"
                [class.text-blue-800]="a.audience === 'Parents'"
                [class.bg-amber-100]="a.audience === 'Classroom'"
                [class.text-amber-800]="a.audience === 'Classroom'"
                class="px-2.5 py-0.5 rounded-full text-xs font-bold"
              >
                Audience: {{ a.audience }} <span *ngIf="a.classroomName">({{ a.classroomName }})</span>
              </span>
            </div>

            <div class="flex items-center gap-2 text-xs text-text-muted">
              <span>By {{ a.authorName }}</span>
              <span>•</span>
              <span>{{ a.createdAt | slice:0:10 }}</span>
            </div>
          </div>

          <p class="text-xs text-text-main leading-relaxed whitespace-pre-line bg-slate-50/50 p-3 rounded-lg border border-border/50">
            {{ a.body }}
          </p>

          <div class="flex items-center justify-between pt-2 text-xs">
            <span *ngIf="a.expiresAt" class="text-text-muted text-xs">
              Expires: {{ a.expiresAt | slice:0:10 }}
            </span>
            <span *ngIf="!a.expiresAt" class="text-text-muted text-xs">
              No expiration date
            </span>

            <div class="flex items-center gap-2">
              <button
                (click)="openEditModal(a)"
                class="px-2.5 py-1 rounded border border-border text-text-main hover:bg-slate-100 font-semibold transition"
              >
                Edit
              </button>
              <button
                (click)="onDelete(a)"
                class="px-2.5 py-1 rounded border border-danger/30 text-danger hover:bg-danger-soft transition"
              >
                Delete
              </button>
            </div>
          </div>
        </article>
      </div>

      <!-- CREATE / EDIT MODAL -->
      <app-dialog
        [(isOpen)]="modalOpen"
        [title]="isEditing ? 'Edit Announcement' : 'Publish Announcement'"
      >
        <div class="space-y-4 text-xs">
          <div>
            <label class="block font-semibold text-text-main mb-1">Title <span class="text-danger">*</span></label>
            <input
              type="text"
              [(ngModel)]="form.title"
              placeholder="e.g. Center closed next Monday for Meskel holiday"
              class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
            />
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block font-semibold text-text-main mb-1">Target Audience <span class="text-danger">*</span></label>
              <select
                [(ngModel)]="form.audience"
                class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
              >
                <option value="All">All Center (Staff & Parents)</option>
                <option value="Parents">Parents Only</option>
                <option value="Staff">Staff Only</option>
                <option value="Classroom">Specific Classroom</option>
              </select>
            </div>

            <div *ngIf="form.audience === 'Classroom'">
              <label class="block font-semibold text-text-main mb-1">Select Classroom <span class="text-danger">*</span></label>
              <select
                [(ngModel)]="form.classroomId"
                class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
              >
                <option value="" disabled>Select classroom...</option>
                <option *ngFor="let room of classrooms" [value]="room.id">{{ room.name }}</option>
              </select>
            </div>

            <div *ngIf="form.audience !== 'Classroom'">
              <label class="block font-semibold text-text-main mb-1">Expiration Date (Optional)</label>
              <input
                type="date"
                [(ngModel)]="form.expiresAt"
                class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
              />
            </div>
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">Announcement Body <span class="text-danger">*</span></label>
            <textarea
              [(ngModel)]="form.body"
              rows="4"
              placeholder="Write announcement details here..."
              class="w-full p-2.5 rounded-input border border-border bg-white text-xs"
            ></textarea>
          </div>
        </div>

        <div dialog-actions class="flex justify-end gap-2">
          <app-button variant="secondary" (click)="modalOpen = false">Cancel</app-button>
          <app-button variant="primary" [loading]="saving" (click)="onSave()">Publish Notice</app-button>
        </div>
      </app-dialog>
    </div>
  `
})
export class AdminAnnouncementsComponent implements OnInit {
  private announcementsService = inject(AnnouncementsService);
  private classroomService = inject(ClassroomService);
  private toast = inject(ToastService);

  announcements: AnnouncementDto[] = [];
  classrooms: ClassroomDto[] = [];
  loading = true;

  modalOpen = false;
  isEditing = false;
  editingId: string | null = null;
  saving = false;

  form: {
    title: string;
    body: string;
    audience: AnnouncementAudience;
    classroomId: string;
    expiresAt: string;
  } = {
    title: '',
    body: '',
    audience: 'All',
    classroomId: '',
    expiresAt: ''
  };

  ngOnInit(): void {
    this.loadAnnouncements();
    this.classroomService.getClassrooms().subscribe({
      next: (res) => (this.classrooms = res),
      error: () => {}
    });
  }

  loadAnnouncements(): void {
    this.loading = true;
    this.announcementsService.getAnnouncements().subscribe({
      next: (res) => {
        this.announcements = res.items;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toast.error('Failed to load announcements');
      }
    });
  }

  openCreateModal(): void {
    this.isEditing = false;
    this.editingId = null;
    this.form = {
      title: '',
      body: '',
      audience: 'All',
      classroomId: this.classrooms.length > 0 ? this.classrooms[0].id : '',
      expiresAt: ''
    };
    this.modalOpen = true;
  }

  openEditModal(a: AnnouncementDto): void {
    this.isEditing = true;
    this.editingId = a.id;
    this.form = {
      title: a.title,
      body: a.body,
      audience: a.audience,
      classroomId: a.classroomId || '',
      expiresAt: a.expiresAt ? a.expiresAt.slice(0, 10) : ''
    };
    this.modalOpen = true;
  }

  onSave(): void {
    if (!this.form.title.trim() || !this.form.body.trim()) {
      this.toast.warning('Title and body are required');
      return;
    }

    this.saving = true;
    const expiresAtIso = this.form.expiresAt ? new Date(this.form.expiresAt).toISOString() : undefined;
    const classroomId = this.form.audience === 'Classroom' ? this.form.classroomId : undefined;

    if (this.isEditing && this.editingId) {
      const req: UpdateAnnouncementRequest = {
        title: this.form.title.trim(),
        body: this.form.body.trim(),
        audience: this.form.audience,
        classroomId: classroomId,
        expiresAt: expiresAtIso
      };

      this.announcementsService.updateAnnouncement(this.editingId, req).subscribe({
        next: () => {
          this.saving = false;
          this.modalOpen = false;
          this.toast.success('Announcement updated');
          this.loadAnnouncements();
        },
        error: (err) => {
          this.saving = false;
          this.toast.error(err.error?.detail || 'Failed to update announcement');
        }
      });
    } else {
      const req: CreateAnnouncementRequest = {
        title: this.form.title.trim(),
        body: this.form.body.trim(),
        audience: this.form.audience,
        classroomId: classroomId,
        expiresAt: expiresAtIso
      };

      this.announcementsService.createAnnouncement(req).subscribe({
        next: () => {
          this.saving = false;
          this.modalOpen = false;
          this.toast.success('Announcement published');
          this.loadAnnouncements();
        },
        error: (err) => {
          this.saving = false;
          this.toast.error(err.error?.detail || 'Failed to create announcement');
        }
      });
    }
  }

  onDelete(a: AnnouncementDto): void {
    if (!confirm(`Are you sure you want to delete "${a.title}"?`)) return;

    this.announcementsService.deleteAnnouncement(a.id).subscribe({
      next: () => {
        this.toast.success('Announcement deleted');
        this.loadAnnouncements();
      },
      error: () => this.toast.error('Failed to delete announcement')
    });
  }
}
