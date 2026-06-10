export interface JsonApiResource<T = Record<string, unknown>> {
  type: string;
  id: string;
  attributes: T;
}

export interface JsonApiCollection<T = Record<string, unknown>> {
  data: JsonApiResource<T>[];
  meta?: JsonApiMeta;
}

export interface JsonApiSingle<T = Record<string, unknown>> {
  data: JsonApiResource<T>;
}

export interface JsonApiMeta {
  total: number;
  from: number;
  offset: number;
}

export interface JsonApiError {
  status: string;
  title: string;
  detail: string;
}

export interface JsonApiErrorResponse {
  errors: JsonApiError[];
}

export interface QuerySpec {
  from: string;
  select?: string[];
  filter?: Record<string, unknown>;
  orderby?: { field: string; direction: 'asc' | 'desc' }[];
  page?: { from: number; offset: number };
}
