import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IncidentsService, IncidentListItemDto, IncidentDetailDto, CreateIncidentRequest, IncidentSeverity, IncidentType } from '../../../core/services/incidents.service';
import { ChildrenService, ChildDto } from '../../../core/services/children.service';
import { ToastService } from '../../../core/services/toast.service';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { DialogComponent } from '../../../shared/components/dialog/dialog.component';

@Component({
  selector: 'app-admin-incidents',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent, DialogComponent],
  template: `
    <div class="space-y-6">
      <!-- Header -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold tracking-tight text-text-main">Incident Reports</h1>
          <p class="text-sm text-text-muted mt-0.5">Track injuries, health episodes, parent notifications, and digital sign-off acknowledgements</p>
        </div>
        <div class="flex items-center gap-3">
          <app-button variant="primary" (click)="openReportModal()">
            + Report Incident
          </app-button>
        </div>
      </div>

      <!-- Quick Metrics -->
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
          <span class="text-xs font-semibold text-text-muted">Total Recorded</span>
          <p class="text-2xl font-bold text-text-main mt-1">{{ incidents.length }}</p>
        </div>
        <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
          <span class="text-xs font-semibold text-text-muted">Serious Severity</span>
          <p class="text-2xl font-bold text-danger mt-1">{{ seriousCount }}</p>
        </div>
        <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
          <span class="text-xs font-semibold text-text-muted">Unacknowledged</span>
          <p class="text-2xl font-bold text-warning mt-1">{{ unacknowledgedCount }}</p>
        </div>
        <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
          <span class="text-xs font-semibold text-text-muted">Parent Acknowledged</span>
          <p class="text-2xl font-bold text-success mt-1">{{ acknowledgedCount }}</p>
        </div>
      </div>

      <!-- Filters -->
      <div class="flex flex-wrap gap-3 bg-surface-card p-4 rounded-card border border-border">
        <select
          [(ngModel)]="severityFilter"
          (ngModelChange)="loadIncidents()"
          class="h-9 px-3 rounded-input border border-border bg-white text-xs text-text-main focus:outline-none focus:border-primary"
        >
          <option value="all">All Severities</option>
          <option value="Minor">Minor</option>
          <option value="Moderate">Moderate</option>
          <option value="Serious">Serious</option>
        </select>

        <select
          [(ngModel)]="ackFilter"
          (ngModelChange)="loadIncidents()"
          class="h-9 px-3 rounded-input border border-border bg-white text-xs text-text-main focus:outline-none focus:border-primary"
        >
          <option value="all">All Acknowledgement States</option>
          <option value="pending">Pending Parent Acknowledgement</option>
          <option value="acknowledged">Acknowledged by Parent</option>
        </select>
      </div>

      <!-- Loading / Empty -->
      <div *ngIf="loading" class="p-12 text-center text-text-muted text-sm">
        Loading incidents...
      </div>

      <div *ngIf="!loading && incidents.length === 0" class="p-12 text-center bg-surface-card rounded-card border border-border">
        <p class="text-sm font-semibold text-text-main">No incidents found</p>
        <p class="text-xs text-text-muted mt-1">There are no recorded incident reports matching the selected criteria.</p>
      </div>

      <!-- Incidents Table -->
      <div *ngIf="!loading && incidents.length > 0" class="bg-surface-card rounded-card border border-border shadow-card overflow-hidden">
        <div class="overflow-x-auto">
          <table class="w-full text-left border-collapse text-xs">
            <thead>
              <tr class="bg-slate-50 border-b border-border text-text-muted uppercase tracking-wider">
                <th class="p-3.5 font-semibold">Date & Time</th>
                <th class="p-3.5 font-semibold">Child</th>
                <th class="p-3.5 font-semibold">Type</th>
                <th class="p-3.5 font-semibold">Severity</th>
                <th class="p-3.5 font-semibold">Description</th>
                <th class="p-3.5 font-semibold text-center">Parent Sign-off</th>
                <th class="p-3.5 font-semibold text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-border">
              <tr *ngFor="let inc of incidents" class="hover:bg-slate-50/50 transition">
                <td class="p-3.5 text-text-main font-mono">
                  {{ inc.occurredAt | slice:0:10 }} {{ inc.occurredAt | slice:11:16 }}
                </td>
                <td class="p-3.5 font-bold text-text-main">{{ inc.childName }}</td>
                <td class="p-3.5 text-text-muted font-medium">{{ inc.type }}</td>
                <td class="p-3.5">
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
                </td>
                <td class="p-3.5 text-text-main max-w-xs truncate" [title]="inc.description">
                  {{ inc.description }}
                </td>
                <td class="p-3.5 text-center">
                  <span
                    *ngIf="inc.isAcknowledged"
                    class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-success-soft text-success text-xs font-bold"
                  >
                    <svg class="w-3 h-3 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                    </svg>
                    Acknowledged
                  </span>
                  <span
                    *ngIf="!inc.isAcknowledged"
                    class="px-2 py-0.5 rounded-full bg-amber-50 text-amber-700 border border-amber-200 text-xs font-semibold"
                  >
                    Pending Sign-off
                  </span>
                </td>
                <td class="p-3.5 text-right">
                  <div class="flex items-center justify-end gap-2">
                    <button
                      *ngIf="!inc.isParentNotified"
                      (click)="notifyParent(inc)"
                      class="px-2 py-1 rounded border border-primary/30 text-primary hover:bg-primary-soft text-xs font-medium transition"
                    >
                      Notify Parent
                    </button>
                    <button
                      (click)="viewDetails(inc)"
                      class="px-2.5 py-1 rounded border border-border text-text-main hover:bg-slate-100 font-semibold transition"
                    >
                      View
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- REPORT INCIDENT MODAL -->
      <app-dialog
        [(isOpen)]="reportModalOpen"
        title="Report New Incident"
      >
        <div class="space-y-4 text-xs">
          <div>
            <label class="block font-semibold text-text-main mb-1">Involved Child <span class="text-danger">*</span></label>
            <select
              [(ngModel)]="reportForm.childId"
              class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
            >
              <option value="" disabled>Select a child...</option>
              <option *ngFor="let c of childrenList" [value]="c.id">
                {{ c.firstName }} {{ c.lastName }} ({{ c.classroomName }})
              </option>
            </select>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block font-semibold text-text-main mb-1">Date & Time <span class="text-danger">*</span></label>
              <input
                type="datetime-local"
                [(ngModel)]="reportForm.occurredAt"
                class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
              />
            </div>
            <div>
              <label class="block font-semibold text-text-main mb-1">Type <span class="text-danger">*</span></label>
              <select
                [(ngModel)]="reportForm.type"
                class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
              >
                <option value="Injury">Injury</option>
                <option value="Illness">Illness</option>
                <option value="Behavioral">Behavioral</option>
                <option value="Other">Other</option>
              </select>
            </div>
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">Severity <span class="text-danger">*</span></label>
            <div class="grid grid-cols-3 gap-2">
              <label
                [class.ring-2]="reportForm.severity === 'Minor'"
                class="p-2 border border-border rounded-lg text-center cursor-pointer hover:bg-slate-50 transition"
              >
                <input type="radio" [(ngModel)]="reportForm.severity" value="Minor" class="sr-only" />
                <span class="block font-bold text-text-main">Minor</span>
                <span class="text-xs text-text-muted">Small scratch/bump</span>
              </label>
              <label
                [class.ring-2]="reportForm.severity === 'Moderate'"
                class="p-2 border border-border rounded-lg text-center cursor-pointer hover:bg-slate-50 transition"
              >
                <input type="radio" [(ngModel)]="reportForm.severity" value="Moderate" class="sr-only" />
                <span class="block font-bold text-amber-700">Moderate</span>
                <span class="text-xs text-text-muted">Ice pack / resting</span>
              </label>
              <label
                [class.ring-2]="reportForm.severity === 'Serious'"
                class="p-2 border border-danger/40 rounded-lg text-center cursor-pointer hover:bg-danger-soft transition"
              >
                <input type="radio" [(ngModel)]="reportForm.severity" value="Serious" class="sr-only" />
                <span class="block font-bold text-danger">Serious</span>
                <span class="text-xs text-danger">Requires medical call</span>
              </label>
            </div>
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">Description of Incident <span class="text-danger">*</span></label>
            <textarea
              [(ngModel)]="reportForm.description"
              rows="3"
              placeholder="Describe exactly what happened, where, and what activities were occurring..."
              class="w-full p-2.5 rounded-input border border-border bg-white text-xs"
            ></textarea>
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">First Aid / Action Taken</label>
            <textarea
              [(ngModel)]="reportForm.actionTaken"
              rows="2"
              placeholder="e.g. Applied cold compress, washed with antiseptic soap, comforted child..."
              class="w-full p-2.5 rounded-input border border-border bg-white text-xs"
            ></textarea>
          </div>
        </div>

        <div dialog-actions class="flex justify-end gap-2">
          <app-button variant="secondary" (click)="reportModalOpen = false">Cancel</app-button>
          <app-button variant="primary" [loading]="saving" (click)="onSubmitReport()">Submit Incident Report</app-button>
        </div>
      </app-dialog>

      <!-- DETAIL MODAL -->
      <app-dialog
        [(isOpen)]="detailModalOpen"
        title="Incident Report Details"
      >
        <div *ngIf="selectedDetail" class="space-y-4 text-xs">
          <div class="p-3 bg-slate-50 border border-border rounded-lg space-y-1">
            <div class="flex justify-between items-center">
              <span class="text-text-muted">Child:</span>
              <strong class="text-sm font-bold text-text-main">{{ selectedDetail.childName }}</strong>
            </div>
            <div class="flex justify-between items-center">
              <span class="text-text-muted">Occurred:</span>
              <span class="font-mono text-text-main">{{ selectedDetail.occurredAt }}</span>
            </div>
            <div class="flex justify-between items-center">
              <span class="text-text-muted">Reported By:</span>
              <span class="text-text-main font-medium">{{ selectedDetail.reportedByName }}</span>
            </div>
          </div>

          <div>
            <span class="font-bold text-text-main block mb-1">Incident Summary</span>
            <p class="p-3 bg-white border border-border rounded-lg text-text-main">
              {{ selectedDetail.description }}
            </p>
          </div>

          <div *ngIf="selectedDetail.actionTaken">
            <span class="font-bold text-text-main block mb-1">Action Taken</span>
            <p class="p-3 bg-white border border-border rounded-lg text-text-main">
              {{ selectedDetail.actionTaken }}
            </p>
          </div>

          <div class="p-3 rounded-lg border border-border bg-slate-50 space-y-2">
            <div class="flex justify-between">
              <span class="text-text-muted">Parent Notification:</span>
              <span *ngIf="selectedDetail.parentNotifiedAt" class="text-success font-semibold">
                Notified on {{ selectedDetail.parentNotifiedAt | slice:0:10 }} by {{ selectedDetail.parentNotifiedByName }}
              </span>
              <span *ngIf="!selectedDetail.parentNotifiedAt" class="text-text-muted italic">Not yet notified</span>
            </div>

            <div class="flex justify-between">
              <span class="text-text-muted">Parent Acknowledgement:</span>
              <span *ngIf="selectedDetail.acknowledgedAt" class="text-success font-bold inline-flex items-center gap-1">
                <svg class="w-3.5 h-3.5 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7" />
                </svg>
                Signed on {{ selectedDetail.acknowledgedAt | slice:0:10 }} by {{ selectedDetail.acknowledgedByName }}
              </span>
              <span *ngIf="!selectedDetail.acknowledgedAt" class="text-amber-700 font-medium">
                Pending parent acknowledgment
              </span>
            </div>
          </div>
        </div>

        <div dialog-actions class="flex justify-end">
          <app-button variant="primary" (click)="detailModalOpen = false">Close</app-button>
        </div>
      </app-dialog>
    </div>
  `
})
export class AdminIncidentsComponent implements OnInit {
  private incidentsService = inject(IncidentsService);
  private childrenService = inject(ChildrenService);
  private toast = inject(ToastService);

