import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ChildrenService, ChildDetailDto } from '../../../core/services/children.service';
import {
  DailyReportsService,
  DailyReportDetailDto,
  MoodType,
  EntryType,
  MealType,
  AmountEaten,
  DiaperType
} from '../../../core/services/daily-reports.service';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { ChildAllergyFlagComponent } from '../../../shared/components/child-allergy-flag/child-allergy-flag.component';
import { TimelineComponent } from '../../../shared/components/timeline/timeline.component';
import { ToastService } from '../../../core/services/toast.service';
import { AvatarComponent } from '../../../shared/components/avatar/avatar.component';

@Component({
  selector: 'app-child-daily-log',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, ButtonComponent, ChildAllergyFlagComponent, TimelineComponent, AvatarComponent],
  template: `
    <div class="space-y-6 max-w-5xl mx-auto pb-12">
      <!-- Top Navigation & Child Header -->
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div class="flex items-center gap-3">
          <a
            routerLink="/staff/my-day"
            class="inline-flex items-center gap-1.5 text-sm font-medium text-text-muted hover:text-text-main transition-colors p-2 rounded-lg hover:bg-slate-100"
          >
            <svg class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M10 19l-7-7m0 0l7-7m-7 7h18" />
            </svg>
            My Day
          </a>
          <span class="text-slate-300">|</span>
          <div class="flex items-center gap-3">
            <app-avatar
              [kind]="'children'"
              [id]="child?.id"
              [name]="child ? child.firstName + ' ' + child.lastName : ''"
              [hasPhoto]="child?.hasPhoto ?? false"
              [photoUpdatedAt]="child?.photoUpdatedAt"
              [size]="64"
            ></app-avatar>
            <div>
              <h1 class="text-xl font-bold text-text-main">{{ child?.firstName }} {{ child?.lastName }}</h1>
              <p class="text-xs text-text-muted">{{ child?.classroomName }}</p>
            </div>
          </div>
          <app-child-allergy-flag [allergies]="child?.allergies" size="md"></app-child-allergy-flag>
        </div>

        <div class="text-xs text-text-muted bg-surface-card px-3 py-1.5 rounded-lg border border-border">
          Date: <strong class="text-text-main">{{ todayDate | date:'mediumDate' }}</strong>
        </div>
      </div>

      <!-- Bento Grid: Left Column Logging Form, Right Column Timeline -->
      <div class="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
        <!-- Left: Operations & Form (7 cols) -->
        <div class="lg:col-span-7 space-y-6">
          <!-- Mood Selection -->
          <div class="bg-surface-card rounded-card border border-border p-5 shadow-card">
            <div class="flex items-center justify-between mb-3">
              <h2 class="text-sm font-bold text-text-main flex items-center gap-2">
                <svg class="w-4 h-4 text-primary" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.828 14.828a4 4 0 01-5.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                </svg>
                <span>Mood Today</span>
              </h2>
              <span *ngIf="dailyReport?.mood" class="text-xs text-primary font-medium">
                Current: {{ dailyReport?.mood }}
              </span>
            </div>

            <div class="grid grid-cols-5 gap-2">
              <button
                *ngFor="let m of moods"
                type="button"
                (click)="selectMood(m.type)"
                [class]="dailyReport?.mood === m.type ? 'bg-primary text-white shadow-sm font-bold scale-105' : 'bg-slate-50 text-text-main hover:bg-slate-100'"
                class="min-h-[44px] flex flex-col items-center justify-center p-2 rounded-xl border border-border transition-all text-xs"
              >
                <ng-container [ngSwitch]="m.type">
                  <!-- Happy -->
                  <svg *ngSwitchCase="'Happy'" class="w-5 h-5 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <circle cx="12" cy="12" r="9" stroke-width="2" />
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.828 14.828a4 4 0 01-5.656 0" />
                    <circle cx="9" cy="9" r="1" fill="currentColor" />
                    <circle cx="15" cy="9" r="1" fill="currentColor" />
                  </svg>
                  <!-- Calm -->
                  <svg *ngSwitchCase="'Calm'" class="w-5 h-5 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <circle cx="12" cy="12" r="9" stroke-width="2" />
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 15h6" />
                    <circle cx="9" cy="9" r="1" fill="currentColor" />
                    <circle cx="15" cy="9" r="1" fill="currentColor" />
                  </svg>
                  <!-- Tired -->
                  <svg *ngSwitchCase="'Tired'" class="w-5 h-5 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <circle cx="12" cy="12" r="9" stroke-width="2" />
                    <circle cx="12" cy="15" r="1.5" stroke-width="2" />
                    <path stroke-linecap="round" stroke-width="2" d="M8 10h2m4 0h2" />
                  </svg>
                  <!-- Fussy -->
                  <svg *ngSwitchCase="'Fussy'" class="w-5 h-5 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <circle cx="12" cy="12" r="9" stroke-width="2" />
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.172 16.172a4 4 0 015.656 0" />
                    <circle cx="9" cy="9" r="1" fill="currentColor" />
                    <circle cx="15" cy="9" r="1" fill="currentColor" />
                  </svg>
                  <!-- Upset -->
                  <svg *ngSwitchCase="'Upset'" class="w-5 h-5 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <circle cx="12" cy="12" r="9" stroke-width="2" />
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9.172 16.5a4 4 0 015.656 0" />
                    <path stroke-linecap="round" stroke-width="2" d="M8.5 9l1.5 1.5m5-1.5l-1.5 1.5" />
                  </svg>
                </ng-container>
                <span>{{ m.label }}</span>
              </button>
            </div>
          </div>

          <!-- Add Entry: Category Picker & Form -->
          <div class="bg-surface-card rounded-card border border-border p-5 shadow-card">
            <div class="flex items-center justify-between mb-4">
              <h2 class="text-sm font-bold text-text-main flex items-center gap-2">
                <svg class="w-4 h-4 text-primary" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                </svg>
                <span>Add to Today's Log</span>
              </h2>
              <span class="text-xs text-primary font-medium">{{ activeCategory }} Entry</span>
            </div>

            <!-- Category Selector Buttons (touch targets >= 44px) -->
            <div class="grid grid-cols-3 sm:grid-cols-6 gap-2 mb-6">
              <button
                *ngFor="let cat of categories"
                type="button"
                (click)="activeCategory = cat.type"
                [class]="activeCategory === cat.type ? 'bg-primary text-white shadow-sm font-semibold' : 'bg-slate-50 text-text-main hover:bg-slate-100'"
                class="min-h-[44px] p-2 rounded-xl border border-border flex flex-col items-center justify-center text-xs transition-colors"
              >
                <ng-container [ngSwitch]="cat.type.toLowerCase()">
                  <!-- Meal -->
                  <svg *ngSwitchCase="'meal'" class="w-4 h-4 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z" />
                  </svg>
                  <!-- Nap -->
                  <svg *ngSwitchCase="'nap'" class="w-4 h-4 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" />
                  </svg>
                  <!-- Diaper -->
                  <svg *ngSwitchCase="'diaper'" class="w-4 h-4 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
                  </svg>
                  <!-- Activity -->
                  <svg *ngSwitchCase="'activity'" class="w-4 h-4 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.828 14.828a4 4 0 01-5.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  <!-- Medication -->
                  <svg *ngSwitchCase="'medication'" class="w-4 h-4 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v3m0 0v3m0-3h3m-3 0H9m12 0a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  <!-- Note -->
                  <svg *ngSwitchDefault class="w-4 h-4 mb-1" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                  </svg>
                </ng-container>
                <span>{{ cat.label }}</span>
              </button>
            </div>

            <!-- Dynamic Form Depending on activeCategory -->
            <form (ngSubmit)="submitEntry()" class="space-y-4 pt-2 border-t border-border">
              <!-- MEAL FORM -->
              <div *ngIf="activeCategory === 'Meal'" class="space-y-3">
                <div class="grid grid-cols-2 gap-3">
                  <div>
                    <label class="block text-xs font-semibold text-text-main mb-1">Meal Type</label>
                    <select
                      [(ngModel)]="mealType"
                      name="mealType"
                      class="w-full px-3 py-2 rounded-input border border-border bg-white text-sm focus:ring-2 focus:ring-primary focus:outline-none"
                    >
                      <option value="Breakfast">Breakfast</option>
                      <option value="Lunch">Lunch</option>
                      <option value="Snack">Snack</option>
                      <option value="Bottle">Bottle</option>
                    </select>
                  </div>
                  <div>
                    <label class="block text-xs font-semibold text-text-main mb-1">Amount Eaten</label>
                    <select
                      [(ngModel)]="amountEaten"
                      name="amountEaten"
                      class="w-full px-3 py-2 rounded-input border border-border bg-white text-sm focus:ring-2 focus:ring-primary focus:outline-none"
                    >
                      <option value="All">All</option>
                      <option value="Most">Most</option>
                      <option value="Some">Some</option>
                      <option value="None">None</option>
                    </select>
                  </div>
                </div>
                <div>
                  <label class="block text-xs font-semibold text-text-main mb-1">Food / Description</label>
                  <input
                    type="text"
                    [(ngModel)]="entryDescription"
                    name="entryDescription"
                    placeholder="e.g. Scrambled eggs, fruit, milk"
                    class="w-full px-3 py-2 rounded-input border border-border bg-white text-sm focus:ring-2 focus:ring-primary focus:outline-none"
                  />
                </div>
              </div>

              <!-- NAP FORM -->
              <div *ngIf="activeCategory === 'Nap'" class="space-y-3">
                <div class="grid grid-cols-2 gap-3">
                  <div>
                    <label class="block text-xs font-semibold text-text-main mb-1">Minutes Slept</label>
                    <input
                      type="number"
                      [(ngModel)]="napMinutes"
                      name="napMinutes"
                      min="10"
                      max="300"
                      class="w-full px-3 py-2 rounded-input border border-border bg-white text-sm focus:ring-2 focus:ring-primary focus:outline-none"
                    />
                  </div>
                  <div>
                    <label class="block text-xs font-semibold text-text-main mb-1">Nap Quality</label>
                    <input
                      type="text"
                      [(ngModel)]="entryDescription"
                      name="entryDescription"
                      placeholder="e.g. Slept peacefully"
                      class="w-full px-3 py-2 rounded-input border border-border bg-white text-sm focus:ring-2 focus:ring-primary focus:outline-none"
                    />
                  </div>
                </div>
              </div>

              <!-- DIAPER FORM -->
              <div *ngIf="activeCategory === 'Diaper'" class="space-y-3">
                <div>
                  <label class="block text-xs font-semibold text-text-main mb-1">Diaper Status</label>
                  <div class="grid grid-cols-4 gap-2">
                    <button
                      *ngFor="let dt of diaperTypes"
                      type="button"
                      (click)="diaperType = dt"
                      [class]="diaperType === dt ? 'bg-primary text-white font-bold' : 'bg-slate-50 text-text-main hover:bg-slate-100'"
                      class="min-h-[44px] py-2 px-3 rounded-input border border-border text-xs transition-colors"
                    >
                      {{ dt }}
                    </button>
                  </div>
                </div>
                <div>
                  <label class="block text-xs font-semibold text-text-main mb-1">Notes (Optional)</label>
                  <input
                    type="text"
                    [(ngModel)]="entryDescription"
                    name="entryDescription"
                    placeholder="e.g. Diaper cream applied"
                    class="w-full px-3 py-2 rounded-input border border-border bg-white text-sm focus:ring-2 focus:ring-primary focus:outline-none"
                  />
                </div>
              </div>

              <!-- ACTIVITY / MEDICATION / NOTE FORM -->
              <div *ngIf="activeCategory === 'Activity' || activeCategory === 'Medication' || activeCategory === 'Note'" class="space-y-3">
                <div>
                  <label class="block text-xs font-semibold text-text-main mb-1">
                    {{ activeCategory }} Description <span class="text-danger">*</span>
                  </label>
                  <textarea
                    [(ngModel)]="entryDescription"
                    name="entryDescription"
                    rows="3"
                    required
                    placeholder="Enter details..."
                    class="w-full px-3 py-2 rounded-input border border-border bg-white text-sm focus:ring-2 focus:ring-primary focus:outline-none"
                  ></textarea>
                </div>
              </div>

              <app-button
                type="submit"
                variant="primary"
                size="md"
                [loading]="isSubmittingEntry"
                customClass="w-full"
              >
                Record {{ activeCategory }} Entry
              </app-button>
            </form>
          </div>

          <!-- General Notes Section -->
          <div class="bg-surface-card rounded-card border border-border p-5 shadow-card space-y-3">
            <h2 class="text-sm font-bold text-text-main">Overall Daily Summary Notes</h2>
            <textarea
              [(ngModel)]="dailyNotes"
              rows="3"
              placeholder="Summary notes for parents about today's behavior, achievements, or reminders..."
              class="w-full px-3 py-2 rounded-input border border-border bg-white text-sm focus:ring-2 focus:ring-primary focus:outline-none"
            ></textarea>
            <div class="flex justify-end">
              <app-button
                type="button"
                variant="outline"
                size="sm"
                [loading]="isSavingNotes"
                (click)="saveDailyNotes()"
              >
                Save Notes
              </app-button>
            </div>
          </div>
        </div>

        <!-- Right: Today's Timeline (5 cols) -->
        <div class="lg:col-span-5 bg-surface-card rounded-card border border-border p-5 shadow-card">
          <div class="flex items-center justify-between pb-3 border-b border-border">
            <h2 class="text-sm font-bold text-text-main flex items-center gap-2">
              <svg class="w-4 h-4 text-primary" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 8v4l3 3m6-3a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <span>Today's Activity Timeline</span>
            </h2>
            <span class="text-xs text-text-muted">
              {{ dailyReport?.entries?.length || 0 }} logged
            </span>
          </div>

          <div class="mt-4">
            <app-timeline [entries]="dailyReport?.entries || []" emptyMessage="No entries logged yet for today. Use the form to record activities."></app-timeline>
          </div>
        </div>
      </div>
    </div>
  `
})
export class ChildDailyLogComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private childrenService = inject(ChildrenService);
  private dailyReportsService = inject(DailyReportsService);
  private toastService = inject(ToastService);

  childId = '';
  child: ChildDetailDto | null = null;
  dailyReport: DailyReportDetailDto | null = null;
  todayDate = new Date();

  moods: { type: MoodType; label: string }[] = [
    { type: 'Happy', label: 'Happy' },
    { type: 'Calm', label: 'Calm' },
    { type: 'Tired', label: 'Tired' },
    { type: 'Fussy', label: 'Fussy' },
    { type: 'Upset', label: 'Upset' },
  ];

  categories: { type: EntryType; label: string }[] = [
    { type: 'Meal', label: 'Meal' },
    { type: 'Nap', label: 'Nap' },
    { type: 'Diaper', label: 'Diaper' },
    { type: 'Activity', label: 'Activity' },
    { type: 'Medication', label: 'Meds' },
    { type: 'Note', label: 'Note' },
  ];

  diaperTypes: DiaperType[] = ['Dry', 'Wet', 'Soiled', 'Both'];

  activeCategory: EntryType = 'Meal';
  mealType: MealType = 'Lunch';
  amountEaten: AmountEaten = 'All';
  napMinutes = 45;
  diaperType: DiaperType = 'Wet';
  entryDescription = '';

  dailyNotes = '';
  isSubmittingEntry = false;
  isSavingNotes = false;

  ngOnInit(): void {
    this.childId = this.route.snapshot.paramMap.get('id') || '';
    if (this.childId) {
      this.loadChild();
      this.loadDailyReport();
    }
  }

  loadChild(): void {
    this.childrenService.getChild(this.childId).subscribe({
      next: (data) => this.child = data,
      error: () => this.toastService.error('Failed to load child details')
    });
  }

  loadDailyReport(): void {
    const todayStr = new Date().toISOString().split('T')[0];
    this.dailyReportsService.getDailyReports(this.childId, todayStr).subscribe({
      next: (reports) => {
        if (reports.length > 0) {
          this.dailyReport = reports[0];
          this.dailyNotes = this.dailyReport.notes || '';
        } else {
          this.dailyReport = null;
          this.dailyNotes = '';
        }
      },
      error: () => this.toastService.error('Failed to load daily report')
    });
  }

  ensureDailyReport(): Promise<DailyReportDetailDto> {
    return new Promise((resolve) => {
      if (this.dailyReport) {
        resolve(this.dailyReport);
        return;
      }
      const todayStr = new Date().toISOString().split('T')[0];
      this.dailyReportsService.createDailyReport({
        childId: this.childId,
        classroomId: this.child?.classroomId || '',
        date: todayStr,
        mood: 'Happy'
      }).subscribe({
        next: (created) => {
          this.dailyReport = created;
          resolve(created);
        },
        error: () => {
          resolve({
            childId: this.childId,
            date: todayStr,
            entries: []
          });
        }
      });
    });
  }

  selectMood(m: MoodType): void {
    this.ensureDailyReport().then((rep) => {
      this.dailyReportsService.updateMoodAndNotes(this.childId, m, this.dailyNotes).subscribe({
        next: (updated) => {
          this.dailyReport = updated;
          this.toastService.success(`Mood set to ${m}`);
        },
        error: () => this.toastService.error('Failed to update mood')
      });
    }).catch(err => this.toastService.error(err));
  }

  saveDailyNotes(): void {
    this.ensureDailyReport().then((rep) => {
      this.isSavingNotes = true;
      this.dailyReportsService.updateMoodAndNotes(this.childId, rep.mood, this.dailyNotes).subscribe({
        next: (updated) => {
          this.isSavingNotes = false;
          this.dailyReport = updated;
          this.toastService.success('Summary notes saved');
        },
        error: () => {
          this.isSavingNotes = false;
          this.toastService.error('Failed to save notes');
        }
      });
    }).catch(err => this.toastService.error(err));
  }

  submitEntry(): void {
    this.ensureDailyReport().then((rep) => {
      const now = new Date();
      const occurredAtStr = now.toISOString();

      let endedAtStr: string | null = null;
      if (this.activeCategory === 'Nap') {
        const ended = new Date(now.getTime() + this.napMinutes * 60000);
        endedAtStr = ended.toISOString();
      }

      const payload = {
        entryType: this.activeCategory,
        occurredAt: occurredAtStr,
        endedAt: endedAtStr,
        mealType: this.activeCategory === 'Meal' ? this.mealType : null,
        amountEaten: this.activeCategory === 'Meal' ? this.amountEaten : null,
        diaperType: this.activeCategory === 'Diaper' ? this.diaperType : null,
        description: this.entryDescription || (this.activeCategory === 'Nap' ? `Slept ${this.napMinutes} mins` : null)
      };

      this.isSubmittingEntry = true;
      this.dailyReportsService.addEntry(this.childId, payload).subscribe({
        next: () => {
          this.isSubmittingEntry = false;
          this.toastService.success(`${this.activeCategory} entry recorded`);
          this.entryDescription = '';
          this.loadDailyReport();
        },
        error: (err) => {
          this.isSubmittingEntry = false;
          this.toastService.error(err.error?.detail || 'Failed to record entry');
        }
      });
    }).catch(err => this.toastService.error(err));
  }
}
