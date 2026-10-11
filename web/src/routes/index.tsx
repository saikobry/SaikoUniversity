import { createFileRoute } from '@tanstack/react-router';

import { ProgramsPage } from '@/features/programs/ProgramsPage';

export const Route = createFileRoute('/')({
  component: ProgramsPage,
});
