/**
 * TEMPORARY diagnostic. Delete once the freeze is diagnosed.
 *
 * Writes counts to localStorage on every check because the renderer process dies before any
 * in-memory value can be read back. localStorage is browser-storage backed and survives the crash.
 */

const THRESHOLD = 200;

function read(): Record<string, number> {
  try {
    return JSON.parse(localStorage.getItem('__renderGuard') ?? '{}');
  } catch {
    return {};
  }
}

function write(next: Record<string, number>) {
  try {
    localStorage.setItem('__renderGuard', JSON.stringify(next));
  } catch {
    // Quota or private mode. Nothing useful to do here.
  }
}

export const renderGuard = {
  check(component: string) {
    const counts = read();
    const next = (counts[component] ?? 0) + 1;
    counts[component] = next;
    write(counts);

    if (next === THRESHOLD) {
      throw new Error(`[renderGuard] ${component} rendered ${THRESHOLD} times: render loop confirmed.`);
    }
  },

  reset() {
    write({});
  },

  snapshot(): Record<string, number> {
    return read();
  },
};
