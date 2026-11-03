import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ClassRatioDto {
  classroomId: string;
  classroomName: string;
  capacity?: number;
  maxChildrenPerStaff: number;
  staffCount: number;
  presentCount: number;
  allowedCount: number;
  maxChildrenAllowed?: number;
  isOverLimit: boolean;
}

export interface AttendanceTrendItemDto {
  date: string;
  present: number;
}

export interface AdminDashboardBillingDto {
  month?: string;
  invoiced: number;
  collected: number;
  outstanding: number;
  overdueCount: number;
  currency?: string;
}

export interface AdminDashboardDto {
  date: string;
  enrolledChildren: number;
  presentToday: number;
  inBuildingNow: number;
  absentToday: number;
  notArrivedToday: number;
  ratios: ClassRatioDto[];
  unacknowledgedIncidents?: number;
  recentIncidents?: any[];
  billing: AdminDashboardBillingDto;
  attendanceTrend: AttendanceTrendItemDto[];
}

export interface StaffDashboardClassroomDto {
  classroomId: string;
  classroomName: string;
  enrolled: number;
  present: number;
  checkedOut: number;
  absent: number;
  notArrived: number;
  staffCount: number;
  isOverLimit: boolean;
}

export interface StaffDashboardDto {
  date: string;
  classrooms: StaffDashboardClassroomDto[];
}

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  private http = inject(HttpClient);

  getAdminDashboard(): Observable<AdminDashboardDto> {
    return this.http.get<AdminDashboardDto>(`${environment.apiUrl}/dashboard/admin`);
  }

  getStaffDashboard(): Observable<StaffDashboardDto> {
    return this.http.get<StaffDashboardDto>(`${environment.apiUrl}/dashboard/staff`);
  }
}
