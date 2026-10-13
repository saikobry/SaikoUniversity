import { useState, type FormEvent } from 'react';

import type { CreateProgramRequest } from '@/api/schema.schemas';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

interface Option {
  id: string;
  name: string;
}

interface CreateProgramModalProps {
  open: boolean;
  onClose: () => void;
  levels: Option[];
  departments: Option[];
  onSubmit: (data: CreateProgramRequest) => Promise<void>;
  isSubmitting: boolean;
  errorMessage?: string | null;
}

export function CreateProgramModal({
  open,
  onClose,
  levels,
  departments,
  onSubmit,
  isSubmitting,
  errorMessage,
}: CreateProgramModalProps) {
  const [code, setCode] = useState('');
  const [name, setName] = useState('');
  const [academicLevelId, setAcademicLevelId] = useState('');
  const [departmentId, setDepartmentId] = useState('');
  const [regulator, setRegulator] = useState('Ched');
  const [requiredCredits, setRequiredCredits] = useState('144');
  const [creditBasis, setCreditBasis] = useState('Hours');
  const [isRegulatorRecognised, setIsRegulatorRecognised] = useState(true);

  // Set default level and department when opened and options are available
  const effectiveLevelId = academicLevelId || levels[0]?.id || '';
  const effectiveDepartmentId = departmentId || departments[0]?.id || '';

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!code.trim() || !name.trim() || !effectiveLevelId || !effectiveDepartmentId) {
      return;
    }

    const credits = Number(requiredCredits);
    if (!Number.isInteger(credits) || credits <= 0) {
      return;
    }

    await onSubmit({
      code: code.trim(),
      name: name.trim(),
      academicLevelId: effectiveLevelId,
      departmentId: effectiveDepartmentId,
      regulator,
      requiredCredits: Number(requiredCredits),
      creditBasis,
      isRegulatorRecognised,
    });
  };

  return (
    <Dialog open={open} onOpenChange={(isOpen) => { if (!isOpen) onClose(); }}>
      <DialogContent className="sm:max-w-xl">
        <DialogHeader>
          <DialogTitle>Create Program</DialogTitle>
          <DialogDescription>
            Add a new academic degree or qualification to the university catalogue.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
        {errorMessage && (
          <div className="rounded-md border border-destructive/20 bg-destructive/10 p-3 text-sm text-destructive">
            {errorMessage}
          </div>
        )}

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-1.5">
            <Label htmlFor="create-code">Program Code</Label>
            <Input
              id="create-code"
              placeholder="e.g. BSCS"
              value={code}
              onChange={(e) => setCode(e.target.value)}
              required
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="create-regulator">Regulator</Label>
            <select
              id="create-regulator"
              value={regulator}
              onChange={(e) => setRegulator(e.target.value)}
              className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            >
              <option value="Ched">CHED</option>
              <option value="Tesda">TESDA</option>
            </select>
          </div>
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="create-name">Program Title</Label>
          <Input
            id="create-name"
            placeholder="e.g. Bachelor of Science in Computer Science"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
          />
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-1.5">
            <Label htmlFor="create-level">Academic Level</Label>
            <select
              id="create-level"
              value={effectiveLevelId}
              onChange={(e) => setAcademicLevelId(e.target.value)}
              className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
              required
            >
              {levels.map((lvl) => (
                <option key={lvl.id} value={lvl.id}>
                  {lvl.name}
                </option>
              ))}
            </select>
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="create-dept">Department</Label>
            <select
              id="create-dept"
              value={effectiveDepartmentId}
              onChange={(e) => setDepartmentId(e.target.value)}
              className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
              required
            >
              {departments.map((dept) => (
                <option key={dept.id} value={dept.id}>
                  {dept.name}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div className="space-y-1.5">
            <Label htmlFor="create-credits">Required Credits</Label>
            <Input
              id="create-credits"
              type="number"
              min="1"
              value={requiredCredits}
              onChange={(e) => setRequiredCredits(e.target.value)}
              required
            />
          </div>

          <div className="space-y-1.5">
            <Label htmlFor="create-basis">Credit Basis</Label>
            <select
              id="create-basis"
              value={creditBasis}
              onChange={(e) => setCreditBasis(e.target.value)}
              className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-xs focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
            >
              <option value="Hours">Hours</option>
              <option value="Units">Units</option>
            </select>
          </div>
        </div>

        <div className="flex items-center gap-2 pt-1">
          <input
            id="create-recognised"
            type="checkbox"
            checked={isRegulatorRecognised}
            onChange={(e) => setIsRegulatorRecognised(e.target.checked)}
            className="h-4 w-4 rounded border-input"
          />
          <Label htmlFor="create-recognised" className="cursor-pointer font-normal">
            Recognised by regulator (eligible to accept students)
          </Label>
        </div>

        <div className="flex justify-end gap-2 pt-3 border-t">
          <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Creating…' : 'Create Program'}
          </Button>
        </div>
      </form>
    </DialogContent>
  </Dialog>
  );
}
