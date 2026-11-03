import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { IncidentsService, IncidentListItemDto, IncidentDetailDto } from '../../../core/services/incidents.service';
import { ToastService } from '../../../core/services/toast.service';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { DialogComponent } from '../../../shared/components/dialog/dialog.component';

@Component({
  selector: 'app-parent-incidents',
  standalone: true,
  imports: [CommonModule, ButtonComponent, DialogComponent],
  template: `
    <div class="space-y-6 max-w-2xl mx-auto pb-12">
      <!-- Header -->
      <div>
        <h1 class="text-xl font-bold tracking-tight text-text-main">Incident & Health Reports</h1>
        <p class="text-xs text-text-muted mt-0.5">Review safety incidents and provide required digital parent sign-offs</p>
      </div>

      <!-- Loading / Empty -->
      <div *ngIf="loading" class="p-12 text-center text-text-muted text-xs">
        Loading incident records...
      </div>

      <div *ngIf="!loading && incidents.length === 0" class="p-12 text-center bg-surface-card rounded-card border border-border">
        <p class="text-sm font-semibold text-text-main">No incident reports</p>
        <p class="text-xs text-text-muted mt-1">There are no reported accidents or health incidents on record for your child.</p>
      </div>

      <!-- Incidents Feed (Mobile-first Cards) -->
      <div *ngIf="!loading && incidents.length > 0" class="space-y-4">
        <article
          *ngFor="let inc of incidents"
          class="bg-surface-card rounded-card border border-border p-4 shadow-card space-y-3"
        >
          <div class="flex items-start justify-between gap-2">
            <div>
              <div class="flex items-center gap-2">
                <span class="font-bold text-sm text-text-main">{{ inc.childName }}</span>
                <span
                  [class.bg-danger-soft]="inc.severity === 'Serious'"
                  [class.text-danger]="inc.severity === 'Serious'"
                  [class.bg-amber-100]="inc.severity === 'Moderate'"
                  [class.text-amber-800]="inc.severity === 'Moderate'"
                  [class.bg-slate-100]="inc.severity === 'Minor'"
                  [class.text-text-muted]="inc.severity === 'Minor'"
                  class="px-2 py-0.5 rounded-full text-xs font-bold"
                >
                  {{ inc.severity }}
                </span>
              </div>
              <span class="text-xs text-text-muted">
                {{ inc.occurredAt | slice:0:10 }} at {{ inc.occurredAt | slice:11:16 }} • Reported by {{ inc.reportedByName }}
              </span>
            </div>

            <span
              *ngIf="inc.isAcknowledged"
              class="px-2 py-0.5 rounded-full bg-success-soft text-success text-xs font-bold shrink-0 flex items-center gap-1"
            >
              <svg class="w-3 h-3 text-success" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
              </svg>
              <span>Acknowledged</span>
            </span>
          </div>

          <p class="text-xs text-text-main bg-slate-50 p-3 rounded-lg border border-border/60">
            {{ inc.description }}
          </p>

          <div class="flex items-center justify-between pt-1">
            <span class="text-xs text-text-muted font-medium">Type: {{ inc.type }}</span>

            <div class="flex gap-2">
              <button
                (click)="openDetail(inc)"
                class="px-3 py-1.5 rounded-input border border-border text-xs font-semibold text-text-main hover:bg-slate-50 transition"
              >
                View Full Details
              </button>

              <button
                *ngIf="!inc.isAcknowledged"
                (click)="onAcknowledge(inc)"
                [disabled]="acknowledgingId === inc.id"
                class="px-3 py-1.5 rounded-input bg-primary text-white text-xs font-bold hover:bg-primary-hover shadow-sm transition"
              >
                {{ acknowledgingId === inc.id ? 'Acknowledging...' : 'Acknowledge Incident' }}
              </button>
            </div>
          </div>
        </article>
      </div>

      <!-- INCIDENT DETAIL MODAL -->
      <app-dialog
        [(isOpen)]="detailModalOpen"
        title="Incident Report Detail"
      >
        <div *ngIf="selectedDetail" class="space-y-4 text-xs">
          <div class="p-3 bg-slate-50 border border-border rounded-lg space-y-1">
            <div class="flex justify-between">
              <span class="text-text-muted">Child:</span>
              <strong class="font-bold text-text-main">{{ selectedDetail.childName }}</strong>
            </div>
            <div class="flex justify-between">
              <span class="text-text-muted">Occurred:</span>
              <span class="font-mono text-text-main">{{ selectedDetail.occurredAt }}</span>
            </div>
            <div class="flex justify-between">
              <span class="text-text-muted">Severity:</span>
              <span class="font-bold text-text-main">{{ selectedDetail.severity }}</span>
            </div>
          </div>

          <div>
            <span class="font-bold text-text-main block mb-1">Incident Description</span>
            <p class="p-3 bg-white border border-border rounded-lg text-text-main">
              {{ selectedDetail.description }}
            </p>
          </div>

          <div *ngIf="selectedDetail.actionTaken">
            <span class="font-bold text-text-main block mb-1">First Aid / Action Taken</span>
            <p class="p-3 bg-white border border-border rounded-lg text-text-main">
              {{ selectedDetail.actionTaken }}
            </p>
          </div>

          <div class="p-3 bg-emerald-50 border border-emerald-200 rounded-lg text-emerald-900 flex items-center gap-2" *ngIf="selectedDetail.acknowledgedAt">
            <svg class="w-4 h-4 text-emerald-600 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
            </svg>
            <span>Acknowledged by {{ selectedDetail.acknowledgedByName }} on {{ selectedDetail.acknowledgedAt | slice:0:10 }}.</span>
          </div>
        </div>

        <div dialog-actions class="flex justify-end gap-2">
          <app-button variant="secondary" (click)="detailModalOpen = false">Close</app-button>
          <app-button
            *ngIf="selectedDetail && !selectedDetail.acknowledgedAt"
            variant="primary"
            (click)="onAcknowledgeFromModal()"
          >
            Acknowledge Incident
          </app-button>
        </div>
      </app-dialog>
    </div>
  `
})
export class ParentIncidentsComponent implements OnInit {
  private incidentsService = inject(IncidentsService);
  private toast = inject(ToastService);