  incidents: IncidentListItemDto[] = [];
  childrenList: ChildDto[] = [];
  loading = true;

  severityFilter = 'all';
  ackFilter = 'all';

  reportModalOpen = false;
  detailModalOpen = false;
  selectedDetail: IncidentDetailDto | null = null;
  saving = false;

  reportForm: {
    childId: string;
    occurredAt: string;
    type: IncidentType;
    severity: IncidentSeverity;
    description: string;
    actionTaken: string;
  } = {
    childId: '',
    occurredAt: new Date().toISOString().slice(0, 16),
    type: 'Injury',
    severity: 'Minor',
    description: '',
    actionTaken: ''
  };

  get seriousCount(): number {
    return this.incidents.filter(i => i.severity === 'Serious').length;
  }

  get unacknowledgedCount(): number {
    return this.incidents.filter(i => !i.isAcknowledged).length;
  }

  get acknowledgedCount(): number {
    return this.incidents.filter(i => i.isAcknowledged).length;
  }

  ngOnInit(): void {
    this.loadIncidents();
    this.childrenService.getChildren().subscribe({
      next: (res) => (this.childrenList = res.items),
      error: () => {}
    });
  }

  loadIncidents(): void {
    this.loading = true;
    const filters: any = {};
    if (this.severityFilter !== 'all') {
      filters.severity = this.severityFilter;
    }
    if (this.ackFilter === 'pending') {
      filters.acknowledged = false;
    } else if (this.ackFilter === 'acknowledged') {
      filters.acknowledged = true;
    }

    this.incidentsService.getIncidents(filters).subscribe({
      next: (res) => {
        this.incidents = res.items;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toast.error('Failed to load incident reports');
      }
    });
  }

