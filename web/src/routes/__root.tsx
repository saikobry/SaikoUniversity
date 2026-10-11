import { createRootRoute, Outlet } from '@tanstack/react-router';

/**
 * Root layout. Owns the page chrome only. It never fetches data.
 *
 * The route tree itself is generated into src/routeTree.gen.ts by @tanstack/router-plugin. This file
 * contributes the root route to it. Never edit routeTree.gen.ts; regenerate it instead.
 *
 * The exported name must be Route. The generator imports it by that exact name.
 */
export const Route = createRootRoute({
  component: RootLayout,
});

function RootLayout() {
  return (
    <div className="min-h-screen bg-background text-foreground">
      <header className="border-b">
        <div className="mx-auto flex max-w-7xl items-center gap-3 px-6 py-4">
          <span className="text-lg font-semibold tracking-tight">Saiko University</span>
          <span className="text-muted-foreground text-sm">Registrar</span>
        </div>
      </header>

      <main className="mx-auto max-w-7xl px-6 py-8">
        <Outlet />
      </main>
    </div>
  );
}
