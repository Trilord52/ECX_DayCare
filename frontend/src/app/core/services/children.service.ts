import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ChildListItemDto {
  id: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  enrollmentDate: string;
  status: 'Active' | 'Withdrawn' | 'Waitlisted' | string;
  classroomId: string;
  classroomName: string;
  feePlanId?: string | null;
  feePlanName?: string | null;
  allergies?: string | null;
  dietaryNotes?: string | null;
  medicalNotes?: string | null;
  hasPhoto?: boolean;
  photoUpdatedAt?: string | null;
}

export type ChildDto = ChildListItemDto;

export interface ChildGuardianDto {
  id?: string;
  guardianId: string;
  firstName?: string;
  lastName?: string;
  fullName?: string;
  phone: string;
  email?: string | null;
  relationship: string;
  isPrimaryContact: boolean;
  canPickUp: boolean;
  isEmergencyContact: boolean;
  isPayer: boolean;
  hasPhoto?: boolean;
  photoUpdatedAt?: string | null;
}

export interface ChildDetailDto extends ChildListItemDto {
  guardians: ChildGuardianDto[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface CreateChildRequest {
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  gender: string;
  enrollmentDate: string;
  classroomId: string;
  feePlanId?: string | null;
  allergies?: string | null;
  dietaryNotes?: string | null;
  medicalNotes?: string | null;
}

export interface GuardianDto {
  id: string;
  firstName: string;
  lastName: string;
  phone: string;
  email?: string | null;
  address?: string | null;
  userId?: string | null;
}

export interface LinkGuardianRequest {
  guardianId: string;
  relationship: string;
  isPrimaryContact: boolean;
  canPickUp: boolean;
  isEmergencyContact: boolean;
  isPayer: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ChildrenService {
  private http = inject(HttpClient);

  getChildren(options?: { search?: string; classroomId?: string; status?: string; page?: number; pageSize?: number }): Observable<PagedResult<ChildListItemDto>> {
    let params = new HttpParams();
    if (options?.search) params = params.set('search', options.search);
    if (options?.classroomId) params = params.set('classroomId', options.classroomId);
    if (options?.status) params = params.set('status', options.status);
    if (options?.page) params = params.set('page', options.page.toString());
    if (options?.pageSize) params = params.set('pageSize', options.pageSize.toString());

    return this.http.get<PagedResult<ChildListItemDto>>(`${environment.apiUrl}/children`, { params });
  }

  getChild(id: string): Observable<ChildDetailDto> {
    return this.http.get<ChildDetailDto>(`${environment.apiUrl}/children/${id}`);
  }

  createChild(req: CreateChildRequest): Observable<ChildListItemDto> {
    return this.http.post<ChildListItemDto>(`${environment.apiUrl}/children`, req);
  }

  updateChild(id: string, req: CreateChildRequest): Observable<ChildListItemDto> {
    return this.http.put<ChildListItemDto>(`${environment.apiUrl}/children/${id}`, req);
  }

  getChildGuardians(childId: string): Observable<ChildGuardianDto[]> {
    return this.http.get<ChildGuardianDto[]>(`${environment.apiUrl}/children/${childId}/guardians`);
  }

  linkGuardian(childId: string, req: LinkGuardianRequest): Observable<ChildGuardianDto> {
    return this.http.post<ChildGuardianDto>(`${environment.apiUrl}/children/${childId}/guardians`, req);
  }

  getAllGuardians(): Observable<GuardianDto[]> {
    return this.http.get<PagedResult<GuardianDto>>(`${environment.apiUrl}/guardians?pageSize=100`).pipe(
      map(res => res?.items || (Array.isArray(res) ? res : []))
    );
  }

  unlinkGuardian(childId: string, guardianId: string): Observable<{ success: boolean }> {
    return this.http.delete<{ success: boolean }>(`${environment.apiUrl}/children/${childId}/guardians/${guardianId}`);
  }

  deleteChild(id: string): Observable<{ success: boolean }> {
    return this.http.delete<{ success: boolean }>(`${environment.apiUrl}/children/${id}`);
  }
}


