import { defineConfig } from 'orval';

/**
 * Generates the typed API client from the OpenAPI snapshot in artifacts/openapi.
 *
 * Regenerate after any API contract change:
 *   powershell -ExecutionPolicy Bypass -File scripts/tools/update-openapi.ps1
 *   npm --prefix web run api:generate
 *
 * Everything under src/api is generated. Never hand-edit it. If a type is wrong, change the
 * controller contract and regenerate; the fix does not belong in generated output.
 */
export default defineConfig({
  'saiko-university-api': {
    input: '../artifacts/openapi/saiko-university.json',
    output: {
      target: './src/api/schema.ts',
      client: 'react-query',
      mode: 'tags-split',
      prettier: false,
      override: {
        mutator: {
          path: './src/api/client.ts',
          name: 'customInstance',
        },
      },
    },
  },
});
