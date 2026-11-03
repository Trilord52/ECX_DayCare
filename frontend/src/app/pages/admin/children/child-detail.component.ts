import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ChildrenService, ChildDetailDto, GuardianDto } from '../../../core/services/children.service';
import { DailyReportsService, DailyReportDetailDto } from '../../../core/services/daily-reports.service';
import { ToastService } from '../../../core/services/toast.service';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';
import { DialogComponent } from '../../../shared/components/dialog/dialog.component';
import { ChildAllergyFlagComponent } from '../../../shared/components/child-allergy-flag/child-allergy-flag.component';
import { TimelineComponent } from '../../../shared/components/timeline/timeline.component';
import { AvatarComponent } from '../../../shared/components/avatar/avatar.component';
import { PhotoUploaderComponent } from '../../../shared/components/photo-uploader/photo-uploader.component';

@Component({
  selector: 'app-child-detail',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, ButtonComponent, BadgeComponent, DialogComponent, ChildAllergyFlagComponent, TimelineComponent, AvatarComponent, PhotoUploaderComponent],
  template: `
    <div *ngIf="child" class="space-y-6 max-w-5xl mx-auto pb-12">
      <!-- Breadcrumb & Back -->
      <nav class="flex items-center gap-2 text-xs text-text-muted">
        <a routerLink="/admin/children" class="hover:text-primary transition-colors">Children</a>
        <span>&gt;</span>
        <span class="text-text-main font-semibold">{{ child.firstName }} {{ child.lastName }}</span>
      </nav>

      <!-- Child Profile Header Banner -->
      <div class="bg-surface-card rounded-card border border-border p-6 shadow-card space-y-4">
        <div class="flex flex-col md:flex-row md:items-center justify-between gap-6">
          <div class="flex items-center gap-4">
            <app-avatar
              [kind]="'children'"
              [id]="child.id"
              [name]="child.firstName + ' ' + child.lastName"
              [hasPhoto]="child.hasPhoto ?? false"
              [photoUpdatedAt]="child.photoUpdatedAt"
              [size]="80"
            ></app-avatar>
            <div>
              <div class="flex items-center gap-2 flex-wrap">
                <h1 class="text-2xl font-bold text-text-main">{{ child.firstName }} {{ child.lastName }}</h1>
                <app-badge [status]="child.status" [text]="child.status"></app-badge>
                <app-child-allergy-flag [allergies]="child.allergies" size="md"></app-child-allergy-flag>
              </div>
              <p class="text-xs text-text-muted mt-1">
                {{ child.classroomName }} • DOB: {{ child.dateOfBirth }} • Enrolled: {{ child.enrollmentDate }}
              </p>
            </div>
          </div>

          <div class="flex items-center gap-2 self-end md:self-auto shrink-0">
            <a
              [routerLink]="['/admin/children', child.id, 'edit']"
              class="px-4 py-2 rounded-input border border-border text-sm font-medium text-text-main hover:bg-slate-100 transition-colors"
            >
              Edit Profile
            </a>
            <a
              [routerLink]="['/admin/children', child.id, 'log']"
              class="px-4 py-2 rounded-input bg-primary text-white text-sm font-semibold hover:bg-primary-hover shadow-sm transition-colors flex items-center gap-1.5"
            >
              Daily Log
            </a>
            <button
              type="button"
              (click)="deleteChild()"
              class="px-4 py-2 rounded-input border border-danger/30 text-danger text-sm font-semibold hover:bg-danger-soft transition-colors"
            >
              Withdraw Child
            </button>
          </div>
        </div>

        <!-- Admin Photo Uploader Controls -->
        <div class="pt-3 border-t border-border">
          <app-photo-uploader
            [kind]="'children'"
            [id]="child.id"
            [name]="child.firstName + ' ' + child.lastName"
            [hasPhoto]="child.hasPhoto ?? false"
            [photoUpdatedAt]="child.photoUpdatedAt"
            [avatarSize]="48"
            (photoChanged)="onPhotoChanged($event)"
          ></app-photo-uploader>
        </div>
      </div>

      <!-- Navigation Tabs -->
      <div class="flex items-center gap-2 border-b border-border overflow-x-auto pb-1">
        <button
          *ngFor="let tab of tabs"
          type="button"
          (click)="activeTab = tab.id"
          [class]="activeTab === tab.id ? 'bg-primary-soft text-primary font-bold border-b-2 border-primary' : 'text-text-muted hover:text-text-main'"
          class="px-4 py-2 text-sm rounded-t-lg transition-colors whitespace-nowrap"
        >
          {{ tab.label }}
        </button>
      </div>

      <!-- TAB 1: OVERVIEW -->
      <div *ngIf="activeTab === 'overview'" class="grid grid-cols-1 md:grid-cols-2 gap-6">
        <div class="bg-surface-card rounded-card border border-border p-5 shadow-card space-y-4">
          <h2 class="text-sm font-bold text-text-main border-b border-border pb-2">Medical & Dietary Profile</h2>
          <div class="space-y-3 text-xs">
            <div>
              <span class="font-semibold text-text-muted uppercase">Allergies:</span>
              <p class="text-text-main font-medium mt-0.5">{{ child.allergies || 'No allergies recorded' }}</p>
            </div>
            <div>
              <span class="font-semibold text-text-muted uppercase">Dietary Notes:</span>
              <p class="text-text-main font-medium mt-0.5">{{ child.dietaryNotes || 'Standard daycare meal plan' }}</p>
            </div>
            <div>
              <span class="font-semibold text-text-muted uppercase">Medical Notes:</span>
              <p class="text-text-main font-medium mt-0.5">{{ child.medicalNotes || 'No special medical conditions' }}</p>
            </div>
          </div>
        </div>

        <div class="bg-surface-card rounded-card border border-border p-5 shadow-card space-y-4">
          <h2 class="text-sm font-bold text-text-main border-b border-border pb-2">Enrollment & Billing Plan</h2>
          <div class="space-y-3 text-xs">
            <div>
              <span class="font-semibold text-text-muted uppercase">Classroom:</span>
              <p class="text-text-main font-medium mt-0.5">{{ child.classroomName }}</p>
            </div>
            <div>
              <span class="font-semibold text-text-muted uppercase">Fee Plan:</span>
              <p class="text-text-main font-medium mt-0.5">{{ child.feePlanName || 'No fee plan assigned' }}</p>
            </div>
            <div>
              <span class="font-semibold text-text-muted uppercase">Gender:</span>
              <p class="text-text-main font-medium mt-0.5">{{ child.gender }}</p>
            </div>
          </div>
        </div>
      </div>

      <!-- TAB 2: GUARDIANS & PICKUP -->
      <div *ngIf="activeTab === 'guardians'" class="bg-surface-card rounded-card border border-border shadow-card overflow-hidden">
        <div class="px-6 py-4 border-b border-border flex items-center justify-between">
          <div>
            <h2 class="text-base font-bold text-text-main">Guardians & Authorized Pickup List</h2>
            <p class="text-xs text-text-muted mt-0.5">Authorised adults for drop-off, pick-up, emergency contacts, and billing</p>
          </div>
          <button
            type="button"
            (click)="openLinkGuardianModal()"
            class="px-3.5 py-1.5 rounded-input bg-primary text-white text-xs font-semibold hover:bg-primary-hover transition-colors"
          >
            + Link Guardian
          </button>
        </div>

        <div *ngIf="child.guardians.length === 0" class="p-8 text-center text-text-muted text-sm">
          No guardians linked yet. Click "+ Link Guardian" to attach a parent or caregiver.
        </div>

        <div *ngIf="child.guardians.length > 0" class="overflow-x-auto">
          <table class="w-full text-left text-sm">
            <thead class="bg-slate-50 border-b border-border text-xs font-semibold text-text-muted uppercase">
              <tr>
                <th class="px-6 py-3">Guardian Name</th>
                <th class="px-6 py-3">Relationship</th>
                <th class="px-6 py-3">Phone</th>
                <th class="px-6 py-3">Authorized Permissions</th>
                <th class="px-6 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-border">
              <tr *ngFor="let g of child.guardians" class="hover:bg-slate-50/50 transition-colors">
                <td class="px-6 py-4">
                  <div class="font-bold text-text-main">{{ g.fullName || (g.firstName + ' ' + g.lastName) }}</div>
                  <div class="text-xs text-text-muted">{{ g.email || 'No email' }}</div>
                </td>
                <td class="px-6 py-4 text-xs font-medium text-text-main">
                  {{ g.relationship }}
                </td>
                <td class="px-6 py-4 text-xs text-text-muted">
                  {{ g.phone }}
                </td>
                <td class="px-6 py-4 text-xs space-x-1">
                  <span *ngIf="g.isPrimaryContact" class="px-2 py-0.5 rounded bg-primary-soft text-primary font-semibold">Primary</span>
                  <span *ngIf="g.canPickUp" class="px-2 py-0.5 rounded bg-success-soft text-success font-semibold">Can Pick Up</span>
                  <span *ngIf="!g.canPickUp" class="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-danger-soft text-danger font-semibold">
                    <svg class="w-3 h-3 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                    </svg>
                    No Pickup
                  </span>
                  <span *ngIf="g.isEmergencyContact" class="px-2 py-0.5 rounded bg-warning-soft text-warning font-semibold">Emergency</span>
                  <span *ngIf="g.isPayer" class="px-2 py-0.5 rounded bg-info-soft text-info font-semibold">Payer</span>
                </td>
                <td class="px-6 py-4 text-xs text-right">
                  <button
                    type="button"
                    (click)="unlinkGuardian(g.guardianId)"
                    class="text-danger hover:underline font-semibold"
                  >
                    Unlink
                  </button>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- TAB 3: DAILY REPORTS -->
      <div *ngIf="activeTab === 'reports'" class="bg-surface-card rounded-card border border-border p-6 shadow-card space-y-4">
        <div class="flex items-center justify-between pb-3 border-b border-border">
          <h2 class="text-base font-bold text-text-main">Today's Daily Log</h2>
          <a
            [routerLink]="['/admin/children', child.id, 'log']"
            class="text-xs font-semibold text-primary hover:underline"
          >
            Open Full Log Editor →
          </a>
        </div>

        <div *ngIf="!todayReport" class="py-6 text-center text-text-muted text-sm">
          No daily report created yet for today. Open the log editor to record meals, naps, and activities.
        </div>

        <div *ngIf="todayReport" class="space-y-3">
          <div class="flex items-center gap-3">
            <span class="text-xs font-bold text-text-muted uppercase">Mood:</span>
            <span class="text-sm font-semibold text-primary">{{ todayReport.mood || 'Not recorded' }}</span>
          </div>

          <div *ngIf="todayReport.notes" class="p-3 rounded-lg bg-slate-50 text-xs text-text-main border border-border">
            <strong>Notes:</strong> {{ todayReport.notes }}
          </div>

          <div class="mt-4">
            <h3 class="text-xs font-bold text-text-muted uppercase mb-3">Entries ({{ todayReport.entries.length }})</h3>
            <app-timeline [entries]="todayReport.entries"></app-timeline>
          </div>
        </div>
      </div>

      <!-- TAB 4: BILLING OVERVIEW -->
      <div *ngIf="activeTab === 'billing'" class="bg-surface-card rounded-card border border-border p-6 shadow-card space-y-4">
        <h2 class="text-base font-bold text-text-main">Child Fee Plan</h2>
        <div class="p-4 rounded-lg bg-slate-50 border border-border">
          <div class="flex items-center justify-between">
            <div>
              <p class="font-bold text-sm text-text-main">{{ child.feePlanName || 'No active plan' }}</p>
              <p class="text-xs text-text-muted mt-0.5">Assigned tuition schedule</p>
            </div>
            <a routerLink="/admin/invoices" class="text-xs font-semibold text-primary hover:underline">
              View All Invoices →
            </a>
          </div>
        </div>
      </div>

      <!-- Link Guardian Modal -->
      <app-dialog [(isOpen)]="showLinkModal" title="Link Guardian to Child">
        <div class="space-y-4">
          <div>
            <label class="block text-xs font-semibold text-text-main uppercase mb-1">Guardian <span class="text-danger">*</span></label>
            <select
              [(ngModel)]="linkGuardianId"
              class="w-full px-3 py-2 rounded-input border border-border bg-white text-sm focus:ring-2 focus:ring-primary focus:outline-none"
            >
              <option value="">-- Choose Guardian --</option>
              <option *ngFor="let g of allGuardians" [value]="g.id">
                {{ g.firstName }} {{ g.lastName }} ({{ g.phone }})
              </option>
            </select>
          </div>

          <div>
            <label class="block text-xs font-semibold text-text-main uppercase mb-1">Relationship <span class="text-danger">*</span></label>
            <select
              [(ngModel)]="linkRelationship"
              class="w-full px-3 py-2 rounded-input border border-border bg-white text-sm focus:ring-2 focus:ring-primary focus:outline-none"
            >
              <option value="Mother">Mother</option>
              <option value="Father">Father</option>
              <option value="Grandparent">Grandparent</option>
              <option value="Sibling">Sibling</option>
              <option value="Relative">Relative</option>
              <option value="Other">Other</option>
            </select>
          </div>

          <div class="space-y-2 pt-2 border-t border-border">
            <label class="flex items-center gap-2 text-xs text-text-main cursor-pointer">
              <input type="checkbox" [(ngModel)]="linkCanPickUp" class="rounded border-border text-primary focus:ring-primary" />
              <span><strong>Authorized for Pickup:</strong> Allowed to pick child up from daycare</span>
            </label>

            <label class="flex items-center gap-2 text-xs text-text-main cursor-pointer">
              <input type="checkbox" [(ngModel)]="linkIsPrimary" class="rounded border-border text-primary focus:ring-primary" />
              <span><strong>Primary Contact:</strong> First contact person</span>
            </label>

            <label class="flex items-center gap-2 text-xs text-text-main cursor-pointer">
              <input type="checkbox" [(ngModel)]="linkIsEmergency" class="rounded border-border text-primary focus:ring-primary" />
              <span><strong>Emergency Contact:</strong> Contact in case of emergencies</span>
            </label>

            <label class="flex items-center gap-2 text-xs text-text-main cursor-pointer">
              <input type="checkbox" [(ngModel)]="linkIsPayer" class="rounded border-border text-primary focus:ring-primary" />
              <span><strong>Invoice Payer:</strong> Invoices generated for this child billed to this guardian</span>
            </label>
          </div>
        </div>

        <div dialog-actions class="flex gap-2">
          <app-button variant="outline" (click)="showLinkModal = false">Cancel</app-button>
          <app-button variant="primary" [loading]="isLinking" (click)="submitLinkGuardian()">Link Guardian</app-button>
        </div>
      </app-dialog>
    </div>
  `
})
export class ChildDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private childrenService = inject(ChildrenService);
  private dailyReportsService = inject(DailyReportsService);
  private toastService = inject(ToastService);

  childId = '';
  child: ChildDetailDto | null = null;
  todayReport: DailyReportDetailDto | null = null;

  tabs = [
    { id: 'overview', label: 'Overview' },
    { id: 'guardians', label: 'Guardians & Pickup' },
    { id: 'reports', label: 'Daily Reports' },
    { id: 'billing', label: 'Billing' },
  ];
  activeTab = 'overview';

  // Linking guardian modal
  showLinkModal = false;
  allGuardians: GuardianDto[] = [];
  linkGuardianId = '';
  linkRelationship = 'Mother';
  linkCanPickUp = true;
  linkIsPrimary = false;
  linkIsEmergency = true;
  linkIsPayer = false;
  isLinking = false;

  ngOnInit(): void {
    this.childId = this.route.snapshot.paramMap.get('id') || '';
    if (this.childId) {
      this.loadChild();
      this.loadTodayReport();
    }
  }

  loadChild(): void {
    this.childrenService.getChild(this.childId).subscribe({
      next: (data) => this.child = data,
      error: () => this.toastService.error('Failed to load child')
    });
  }

  loadTodayReport(): void {
    const todayStr = new Date().toISOString().split('T')[0];
    this.dailyReportsService.getDailyReports(this.childId, todayStr).subscribe({
      next: (res) => {
        if (res.length > 0) this.todayReport = res[0];
      }
    });
  }

  openLinkGuardianModal(): void {
    this.childrenService.getAllGuardians().subscribe({
      next: (g: any) => {
        this.allGuardians = Array.isArray(g) ? g : (g?.items || []);
      },
      error: () => this.toastService.error('Failed to load guardians')
    });
    this.linkGuardianId = '';
    this.linkRelationship = 'Mother';
    this.linkCanPickUp = true;
    this.linkIsPrimary = false;
    this.linkIsEmergency = true;
    this.linkIsPayer = false;
    this.showLinkModal = true;
  }

  unlinkGuardian(guardianId: string): void {
    if (!confirm('Are you sure you want to unlink this guardian?')) return;
    this.childrenService.unlinkGuardian(this.childId, guardianId).subscribe({
      next: () => {
        this.toastService.success('Guardian unlinked successfully');
        this.loadChild();
      },
      error: (err) => {
        this.toastService.error(err.error?.detail || err.error?.title || 'Failed to unlink guardian');
      }
    });
  }

  submitLinkGuardian(): void {
    if (!this.linkGuardianId) {
      this.toastService.warning('Please select a guardian');
      return;
    }
    this.isLinking = true;
    this.childrenService.linkGuardian(this.childId, {
      guardianId: this.linkGuardianId,
      relationship: this.linkRelationship,
      canPickUp: this.linkCanPickUp,
      isPrimaryContact: this.linkIsPrimary,
      isEmergencyContact: this.linkIsEmergency,
      isPayer: this.linkIsPayer
    }).subscribe({
      next: () => {
        this.isLinking = false;
        this.showLinkModal = false;
        this.toastService.success('Guardian linked successfully!');
        this.loadChild();
      },
      error: (err) => {
        this.isLinking = false;
        this.toastService.error(err.error?.detail || 'Failed to link guardian');
      }
    });
  }

  onPhotoChanged(event: { hasPhoto: boolean; photoUpdatedAt: string | null }): void {
    if (this.child) {
      this.child.hasPhoto = event.hasPhoto;
      this.child.photoUpdatedAt = event.photoUpdatedAt;
    }
  }

  deleteChild(): void {
    if (!this.child) return;
    if (!confirm(`Are you sure you want to withdraw/remove ${this.child.firstName} ${this.child.lastName}? This will archive the child while preserving billing and incident records.`)) return;

    this.childrenService.deleteChild(this.childId).subscribe({
      next: () => {
        this.toastService.success('Child withdrawn successfully');
        this.router.navigate(['/admin/children']);
      },
      error: (err) => {
        this.toastService.error(err.error?.detail || err.error?.title || 'Failed to remove child');
      }
    });
  }
}
