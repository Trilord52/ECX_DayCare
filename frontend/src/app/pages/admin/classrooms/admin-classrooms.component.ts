import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ClassroomService, ClassroomDto, CreateClassroomRequest, UpdateClassroomRequest } from '../../../core/services/classroom.service';
import { ToastService } from '../../../core/services/toast.service';
import { ButtonComponent } from '../../../shared/components/button/button.component';
import { DialogComponent } from '../../../shared/components/dialog/dialog.component';

@Component({
  selector: 'app-admin-classrooms',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent, DialogComponent],
  template: `
    <div class="space-y-6">
      <!-- Header -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold tracking-tight text-text-main">Classrooms & Capacity</h1>
          <p class="text-sm text-text-muted mt-0.5">Configure classroom age brackets, max capacities, and regulatory staff-to-child ratio limits</p>
        </div>
        <div class="flex items-center gap-3">
          <app-button variant="primary" (click)="openCreateModal()">
            + Add Classroom
          </app-button>
        </div>
      </div>

      <!-- Loading / Empty -->
      <div *ngIf="loading" class="p-12 text-center text-text-muted text-sm">
        Loading classrooms...
      </div>

      <div *ngIf="!loading && classrooms.length === 0" class="p-12 text-center bg-surface-card rounded-card border border-border">
        <p class="text-sm font-semibold text-text-main">No classrooms configured</p>
        <p class="text-xs text-text-muted mt-1">Add your daycare's first classroom room to begin enrolling children.</p>
      </div>

      <!-- Classrooms Card Grid -->
      <div *ngIf="!loading && classrooms.length > 0" class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-5">
        <article
          *ngFor="let room of classrooms"
          class="bg-surface-card rounded-card border border-border p-5 shadow-card hover:shadow-md transition flex flex-col justify-between"
        >
          <div class="space-y-4">
            <!-- Card Header -->
            <div class="flex items-start justify-between gap-2">
              <div>
                <h3 class="text-base font-bold text-text-main">{{ room.name }}</h3>
                <span class="text-xs text-text-muted">
                  Ages: {{ room.minAgeMonths }} - {{ room.maxAgeMonths }} months
                  ({{ getAgeRangeDesc(room.minAgeMonths, room.maxAgeMonths) }})
                </span>
              </div>
              <span
                [class.bg-success-soft]="room.isActive"
                [class.text-success]="room.isActive"
                [class.bg-slate-100]="!room.isActive"
                [class.text-text-muted]="!room.isActive"
                class="px-2 py-0.5 rounded-full text-xs font-bold"
              >
                {{ room.isActive ? 'Active' : 'Inactive' }}
              </span>
            </div>

            <p *ngIf="room.description" class="text-xs text-text-muted line-clamp-2">
              {{ room.description }}
            </p>

            <!-- Metrics Box -->
            <div class="p-3 bg-slate-50 border border-border rounded-lg text-xs space-y-2">
              <div class="flex justify-between items-center">
                <span class="text-text-muted">Max Capacity:</span>
                <strong class="font-bold text-text-main">{{ room.capacity }} children</strong>
              </div>
              <div class="flex justify-between items-center">
                <span class="text-text-muted">Max Children / Staff:</span>
                <strong class="font-bold text-primary">1 : {{ room.maxChildrenPerStaff }}</strong>
              </div>
            </div>
          </div>

          <div class="mt-4 pt-3 border-t border-border flex justify-end">
            <app-button variant="secondary" (click)="openEditModal(room)">
              Edit Classroom
            </app-button>
          </div>
        </article>
      </div>

      <!-- CREATE / EDIT MODAL -->
      <app-dialog
        [(isOpen)]="modalOpen"
        [title]="isEditing ? 'Edit Classroom' : 'Create New Classroom'"
      >
        <div class="space-y-4 text-xs">
          <div>
            <label class="block font-semibold text-text-main mb-1">Classroom Name <span class="text-danger">*</span></label>
            <input
              type="text"
              [(ngModel)]="form.name"
              placeholder="e.g. Toddlers, Preschool Blue"
              class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
            />
          </div>

          <div>
            <label class="block font-semibold text-text-main mb-1">Description</label>
            <textarea
              [(ngModel)]="form.description"
              rows="2"
              placeholder="e.g. Early walking room with dedicated reading nook..."
              class="w-full p-2.5 rounded-input border border-border bg-white text-xs"
            ></textarea>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block font-semibold text-text-main mb-1">Min Age (Months) <span class="text-danger">*</span></label>
              <input
                type="number"
                [(ngModel)]="form.minAgeMonths"
                min="0"
                class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
              />
            </div>
            <div>
              <label class="block font-semibold text-text-main mb-1">Max Age (Months) <span class="text-danger">*</span></label>
              <input
                type="number"
                [(ngModel)]="form.maxAgeMonths"
                min="0"
                class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
              />
            </div>
          </div>

          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block font-semibold text-text-main mb-1">Max Capacity <span class="text-danger">*</span></label>
              <input
                type="number"
                [(ngModel)]="form.capacity"
                min="1"
                placeholder="e.g. 15"
                class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
              />
            </div>
            <div>
              <label class="block font-semibold text-text-main mb-1">Max Kids per Staff <span class="text-danger">*</span></label>
              <input
                type="number"
                [(ngModel)]="form.maxChildrenPerStaff"
                min="1"
                placeholder="e.g. 6"
                class="w-full h-9 px-3 rounded-input border border-border bg-white text-xs"
              />
            </div>
          </div>

          <div *ngIf="isEditing" class="pt-1">
            <label class="flex items-center gap-2 cursor-pointer">
              <input
                type="checkbox"
                [(ngModel)]="form.isActive"
                class="rounded border-border text-primary focus:ring-primary"
              />
              <span class="text-text-main font-medium">Classroom is currently active</span>
            </label>
          </div>
        </div>

        <div dialog-actions class="flex justify-end gap-2">
          <app-button variant="secondary" (click)="modalOpen = false">Cancel</app-button>
          <app-button variant="primary" [loading]="saving" (click)="onSave()">Save Classroom</app-button>
        </div>
      </app-dialog>
    </div>
  `
})
export class AdminClassroomsComponent implements OnInit {
  private classroomService = inject(ClassroomService);
  private toast = inject(ToastService);

