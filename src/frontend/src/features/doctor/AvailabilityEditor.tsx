import { useState, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { getMyAvailability, setMyAvailability } from '../../shared/api/availability';
import type { AvailabilitySlot, AvailabilityWindow, DayOfWeek } from '../../shared/api/availability';
import { getApiError } from '../../shared/api/errors';
import { Button, Chip } from '../../shared/components';

const DAYS = [
  'Monday',
  'Tuesday',
  'Wednesday',
  'Thursday',
  'Friday',
  'Saturday',
  'Sunday',
] as const;

const TIME_OPTIONS: { value: string; label: string }[] = (() => {
  const opts: { value: string; label: string }[] = [];
  for (let h = 0; h < 24; h++) {
    for (const m of [0, 30]) {
      const hh = String(h).padStart(2, '0');
      const mm = String(m).padStart(2, '0');
      const value = `${hh}:${mm}`;
      const hour12 = h === 0 ? 12 : h > 12 ? h - 12 : h;
      const ampm = h < 12 ? 'AM' : 'PM';
      const label = `${hour12}:${mm} ${ampm}`;
      opts.push({ value, label });
    }
  }
  return opts;
})();

type SlotMap = Record<DayOfWeek, AvailabilitySlot[]>;

function emptySlotMap(): SlotMap {
  return Object.fromEntries(DAYS.map((d) => [d, []])) as SlotMap;
}

function windowsToSlotMap(windows: AvailabilityWindow[]): SlotMap {
  const map = emptySlotMap();
  for (const w of windows) {
    const day = w.dayOfWeek as DayOfWeek;
    if (day in map) {
      map[day].push({ dayOfWeek: w.dayOfWeek, startTime: w.startTime, endTime: w.endTime });
    }
  }
  return map;
}

function slotsOverlap(a: AvailabilitySlot, b: AvailabilitySlot): boolean {
  return a.startTime < b.endTime && b.startTime < a.endTime;
}

function validateSlot(
  slot: { startTime: string; endTime: string },
  existing: AvailabilitySlot[],
): string | null {
  if (!slot.startTime || !slot.endTime) return 'Both start and end time are required.';
  if (slot.startTime >= slot.endTime) return 'Start time must be before end time.';
  const candidate = { dayOfWeek: '', startTime: slot.startTime, endTime: slot.endTime };
  for (const e of existing) {
    if (slotsOverlap(candidate, e)) return 'This window overlaps with an existing one.';
  }
  return null;
}

export default function AvailabilityEditor() {
  const queryClient = useQueryClient();

  const { data: windows, isLoading, isError } = useQuery({
    queryKey: ['my-availability'],
    queryFn: getMyAvailability,
  });

  const [slots, setSlots] = useState<SlotMap | null>(null);
  const [addingDay, setAddingDay] = useState<DayOfWeek | null>(null);
  const [newStart, setNewStart] = useState('');
  const [newEnd, setNewEnd] = useState('');
  const [validationError, setValidationError] = useState<string | null>(null);

  // Derive editable map: use local state if edited, otherwise derive from server data
  const editMap = slots ?? (windows ? windowsToSlotMap(windows) : emptySlotMap());

  const ensureLocal = useCallback(() => {
    if (!slots && windows) {
      setSlots(windowsToSlotMap(windows));
    }
  }, [slots, windows]);

  const removeSlot = useCallback(
    (day: DayOfWeek, index: number) => {
      ensureLocal();
      setSlots((prev) => {
        const map = prev ?? (windows ? windowsToSlotMap(windows) : emptySlotMap());
        return {
          ...map,
          [day]: map[day].filter((_, i) => i !== index),
        };
      });
    },
    [ensureLocal, windows],
  );

  const addSlot = useCallback(
    (day: DayOfWeek) => {
      const error = validateSlot({ startTime: newStart, endTime: newEnd }, editMap[day]);
      if (error) {
        setValidationError(error);
        return;
      }
      ensureLocal();
      setSlots((prev) => {
        const map = prev ?? (windows ? windowsToSlotMap(windows) : emptySlotMap());
        return {
          ...map,
          [day]: [
            ...map[day],
            { dayOfWeek: day, startTime: newStart, endTime: newEnd },
          ],
        };
      });
      setAddingDay(null);
      setNewStart('');
      setNewEnd('');
      setValidationError(null);
    },
    [newStart, newEnd, editMap, ensureLocal, windows],
  );

  const save = useMutation({
    mutationFn: () => {
      const allSlots = DAYS.flatMap((day) => editMap[day]);
      return setMyAvailability(allSlots);
    },
    onSuccess: () => {
      setSlots(null);
      queryClient.invalidateQueries({ queryKey: ['my-availability'] });
    },
  });

  if (isLoading) return <p className="text-dim">Loading availability...</p>;
  if (isError) return <p className="auth-error">Failed to load availability.</p>;

  return (
    <div className="avail-editor">
      <div className="avail-grid">
        {DAYS.map((day) => (
          <div key={day} className="avail-day-card">
            <h3 className="avail-day-heading">{day}</h3>

            <div className="avail-chips">
              {editMap[day].length === 0 ? (
                <span className="text-dim">No windows</span>
              ) : (
                editMap[day].map((slot, i) => (
                  <span key={i} className="avail-chip">
                    <Chip status={`${slot.startTime} - ${slot.endTime}`} />
                    <button
                      type="button"
                      className="avail-chip-remove"
                      aria-label={`Remove ${slot.startTime} - ${slot.endTime} on ${day}`}
                      onClick={() => removeSlot(day, i)}
                    >
                      &times;
                    </button>
                  </span>
                ))
              )}
            </div>

            {addingDay === day ? (
              <div className="avail-add-form">
                <div className="avail-time-row">
                  <label className="field">
                    <span>Start</span>
                    <select
                      value={newStart}
                      onChange={(e) => {
                        setNewStart(e.target.value);
                        setValidationError(null);
                      }}
                    >
                      {TIME_OPTIONS.map((t) => (
                        <option key={t.value} value={t.value}>{t.label}</option>
                      ))}
                    </select>
                  </label>
                  <label className="field">
                    <span>End</span>
                    <select
                      value={newEnd}
                      onChange={(e) => {
                        setNewEnd(e.target.value);
                        setValidationError(null);
                      }}
                    >
                      {TIME_OPTIONS.map((t) => (
                        <option key={t.value} value={t.value}>{t.label}</option>
                      ))}
                    </select>
                  </label>
                </div>
                {validationError ? (
                  <p className="auth-error">{validationError}</p>
                ) : null}
                <div className="avail-add-actions">
                  <Button variant="primary" size="sm" onClick={() => addSlot(day)}>
                    Add
                  </Button>
                  <Button
                    size="sm"
                    onClick={() => {
                      setAddingDay(null);
                      setNewStart('');
                      setNewEnd('');
                      setValidationError(null);
                    }}
                  >
                    Cancel
                  </Button>
                </div>
              </div>
            ) : (
              <Button
                variant="ghost"
                size="sm"
                onClick={() => {
                  setAddingDay(day);
                  setNewStart('09:00');
                  setNewEnd('17:00');
                  setValidationError(null);
                }}
              >
                + Add window
              </Button>
            )}
          </div>
        ))}
      </div>

      {save.isError ? (
        <p className="auth-error">{getApiError(save.error)}</p>
      ) : null}

      <Button
        variant="primary"
        block
        disabled={save.isPending}
        onClick={() => save.mutate()}
      >
        {save.isPending ? 'Saving...' : 'Save availability'}
      </Button>
    </div>
  );
}
