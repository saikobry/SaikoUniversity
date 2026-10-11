import axios, { type AxiosInstance } from 'axios';

/**
 * Origin of the API, with no trailing slash. Empty by default.
 *
 * Orval generates full paths such as `/api/v1/programs` straight from the OpenAPI document, so the
 * mutator must NOT prepend a base path. Adding `/api` here produced `/api/api/v1/programs`.
 *
 * Empty is correct in development, where Vite proxies /api to the local API, and in production,
 * where the API serves the built SPA from the same origin. Everything stays same-origin and no CORS
 * configuration is needed.
 *
 * Set VITE_API_BASE_URL to an absolute origin only when the SPA and the API are hosted on different
 * origins. That deployment requires CORS on the API, which is not configured today.
 */
export const API_ORIGIN = (import.meta.env.VITE_API_BASE_URL ?? '').replace(/\/$/, '');

/**
 * Requests must not hang forever. fetch has no built-in timeout; axios does not either unless one is
 * set, so it is set explicitly here.
 */
export const REQUEST_TIMEOUT_MS = 30_000;

/**
 * The single axios instance. Every generated request goes through it.
 *
 * This is the only place the SPA talks to the network. Feature code never imports axios.
 */
export const apiClient: AxiosInstance = axios.create({
  baseURL: API_ORIGIN || undefined,
  timeout: REQUEST_TIMEOUT_MS,
  headers: {
    'Content-Type': 'application/json',
  },
});

/**
 * Response interceptor. Normalises every failure into an Error carrying the API's own message.
 *
 * Without this, a component has to know about axios response shapes to show a useful error. After
 * this, `error.message` is always something a registrar can read.
 */
apiClient.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (axios.isAxiosError(error)) {
      const status = error.response?.status;
      const body = error.response?.data as { message?: string; code?: string } | undefined;
      const detail = body?.message ?? error.message;

      return Promise.reject(
        new Error(
          status
            ? `API ${status} ${error.config?.method?.toUpperCase() ?? ''} ${error.config?.url ?? ''}: ${detail}`
            : detail,
        ),
      );
    }

    return Promise.reject(error);
  },
);

/**
 * Mutator for the Orval-generated client.
 *
 * Orval calls this as (url, options). Keep that signature: the generated code depends on it.
 *
 * It returns `{ data, status, headers }` because that is the shape Orval 8 types every response as.
 * Do not return the axios response itself; the generated types would then describe a runtime that
 * does not exist.
 */
export const customInstance = async <T>(
  url: string,
  options: {
    method?: string;
    data?: unknown;
    /** Orval emits the request payload under `body`; axios wants `data`. */
    body?: unknown;
    headers?: Record<string, string>;
    signal?: AbortSignal;
  } = {},
): Promise<T> => {
  const response = await apiClient.request({
    url,
    method: options.method ?? 'GET',
    data: options.body ?? options.data,
    headers: options.headers,
    signal: options.signal,
  });

  return {
    data: response.data,
    status: response.status,
    headers: response.headers,
  } as T;
};
