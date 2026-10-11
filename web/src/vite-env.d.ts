/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** Origin of the API. Empty when same-origin. Not a path prefix. */
  readonly VITE_API_BASE_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
