import { useEffect, useState, type FormEvent } from 'react';

import type { ProgramResponse, UpdateProgramRequest } from '@/api/schema.schemas';
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

interface EditProgramModalProps {
  program: ProgramResponse | null;
  open: boolean;
  onClose: () => void;
  onSubmit: (id: string, data: UpdateProgramRequest) => Promise<void>;
  isSubmitting: boolean;
  errorMessage?: string | null;
}

export function EditProgramModal({
  program,
  open,
  onClose,
  onSubmit,
  isSubmitting,
  errorMessage,
}: EditProgramModalProps) {
  const [name, setName] = useState('');
  const [requiredCredits, setRequiredCredits] = useState('144');

  useEffect(() => {
    if (program) {
      setName(program.name);
      setRequiredCredits(String(program.requiredCredits));
    }
  }, [program]);

  if (!program) return null;

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    if (!name.trim()) return;

    const credits = Number(requiredCredits);
    if (!Number.isInteger(credits) || credits <= 0) return;

    await onSubmit(program.id, {
      name: name.trim(),
      requiredCredits: Number(requiredCredits),
    });
  };

  return (
    <Dialog open={open} onOpenChange={(isOpen) => { if (!isOpen) onClose(); }}>
      <DialogContent className="sm:max-w-xl">
        <DialogHeader>
          <DialogTitle>Edit Program: {program.code}</DialogTitle>
          <DialogDescription>
            Update program title or credit requirements.
          </DialogDescription>
        </DialogHeader>
        <form onSubmit={handleSubmit} className="space-y-4">
        {errorMessage && (
          <div className="rounded-md border border-destructive/20 bg-destructive/10 p-3 text-sm text-destructive">
            {errorMessage}
          </div>
        )}

        <div className="rounded-md border bg-muted/40 p-3 text-sm space-y-1">
          <div className="flex justify-between">
            <span className="text-muted-foreground">Department:</span>
            <span className="font-medium">{program.departmentName}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Level:</span>
            <span className="font-medium">{program.academicLevelCode}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Regulator / Basis:</span>
            <span className="font-medium">
              {program.regulator} ({program.creditBasis})
            </span>
          </div>
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="edit-name">Program Title</Label>
          <Input
            id="edit-name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            required
          />
        </div>

        <div className="space-y-1.5">
          <Label htmlFor="edit-credits">Required Credits ({program.creditBasis})</Label>
          <Input
            id="edit-credits"
            type="number"
            min="1"
            value={requiredCredits}
            onChange={(e) => setRequiredCredits(e.target.value)}
            required
          />
        </div>

        <div className="flex justify-end gap-2 pt-3 border-t">
          <Button type="button" variant="outline" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting ? 'Saving…' : 'Save Changes'}
          </Button>
        </div>
      </form>
    </DialogContent>
  </Dialog>
  );
}
