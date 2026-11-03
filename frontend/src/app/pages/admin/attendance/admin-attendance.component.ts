import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ClassroomService, ClassroomDto } from '../../../core/services/classroom.service';
import { AttendanceService, ClassroomRosterDto, RosterItemDto } from '../../../core/services/attendance.service';
import { ChildrenService } from '../../../core/services/children.service';
import { ToastService } from '../../../core/services/toast.service';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';
import { DialogComponent } from '../../../shared/components/dialog/dialog.component';
import { AvatarComponent } from '../../../shared/components/avatar/avatar.component';

@Component({
  selector: 'app-admin-attendance',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, ButtonComponent, BadgeComponent, DialogComponent, AvatarComponent],
  template: `
    <div class="space-y-6">
      <!-- Top header -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-text-main tracking-tight">Attendance Board</h1>
          <p class="text-sm text-text-muted mt-1">Real-time attendance tracking, ratios, and check-in/out across classrooms</p>
        </div>
        <div class="text-xs text-text-muted bg-surface-card px-3 py-1.5 rounded-lg border border-border self-start sm:self-auto">
          Today: <strong class="text-text-main">{{ todayDate | date:'fullDate' }}</strong>
        </div>
      </div>

      <!-- Ratio Status Cards for all classrooms -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div
          *ngFor="let ratio of ratiosList"
          [class]="ratio.isOverLimit ? 'bg-danger-soft border-danger/40 ring-1 ring-danger/30' : 'bg-surface-card border-border'"
          class="rounded-card border p-4 shadow-card flex flex-col justify-between transition-all"
        >
          <div class="flex items-start justify-between">
            <div>
              <h3 class="text-base font-bold text-text-main">{{ ratio.classroomName }}</h3>
              <p class="text-xs text-text-muted mt-0.5">Capacity: {{ ratio.capacity }}</p>
            </div>
            <span
              [class]="ratio.isOverLimit ? 'bg-danger text-white' : 'bg-success-soft text-success'"
              class="px-2 py-0.5 rounded-full text-xs font-semibold"
            >
              {{ ratio.isOverLimit ? 'Over Ratio Limit' : 'Compliant' }}
            </span>
          </div>

          <div class="mt-4 pt-3 border-t border-border/60 flex items-center justify-between text-xs">
            <div>
              <span class="font-bold text-text-main">{{ ratio.presentCount }} children</span>,
              <span class="text-text-muted"> {{ ratio.staffCount }} staff</span>
            </div>
            <span class="text-text-muted">Limit: {{ ratio.maxChildrenAllowed }}</span>
          </div>
        </div>
      </div>

      <!-- Classroom Filter Tabs -->
      <div class="flex items-center gap-2 border-b border-border pb-3 overflow-x-auto">
        <button
          type="button"
          *ngFor="let c of classrooms"
          (click)="selectClassroom(c.id)"
          [class]="c.id === selectedClassroomId ? 'bg-primary text-white font-semibold shadow-sm' : 'bg-slate-100 text-text-muted hover:bg-slate-200'"
          class="px-4 py-2 rounded-lg text-sm transition-colors flex items-center gap-2 shrink-0"
        >
          <span>{{ c.name }}</span>
          <span
            *ngIf="getPresentCountForClassroom(c.id) > 0"
            class="px-1.5 py-0.5 rounded-full text-xs font-bold"
            [class]="c.id === selectedClassroomId ? 'bg-white text-primary' : 'bg-slate-200 text-text-main'"
          >
            {{ getPresentCountForClassroom(c.id) }} present
          </span>
        </button>
      </div>

      <!-- Roster Table / Card -->
      <div class="bg-surface-card rounded-card border border-border shadow-card overflow-hidden">
        <div class="px-6 py-4 border-b border-border flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div class="flex items-center gap-3">
            <h2 class="text-base font-bold text-text-main">{{ activeClassroomName }} Children</h2>
            <span class="text-xs text-text-muted">({{ roster?.children?.length || 0 }} total)</span>
          </div>

          <!-- Status Filter -->
          <div class="flex items-center gap-2 text-xs">
            <span class="text-text-muted">Filter:</span>
            <select
              [(ngModel)]="statusFilter"
              class="px-2.5 py-1.5 rounded-input border border-border bg-white text-xs focus:ring-1 focus:ring-primary focus:outline-none"
            >
              <option value="ALL">All Statuses</option>
              <option value="Present">Present</option>
              <option value="NotArrived">Not Arrived</option>
              <option value="CheckedOut">Checked Out</option>
              <option value="Absent">Absent</option>
            </select>
          </div>
        </div>

        <div *ngIf="isLoading" class="p-8 text-center text-text-muted text-sm">
          Loading classroom attendance...
        </div>

        <div *ngIf="!isLoading && filteredChildren.length === 0" class="p-8 text-center text-text-muted text-sm">
          No children match the current filter.
        </div>

        <!-- Children Table -->
        <div *ngIf="!isLoading && filteredChildren.length > 0" class="overflow-x-auto">
          <table class="w-full text-left text-sm">
            <thead class="bg-slate-50 border-b border-border text-xs font-semibold text-text-muted uppercase">
              <tr>
                <th class="px-6 py-3">Child</th>
                <th class="px-6 py-3">Status</th>
                <th class="px-6 py-3">Arrival / Departure</th>
                <th class="px-6 py-3">Allergies & Medical</th>
                <th class="px-6 py-3 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-border">
              <tr *ngFor="let child of filteredChildren" class="hover:bg-slate-50/50 transition-colors">
                <td class="px-6 py-4 font-medium text-text-main flex items-center gap-3">
                  <app-avatar
                    [kind]="'children'"
                    [id]="child.childId"
                    [name]="child.firstName + ' ' + child.lastName"
                    [hasPhoto]="child.hasPhoto ?? false"
                    [photoUpdatedAt]="child.photoUpdatedAt"
                    [size]="48"
                  ></app-avatar>
                  <div>
                    <a [routerLink]="['/admin/children', child.childId]" class="font-bold text-primary hover:underline">
                      {{ child.firstName }} {{ child.lastName }}
                    </a>
                  </div>
                </td>
                <td class="px-6 py-4">
                  <app-badge [status]="child.status" [text]="child.status"></app-badge>
                </td>
                <td class="px-6 py-4 text-xs text-text-muted">
                  <div *ngIf="child.checkInAt">Checked In: {{ child.checkInAt | date:'shortTime' }}</div>
                  <div *ngIf="child.checkOutAt">Checked Out: {{ child.checkOutAt | date:'shortTime' }}</div>
                  <div *ngIf="child.absenceReason">Absent: {{ child.absenceReason }}</div>
                  <div *ngIf="!child.checkInAt && !child.absenceReason" class="text-text-muted">Not checked in</div>
                </td>
                <td class="px-6 py-4 text-xs">
                  <span *ngIf="child.allergies" class="inline-flex px-2 py-0.5 rounded bg-danger-soft text-danger font-medium mr-1">
                    Allergy: {{ child.allergies }}
                  </span>
                  <span *ngIf="child.medicalNotes" class="inline-flex px-2 py-0.5 rounded bg-warning-soft text-warning font-medium">
                    {{ child.medicalNotes }}
                  </span>
                  <span *ngIf="!child.allergies && !child.medicalNotes" class="text-text-muted">None</span>
                </td>
                <td class="px-6 py-4 text-right">
                  <div class="flex items-center justify-end gap-2">
                    <button
                      *ngIf="child.status === 'NotArrived'"
                      type="button"
                      (click)="openCheckInModal(child)"
                      class="px-3 py-1.5 rounded-input bg-primary text-white text-xs font-medium hover:bg-primary-hover shadow-sm"
                    >
                      Check In
                    </button>
                    <button
                      *ngIf="child.status === 'NotArrived'"
                      type="button"
                      (click)="openAbsentModal(child)"
                      class="px-2.5 py-1.5 rounded-input border border-danger/30 text-danger hover:bg-danger-soft text-xs font-medium transition"
                    >
                      Absent
                    </button>
                    <button
                      *ngIf="child.status === 'Present'"
                      type="button"
                      (click)="openCheckOutModal(child)"
                      class="px-3 py-1.5 rounded-input bg-slate-900 text-white text-xs font-medium hover:bg-slate-800"
                    >
                      Check Out
                    </button>
                    <a
                      [routerLink]="['/admin/children', child.childId, 'log']"
                      class="px-2.5 py-1.5 rounded-input border border-border text-text-muted text-xs hover:bg-slate-100"
                    >
                      Daily Log
                    </a>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Check In Modal -->
      <app-dialog [(isOpen)]="showCheckInModal" [title]="'Check In: ' + (activeChild?.firstName + ' ' + activeChild?.lastName)">
        <div class="space-y-4">
          <p class="text-xs text-text-muted">Record child check-in for the current session.</p>
          <div>
            <label class="block text-xs font-semibold text-text-main uppercase mb-2">Dropped off by Guardian (Optional)</label>
            <div class="space-y-2 max-h-48 overflow-y-auto pr-1" role="radiogroup" aria-label="Select drop-off guardian">
              <label
                [class]="selectedCheckInGuardianId === null ? 'border-primary bg-primary-soft/30 ring-1 ring-primary' : 'border-border hover:bg-slate-50'"
                class="flex items-center gap-3 p-2.5 rounded-lg border cursor-pointer transition-colors focus-within:ring-2 focus-within:ring-primary focus-within:ring-offset-1 min-h-[44px]"
              >
                <input
                  type="radio"
                  name="adminCheckInGuardian"
                  [value]="null"
                  [checked]="selectedCheckInGuardianId === null"
                  (change)="selectedCheckInGuardianId = null"
                  class="h-4 w-4 text-primary border-slate-500 focus:ring-primary shrink-0"
                  aria-label="No specific guardian, General arrival"
                />
                <div class="w-10 h-10 rounded-full bg-slate-200 text-text-muted font-bold flex items-center justify-center text-xs shrink-0">
                  -
                </div>
                <div class="flex-1">
                  <p class="text-xs font-semibold text-text-main">No specific guardian</p>
                  <p class="text-xs text-text-muted">General arrival</p>
                </div>
              </label>

              <label
                *ngFor="let g of childGuardians"
                [class]="selectedCheckInGuardianId === g.guardianId ? 'border-primary bg-primary-soft/30 ring-1 ring-primary' : 'border-border hover:bg-slate-50'"
                class="flex items-center gap-3 p-2.5 rounded-lg border cursor-pointer transition-colors focus-within:ring-2 focus-within:ring-primary focus-within:ring-offset-1 min-h-[44px]"
              >
                <input
                  type="radio"
                  name="adminCheckInGuardian"
                  [value]="g.guardianId"
                  [checked]="selectedCheckInGuardianId === g.guardianId"
                  (change)="selectedCheckInGuardianId = g.guardianId"
                  class="h-4 w-4 text-primary border-slate-500 focus:ring-primary shrink-0"
                  [attr.aria-label]="g.firstName + ' ' + g.lastName + ', ' + g.relationship"
                />
                <app-avatar
                  [kind]="'guardians'"
                  [id]="g.guardianId"
                  [name]="g.firstName + ' ' + g.lastName"
                  [hasPhoto]="g.hasPhoto ?? false"
                  [photoUpdatedAt]="g.photoUpdatedAt"
                  [size]="40"
                ></app-avatar>
                <div class="flex-1">
                  <p class="text-xs font-semibold text-text-main">{{ g.firstName }} {{ g.lastName }}</p>
                  <p class="text-xs text-text-muted">{{ g.relationship }} • {{ g.phone }}</p>
                </div>
              </label>
            </div>
          </div>
          <div>
            <label class="block text-xs font-semibold text-text-main uppercase mb-1">Notes</label>
            <input
              type="text"
              [(ngModel)]="checkInNotes"
              placeholder="e.g. Arrived happily"
              class="w-full px-3 py-2 rounded-input border border-border bg-white text-sm focus:ring-2 focus:ring-primary focus:outline-none"
            />
          </div>
        </div>
        <div dialog-actions class="flex gap-2">
          <app-button variant="outline" (click)="showCheckInModal = false">Cancel</app-button>
          <app-button variant="primary" [loading]="isSubmitting" (click)="submitCheckIn()">Confirm Check In</app-button>
        </div>
      </app-dialog>

      <!-- Check Out Modal -->
      <app-dialog [(isOpen)]="showCheckOutModal" [title]="'Check Out: ' + (activeChild?.firstName + ' ' + activeChild?.lastName)" maxWidth="lg">
        <div class="space-y-4">
          <div>
            <label class="block text-xs font-semibold text-text-main uppercase mb-2">
              Authorized Pickup Guardian <span class="text-danger">*</span>
            </label>
            <div class="space-y-2 max-h-72 overflow-y-auto pr-1" role="radiogroup" aria-label="Select pickup guardian">
              <label
                *ngFor="let g of childGuardians"
                [class]="selectedCheckOutGuardianId === g.guardianId ? 'border-primary bg-primary-soft/30 ring-2 ring-primary' : 'border-border hover:bg-slate-50'"
                class="flex items-center gap-4 p-3 rounded-card border cursor-pointer transition-colors focus-within:ring-2 focus-within:ring-primary focus-within:ring-offset-1 min-h-[44px]"
              >
                <input
                  type="radio"
                  name="adminCheckOutGuardian"
                  [value]="g.guardianId"
                  [checked]="selectedCheckOutGuardianId === g.guardianId"
                  (change)="selectedCheckOutGuardianId = g.guardianId"
                  class="h-4 w-4 text-primary border-slate-500 focus:ring-primary shrink-0"
                  [attr.aria-label]="g.firstName + ' ' + g.lastName + ', ' + g.relationship + (g.canPickUp ? ', Authorized Pickup' : ', Not Authorized')"
                />
                <app-avatar
                  [kind]="'guardians'"
                  [id]="g.guardianId"
                  [name]="g.firstName + ' ' + g.lastName"
                  [hasPhoto]="g.hasPhoto ?? false"
                  [photoUpdatedAt]="g.photoUpdatedAt"
                  [size]="96"
                ></app-avatar>
                <div class="flex-1 min-w-0">
                  <div class="flex items-center gap-2 flex-wrap">
                    <p class="text-sm font-bold text-text-main">{{ g.firstName }} {{ g.lastName }}</p>
                    <span
                      class="px-2 py-0.5 rounded-full text-xs font-bold inline-flex items-center gap-1"
                      [class]="g.canPickUp ? 'bg-success-soft text-green-800' : 'bg-danger-soft text-red-800'"
                    >
                      <svg *ngIf="g.canPickUp" class="w-3 h-3 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M5 13l4 4L19 7" />
                      </svg>
                      <svg *ngIf="!g.canPickUp" class="w-3 h-3 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M18.364 18.364A9 9 0 005.636 5.636m12.728 12.728A9 9 0 015.636 5.636m12.728 12.728L5.636 5.636" />
                      </svg>
                      <span>{{ g.canPickUp ? 'Authorized Pickup' : 'Not on pickup list' }}</span>
                    </span>
                  </div>
                  <p class="text-xs text-text-muted mt-0.5">{{ g.relationship }} • {{ g.phone }}</p>
                  <p *ngIf="g.canPickUp" class="text-xs text-green-700 font-medium mt-1">
                    Verified photo match for authorized pickup
                  </p>
                </div>
              </label>
            </div>
          </div>
          <div *ngIf="selectedGuardianNotAuthorized" class="p-2.5 bg-danger-soft border border-danger/20 rounded-input text-red-800 text-xs flex items-center gap-1.5">
            <svg class="w-4 h-4 shrink-0 text-red-800" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
            <span><strong>Warning:</strong> Selected guardian is <u>Not on pickup list</u>. Check-out will be rejected by policy (BR-10).</span>
          </div>

          <!-- Pickup Verification Checkbox -->
          <label
            for="adminPickupIdVerified"
            class="min-h-[44px] p-3 rounded-input bg-slate-50 border border-slate-300 flex items-center gap-3 cursor-pointer hover:bg-slate-100 transition-colors"
          >
            <input
              type="checkbox"
              id="adminPickupIdVerified"
              [(ngModel)]="pickupIdVerified"
              class="h-5 w-5 text-primary rounded border-slate-500 focus:ring-primary shrink-0"
            />
            <span class="text-xs text-text-main select-none">
              <strong>ID Verified:</strong> Guardian identity matches authorized pickup authorization.
            </span>
          </label>
        </div>
        <div dialog-actions class="flex gap-2">
          <app-button variant="outline" (click)="showCheckOutModal = false">Cancel</app-button>
          <app-button
            variant="primary"
            [loading]="isSubmitting"
            [disabled]="!pickupIdVerified || !selectedCheckOutGuardianId || selectedGuardianNotAuthorized"
            (click)="submitCheckOut()"
          >
            {{ checkOutButtonLabel }}
          </app-button>
        </div>
      </app-dialog>

      <!-- Mark Absent Modal -->
      <app-dialog [(isOpen)]="showAbsentModal" [title]="'Mark Absent: ' + (activeChild?.firstName + ' ' + activeChild?.lastName)">
        <div class="space-y-4">
          <p class="text-xs text-text-muted">Record an absence for this child today.</p>
          <div>
            <label class="block text-xs font-semibold text-text-main uppercase mb-1">
              Absence Reason <span class="text-danger">*</span>
            </label>
            <input
              type="text"
              [(ngModel)]="absentReason"
              placeholder="e.g. Sick, Family travel, Doctor appointment"
              class="w-full px-3 py-2 rounded-input border border-border bg-white text-sm focus:ring-2 focus:ring-primary focus:outline-none"
            />
          </div>
        </div>
        <div dialog-actions class="flex gap-2">
          <app-button variant="outline" (click)="showAbsentModal = false">Cancel</app-button>
          <app-button variant="danger" [loading]="isSubmitting" (click)="submitAbsent()">Mark Absent</app-button>
        </div>
      </app-dialog>
    </div>
  `
})
export class AdminAttendanceComponent implements OnInit {
  private classroomService = inject(ClassroomService);
  private attendanceService = inject(AttendanceService);
  private childrenService = inject(ChildrenService);
  private toastService = inject(ToastService);

