import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ClassroomDto {
  id: string;
  name: string;
  minAgeMonths: number;
  maxAgeMonths: number;
  capacity: number;
  maxChildrenPerStaff: number;
  description?: string;
  isActive: boolean;
}

export interface CreateClassroomRequest {
  name: string;
  description?: string;
  minAgeMonths: number;
  maxAgeMonths: number;
  capacity: number;
  maxChildrenPerStaff: number;
}

export interface UpdateClassroomRequest {
  name: string;
  description?: string;
  minAgeMonths: number;
  maxAgeMonths: number;
  capacity: number;
  maxChildrenPerStaff: number;
  isActive: boolean;
}

export type ClassroomSummaryDto = ClassroomDto;

@Injectable({
  providedIn: 'root'
})
export class ClassroomService {
  private http = inject(HttpClient);

  getClassrooms(): Observable<ClassroomDto[]> {
    return this.http.get<ClassroomDto[]>(`${environment.apiUrl}/classrooms`);
  }

  getClassroom(id: string): Observable<ClassroomDto> {
    return this.http.get<ClassroomDto>(`${environment.apiUrl}/classrooms/${id}`);
  }

  createClassroom(data: CreateClassroomRequest): Observable<ClassroomDto> {
    return this.http.post<ClassroomDto>(`${environment.apiUrl}/classrooms`, data);
  }

  updateClassroom(id: string, data: UpdateClassroomRequest): Observable<ClassroomDto> {
    return this.http.put<ClassroomDto>(`${environment.apiUrl}/classrooms/${id}`, data);
  }
}
