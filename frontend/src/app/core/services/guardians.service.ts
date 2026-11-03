import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface GuardianDto {
  id: string;
  firstName: string;
  lastName: string;
  phone: string;
  email: string | null;
  address: string | null;
  hasAccount: boolean;
  hasPhoto?: boolean;
  photoUpdatedAt?: string | null;
  children: {
    childId: string;
    childName: string;
    relationship: string;
    canPickUp: boolean;
    isPayer: boolean;
    isEmergencyContact: boolean;
  }[];
}

export interface CreateGuardianRequest {
  firstName: string;
  lastName: string;
  phone: string;
  email?: string | null;
  address?: string | null;
  createAccount?: boolean;
}

export interface UpdateGuardianRequest {
  firstName: string;
  lastName: string;
  phone: string;
  email?: string | null;
  address?: string | null;
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
export class GuardiansService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiBaseUrl}/guardians`;

  getGuardians(search?: string, page = 1, pageSize = 50): Observable<PagedResult<GuardianDto>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (search && search.trim()) {
      params = params.set('search', search.trim());
    }

    return this.http.get<PagedResult<GuardianDto>>(this.baseUrl, { params });
  }

  getGuardian(id: string): Observable<GuardianDto> {
    return this.http.get<GuardianDto>(`${this.baseUrl}/${id}`);
  }

  createGuardian(data: CreateGuardianRequest): Observable<{ guardian: GuardianDto; temporaryPassword?: string }> {
    return this.http.post<{ guardian: GuardianDto; temporaryPassword?: string }>(this.baseUrl, data);
  }

  updateGuardian(id: string, data: UpdateGuardianRequest): Observable<GuardianDto> {
    return this.http.put<GuardianDto>(`${this.baseUrl}/${id}`, data);
  }

  deleteGuardian(id: string): Observable<{ success: boolean; message: string }> {
    return this.http.delete<{ success: boolean; message: string }>(`${this.baseUrl}/${id}`);
  }

  createAccount(id: string, email: string): Observable<{ temporaryPassword?: string }> {
    return this.http.post<{ temporaryPassword?: string }>(`${this.baseUrl}/${id}/account`, { email });
  }

  resetPassword(id: string): Observable<{ temporaryPassword?: string }> {
    return this.http.post<{ temporaryPassword?: string }>(`${this.baseUrl}/${id}/reset-password`, {});
  }
}

