import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface StaffDto {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  jobTitle: string;
  hireDate: string;
  isActive: boolean;
  hasPhoto?: boolean;
  photoUpdatedAt?: string | null;
  classrooms: {
    classroomId: string;
    classroomName: string;
  }[];
}

export interface CreateStaffRequest {
  firstName: string;
  lastName: string;
  email: string;
  phone: string;
  jobTitle: string;
  hireDate: string;
  classroomIds?: string[];
}

export interface UpdateStaffRequest {
  firstName: string;
  lastName: string;
  phone: string;
  jobTitle: string;
  hireDate: string;
  classroomIds?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class StaffService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiBaseUrl}/staff`;
  private classroomsUrl = `${environment.apiBaseUrl}/classrooms`;

  getStaff(): Observable<StaffDto[]> {
    return this.http.get<StaffDto[]>(this.baseUrl);
  }

  getStaffById(id: string): Observable<StaffDto> {
    return this.http.get<StaffDto>(`${this.baseUrl}/${id}`);
  }

  createStaff(data: CreateStaffRequest): Observable<{ staff: StaffDto; temporaryPassword?: string }> {
    return this.http.post<{ staff: StaffDto; temporaryPassword?: string }>(this.baseUrl, data);
  }

  updateStaff(id: string, data: UpdateStaffRequest): Observable<StaffDto> {
    return this.http.put<StaffDto>(`${this.baseUrl}/${id}`, data);
  }

  deleteStaff(id: string): Observable<{ success: boolean; message: string }> {
    return this.http.delete<{ success: boolean; message: string }>(`${this.baseUrl}/${id}`);
  }

  resetPassword(id: string): Observable<{ temporaryPassword?: string }> {
    return this.http.post<{ temporaryPassword?: string }>(`${this.baseUrl}/${id}/reset-password`, {});
  }

  assignClassroomStaff(classroomId: string, staffIds: string[]): Observable<any> {
    return this.http.put(`${this.classroomsUrl}/${classroomId}/staff`, { staffIds });
  }
}

