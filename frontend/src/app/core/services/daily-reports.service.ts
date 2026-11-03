import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { map, catchError } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export type MoodType = 'Happy' | 'Calm' | 'Tired' | 'Fussy' | 'Upset';
export type EntryType = 'Meal' | 'Nap' | 'Diaper' | 'Activity' | 'Medication' | 'Note';
export type MealType = 'Breakfast' | 'Lunch' | 'Snack' | 'Bottle';
export type AmountEaten = 'None' | 'Some' | 'Most' | 'All';
export type DiaperType = 'Dry' | 'Wet' | 'Soiled' | 'Both';

export interface DailyReportEntryDto {
  id: string;
  dailyReportId: string;
  entryType: EntryType;
  occurredAt: string;
  endedAt?: string | null;
  mealType?: MealType | null;
  amountEaten?: AmountEaten | null;
  diaperType?: DiaperType | null;
  description?: string | null;
  createdByUserId?: string;
  createdByName?: string;
}

export interface DailyReportDetailDto {
  id?: string | null;
  childId: string;
  classroomId?: string | null;
  date: string;
  mood?: MoodType | null;
  notes?: string | null;
  entries: DailyReportEntryDto[];
}

export interface CreateDailyReportRequest {
  childId: string;
  classroomId?: string;
  date: string;
  mood?: MoodType | null;
  notes?: string | null;
}

export interface AddDailyReportEntryRequest {
  entryType: EntryType;
  occurredAt: string;
  endedAt?: string | null;
  mealType?: MealType | null;
  amountEaten?: AmountEaten | null;
  diaperType?: DiaperType | null;
  description?: string | null;
}

@Injectable({
  providedIn: 'root'
})
export class DailyReportsService {
  private http = inject(HttpClient);

  getDailyReports(childId: string, date?: string): Observable<DailyReportDetailDto[]> {
    const d = date || new Date().toISOString().split('T')[0];
    return this.http.get<DailyReportDetailDto>(`${environment.apiUrl}/children/${childId}/daily-reports/${d}`).pipe(
      map(report => (report ? [report] : [])),
      catchError(() => of([]))
    );
  }

  getTodayReport(childId: string): Observable<DailyReportDetailDto | null> {
    const today = new Date().toISOString().split('T')[0];
    return this.http.get<DailyReportDetailDto>(`${environment.apiUrl}/children/${childId}/daily-reports/${today}`).pipe(
      map(rep => (rep && (rep.id || (rep.entries && rep.entries.length > 0) || rep.mood || rep.notes) ? rep : null)),
      catchError(() => of(null))
    );
  }

  getDailyReport(childId: string, date?: string): Observable<DailyReportDetailDto> {
    const d = date || new Date().toISOString().split('T')[0];
    return this.http.get<DailyReportDetailDto>(`${environment.apiUrl}/children/${childId}/daily-reports/${d}`);
  }

  createDailyReport(req: CreateDailyReportRequest): Observable<DailyReportDetailDto> {
    return this.http.put<DailyReportDetailDto>(`${environment.apiUrl}/children/${req.childId}/daily-reports/${req.date}`, {
      mood: req.mood,
      notes: req.notes
    });
  }

  updateMoodAndNotes(childId: string, mood?: MoodType | null, notes?: string | null, date?: string): Observable<DailyReportDetailDto> {
    const d = date || new Date().toISOString().split('T')[0];
    return this.http.put<DailyReportDetailDto>(`${environment.apiUrl}/children/${childId}/daily-reports/${d}`, {
      mood,
      notes
    });
  }

  addEntry(childId: string, req: AddDailyReportEntryRequest, date?: string): Observable<DailyReportEntryDto> {
    const d = date || new Date().toISOString().split('T')[0];
    return this.http.post<DailyReportEntryDto>(`${environment.apiUrl}/children/${childId}/daily-reports/${d}/entries`, req);
  }
}
