import { Injectable, inject, OnDestroy } from '@angular/core';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Observable, of, Subject } from 'rxjs';
import { catchError, map, shareReplay, tap } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface PhotoUploadResult {
  hasPhoto: boolean;
  photoUpdatedAt: string;
}

@Injectable({
  providedIn: 'root'
})
export class PhotoService implements OnDestroy {
  private http = inject(HttpClient);

  // Cache object URLs keyed by `${kind}:${id}:${version}`
  private blobUrlCache = new Map<string, string>();
  // In-flight observable cache to prevent duplicate concurrent network requests
  private inFlightRequests = new Map<string, Observable<string | null>>();

  load(kind: string, id: string, version?: string | null): Observable<string | null> {
    if (!id || !kind) {
      return of(null);
    }

    const normalizedKind = kind.toLowerCase();
    const cacheKey = `${normalizedKind}:${id}:${version || ''}`;

    if (this.blobUrlCache.has(cacheKey)) {
      return of(this.blobUrlCache.get(cacheKey)!);
    }

    if (this.inFlightRequests.has(cacheKey)) {
      return this.inFlightRequests.get(cacheKey)!;
    }

    const request$ = this.http.get(`${environment.apiUrl}/${normalizedKind}/${id}/photo`, {
      responseType: 'blob'
    }).pipe(
      map(blob => {
        const objectUrl = URL.createObjectURL(blob);
        this.blobUrlCache.set(cacheKey, objectUrl);
        return objectUrl;
      }),
      catchError((error: HttpErrorResponse) => {
        // Return null on 404 NOT_FOUND or 403 FORBIDDEN without failing UI
        return of(null);
      }),
      tap(() => {
        this.inFlightRequests.delete(cacheKey);
      }),
      shareReplay(1)
    );

    this.inFlightRequests.set(cacheKey, request$);
    return request$;
  }

  upload(kind: string, id: string, file: File): Observable<PhotoUploadResult> {
    const normalizedKind = kind.toLowerCase();
    const formData = new FormData();
    formData.append('file', file, file.name);

    return this.http.put<PhotoUploadResult>(`${environment.apiUrl}/${normalizedKind}/${id}/photo`, formData).pipe(
      tap(() => {
        this.invalidate(normalizedKind, id);
      })
    );
  }

  delete(kind: string, id: string): Observable<void> {
    const normalizedKind = kind.toLowerCase();
    return this.http.delete<void>(`${environment.apiUrl}/${normalizedKind}/${id}/photo`).pipe(
      tap(() => {
        this.invalidate(normalizedKind, id);
      })
    );
  }

  invalidate(kind: string, id: string): void {
    const normalizedKind = kind.toLowerCase();
    const prefix = `${normalizedKind}:${id}:`;

    for (const [key, url] of this.blobUrlCache.entries()) {
      if (key.startsWith(prefix)) {
        URL.revokeObjectURL(url);
        this.blobUrlCache.delete(key);
      }
    }
    for (const key of this.inFlightRequests.keys()) {
      if (key.startsWith(prefix)) {
        this.inFlightRequests.delete(key);
      }
    }
  }

  ngOnDestroy(): void {
    for (const url of this.blobUrlCache.values()) {
      URL.revokeObjectURL(url);
    }
    this.blobUrlCache.clear();
    this.inFlightRequests.clear();
  }
}

