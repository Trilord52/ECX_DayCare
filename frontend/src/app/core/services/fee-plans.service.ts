import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface FeePlanDto {
  id: string;
  name: string;
  description?: string | null;
  monthlyAmount: number;
  isActive: boolean;
}

export interface CreateFeePlanRequest {
  name: string;
  description?: string | null;
  monthlyAmount: number;
}

export interface UpdateFeePlanRequest {
  name: string;
  description?: string | null;
  monthlyAmount: number;
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class FeePlansService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/fee-plans`;

  getFeePlans(): Observable<FeePlanDto[]> {
    return this.http.get<FeePlanDto[]>(this.baseUrl);
  }

  createFeePlan(data: CreateFeePlanRequest): Observable<FeePlanDto> {
    return this.http.post<FeePlanDto>(this.baseUrl, data);
  }

  updateFeePlan(id: string, data: UpdateFeePlanRequest): Observable<FeePlanDto> {
    return this.http.put<FeePlanDto>(`${this.baseUrl}/${id}`, data);
  }
}

