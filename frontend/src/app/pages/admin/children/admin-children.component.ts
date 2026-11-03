import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ChildrenService, ChildListItemDto } from '../../../core/services/children.service';
import { ClassroomService, ClassroomDto } from '../../../core/services/classroom.service';
import { ToastService } from '../../../core/services/toast.service';
import { BadgeComponent } from '../../../shared/components/badge/badge.component';
import { ChildAllergyFlagComponent } from '../../../shared/components/child-allergy-flag/child-allergy-flag.component';
import { AvatarComponent } from '../../../shared/components/avatar/avatar.component';

@Component({
  selector: 'app-admin-children',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, BadgeComponent, ChildAllergyFlagComponent, AvatarComponent],
  template: `
    <div class="space-y-6">
      <!-- Header -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 class="text-2xl font-bold text-text-main tracking-tight">Children Directory</h1>
          <p class="text-sm text-text-muted mt-1">Manage enrollments, classrooms, guardian contacts, and medical profiles</p>
        </div>
        <a
          routerLink="/admin/children/new"
          class="inline-flex items-center gap-2 px-4 py-2 rounded-input bg-primary text-white text-sm font-semibold hover:bg-primary-hover shadow-sm transition-colors self-start sm:self-auto"
        >
          <svg class="w-4 h-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
          </svg>
          Add Child
        </a>
      </div>

      <!-- Filters Row -->
      <div class="bg-surface-card rounded-card border border-border p-4 shadow-card flex flex-col sm:flex-row gap-3">
        <div class="flex-1">
          <input
            type="text"
            [(ngModel)]="searchQuery"
            (ngModelChange)="onFilterChange()"
            aria-label="Search children"
            placeholder="Search by first or last name..."
            class="w-full px-3.5 py-2 rounded-input border border-border bg-white text-sm focus:ring-2 focus:ring-primary focus:outline-none"
          />
        </div>
        <div class="w-full sm:w-48">
          <select
            [(ngModel)]="selectedClassroomId"
            (ngModelChange)="onFilterChange()"
            aria-label="Filter by classroom"
            class="w-full px-3 py-2 rounded-input border border-border bg-white text-sm focus:ring-2 focus:ring-primary focus:outline-none"
          >
            <option value="">All Classrooms</option>
            <option *ngFor="let c of classrooms" [value]="c.id">{{ c.name }}</option>
          </select>
        </div>
        <div class="w-full sm:w-40">
          <select
            [(ngModel)]="selectedStatus"
            (ngModelChange)="onFilterChange()"
            class="w-full px-3 py-2 rounded-input border border-border bg-white text-sm focus:ring-2 focus:ring-primary focus:outline-none"
          >
            <option value="">All Statuses</option>
            <option value="Active">Active</option>
            <option value="Waitlisted">Waitlisted</option>
            <option value="Withdrawn">Withdrawn</option>
          </select>
        </div>
      </div>

      <!-- Children Table -->
      <div class="bg-surface-card rounded-card border border-border shadow-card overflow-hidden">
        <div *ngIf="isLoading" class="p-12 text-center text-text-muted text-sm">
          Loading children directory...
        </div>

        <div *ngIf="!isLoading && children.length === 0" class="p-12 text-center text-text-muted text-sm">
          No children found matching your search criteria.
        </div>

        <div *ngIf="!isLoading && children.length > 0" class="overflow-x-auto">
          <table class="w-full text-left text-sm">
            <thead class="bg-slate-50 border-b border-border text-xs font-semibold text-text-muted uppercase">
              <tr>
                <th class="px-6 py-3.5">Child</th>
                <th class="px-6 py-3.5">Classroom</th>
                <th class="px-6 py-3.5">Fee Plan</th>
                <th class="px-6 py-3.5">Status</th>
                <th class="px-6 py-3.5">Allergies & Medical</th>
                <th class="px-6 py-3.5 text-right">Actions</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-border">
              <tr *ngFor="let child of children" class="hover:bg-slate-50/50 transition-colors">
                <td class="px-6 py-4">
                  <div class="flex items-center gap-3">
                    <app-avatar
                      [kind]="'children'"
                      [id]="child.id"
                      [name]="child.firstName + ' ' + child.lastName"
                      [hasPhoto]="child.hasPhoto ?? false"
                      [photoUpdatedAt]="child.photoUpdatedAt"
                      [size]="32"
                    ></app-avatar>
                    <div>
                      <a [routerLink]="['/admin/children', child.id]" class="font-bold text-text-main hover:text-primary transition-colors">
                        {{ child.firstName }} {{ child.lastName }}
                      </a>
                      <p class="text-xs text-text-muted">DOB: {{ child.dateOfBirth }}</p>
                    </div>
                  </div>
                </td>
                <td class="px-6 py-4 text-sm font-medium text-text-main">
                  {{ child.classroomName }}
                </td>
                <td class="px-6 py-4 text-sm text-text-muted">
                  {{ child.feePlanName || 'No plan' }}
                </td>
                <td class="px-6 py-4">
                  <app-badge [status]="child.status" [text]="child.status"></app-badge>
                </td>
                <td class="px-6 py-4 text-xs">
                  <app-child-allergy-flag [allergies]="child.allergies" class="mb-1 mr-1"></app-child-allergy-flag>
                  <div *ngIf="child.medicalNotes" class="inline-flex items-center gap-1 px-2 py-0.5 rounded bg-warning-soft text-warning font-medium">
                    <svg class="w-3 h-3 shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M12 4v16m8-8H4" />
                    </svg>
                    <span>{{ child.medicalNotes }}</span>
                  </div>
                  <span *ngIf="!child.allergies && !child.medicalNotes" class="text-text-muted">None</span>
                </td>
                <td class="px-6 py-4 text-right">
                  <div class="flex items-center justify-end gap-2">
                    <a
                      [routerLink]="['/admin/children', child.id]"
                      class="px-3 py-1.5 rounded-input border border-border text-xs font-medium text-text-main hover:bg-slate-100 transition-colors"
                    >
                      View
                    </a>
                    <a
                      [routerLink]="['/admin/children', child.id, 'edit']"
                      class="px-3 py-1.5 rounded-input border border-border text-xs font-medium text-text-muted hover:bg-slate-100 transition-colors"
                    >
                      Edit
                    </a>
                    <button
                      type="button"
                      (click)="deleteChild(child)"
                      class="px-2.5 py-1.5 rounded-input border border-border text-xs font-medium text-danger hover:bg-danger-soft transition-colors"
                    >
                      Withdraw
                    </button>
                  </div>
                </td>
              </tr>
            </tbody>
          </table>
        </div>

        <!-- Footer / Count -->
        <div class="px-6 py-3 bg-slate-50 border-t border-border flex items-center justify-between text-xs text-text-muted">
          <span>Showing {{ children.length }} children</span>
          <span>Page {{ page }} of {{ totalPages || 1 }}</span>
        </div>
      </div>
    </div>
  `
})
export class AdminChildrenComponent implements OnInit {
  private childrenService = inject(ChildrenService);
  private classroomService = inject(ClassroomService);
  private toastService = inject(ToastService);

