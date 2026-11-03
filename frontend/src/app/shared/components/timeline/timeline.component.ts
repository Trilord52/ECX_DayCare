import { Component, Input } from '@angular/core';
import { CommonModule } from '@angular/common';

export interface TimelineEntryItem {
  id?: string;
  entryType: 'Meal' | 'Nap' | 'Diaper' | 'Activity' | 'Medication' | 'Note' | string;
  occurredAt: string;
  endedAt?: string | null;
  mealType?: string | null;
  amountEaten?: string | null;
  diaperType?: string | null;
  description?: string | null;
  createdByName?: string | null;
}

@Component({
  selector: 'app-timeline',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="space-y-4">
      <div *ngIf="!entries || entries.length === 0" class="py-8 text-center text-text-muted text-sm">
        {{ emptyMessage }}
      </div>

      <div *ngIf="entries && entries.length > 0" class="relative pl-6 space-y-6 before:absolute before:left-2.5 before:top-2 before:bottom-2 before:w-0.5 before:bg-slate-200">
        <div
          *ngFor="let item of entries"
          class="relative flex items-start gap-3 group"
        >
          <!-- Icon node -->
          <div
            [class]="getIconBg(item.entryType)"
            class="absolute -left-6 top-0.5 w-6 h-6 rounded-full flex items-center justify-center text-xs shadow-sm ring-4 ring-white"
            aria-hidden="true"
          >
            <ng-container [ngSwitch]="item.entryType.toLowerCase()">
              <!-- Meal -->
              <svg *ngSwitchCase="'meal'" class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z" />
              </svg>
              <!-- Nap -->
              <svg *ngSwitchCase="'nap'" class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" />
              </svg>
              <!-- Diaper -->
              <svg *ngSwitchCase="'diaper'" class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
              </svg>
              <!-- Activity -->
              <svg *ngSwitchCase="'activity'" class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M14.828 14.828a4 4 0 01-5.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <!-- Medication -->
              <svg *ngSwitchCase="'medication'" class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v3m0 0v3m0-3h3m-3 0H9m12 0a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <!-- Note / default -->
              <svg *ngSwitchDefault class="w-3.5 h-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
            </ng-container>
          </div>

          <!-- Content block -->
          <div class="flex-1 bg-slate-50/70 hover:bg-slate-50 p-3 rounded-lg border border-border transition-colors">
            <div class="flex items-center justify-between gap-2 flex-wrap">
              <div class="flex items-center gap-2">
                <span class="font-bold text-xs text-text-main">{{ item.entryType }}</span>
                <span *ngIf="item.mealType" class="text-xs text-primary font-medium">({{ item.mealType }})</span>
                <span *ngIf="item.amountEaten" class="text-xs text-text-muted font-normal">• {{ item.amountEaten }}</span>
                <span *ngIf="item.diaperType" class="text-xs text-text-muted font-normal">• {{ item.diaperType }}</span>
              </div>
              <time class="text-xs font-medium text-text-subtle">
                {{ item.occurredAt | date:'shortTime':'Africa/Addis_Ababa' }}
                <span *ngIf="item.endedAt"> - {{ item.endedAt | date:'shortTime':'Africa/Addis_Ababa' }}</span>
              </time>
            </div>

            <p *ngIf="item.description" class="text-xs text-text-main mt-1 leading-relaxed">
              {{ item.description }}
            </p>

            <div *ngIf="item.createdByName" class="text-xs text-text-subtle mt-1.5 flex items-center gap-1">
              <span>Logged by {{ item.createdByName }}</span>
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class TimelineComponent {
  @Input() entries: TimelineEntryItem[] = [];
  @Input() emptyMessage = 'No timeline entries recorded yet today.';

  getIconBg(type: string): string {
    switch (type.toLowerCase()) {
      case 'meal': return 'bg-amber-100 text-amber-700';
      case 'nap': return 'bg-indigo-100 text-indigo-700';
      case 'diaper': return 'bg-teal-100 text-teal-700';
      case 'activity': return 'bg-pink-100 text-pink-700';
      case 'medication': return 'bg-red-100 text-red-700';
      default: return 'bg-slate-100 text-slate-700';
    }
  }
}