  incidents: IncidentListItemDto[] = [];
  loading = true;
  acknowledgingId: string | null = null;

  detailModalOpen = false;
  selectedDetail: IncidentDetailDto | null = null;

  ngOnInit(): void {
    this.loadIncidents();
  }

  loadIncidents(): void {
    this.loading = true;
    this.incidentsService.getIncidents().subscribe({
      next: (res) => {
        this.incidents = res.items;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toast.error('Failed to load incidents');
      }
    });
  }

  openDetail(inc: IncidentListItemDto): void {
    this.incidentsService.getIncident(inc.id).subscribe({
      next: (res) => {
        this.selectedDetail = res;
        this.detailModalOpen = true;
      },
      error: () => this.toast.error('Failed to load incident detail')
    });
  }

  onAcknowledge(inc: IncidentListItemDto): void {
    this.acknowledgingId = inc.id;
    this.incidentsService.acknowledge(inc.id).subscribe({
      next: () => {
        this.acknowledgingId = null;
        this.toast.success('Incident acknowledged and recorded');
        this.loadIncidents();
      },
      error: (err) => {
        this.acknowledgingId = null;
        this.toast.error(err.error?.detail || 'Failed to acknowledge incident');
      }
    });
  }

  onAcknowledgeFromModal(): void {
    if (!this.selectedDetail) return;
    this.incidentsService.acknowledge(this.selectedDetail.id).subscribe({
      next: () => {
        this.detailModalOpen = false;
        this.toast.success('Incident acknowledged and recorded');
        this.loadIncidents();
      },
      error: (err) => {
        this.toast.error(err.error?.detail || 'Failed to acknowledge incident');
      }
    });
  }
}
