import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface RosterItemDto {
  childId: string;
  firstName: string;
  lastName: string;
  fullName?: string;
  status: 'Present' | 'Absent' | 'NotArrived' | 'CheckedOut' | string;
  checkInAt?: string | null;
  checkOutAt?: string | null;
  allergies?: string | null;
  dietaryNotes?: string | null;
  medicalNotes?: string | null;
  attendanceId?: string | null;
  absenceReason?: string | null;
  guardians?: any[];
  hasPhoto?: boolean;
  photoUpdatedAt?: string | null;
}

export interface ClassroomRosterDto {
  classroomId: string;
  classroomName: string;
  date: string;
  children: RosterItemDto[];
  staffCount: number;
  presentCount: number;
  isOverLimit: boolean;
  maxChildrenAllowed: number;
}

export interface CheckInRequest {
  childId: string;
  classroomId?: string;
  checkedInByGuardianId?: string | null;
  notes?: string | null;
}

export interface CheckOutRequest {
  childId: string;
  attendanceId?: string | null;
  checkedOutByGuardianId: string;
  pickupIdVerified: boolean;
  notes?: string | null;
}

export interface RecordAbsenceRequest {
  childId: string;
  classroomId?: string;
  date: string;
  reason: string;
}

export interface CheckInResponse {
  record: any;
  overRatioWarning: boolean;
}

export interface CheckOutResponse {
  id: string;
  checkOutAt: string;
  lateMinutes: number;
}

import { forkJoin, map } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AttendanceService {
  private http = inject(HttpClient);

  getClassroomRoster(classroomId: string, date?: string): Observable<ClassroomRosterDto> {
    let params = new HttpParams();
    if (date) {
      params = params.set('date', date);
    }

    return forkJoin({
      roster: this.http.get<any[]>(`${environment.apiUrl}/classrooms/${classroomId}/roster`, { params }),
      ratios: this.http.get<any[]>(`${environment.apiUrl}/attendance/ratios`, { params })
    }).pipe(
      map(({ roster, ratios }) => {
        const ratio = ratios.find(r => r.classroomId === classroomId);
        const children: RosterItemDto[] = (roster || []).map(r => {
          const parts = (r.fullName || '').trim().split(' ');
          const firstName = parts[0] || '';
          const lastName = parts.slice(1).join(' ') || '';
          return {
            childId: r.childId,
            firstName,
            lastName,
            fullName: r.fullName,
            status: r.status,
            checkInAt: r.checkInAt,
            checkOutAt: r.checkOutAt,
            allergies: r.allergies,
            medicalNotes: r.medicalNotes,
            attendanceId: r.attendanceId,
            guardians: r.guardians || [],
            hasPhoto: r.hasPhoto ?? false,
            photoUpdatedAt: r.photoUpdatedAt ?? null
          };
        });

        return {
          classroomId,
          classroomName: ratio?.classroomName || '',
          date: date || new Date().toISOString().split('T')[0],
          children,
          staffCount: ratio?.staffCount || 0,
          presentCount: ratio?.presentCount ?? children.filter(c => c.status === 'Present').length,
          isOverLimit: ratio?.isOverLimit ?? false,
          maxChildrenAllowed: ratio?.allowedCount ?? 0
        };
      })
    );
  }

  checkIn(req: CheckInRequest): Observable<CheckInResponse> {
    return this.http.post<CheckInResponse>(`${environment.apiUrl}/attendance/check-in`, {
      childId: req.childId,
      droppedOffByGuardianId: req.checkedInByGuardianId,
      notes: req.notes
    });
  }

  checkOut(req: CheckOutRequest): Observable<any> {
    const recordId = req.attendanceId || req.childId;
    return this.http.post<any>(`${environment.apiUrl}/attendance/${recordId}/check-out`, {
      pickedUpByGuardianId: req.checkedOutByGuardianId,
      idVerified: req.pickupIdVerified
    });
  }

  recordAbsence(req: RecordAbsenceRequest): Observable<any> {
    return this.http.post<any>(`${environment.apiUrl}/attendance/absences`, {
      childId: req.childId,
      date: req.date,
      reason: req.reason
    });
  }

  getRatios(date?: string): Observable<any[]> {
    let params = new HttpParams();
    if (date) {
      params = params.set('date', date);
    }
    return this.http.get<any[]>(`${environment.apiUrl}/attendance/ratios`, { params });
  }
}
