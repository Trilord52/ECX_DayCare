import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AnnouncementsService, AnnouncementDto } from '../../../core/services/announcements.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-staff-announcements',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="space-y-6 max-w-4xl mx-auto pb-12">
      <!-- Header -->
      <div>
        <h1 class="text-2xl font-bold tracking-tight text-text-main">Staff Bulletins & Announcements</h1>
        <p class="text-sm text-text-muted mt-0.5">Notices and center updates shared with teaching and caregiving staff</p>
      </div>

      <!-- Loading / Empty -->
      <div *ngIf="loading" class="p-12 text-center text-text-muted text-sm">
        Loading bulletins...
      </div>

      <div *ngIf="!loading && announcements.length === 0" class="p-12 text-center bg-surface-card rounded-card border border-border">
        <p class="text-sm font-semibold text-text-main">No announcements for staff</p>
      </div>

      <!-- Announcements List -->
      <div *ngIf="!loading && announcements.length > 0" class="space-y-4">
        <article
          *ngFor="let a of announcements"
          class="bg-surface-card rounded-card border border-border p-5 shadow-card space-y-3"
        >
          <div class="flex items-start justify-between gap-3">
            <div>
              <h2 class="text-base font-bold text-text-main">{{ a.title }}</h2>
              <span class="text-xs text-text-muted">By {{ a.authorName }} • {{ a.createdAt | slice:0:10 }}</span>
            </div>
            <span
              [class.bg-purple-100]="a.audience === 'All'"
              [class.text-purple-800]="a.audience === 'All'"
              [class.bg-emerald-100]="a.audience === 'Staff'"
              [class.text-emerald-800]="a.audience === 'Staff'"
              class="px-2.5 py-0.5 rounded-full text-xs font-bold"
            >
              {{ a.audience }}
            </span>
          </div>

          <p class="text-xs text-text-main leading-relaxed whitespace-pre-line bg-slate-50 p-3.5 rounded-lg border border-border/50">
            {{ a.body }}
          </p>
        </article>
      </div>
    </div>
  `
})
export class StaffAnnouncementsComponent implements OnInit {
  private announcementsService = inject(AnnouncementsService);
  private toast = inject(ToastService);

  announcements: AnnouncementDto[] = [];
  loading = true;

  ngOnInit(): void {
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
}