  children: ChildListItemDto[] = [];
  classrooms: ClassroomDto[] = [];
  isLoading = false;

  searchQuery = '';
  selectedClassroomId = '';
  selectedStatus = '';
  page = 1;
  totalPages = 1;

  ngOnInit(): void {
    this.loadClassrooms();
    this.loadChildren();
  }

  loadClassrooms(): void {
    this.classroomService.getClassrooms().subscribe({
      next: (cls) => this.classrooms = cls
    });
  }

  loadChildren(): void {
    this.isLoading = true;
    this.childrenService.getChildren({
      search: this.searchQuery || undefined,
      classroomId: this.selectedClassroomId || undefined,
      status: this.selectedStatus || undefined,
      page: this.page,
      pageSize: 50
    }).subscribe({
      next: (res) => {
        this.children = res.items;
        this.totalPages = res.totalPages;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
        this.toastService.error('Failed to load children');
      }
    });
  }

  onFilterChange(): void {
    this.page = 1;
    this.loadChildren();
  }

  deleteChild(child: ChildListItemDto): void {
    if (!confirm(`Are you sure you want to withdraw/remove ${child.firstName} ${child.lastName}? This will archive the child while preserving billing and incident records.`)) return;

    this.childrenService.deleteChild(child.id).subscribe({
      next: () => {
        this.toastService.success('Child withdrawn successfully');
        this.loadChildren();
      },
      error: (err) => {
        this.toastService.error(err.error?.detail || err.error?.title || 'Failed to remove child');
      }
    });
  }
}

