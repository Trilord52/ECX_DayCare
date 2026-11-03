import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AttendanceReportItemDto {
  date: string;
  childId: string;
  childName: string;
  classroomId: string;
  classroomName: string;
  status: string;
  checkInAt?: string | null;
  checkOutAt?: string | null;
  lateMinutes: number;
  absenceReason?: string | null;
}

export interface AttendanceReportDto {
  from: string;
  to: string;
  totalRecords: number;
  totalPresent: number;
  totalAbsences: number;
  totalLatePickups: number;
  totalLateMinutes: number;
  items: AttendanceReportItemDto[];
}

export interface BillingReportItemDto {
  id: string;
  invoiceNumber: string;
  childId: string;
  childName: string;
  guardianId: string;
  guardianName: string;
  issueDate: string;
  dueDate: string;
  status: string;
  totalAmount: number;
  amountPaid: number;
  balance: number;
  isOverdue: boolean;
}

export interface BillingReportDto {
  from: string;
  to: string;
  totalInvoices: number;
  totalBilled: number;
  totalCollected: number;
  totalOutstanding: number;
  overdueInvoicesCount: number;
  currency: string;
  items: BillingReportItemDto[];
}

@Injectable({
  providedIn: 'root'
})
export class ReportsService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/reports`;

  getAttendanceReport(from?: string, to?: string, classroomId?: string): Observable<AttendanceReportDto> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    if (classroomId) params = params.set('classroomId', classroomId);

    return this.http.get<AttendanceReportDto>(`${this.baseUrl}/attendance`, { params });
  }

  downloadAttendanceCsv(from?: string, to?: string, classroomId?: string): Observable<Blob> {
    let params = new HttpParams().set('format', 'csv');
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    if (classroomId) params = params.set('classroomId', classroomId);

    return this.http.get(`${this.baseUrl}/attendance`, {
      params,
      responseType: 'blob'
    });
  }

  getBillingReport(from?: string, to?: string): Observable<BillingReportDto> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);

    return this.http.get<BillingReportDto>(`${this.baseUrl}/billing`, { params });
  }

  downloadBillingCsv(from?: string, to?: string): Observable<Blob> {
    let params = new HttpParams().set('format', 'csv');
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);

    return this.http.get(`${this.baseUrl}/billing`, {
      params,
      responseType: 'blob'
    });
  }
}