  classrooms: ClassroomDto[] = [];
  loading = true;

  modalOpen = false;
  isEditing = false;
  editingId: string | null = null;
  saving = false;

  form: {
    name: string;
    description: string;
    minAgeMonths: number;
    maxAgeMonths: number;
    capacity: number;
    maxChildrenPerStaff: number;
    isActive: boolean;
  } = {
    name: '',
    description: '',
    minAgeMonths: 12,
    maxAgeMonths: 36,
    capacity: 12,
    maxChildrenPerStaff: 6,
    isActive: true
  };

  ngOnInit(): void {
    this.loadClassrooms();
  }

  loadClassrooms(): void {
    this.loading = true;
    this.classroomService.getClassrooms().subscribe({
      next: (res) => {
        this.classrooms = res;
        this.loading = false;
      },
      error: () => {
        this.loading = false;
        this.toast.error('Failed to load classrooms');
      }
    });
  }

  getAgeRangeDesc(min: number, max: number): string {
    const minYears = (min / 12).toFixed(1).replace('.0', '');
    const maxYears = (max / 12).toFixed(1).replace('.0', '');
    return `~${minYears} - ${maxYears} yrs`;
  }

  openCreateModal(): void {
    this.isEditing = false;
    this.editingId = null;
    this.form = {
      name: '',
      description: '',
      minAgeMonths: 12,
      maxAgeMonths: 36,
      capacity: 12,
      maxChildrenPerStaff: 6,
      isActive: true
    };
    this.modalOpen = true;
  }

  openEditModal(room: ClassroomDto): void {
    this.isEditing = true;
    this.editingId = room.id;
    this.form = {
      name: room.name,
      description: room.description || '',
      minAgeMonths: room.minAgeMonths,
      maxAgeMonths: room.maxAgeMonths,
      capacity: room.capacity,
      maxChildrenPerStaff: room.maxChildrenPerStaff,
      isActive: room.isActive
    };
    this.modalOpen = true;
  }

  onSave(): void {
    if (!this.form.name.trim() || this.form.capacity <= 0 || this.form.maxChildrenPerStaff <= 0) {
      this.toast.warning('Name, capacity, and ratio limit are required');
      return;
    }

    if (this.form.minAgeMonths >= this.form.maxAgeMonths) {
      this.toast.warning('Min age must be strictly less than Max age');
      return;
    }

    this.saving = true;
    if (this.isEditing && this.editingId) {
      const req: UpdateClassroomRequest = {
        name: this.form.name,
        description: this.form.description.trim() || undefined,
        minAgeMonths: this.form.minAgeMonths,
        maxAgeMonths: this.form.maxAgeMonths,
        capacity: this.form.capacity,
        maxChildrenPerStaff: this.form.maxChildrenPerStaff,
        isActive: this.form.isActive
      };

      this.classroomService.updateClassroom(this.editingId, req).subscribe({
        next: () => {
          this.saving = false;
          this.modalOpen = false;
          this.toast.success('Classroom updated successfully');
          this.loadClassrooms();
        },
        error: (err) => {
          this.saving = false;
          this.toast.error(err.error?.detail || 'Failed to update classroom');
        }
      });
    } else {
      const req: CreateClassroomRequest = {
        name: this.form.name,
        description: this.form.description.trim() || undefined,
        minAgeMonths: this.form.minAgeMonths,
        maxAgeMonths: this.form.maxAgeMonths,
        capacity: this.form.capacity,
        maxChildrenPerStaff: this.form.maxChildrenPerStaff
      };

      this.classroomService.createClassroom(req).subscribe({
        next: () => {
          this.saving = false;
          this.modalOpen = false;
          this.toast.success('Classroom created successfully');
          this.loadClassrooms();
        },
        error: (err) => {
          this.saving = false;
          this.toast.error(err.error?.detail || 'Failed to create classroom');
        }
      });
    }
  }
}
