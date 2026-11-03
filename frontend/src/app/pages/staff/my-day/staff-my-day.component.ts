import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ClassroomService, ClassroomDto } from '../../../core/services/classroom.service';
import { AttendanceService, ClassroomRosterDto, RosterItemDto } from '../../../core/services/attendance.service';
import { ChildrenService, GuardianDto } from '../../../core/services/children.service';
import { ToastService } from '../../../core/services/toast.service';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';
import { DialogComponent } from '../../../shared/components/dialog/dialog.component';
import { ChildAllergyFlagComponent } from '../../../shared/components/child-allergy-flag/child-allergy-flag.component';
import { AvatarComponent } from '../../../shared/components/avatar/avatar.component';

@Component({
  selector: 'app-staff-my-day',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, ButtonComponent, BadgeComponent, DialogComponent, ChildAllergyFlagComponent, AvatarComponent],
  template: `
    <div class="space-y-6">
      <!-- Header -->
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-4">
        <div>
          <div class="flex items-center gap-2">
            <h1 class="text-2xl font-bold text-text-main tracking-tight">My Day</h1>
            <span class="inline-flex items-center px-2.5 py-0.5 rounded-full bg-primary-soft text-primary text-xs font-medium">
              Active Shift
            </span>
          </div>
          <p class="text-sm text-text-muted mt-1">Classroom roster, attendance tracking, and daily activity logs</p>
        </div>

        <!-- Classroom Tab Switcher -->
        <div class="inline-flex p-1 rounded-xl bg-slate-100 border border-border gap-1 self-start md:self-auto">
          <button
            *ngFor="let c of classrooms"
            type="button"
            (click)="selectClassroom(c.id)"
            [class]="c.id === selectedClassroomId ? 'bg-primary text-white shadow-sm font-semibold' : 'text-text-muted hover:text-text-main hover:bg-slate-200'"
            class="min-h-[40px] px-4 py-1.5 rounded-lg text-sm transition-all flex items-center gap-2"
          >
            <span>{{ c.name }}</span>
            <span *ngIf="isClassroomOverRatio(c.id)" class="w-2 h-2 rounded-full bg-danger"></span>
          </button>
        </div>
      </div>

      <!-- Ratio Alert Banner if Over Limit -->
      <div
        *ngIf="roster?.isOverLimit"
        class="rounded-card bg-danger-soft border border-danger/30 p-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4 shadow-sm"
      >
        <div class="flex items-start sm:items-center gap-3">
          <div class="w-10 h-10 rounded-lg bg-danger text-white flex items-center justify-center shrink-0">
            <svg class="w-6 h-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
            </svg>
          </div>
          <div>
            <div class="flex items-center gap-2 flex-wrap">
              <span class="font-bold text-danger">
                {{ roster?.classroomName }}: {{ roster?.presentCount }} children, {{ roster?.staffCount }} staff, limit {{ roster?.maxChildrenAllowed }}
              </span>
              <span class="inline-flex items-center px-2 py-0.5 rounded-full bg-danger text-white text-xs font-bold uppercase tracking-wider">
                Over Ratio Limit
              </span>
            </div>
            <p class="text-xs text-text-muted mt-0.5">Classroom requires caregiver assistance immediately to satisfy ratio standards.</p>
          </div>
        </div>
      </div>

      <!-- Metric Counter Summary Cards -->
      <div class="grid grid-cols-2 md:grid-cols-4 gap-4">
        <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
          <div class="flex items-center justify-between text-xs font-semibold text-text-muted">
            <span>Present</span>
            <span class="w-2 h-2 rounded-full bg-success"></span>
          </div>
          <p class="text-3xl font-bold text-primary mt-2">{{ presentCount }}</p>
          <p class="text-xs text-text-subtle mt-0.5">children in building</p>
        </div>

        <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
          <div class="flex items-center justify-between text-xs font-semibold text-text-muted">
            <span>Not Arrived</span>
            <span class="w-2 h-2 rounded-full bg-slate-300"></span>
          </div>
          <p class="text-3xl font-bold text-text-main mt-2">{{ notArrivedCount }}</p>
          <p class="text-xs text-text-subtle mt-0.5">expected today</p>
        </div>

        <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
          <div class="flex items-center justify-between text-xs font-semibold text-text-muted">
            <span>Checked Out</span>
            <span class="w-2 h-2 rounded-full bg-info"></span>
          </div>
          <p class="text-3xl font-bold text-text-main mt-2">{{ checkedOutCount }}</p>
          <p class="text-xs text-text-subtle mt-0.5">departed</p>
        </div>

        <div class="bg-surface-card rounded-card border border-border p-4 shadow-card">
          <div class="flex items-center justify-between text-xs font-semibold text-text-muted">
            <span>Absent</span>
            <span class="w-2 h-2 rounded-full bg-warning"></span>
          </div>
          <p class="text-3xl font-bold text-warning mt-2">{{ absentCount }}</p>
          <p class="text-xs text-text-subtle mt-0.5">reported absent</p>
        </div>
      </div>

      <!-- Classroom Roster List -->
      <div class="bg-surface-card rounded-card border border-border shadow-card overflow-hidden">
        <div class="px-6 py-4 border-b border-border flex items-center justify-between">
          <h2 class="text-base font-bold text-text-main">
            {{ roster?.classroomName || 'Classroom' }} Roster
          </h2>
          <span class="text-xs text-text-muted">
            {{ roster?.children?.length || 0 }} enrolled
          </span>
        </div>

        <div *ngIf="isLoading" class="p-8 text-center text-text-muted text-sm">
          Loading classroom roster...
        </div>

        <div *ngIf="!isLoading && (!roster?.children || roster?.children?.length === 0)" class="p-8 text-center text-text-muted text-sm">
          No children found for this classroom.
        </div>

        <div *ngIf="!isLoading && roster?.children" class="divide-y divide-border">
          <div
            *ngFor="let child of roster?.children"
            class="p-4 sm:p-5 flex flex-col sm:flex-row sm:items-center justify-between gap-4 hover:bg-slate-50/50 transition-colors"
          >
            <!-- Child Info -->
            <div class="flex items-start gap-4">
              <app-avatar
                [kind]="'children'"
                [id]="child.childId"
                [name]="child.firstName + ' ' + child.lastName"
                [hasPhoto]="child.hasPhoto ?? false"
                [photoUpdatedAt]="child.photoUpdatedAt"
                [size]="48"
              ></app-avatar>
              <div>
                <div class="flex items-center gap-2 flex-wrap">
                  <h3 class="text-base font-bold text-text-main">
                    {{ child.firstName }} {{ child.lastName }}
                  </h3>
                  <app-badge [status]="child.status" [text]="child.status"></app-badge>

                  <!-- Allergy / Medical Tags -->
                  <app-child-allergy-flag [allergies]="child.allergies"></app-child-allergy-flag>
                  <span
                    *ngIf="child.medicalNotes"
                    class="inline-flex items-center gap-1 px-2 py-0.5 rounded-full bg-warning-soft text-warning text-xs font-medium border border-warning/20"
                  >
                    <svg class="w-3 h-3 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                    </svg>
                    <span>{{ child.medicalNotes }}</span>
                  </span>
                </div>

                <div class="text-xs text-text-muted mt-1 space-x-3">
                  <span *ngIf="child.checkInAt">In: {{ child.checkInAt | date:'shortTime' }}</span>
                  <span *ngIf="child.checkOutAt">Out: {{ child.checkOutAt | date:'shortTime' }}</span>
                  <span *ngIf="child.absenceReason">Reason: {{ child.absenceReason }}</span>
                </div>
              </div>
            </div>

            <!-- Action Buttons (touch target >= 44px) -->
            <div class="flex items-center gap-2 self-end sm:self-auto">
              <!-- When Not Arrived: Check In and Mark Absent -->
              <ng-container *ngIf="child.status === 'NotArrived'">
                <button
                  type="button"
                  (click)="openCheckInModal(child)"
                  class="min-h-[44px] px-4 py-2 rounded-input bg-primary text-white text-sm font-medium hover:bg-primary-hover transition-colors shadow-sm"
                >
                  Check In
                </button>
                <button
                  type="button"
                  (click)="openAbsentModal(child)"
                  class="min-h-[44px] px-3 py-2 rounded-input border border-border text-text-muted text-sm font-medium hover:bg-slate-100 transition-colors"
                >
                  Absent
                </button>
              </ng-container>

              <!-- When Present: Daily Log & Check Out -->
              <ng-container *ngIf="child.status === 'Present'">
                <a
                  [routerLink]="['/staff/children', child.childId, 'log']"
                  class="min-h-[44px] px-4 py-2 rounded-input bg-primary-soft text-primary text-sm font-medium hover:bg-teal-100 transition-colors flex items-center gap-1.5"
                >
                  <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                  </svg>
                  Daily Log
                </a>
                <button
                  type="button"
                  (click)="openCheckOutModal(child)"
                  class="min-h-[44px] px-4 py-2 rounded-input bg-slate-900 text-white text-sm font-medium hover:bg-slate-800 transition-colors"
                >
                  Check Out
                </button>
              </ng-container>

              <!-- When Checked Out or Absent: View Log link -->
              <ng-container *ngIf="child.status === 'CheckedOut' || child.status === 'Absent'">
                <a
                  [routerLink]="['/staff/children', child.childId, 'log']"
                  class="min-h-[44px] px-3 py-2 rounded-input border border-border text-text-muted text-sm font-medium hover:bg-slate-100 transition-colors flex items-center gap-1"
                >
                  View Log
                </a>
              </ng-container>
            </div>
          </div>
        </div>
      </div>

      <!-- Check In Modal -->
      <app-dialog
        [(isOpen)]="showCheckInModal"
        [title]="'Check In: ' + (activeChild?.firstName + ' ' + activeChild?.lastName)"
      >
        <div class="space-y-4">
          <p class="text-sm text-text-muted">Record arrival for today's session.</p>

          <div>
            <label class="block text-xs font-semibold text-text-main uppercase tracking-wider mb-2">
              Dropped off by Guardian (Optional)
            </label>
            <div class="space-y-2 max-h-48 overflow-y-auto pr-1" role="radiogroup" aria-label="Select drop-off guardian">
              <label
                [class]="selectedCheckInGuardianId === null ? 'border-primary bg-primary-soft/30 ring-1 ring-primary' : 'border-border hover:bg-slate-50'"
                class="flex items-center gap-3 p-2.5 rounded-lg border cursor-pointer transition-colors focus-within:ring-2 focus-within:ring-primary focus-within:ring-offset-1 min-h-[44px]"
              >
                <input
                  type="radio"
                  name="staffCheckInGuardian"
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
                  name="staffCheckInGuardian"
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
            <label class="block text-xs font-semibold text-text-main uppercase tracking-wider mb-1">
              Drop-off Notes
            </label>
            <input
              type="text"
              [(ngModel)]="checkInNotes"
              placeholder="e.g. Grandma dropped off, has lunchbox"
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
      <app-dialog
        [(isOpen)]="showCheckOutModal"
        [title]="'Check Out: ' + (activeChild?.firstName + ' ' + activeChild?.lastName)"
        maxWidth="lg"
      >
        <div class="space-y-4">
          <div>
            <label class="block text-xs font-semibold text-text-main uppercase tracking-wider mb-2">
              Picked up by Guardian <span class="text-danger">*</span>
            </label>
            <div class="space-y-2 max-h-72 overflow-y-auto pr-1" role="radiogroup" aria-label="Select pickup guardian">
              <label
                *ngFor="let g of childGuardians"
                [class]="selectedCheckOutGuardianId === g.guardianId ? 'border-primary bg-primary-soft/30 ring-2 ring-primary' : 'border-border hover:bg-slate-50'"
                class="flex items-center gap-4 p-3 rounded-card border cursor-pointer transition-colors focus-within:ring-2 focus-within:ring-primary focus-within:ring-offset-1 min-h-[44px]"
              >
                <input
                  type="radio"
                  name="staffCheckOutGuardian"
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
                      <span>{{ g.canPickUp ? 'Authorized Pickup' : 'NOT Authorized' }}</span>
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
            <span><strong>Warning:</strong> Selected guardian is <u>NOT Authorized</u> for pickup (BR-10). Check-out is blocked.</span>
          </div>

          <!-- Pickup Verification Checkbox -->
          <label
            for="staffPickupIdVerified"
            class="min-h-[44px] p-3 rounded-input bg-slate-50 border border-slate-300 flex items-center gap-3 cursor-pointer hover:bg-slate-100 transition-colors"
          >
            <input
              type="checkbox"
              id="staffPickupIdVerified"
              [(ngModel)]="pickupIdVerified"
              class="h-5 w-5 text-primary rounded border-slate-500 focus:ring-primary shrink-0"
            />
            <span class="text-xs text-text-main select-none">
              <strong>Photo ID Verified:</strong> I confirmed guardian identity and verified authorized pickup credentials.
            </span>
          </label>

          <div>
            <label class="block text-xs font-semibold text-text-main uppercase tracking-wider mb-1">
              Check-out Notes
            </label>
            <input
              type="text"
              [(ngModel)]="checkOutNotes"
              placeholder="e.g. Picked up on time"
              class="w-full px-3 py-2 rounded-input border border-border bg-white text-sm focus:ring-2 focus:ring-primary focus:outline-none"
            />
          </div>
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

      <!-- Record Absence Modal -->
      <app-dialog
        [(isOpen)]="showAbsentModal"
        [title]="'Mark Absent: ' + (activeChild?.firstName + ' ' + activeChild?.lastName)"
      >
        <div class="space-y-4">
          <div>
            <label class="block text-xs font-semibold text-text-main uppercase tracking-wider mb-1">
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
export class StaffMyDayComponent implements OnInit {
  private classroomService = inject(ClassroomService);
  private attendanceService = inject(AttendanceService);
  private childrenService = inject(ChildrenService);
  private toastService = inject(ToastService);

  classrooms: ClassroomDto[] = [];
  selectedClassroomId = '';
  roster: ClassroomRosterDto | null = null;
  isLoading = false;
  isSubmitting = false;

  // Active child for modal actions
  activeChild: RosterItemDto | null = null;
  childGuardians: any[] = [];

  // Check in
  showCheckInModal = false;
  selectedCheckInGuardianId: string | null = null;
  checkInNotes = '';

  // Check out
  showCheckOutModal = false;
  selectedCheckOutGuardianId = '';
  pickupIdVerified = false;
  checkOutNotes = '';

  get selectedCheckOutGuardianName(): string {
    if (!this.selectedCheckOutGuardianId) return '';
    const g = this.childGuardians.find(x => x.guardianId === this.selectedCheckOutGuardianId);
    return g ? `${g.firstName} ${g.lastName}` : '';
  }

  get selectedGuardianNotAuthorized(): boolean {
    if (!this.selectedCheckOutGuardianId) return false;
    const g = this.childGuardians.find(x => x.guardianId === this.selectedCheckOutGuardianId);
    return g ? !g.canPickUp : false;
  }

  get checkOutButtonLabel(): string {
    const childName = this.activeChild?.firstName || 'child';
    const guardianName = this.selectedCheckOutGuardianName || 'guardian';
    return `Release ${childName} to ${guardianName}`;
  }

  // Absent
  showAbsentModal = false;
  absentReason = '';

  get presentCount(): number {
    return this.roster?.children?.filter(c => c.status === 'Present').length || 0;
  }

  get notArrivedCount(): number {
    return this.roster?.children?.filter(c => c.status === 'NotArrived').length || 0;
  }

  get checkedOutCount(): number {
    return this.roster?.children?.filter(c => c.status === 'CheckedOut').length || 0;
  }

  get absentCount(): number {
    return this.roster?.children?.filter(c => c.status === 'Absent').length || 0;
  }

  ngOnInit(): void {
    this.loadClassrooms();
  }

  loadClassrooms(): void {
    this.classroomService.getClassrooms().subscribe({
      next: (data) => {
        this.classrooms = data;
        if (data.length > 0 && !this.selectedClassroomId) {
          // Default to Toddlers if present, else first
          const toddlers = data.find(c => c.name.toLowerCase().includes('toddler'));
          this.selectClassroom(toddlers ? toddlers.id : data[0].id);
        }
      },
      error: () => this.toastService.error('Failed to load classrooms')
    });
  }

  selectClassroom(id: string): void {
    this.selectedClassroomId = id;
    this.loadRoster();
  }

  loadRoster(): void {
    if (!this.selectedClassroomId) return;
    this.isLoading = true;
    this.attendanceService.getClassroomRoster(this.selectedClassroomId).subscribe({
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

  isClassroomOverRatio(id: string): boolean {
    if (this.roster && this.roster.classroomId === id) {
      return this.roster.isOverLimit;
    }
    return false;
  }

  openCheckInModal(child: RosterItemDto): void {
    this.activeChild = child;
    this.selectedCheckInGuardianId = null;
    this.checkInNotes = '';
    this.showCheckInModal = true;
    this.loadChildGuardians(child.childId);
  }

  openCheckOutModal(child: RosterItemDto): void {
    this.activeChild = child;
    this.selectedCheckOutGuardianId = '';
    this.pickupIdVerified = false;
    this.checkOutNotes = '';
    this.showCheckOutModal = true;
    this.loadChildGuardians(child.childId);
  }

  openAbsentModal(child: RosterItemDto): void {
    this.activeChild = child;
    this.absentReason = '';
    this.showAbsentModal = true;
  }

  loadChildGuardians(childId: string): void {
    this.childrenService.getChildGuardians(childId).subscribe({
      next: (guardians) => this.childGuardians = guardians,
      error: () => this.childGuardians = []
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
        this.toastService.success(`${this.activeChild?.firstName} checked in successfully!`);
        this.loadRoster();
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
      pickupIdVerified: this.pickupIdVerified,
      notes: this.checkOutNotes
    }).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.showCheckOutModal = false;
        this.toastService.success(`${this.activeChild?.firstName} checked out successfully!`);
        this.loadRoster();
      },
      error: (err) => {
        this.isSubmitting = false;
        this.toastService.error(err.error?.detail || 'Check-out failed');
      }
    });
  }

  submitAbsent(): void {
    if (!this.activeChild || !this.absentReason) {
      this.toastService.warning('Please provide an absence reason');
      return;
    }
    const todayStr = new Date().toISOString().split('T')[0];
    this.isSubmitting = true;
    this.attendanceService.recordAbsence({
      childId: this.activeChild.childId,
      classroomId: this.selectedClassroomId,
      date: todayStr,
      reason: this.absentReason
    }).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.showAbsentModal = false;
        this.toastService.success(`${this.activeChild?.firstName} marked absent.`);
        this.loadRoster();
      },
      error: (err) => {
        this.isSubmitting = false;
        this.toastService.error(err.error?.detail || 'Failed to record absence');
      }
    });
  }
}