  todayDate = new Date();
  classrooms: ClassroomDto[] = [];
  selectedClassroomId = '';
  roster: ClassroomRosterDto | null = null;
  ratiosList: any[] = [];
  isLoading = false;
  isSubmitting = false;

  statusFilter = 'ALL';

  activeChild: RosterItemDto | null = null;
  childGuardians: any[] = [];
  showCheckInModal = false;
  selectedCheckInGuardianId: string | null = null;
  checkInNotes = '';

  showCheckOutModal = false;
  selectedCheckOutGuardianId = '';
  pickupIdVerified = false;

  showAbsentModal = false;
  absentReason = '';

  get selectedCheckOutGuardianName(): string {
    if (!this.selectedCheckOutGuardianId) return '';
    const g = this.childGuardians.find(x => x.guardianId === this.selectedCheckOutGuardianId);
    return g ? `${g.firstName} ${g.lastName}` : '';
  }

  get checkOutButtonLabel(): string {
    const childName = this.activeChild?.firstName || 'child';
    const guardianName = this.selectedCheckOutGuardianName || 'guardian';
    return `Release ${childName} to ${guardianName}`;
  }

  get selectedGuardianNotAuthorized(): boolean {
    if (!this.selectedCheckOutGuardianId) return false;
    const g = this.childGuardians.find(x => x.guardianId === this.selectedCheckOutGuardianId);
    return g ? !g.canPickUp : false;
  }

