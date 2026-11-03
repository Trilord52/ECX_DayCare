import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type AnnouncementAudience = 'All' | 'Staff' | 'Parents' | 'Classroom';

export interface AnnouncementDto {
  id: string;
  title: string;
  body: string;
  audience: AnnouncementAudience;
  classroomId?: string | null;
  classroomName?: string | null;
  authorName: string;
  createdAt: string;
  expiresAt?: string | null;
}

export interface CreateAnnouncementRequest {
  title: string;
  body: string;
  audience: AnnouncementAudience;
  classroomId?: string | null;
  expiresAt?: string | null;
}

export interface UpdateAnnouncementRequest {
  title: string;
  body: string;
  audience: AnnouncementAudience;
  classroomId?: string | null;
  expiresAt?: string | null;
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
export class AnnouncementsService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/announcements`;

  getAnnouncements(page = 1, pageSize = 50): Observable<PagedResult<AnnouncementDto>> {
    const params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    return this.http.get<PagedResult<AnnouncementDto>>(this.baseUrl, { params });
  }

  createAnnouncement(data: CreateAnnouncementRequest): Observable<AnnouncementDto> {
    return this.http.post<AnnouncementDto>(this.baseUrl, data);
  }

  updateAnnouncement(id: string, data: UpdateAnnouncementRequest): Observable<AnnouncementDto> {
    return this.http.put<AnnouncementDto>(`${this.baseUrl}/${id}`, data);
  }

  deleteAnnouncement(id: string): Observable<{ success: boolean; message: string }> {
    return this.http.delete<{ success: boolean; message: string }>(`${this.baseUrl}/${id}`);
  }
}