  openReportModal(): void {
    this.reportForm = {
      childId: this.childrenList.length > 0 ? this.childrenList[0].id : '',
      occurredAt: new Date().toISOString().slice(0, 16),
      type: 'Injury',
      severity: 'Minor',
      description: '',
      actionTaken: ''
    };
    this.reportModalOpen = true;
  }

  onSubmitReport(): void {
    if (!this.reportForm.childId || !this.reportForm.description.trim()) {
      this.toast.warning('Please select a child and describe the incident');
      return;
    }

    this.saving = true;
    const req: CreateIncidentRequest = {
      childId: this.reportForm.childId,
      occurredAt: new Date(this.reportForm.occurredAt).toISOString(),
      type: this.reportForm.type,
      severity: this.reportForm.severity,
      description: this.reportForm.description.trim(),
      actionTaken: this.reportForm.actionTaken.trim() || undefined
    };

    this.incidentsService.createIncident(req).subscribe({
      next: () => {
        this.saving = false;
        this.reportModalOpen = false;
        this.toast.success('Incident reported successfully');
        this.loadIncidents();
      },
      error: (err) => {
        this.saving = false;
        this.toast.error(err.error?.detail || 'Failed to report incident');
      }
    });
  }

  viewDetails(inc: IncidentListItemDto): void {
    this.incidentsService.getIncident(inc.id).subscribe({
      next: (res) => {
        this.selectedDetail = res;
        this.detailModalOpen = true;
      },
      error: () => this.toast.error('Failed to load incident details')
    });
  }

  notifyParent(inc: IncidentListItemDto): void {
    this.incidentsService.notifyParent(inc.id).subscribe({
      next: () => {
        this.toast.success('Parent notification recorded');
        this.loadIncidents();
      },
      error: (err) => {
        this.toast.error(err.error?.detail || 'Failed to notify parent');
      }
    });
  }
}