  get activeClassroomName(): string {
    return this.classrooms.find(c => c.id === this.selectedClassroomId)?.name || 'Classroom';
  }

  get filteredChildren(): RosterItemDto[] {
    if (!this.roster?.children) return [];
    if (this.statusFilter === 'ALL') return this.roster.children;
    return this.roster.children.filter(c => c.status === this.statusFilter);
  }

  ngOnInit(): void {
    this.loadClassroomsAndRatios();
  }

  loadClassroomsAndRatios(): void {
    this.classroomService.getClassrooms().subscribe({
      next: (cls) => {
        this.classrooms = cls;
        if (cls.length > 0) {
          this.selectClassroom(cls[0].id);
        }
      }
    });

    this.attendanceService.getRatios().subscribe({
      next: (res: any) => this.ratiosList = Array.isArray(res) ? res : (res?.classrooms || [])
    });
  }

  getPresentCountForClassroom(id: string): number {
    const ratio = this.ratiosList.find(r => r.classroomId === id);
    return ratio ? ratio.presentCount : 0;
  }

  selectClassroom(id: string): void {
    this.selectedClassroomId = id;
    this.isLoading = true;
    this.attendanceService.getClassroomRoster(id).subscribe({
      next: (data) => {
        this.roster = data;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.toastService.error('Failed to load roster');
      }
    });
  }

