import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type IncidentType = 'Injury' | 'Illness' | 'Behavioral' | 'Other';
export type IncidentSeverity = 'Minor' | 'Moderate' | 'Serious';

export interface IncidentListItemDto {
  id: string;
  childId: string;
  childName: string;
  occurredAt: string;
  type: IncidentType;
  severity: IncidentSeverity;
  description: string;
  isParentNotified: boolean;
  isAcknowledged: boolean;
  acknowledgedAt?: string | null;
  reportedByName: string;
}

export interface IncidentDetailDto {
  id: string;
  childId: string;
  childName: string;
  classroomId?: string | null;
  classroomName?: string | null;
  occurredAt: string;
  type: IncidentType;
  severity: IncidentSeverity;
  description: string;
  actionTaken?: string | null;
  reportedByUserId: string;
  reportedByName: string;
  parentNotifiedAt?: string | null;
  parentNotifiedByName?: string | null;
  acknowledgedAt?: string | null;
  acknowledgedByName?: string | null;
  createdAt: string;
}

export interface CreateIncidentRequest {
  childId: string;
  occurredAt: string;
  type: IncidentType;
  severity: IncidentSeverity;
  description: string;
  classroomId?: string | null;
  actionTaken?: string | null;
  parentNotifiedAt?: string | null;
  parentNotifiedByName?: string | null;
}

export interface UpdateIncidentRequest {
  occurredAt: string;
  type: IncidentType;
  severity: IncidentSeverity;
  description: string;
  actionTaken?: string | null;
  parentNotifiedAt?: string | null;
  parentNotifiedByName?: string | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class IncidentsService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/incidents`;

  getIncidents(filters?: {
    childId?: string;
    classroomId?: string;
    severity?: IncidentSeverity;
    acknowledged?: boolean;
    page?: number;
    pageSize?: number;
  }): Observable<PagedResult<IncidentListItemDto>> {
    let params = new HttpParams();
    if (filters?.page) params = params.set('page', filters.page.toString());
    if (filters?.pageSize) params = params.set('pageSize', filters.pageSize.toString());
    if (filters?.childId) params = params.set('childId', filters.childId);
    if (filters?.classroomId) params = params.set('classroomId', filters.classroomId);
    if (filters?.severity) params = params.set('severity', filters.severity);
    if (filters?.acknowledged !== undefined) params = params.set('acknowledged', filters.acknowledged.toString());

    return this.http.get<PagedResult<IncidentListItemDto>>(this.baseUrl, { params });
  }

  getIncident(id: string): Observable<IncidentDetailDto> {
    return this.http.get<IncidentDetailDto>(`${this.baseUrl}/${id}`);
  }

  createIncident(data: CreateIncidentRequest): Observable<IncidentDetailDto> {
    return this.http.post<IncidentDetailDto>(this.baseUrl, data);
  }

  updateIncident(id: string, data: UpdateIncidentRequest): Observable<IncidentDetailDto> {
    return this.http.put<IncidentDetailDto>(`${this.baseUrl}/${id}`, data);
  }

  notifyParent(id: string): Observable<IncidentDetailDto> {
    return this.http.post<IncidentDetailDto>(`${this.baseUrl}/${id}/notify-parent`, {});
  }

  acknowledge(id: string): Observable<IncidentDetailDto> {
    return this.http.post<IncidentDetailDto>(`${this.baseUrl}/${id}/acknowledge`, {});
  }
}

