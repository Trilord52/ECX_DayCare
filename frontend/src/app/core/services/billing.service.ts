import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult } from './children.service';

export interface InvoiceLineDto {
  id: string;
  lineType: string;
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface PaymentDto {
  id: string;
  amount: number;
  method: string;
  reference?: string | null;
  paidAt: string;
  notes?: string | null;
  recordedByName: string;
  voidedAt?: string | null;
  voidReason?: string | null;
}

export interface InvoiceDto {
  id: string;
  invoiceNumber: string;
  childId: string;
  childName: string;
  guardianId: string;
  guardianName: string;
  periodStart: string;
  periodEnd: string;
  issueDate: string;
  dueDate: string;
  status: 'Issued' | 'PartiallyPaid' | 'Paid' | 'Void' | string;
  currency: string;
  totalAmount: number;
  amountPaid: number;
  balance: number;
  voidReason?: string | null;
  voidedAt?: string | null;
  lines?: InvoiceLineDto[];
  payments?: PaymentDto[];
}

export interface GenerateInvoicesRequest {
  periodStart: string;
  periodEnd: string;
  dueDate: string;
}

export interface GenerateInvoicesResultDto {
  generatedCount: number;
  totalAmount: number;
  skippedCount: number;
  errors: string[];
}

export interface RecordPaymentRequest {
  amount: number;
  method: string;
  reference?: string | null;
  notes?: string | null;
}

export interface AddAdjustmentRequest {
  lineType: 'Tuition' | 'LateFee' | 'Discount' | 'Registration' | 'Adjustment' | string;
  description: string;
  amount: number;
}

export interface VoidInvoiceRequest {
  reason: string;
}

export interface FeePlanDto {
  id: string;
  name: string;
  description?: string | null;
  monthlyAmount: number;
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class BillingService {
  private http = inject(HttpClient);

  getInvoices(options?: { status?: string; search?: string; page?: number; pageSize?: number }): Observable<PagedResult<InvoiceDto>> {
    let params = new HttpParams();
    if (options?.status) params = params.set('status', options.status);
    if (options?.search) params = params.set('search', options.search);
    if (options?.page) params = params.set('page', options.page.toString());
    if (options?.pageSize) params = params.set('pageSize', options.pageSize.toString());

    return this.http.get<PagedResult<InvoiceDto>>(`${environment.apiUrl}/invoices`, { params });
  }

  getInvoice(id: string): Observable<InvoiceDto> {
    return this.http.get<InvoiceDto>(`${environment.apiUrl}/invoices/${id}`);
  }

  generateInvoices(req: GenerateInvoicesRequest): Observable<GenerateInvoicesResultDto> {
    return this.http.post<GenerateInvoicesResultDto>(`${environment.apiUrl}/invoices/generate`, req);
  }

  recordPayment(invoiceId: string, req: RecordPaymentRequest): Observable<PaymentDto> {
    return this.http.post<PaymentDto>(`${environment.apiUrl}/invoices/${invoiceId}/payments`, req);
  }

  addAdjustment(invoiceId: string, req: AddAdjustmentRequest): Observable<InvoiceLineDto> {
    return this.http.post<InvoiceLineDto>(`${environment.apiUrl}/invoices/${invoiceId}/adjustments`, req);
  }

  voidInvoice(invoiceId: string, req: VoidInvoiceRequest): Observable<void> {
    return this.http.post<void>(`${environment.apiUrl}/invoices/${invoiceId}/void`, req);
  }

  payOnline(invoiceId: string, amount: number): Observable<PaymentDto> {
    return this.http.post<PaymentDto>(`${environment.apiUrl}/invoices/${invoiceId}/pay-online`, { amount });
  }

  getFeePlans(): Observable<FeePlanDto[]> {
    return this.http.get<FeePlanDto[]>(`${environment.apiUrl}/fee-plans`);
  }
}
