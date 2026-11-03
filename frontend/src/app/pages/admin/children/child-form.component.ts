import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ChildrenService, CreateChildRequest } from '../../../core/services/children.service';
import { ClassroomService, ClassroomSummaryDto } from '../../../core/services/classroom.service';
import { BillingService, FeePlanDto } from '../../../core/services/billing.service';
import { ToastService } from '../../../core/services/toast.service';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { PhotoUploaderComponent } from '../../../shared/components/photo-uploader/photo-uploader.component';

@Component({
  selector: 'app-child-form',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, ButtonComponent, PhotoUploaderComponent],
  template: `
    <div class="w-full max-w-2xl mx-auto pb-20">
      <!-- Header & Breadcrumbs -->
      <nav class="flex items-center gap-2 text-xs text-text-muted mb-3">
        <a routerLink="/admin/children" class="hover:text-primary transition-colors flex items-center gap-1">
          <span>&larr; Children</span>
        </a>
        <span>/</span>
        <span class="text-text-main font-semibold">{{ isEdit ? 'Edit child' : 'Add child' }}</span>
      </nav>

      <div class="flex items-center justify-between mb-6">
        <div>
          <h1 class="text-2xl font-bold text-text-main tracking-tight">{{ isEdit ? 'Edit child record' : 'Add child' }}</h1>
          <p class="text-xs text-text-muted mt-0.5">Enter enrollment records, classroom placement, and health notes.</p>
        </div>
        <span class="px-2.5 py-1 rounded-full bg-slate-100 text-text-muted text-xs font-medium">
          {{ isEdit ? 'Updating Profile' : 'New Enrollment' }}
        </span>
      </div>

      <form (ngSubmit)="onSubmit()" class="space-y-6">
        <!-- Photo Management (Edit Mode) -->
        <section *ngIf="isEdit && childId">
          <app-photo-uploader
            [kind]="'children'"
            [id]="childId"
            [name]="form.firstName + ' ' + form.lastName"
            [hasPhoto]="childHasPhoto"
            [photoUpdatedAt]="childPhotoUpdatedAt"
            [avatarSize]="80"
            (photoChanged)="onPhotoChanged($event)"
          ></app-photo-uploader>
        </section>

        <!-- Create Mode Photo Notice -->
        <section *ngIf="!isEdit" class="p-4 rounded-card bg-slate-50 border border-dashed border-border text-center space-y-1">
          <p class="text-xs font-semibold text-text-main">Profile Photo</p>
          <p class="text-xs text-text-muted">Save first, then add a photo</p>
        </section>

        <!-- SECTION 1: Identity -->
        <section class="bg-surface-card rounded-card p-6 border border-border shadow-card space-y-4">
          <div class="flex items-center gap-2 pb-3 border-b border-border">
            <span class="w-6 h-6 rounded-full bg-primary-soft text-primary font-bold text-xs flex items-center justify-center">1</span>
            <div>
              <h2 class="text-sm font-bold text-text-main">Child Details</h2>
              <p class="text-xs text-text-muted">Primary legal identity for administrative documentation.</p>
            </div>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label for="firstName" class="block text-xs font-semibold text-text-main mb-1">First Name <span class="text-danger">*</span></label>
              <input
                id="firstName"
                type="text"
                [(ngModel)]="form.firstName"
                name="firstName"
                required
                placeholder="e.g. Abel"
                [attr.aria-invalid]="getFieldError('firstName') ? 'true' : null"
                [attr.aria-describedby]="getFieldError('firstName') ? 'firstName-error' : null"
                class="w-full h-10 px-3 rounded-input border border-border bg-white text-text-main text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
              />
              <p *ngIf="getFieldError('firstName')" id="firstName-error" role="alert" class="text-xs text-danger mt-1">{{ getFieldError('firstName') }}</p>
            </div>
            <div>
              <label for="lastName" class="block text-xs font-semibold text-text-main mb-1">Last Name <span class="text-danger">*</span></label>
              <input
                id="lastName"
                type="text"
                [(ngModel)]="form.lastName"
                name="lastName"
                required
                placeholder="e.g. Tesfaye"
                [attr.aria-invalid]="getFieldError('lastName') ? 'true' : null"
                [attr.aria-describedby]="getFieldError('lastName') ? 'lastName-error' : null"
                class="w-full h-10 px-3 rounded-input border border-border bg-white text-text-main text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
              />
              <p *ngIf="getFieldError('lastName')" id="lastName-error" role="alert" class="text-xs text-danger mt-1">{{ getFieldError('lastName') }}</p>
            </div>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label for="dateOfBirth" class="block text-xs font-semibold text-text-main mb-1">Date of Birth <span class="text-danger">*</span></label>
              <input
                id="dateOfBirth"
                type="date"
                [(ngModel)]="form.dateOfBirth"
                name="dateOfBirth"
                required
                [max]="today"
                [attr.aria-invalid]="(getFieldError('dateOfBirth') || form.dateOfBirth > today) ? 'true' : null"
                [attr.aria-describedby]="getFieldError('dateOfBirth') ? 'dateOfBirth-error' : (form.dateOfBirth > today ? 'dateOfBirth-future-error' : null)"
                class="w-full h-10 px-3 rounded-input border border-border bg-white text-text-main text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
              />
              <p *ngIf="form.dateOfBirth > today" id="dateOfBirth-future-error" role="alert" class="text-xs text-danger mt-1">Date of birth cannot be in the future.</p>
              <p *ngIf="getFieldError('dateOfBirth')" id="dateOfBirth-error" role="alert" class="text-xs text-danger mt-1">{{ getFieldError('dateOfBirth') }}</p>
            </div>
            <div>
              <label for="gender" class="block text-xs font-semibold text-text-main mb-1">Gender <span class="text-danger">*</span></label>
              <select
                id="gender"
                [(ngModel)]="form.gender"
                name="gender"
                required
                [attr.aria-invalid]="getFieldError('gender') ? 'true' : null"
                [attr.aria-describedby]="getFieldError('gender') ? 'gender-error' : null"
                class="w-full h-10 px-3 rounded-input border border-border bg-white text-text-main text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
              >
                <option value="Male">Male</option>
                <option value="Female">Female</option>
                <option value="Other">Other</option>
              </select>
              <p *ngIf="getFieldError('gender')" id="gender-error" role="alert" class="text-xs text-danger mt-1">{{ getFieldError('gender') }}</p>
            </div>
          </div>
        </section>

        <!-- SECTION 2: Enrollment & Classroom Placement -->
        <section class="bg-surface-card rounded-card p-6 border border-border shadow-card space-y-4">
          <div class="flex items-center gap-2 pb-3 border-b border-border">
            <span class="w-6 h-6 rounded-full bg-primary-soft text-primary font-bold text-xs flex items-center justify-center">2</span>
            <div>
              <h2 class="text-sm font-bold text-text-main">Enrollment & Classroom Placement</h2>
              <p class="text-xs text-text-muted">Classroom assignment and tuition billing plan.</p>
            </div>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label for="classroomId" class="block text-xs font-semibold text-text-main mb-1">Classroom <span class="text-danger">*</span></label>
              <select
                id="classroomId"
                [(ngModel)]="form.classroomId"
                name="classroomId"
                required
                [attr.aria-invalid]="getFieldError('classroomId') ? 'true' : null"
                [attr.aria-describedby]="getFieldError('classroomId') ? 'classroomId-error' : null"
                class="w-full h-10 px-3 rounded-input border border-border bg-white text-text-main text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
              >
                <option value="" disabled>Select classroom</option>
                <option *ngFor="let c of classrooms" [value]="c.id">
                  {{ c.name }} (Capacity: {{ c.capacity }})
                </option>
              </select>
              <p *ngIf="getFieldError('classroomId')" id="classroomId-error" role="alert" class="text-xs text-danger mt-1">{{ getFieldError('classroomId') }}</p>
            </div>

            <div>
              <label for="enrollmentDate" class="block text-xs font-semibold text-text-main mb-1">Enrollment Date <span class="text-danger">*</span></label>
              <input
                id="enrollmentDate"
                type="date"
                [(ngModel)]="form.enrollmentDate"
                name="enrollmentDate"
                required
                [attr.aria-invalid]="getFieldError('enrollmentDate') ? 'true' : null"
                [attr.aria-describedby]="getFieldError('enrollmentDate') ? 'enrollmentDate-error' : null"
                class="w-full h-10 px-3 rounded-input border border-border bg-white text-text-main text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
              />
              <p *ngIf="getFieldError('enrollmentDate')" id="enrollmentDate-error" role="alert" class="text-xs text-danger mt-1">{{ getFieldError('enrollmentDate') }}</p>
            </div>
          </div>

          <div>
            <label for="feePlanId" class="block text-xs font-semibold text-text-main mb-1">Tuition Fee Plan</label>
            <select
              id="feePlanId"
              [(ngModel)]="form.feePlanId"
              name="feePlanId"
              class="w-full h-10 px-3 rounded-input border border-border bg-white text-text-main text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
            >
              <option [ngValue]="null">No tuition plan assigned</option>
              <option *ngFor="let fp of feePlans" [value]="fp.id">
                {{ fp.name }} - ETB {{ fp.monthlyAmount | number:'1.2-2' }}/month
              </option>
            </select>
          </div>
        </section>

        <!-- SECTION 3: Health & Special Needs -->
        <section class="bg-surface-card rounded-card p-6 border border-border shadow-card space-y-4">
          <div class="flex items-center gap-2 pb-3 border-b border-border">
            <span class="w-6 h-6 rounded-full bg-primary-soft text-primary font-bold text-xs flex items-center justify-center">3</span>
            <div>
              <h2 class="text-sm font-bold text-text-main">Health, Dietary & Medical Notes</h2>
              <p class="text-xs text-text-muted">Critical safety instructions for staff daily care.</p>
            </div>
          </div>

          <div>
            <label class="block text-xs font-semibold text-text-main mb-1">Known Allergies</label>
            <input
              type="text"
              [(ngModel)]="form.allergies"
              name="allergies"
              placeholder="e.g. Peanuts, Dairy, Penicillin (leave empty if none)"
              class="w-full h-10 px-3 rounded-input border border-border bg-white text-text-main text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
            />
            <p class="text-xs text-text-muted mt-1">If entered, this child will receive a high-visibility allergy badge on staff rosters.</p>
          </div>

          <div class="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div>
              <label class="block text-xs font-semibold text-text-main mb-1">Dietary Notes</label>
              <textarea
                [(ngModel)]="form.dietaryNotes"
                name="dietaryNotes"
                rows="3"
                placeholder="e.g. Vegetarian, no sugar, lactose-free milk"
                class="w-full p-3 rounded-input border border-border bg-white text-text-main text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
              ></textarea>
            </div>

            <div>
              <label class="block text-xs font-semibold text-text-main mb-1">Medical / Emergency Notes</label>
              <textarea
                [(ngModel)]="form.medicalNotes"
                name="medicalNotes"
                rows="3"
                placeholder="e.g. Inhaler stored in infant room cabinet"
                class="w-full p-3 rounded-input border border-border bg-white text-text-main text-sm focus:outline-none focus:ring-2 focus:ring-primary/20 focus:border-primary transition"
              ></textarea>
            </div>
          </div>
        </section>

        <!-- Form Actions -->
        <div class="flex items-center justify-end gap-3 pt-4">
          <a
            [routerLink]="isEdit ? ['/admin/children', childId] : '/admin/children'"
            class="px-5 py-2.5 rounded-input border border-border text-sm font-medium text-text-main hover:bg-slate-100 transition-colors"
          >
            Cancel
          </a>
          <app-button
            [loading]="submitting"
            type="submit"
            variant="primary"
          >
            {{ isEdit ? 'Save Changes' : 'Create Enrollment' }}
          </app-button>
        </div>
      </form>
    </div>
  `
})
export class ChildFormComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private childrenService = inject(ChildrenService);
  private classroomService = inject(ClassroomService);
  private billingService = inject(BillingService);
  private toast = inject(ToastService);

  isEdit = false;
  childId: string | null = null;
  childHasPhoto = false;
  childPhotoUpdatedAt: string | null = null;
  submitting = false;
  today = new Intl.DateTimeFormat('en-CA', { timeZone: 'Africa/Addis_Ababa' }).format(new Date());

  classrooms: ClassroomSummaryDto[] = [];
  feePlans: FeePlanDto[] = [];
  serverErrors: Record<string, string[]> = {};

  form: CreateChildRequest = {
    firstName: '',
    lastName: '',
    dateOfBirth: '',
    gender: 'Male',
    enrollmentDate: this.today,
    classroomId: '',
    feePlanId: null,
    allergies: '',
    dietaryNotes: '',
    medicalNotes: ''
  };

  getFieldError(field: string): string | null {
    if (this.serverErrors[field] && this.serverErrors[field].length > 0) {
      return this.serverErrors[field][0];
    }
    const match = Object.keys(this.serverErrors).find(k => k.toLowerCase() === field.toLowerCase());
    return match && this.serverErrors[match].length > 0 ? this.serverErrors[match][0] : null;
  }

  ngOnInit(): void {
    this.loadReferenceData();
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.isEdit = true;
      this.childId = id;
      this.loadChild(id);
    }
  }

  loadReferenceData(): void {
    this.classroomService.getClassrooms().subscribe({
      next: (res) => (this.classrooms = res),
      error: () => this.toast.error('Failed to load classrooms')
    });

    this.billingService.getFeePlans().subscribe({
      next: (res) => (this.feePlans = res.filter(fp => fp.isActive)),
      error: () => this.toast.error('Failed to load fee plans')
    });
  }

  loadChild(id: string): void {
    this.childrenService.getChild(id).subscribe({
      next: (child) => {
        this.childHasPhoto = child.hasPhoto ?? false;
        this.childPhotoUpdatedAt = child.photoUpdatedAt ?? null;
        this.form = {
          firstName: child.firstName,
          lastName: child.lastName,
          dateOfBirth: child.dateOfBirth,
          gender: child.gender,
          enrollmentDate: child.enrollmentDate,
          classroomId: child.classroomId,
          feePlanId: child.feePlanId || null,
          allergies: child.allergies || '',
          dietaryNotes: child.dietaryNotes || '',
          medicalNotes: child.medicalNotes || ''
        };
      },
      error: () => {
        this.toast.error('Failed to load child profile');
        this.router.navigate(['/admin/children']);
      }
    });
  }

  onPhotoChanged(event: { hasPhoto: boolean; photoUpdatedAt: string | null }): void {
    this.childHasPhoto = event.hasPhoto;
    this.childPhotoUpdatedAt = event.photoUpdatedAt;
  }

  onSubmit(): void {
    this.serverErrors = {};

    if (!this.form.firstName || !this.form.lastName || !this.form.dateOfBirth || !this.form.classroomId) {
      this.toast.warning('Please fill in all required fields');
      return;
    }

    if (this.form.dateOfBirth > this.today) {
      this.toast.error('Date of birth cannot be in the future');
      return;
    }

    this.submitting = true;

    if (this.isEdit && this.childId) {
      this.childrenService.updateChild(this.childId, this.form).subscribe({
        next: (res) => {
          this.submitting = false;
          this.toast.success('Child profile updated successfully');
          this.router.navigate(['/admin/children', res.id]);
        },
        error: (err) => {
          this.submitting = false;
          this.serverErrors = err.error?.errors || {};
          const msg = err.error?.detail || err.error?.title || 'Failed to update child';
          this.toast.error(msg);
        }
      });
    } else {
      this.childrenService.createChild(this.form).subscribe({
        next: (res) => {
          this.submitting = false;
          this.toast.success('Child enrolled successfully');
          this.router.navigate(['/admin/children', res.id]);
        },
        error: (err) => {
          this.submitting = false;
          this.serverErrors = err.error?.errors || {};
          const msg = err.error?.detail || err.error?.title || 'Failed to create child';
          this.toast.error(msg);
        }
      });
    }
  }
}
