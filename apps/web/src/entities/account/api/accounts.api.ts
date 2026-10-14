import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { AccountItem, CreateAccountRequest, UpdateAccountRequest } from '@istpetdev/shared-core';
import { environment } from '@shared/config';

@Injectable({ providedIn: 'root' })
export class AccountsApi {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/api/v1/accounts`;

  list(): Observable<AccountItem[]> {
    return this.http.get<AccountItem[]>(this.base);
  }

  create(input: CreateAccountRequest): Observable<AccountItem> {
    return this.http.post<AccountItem>(this.base, input);
  }

  update(id: string, input: UpdateAccountRequest): Observable<AccountItem> {
    return this.http.patch<AccountItem>(`${this.base}/${encodeURIComponent(id)}`, input);
  }

  deactivate(id: string): Observable<AccountItem> {
    return this.http.post<AccountItem>(`${this.base}/${encodeURIComponent(id)}/deactivate`, {});
  }

  reactivate(id: string): Observable<AccountItem> {
    return this.http.post<AccountItem>(`${this.base}/${encodeURIComponent(id)}/reactivate`, {});
  }

  resendInvitation(id: string): Observable<void> {
    return this.http.post<void>(`${this.base}/${encodeURIComponent(id)}/resend-invitation`, {});
  }
}
