import type { ApiErrorItem } from './types';

/** An error response from the API, with the error codes and messages the back end returned. */
export class ApiError extends Error {
  constructor(
    public readonly status: number,
    public readonly errors: ApiErrorItem[],
  ) {
    super(errors.map((e) => e.message).join(' '));
  }
}

type QueryValue = string | number | boolean | undefined | null | Array<string | number>;

/** Builds "?a=1&b=x&b=y" from an object, skipping empty values. Arrays repeat the key. */
export function toQueryString(params: Record<string, QueryValue>): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') continue;
    if (Array.isArray(value)) {
      value.forEach((item) => search.append(key, String(item)));
    } else {
      search.append(key, String(value));
    }
  }
  const query = search.toString();
  return query ? `?${query}` : '';
}

export async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`/api${path}`, {
    ...init,
    headers: { 'Content-Type': 'application/json', ...init?.headers },
  });

  if (!response.ok) {
    let errors: ApiErrorItem[] = [
      { code: `HTTP_${response.status}`, message: 'Something went wrong. Please try again.' },
    ];
    try {
      const body = await response.json();
      if (Array.isArray(body?.errors) && body.errors.length > 0) errors = body.errors;
    } catch {
      // The body was not JSON; keep the generic message.
    }
    throw new ApiError(response.status, errors);
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}
