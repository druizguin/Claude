import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { JsonApiCollection, JsonApiSingle, QuerySpec } from '../models/jsonapi.model';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api';

  private headers = new HttpHeaders({
    'Content-Type': 'application/json',
    'Accept': 'application/vnd.api+json',
    'X-User-Name': 'WebClient'
  });

  getCollection<T>(path: string, from = 0, offset = 20): Observable<JsonApiCollection<T>> {
    return this.http.get<JsonApiCollection<T>>(
      `${this.baseUrl}/${path}?from=${from}&offset=${offset}`,
      { headers: this.headers }
    );
  }

  query<T>(path: string, spec: QuerySpec): Observable<JsonApiCollection<T>> {
    return this.http.post<JsonApiCollection<T>>(
      `${this.baseUrl}/${path}/query`,
      spec,
      { headers: this.headers }
    );
  }

  getOne<T>(path: string, id: string): Observable<JsonApiSingle<T>> {
    return this.http.get<JsonApiSingle<T>>(
      `${this.baseUrl}/${path}/${id}`,
      { headers: this.headers }
    );
  }

  create<T>(path: string, attributes: Partial<T>): Observable<JsonApiSingle<T>> {
    const body = { data: { type: path, attributes } };
    return this.http.post<JsonApiSingle<T>>(
      `${this.baseUrl}/${path}`,
      body,
      { headers: this.headers }
    );
  }

  update<T>(path: string, id: string, attributes: Partial<T>): Observable<JsonApiSingle<T>> {
    const body = { data: { type: path, id, attributes } };
    return this.http.patch<JsonApiSingle<T>>(
      `${this.baseUrl}/${path}/${id}`,
      body,
      { headers: this.headers }
    );
  }

  delete(path: string, id: string): Observable<void> {
    return this.http.delete<void>(
      `${this.baseUrl}/${path}/${id}`,
      { headers: this.headers }
    );
  }

  getAuditByEntity(entityId: string): Observable<JsonApiCollection> {
    return this.http.get<JsonApiCollection>(
      `${this.baseUrl}/audit/entity/${entityId}`,
      { headers: this.headers }
    );
  }
}
