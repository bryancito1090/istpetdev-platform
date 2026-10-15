import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { firstValueFrom } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { HttpFieldApiService } from './http-field-api.service';
import { environment } from '../config/environment';

describe('Carga real de evidencia #115', () => {
  let api: HttpFieldApiService;
  let http: HttpTestingController;
  const request = { operationId: 'op', kind: 'photo' as const, mimeType: 'image/jpeg', byteSize: 5, sha256: 'hash' };
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    api = TestBed.inject(HttpFieldApiService);
    http = TestBed.inject(HttpTestingController);
  });
  afterEach(() => http.verify());

  it('envía el Blob a la URL autorizada antes de dar por terminada la carga', async () => {
    const blob = new Blob(['photo']);
    const done = firstValueFrom(api.uploadEvidence('receipt', request, blob));
    const authorization = http.expectOne(`${environment.apiUrl}/api/v1/receipts/receipt/evidence-uploads`);
    expect(authorization.request.body).toEqual(request);
    authorization.flush({ evidenceId: 'ev', uploadUrl: 'https://private.example/put',
      requiredHeaders: { 'Content-Type': 'image/jpeg', 'x-amz-checksum-sha256': 'checksum' } });
    const upload = http.expectOne('https://private.example/put');
    expect(upload.request.method).toBe('PUT');
    expect(upload.request.body).toBe(blob);
    expect(upload.request.headers.get('x-amz-checksum-sha256')).toBe('checksum');
    expect(upload.request.headers.has('Authorization')).toBe(false);
    upload.flush(null);
    expect((await done).evidenceId).toBe('ev');
  });

  it('una URL de ejemplo no cuenta como archivo subido', async () => {
    const done = firstValueFrom(api.uploadEvidence('receipt', request, new Blob(['photo'])));
    const rejected = expect(done).rejects.toMatchObject({ status: 503 });
    http.expectOne(`${environment.apiUrl}/api/v1/receipts/receipt/evidence-uploads`).flush({ evidenceId: 'ev', uploadUrl: 'https://s3.istpetdev.local/evidences/ev' });
    await rejected;
  });
});
