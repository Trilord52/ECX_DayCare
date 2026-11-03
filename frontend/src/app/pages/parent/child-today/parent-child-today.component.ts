import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ChildrenService, ChildDetailDto } from '../../../core/services/children.service';
import { DailyReportsService, DailyReportDetailDto, DailyReportEntryDto } from '../../../core/services/daily-reports.service';
import { ToastService } from '../../../core/services/toast.service';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';
import { ChildAllergyFlagComponent } from '../../../shared/components/child-allergy-flag/child-allergy-flag.component';
import { TimelineComponent } from '../../../shared/components/timeline/timeline.component';
import { AvatarComponent } from '../../../shared/components/avatar/avatar.component';

@Component({
  selector: 'app-parent-child-today',
  standalone: true,
  imports: [CommonModule, RouterLink, BadgeComponent, ChildAllergyFlagComponent, TimelineComponent, AvatarComponent],
  template: `
    <div class="max-w-md mx-auto space-y-6 pb-20">
      <!-- Breadcrumb / Back button -->
      <nav class="flex items-center gap-2 text-xs text-text-muted">
        <a routerLink="/parent/home" class="hover:text-primary transition-colors flex items-center gap-1">
          <span>&larr; Back to Children</span>
        </a>
      </nav>

      <!-- Error / Forbidden State -->
      <div *ngIf="errorMessage" class="p-6 rounded-card bg-danger-soft border border-danger text-danger text-center space-y-3">
        <div class="w-12 h-12 mx-auto rounded-full bg-danger/10 text-danger flex items-center justify-center">
          <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
          </svg>
        </div>
        <h2 class="text-base font-bold">Access Refused</h2>
        <p class="text-xs">{{ errorMessage }}</p>
        <div class="pt-2">
          <a routerLink="/parent/home" class="px-4 py-2 rounded-input bg-danger text-white text-xs font-bold inline-block">
            Return to Family Home
          </a>
        </div>
      </div>

      <!-- Loading State -->
      <div *ngIf="loading && !errorMessage" class="py-16 text-center text-text-muted">
        <div class="animate-spin inline-block w-8 h-8 border-3 border-primary border-t-transparent rounded-full mb-3"></div>
        <p class="text-xs font-medium">Loading child's daily schedule...</p>
      </div>

      <!-- Child Profile & Today Summary -->
      <div *ngIf="!loading && child" class="space-y-6">
        <!-- Profile Card -->
        <div class="bg-surface-card rounded-card border border-border p-5 shadow-card space-y-4">
          <div class="flex items-start justify-between gap-3">
            <div class="flex items-center gap-3">
              <app-avatar
                [kind]="'children'"
                [id]="child.id"
                [name]="child.firstName + ' ' + child.lastName"
                [hasPhoto]="child.hasPhoto ?? false"
                [photoUpdatedAt]="child.photoUpdatedAt"
                [size]="64"
              ></app-avatar>
              <div>
                <h1 class="text-lg font-bold text-text-main">{{ child.firstName }} {{ child.lastName }}</h1>
                <p class="text-xs text-text-muted">{{ child.classroomName }}</p>
                <p class="text-xs text-text-muted mt-0.5">DOB: {{ child.dateOfBirth }}</p>
              </div>
            </div>

            <app-badge [status]="child.status" [text]="child.status"></app-badge>
          </div>

          <!-- Allergy Banner if present -->
          <div *ngIf="child.allergies" class="pt-1">
            <app-child-allergy-flag [allergies]="child.allergies" size="md"></app-child-allergy-flag>
          </div>

          <!-- Metric Chips (Check-in, Mood) -->
          <div class="grid grid-cols-2 gap-3 pt-2 border-t border-border">
            <div class="p-2.5 rounded-lg bg-slate-50 border border-border text-xs">
              <span class="text-text-muted uppercase text-xs font-semibold block">Today's Status</span>
              <span class="font-bold text-text-main text-sm mt-0.5 block">{{ child.status }}</span>
            </div>

            <div class="p-2.5 rounded-lg bg-slate-50 border border-border text-xs">
              <span class="text-text-muted uppercase text-xs font-semibold block">Current Mood</span>
              <span class="font-bold text-primary text-sm mt-0.5 block">{{ report?.mood || 'Normal' }}</span>
            </div>
          </div>
        </div>

        <!-- Routine Timeline -->
        <div class="bg-surface-card rounded-card border border-border p-5 shadow-card space-y-4">
          <div class="flex items-center justify-between border-b border-border pb-3">
            <div>
              <h2 class="text-sm font-bold text-text-main">Today's Daily Routine</h2>
              <p class="text-xs text-text-muted">Logged by classroom teachers in real time</p>
            </div>
            <span class="text-xs font-semibold px-2.5 py-1 rounded-full bg-slate-100 text-text-main flex items-center gap-1.5">
              <svg class="w-3.5 h-3.5 text-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M8 7V3m8 4V3m-9 8h10M5 21h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
              <span>Today</span>
            </span>
          </div>

          <!-- Reusable Shared Timeline -->
          <app-timeline [entries]="report?.entries || []"></app-timeline>

          <!-- Educator Daily Notes -->
          <div *ngIf="report?.notes" class="mt-4 p-3.5 rounded-lg bg-primary-soft/30 border border-primary/20 text-xs">
            <span class="font-bold text-primary block mb-1">Teacher's Note:</span>
            <p class="text-text-main italic">{{ report?.notes }}</p>
          </div>
        </div>
      </div>
    </div>
  `
})
export class ParentChildTodayComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private childrenService = inject(ChildrenService);
  private dailyReportsService = inject(DailyReportsService);
  private toast = inject(ToastService);

  child: ChildDetailDto | null = null;
  report: DailyReportDetailDto | null = null;
  loading = true;
  errorMessage = '';

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadChildAndReport(id);
    }
  }

  loadChildAndReport(id: string): void {
    this.loading = true;
    this.errorMessage = '';

    this.childrenService.getChild(id).subscribe({
      next: (child) => {
        this.child = child;
        this.dailyReportsService.getTodayReport(id).subscribe({
          next: (rep: DailyReportDetailDto | null) => {
            this.report = rep;
            this.loading = false;
          },
          error: () => {
            this.report = null;
            this.loading = false;
          }
        });
      },
      error: (err) => {
        this.loading = false;
        if (err.status === 403 || err.status === 404) {
          this.errorMessage = 'You are not authorized to view this child. Access is restricted to assigned guardians only.';
        } else {
          this.errorMessage = 'Unable to load child information.';
        }
      }
    });
  }
}