  openCheckInModal(child: RosterItemDto): void {
    this.activeChild = child;
    this.selectedCheckInGuardianId = null;
    this.checkInNotes = '';
    this.showCheckInModal = true;
    this.childrenService.getChildGuardians(child.childId).subscribe({
      next: (g) => this.childGuardians = g
    });
  }

  openCheckOutModal(child: RosterItemDto): void {
    this.activeChild = child;
    this.selectedCheckOutGuardianId = '';
    this.pickupIdVerified = false;
    this.showCheckOutModal = true;
    this.childrenService.getChildGuardians(child.childId).subscribe({
      next: (g) => this.childGuardians = g
    });
  }

  submitCheckIn(): void {
    if (!this.activeChild) return;
    this.isSubmitting = true;
    this.attendanceService.checkIn({
      childId: this.activeChild.childId,
      classroomId: this.selectedClassroomId,
      checkedInByGuardianId: this.selectedCheckInGuardianId,
      notes: this.checkInNotes
    }).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.showCheckInModal = false;
        this.toastService.success(`${this.activeChild?.firstName} checked in.`);
        this.selectClassroom(this.selectedClassroomId);
      },
      error: (err) => {
        this.isSubmitting = false;
        this.toastService.error(err.error?.detail || 'Check-in failed');
      }
    });
  }

  submitCheckOut(): void {
    if (!this.activeChild || !this.selectedCheckOutGuardianId) {
      this.toastService.warning('Please select a pickup guardian');
      return;
    }
    this.isSubmitting = true;
    this.attendanceService.checkOut({
      childId: this.activeChild.childId,
      attendanceId: this.activeChild.attendanceId,
      checkedOutByGuardianId: this.selectedCheckOutGuardianId,
      pickupIdVerified: this.pickupIdVerified
    }).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.showCheckOutModal = false;
        this.toastService.success(`${this.activeChild?.firstName} checked out.`);
        this.selectClassroom(this.selectedClassroomId);
      },
      error: (err) => {
        this.isSubmitting = false;
        this.toastService.error(err.error?.detail || 'Check-out failed');
      }
    });
  }

  openAbsentModal(child: RosterItemDto): void {
    this.activeChild = child;
    this.absentReason = '';
    this.showAbsentModal = true;
  }

  submitAbsent(): void {
    if (!this.activeChild || !this.absentReason.trim()) {
      this.toastService.warning('Please enter an absence reason.');
      return;
    }
    const todayStr = new Date().toISOString().split('T')[0];
    this.isSubmitting = true;
    this.attendanceService.recordAbsence({
      childId: this.activeChild.childId,
      classroomId: this.selectedClassroomId,
      date: todayStr,
      reason: this.absentReason.trim()
    }).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.showAbsentModal = false;
        this.toastService.success(`${this.activeChild?.firstName} marked absent.`);
        this.selectClassroom(this.selectedClassroomId);
      },
      error: (err: any) => {
        this.isSubmitting = false;
        this.toastService.error(err.error?.detail || err.error?.title || 'Failed to mark child absent.');
      }
    });
  }
}

