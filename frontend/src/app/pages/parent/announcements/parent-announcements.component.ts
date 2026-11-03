import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AnnouncementsService, AnnouncementDto } from '../../../core/services/announcements.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-parent-announcements',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="space-y-6 max-w-2xl mx-auto pb-12">
      <!-- Header -->
      <div>
        <h1 class="text-xl font-bold tracking-tight text-text-main">Center Notices & Bulletins</h1>
        <p class="text-xs text-text-muted mt-0.5">Stay up to date with daycare events, holiday closures, and messages from staff</p>
      </div>

      <!-- Loading / Empty -->
      <div *ngIf="loading" class="p-12 text-center text-text-muted text-xs">
        Loading bulletins...
      </div>

      <div *ngIf="!loading && announcements.length === 0" class="p-12 text-center bg-surface-card rounded-card border border-border">
        <p class="text-sm font-semibold text-text-main">No announcements at this time</p>
        <p class="text-xs text-text-muted mt-1">Check back later for news and center announcements.</p>
      </div>

      <!-- Announcements List -->
      <div *ngIf="!loading && announcements.length > 0" class="space-y-4">
        <article
          *ngFor="let a of announcements"
          class="bg-surface-card rounded-card border border-border p-4 shadow-card space-y-2.5"
        >
          <div class="flex items-start justify-between gap-2">
            <h2 class="text-sm font-bold text-text-main">{{ a.title }}</h2>
            <span class="text-xs text-text-muted font-mono shrink-0">{{ a.createdAt | slice:0:10 }}</span>
          </div>

          <p class="text-xs text-text-main leading-relaxed whitespace-pre-line bg-slate-50 p-3 rounded-lg border border-border/50">
            {{ a.body }}
          </p>

          <div class="flex items-center justify-between text-xs text-text-muted pt-1">
            <span>Posted by: <strong>{{ a.authorName }}</strong></span>
            <span *ngIf="a.classroomName" class="text-primary font-medium">Class: {{ a.classroomName }}</span>
          </div>
        </article>
      </div>
    </div>
  `
})
export class ParentAnnouncementsComponent implements OnInit {
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
