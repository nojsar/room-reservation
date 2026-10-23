import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  MenuItem,
  Stack,
  TextField,
  Typography,
} from '@mui/material';
import { DateTimePicker } from '@mui/x-date-pickers';
import dayjs, { type Dayjs } from 'dayjs';
import { useEffect, useState, type FormEvent } from 'react';
import { ApiError } from '../api/client';
import { useBookingSettings, useRooms, useSaveReservation, useUsers } from '../api/queries';
import type { Reservation } from '../api/types';

interface ReservationDialogProps {
  open: boolean;
  /** Set to edit an existing reservation; empty to create a new one. */
  reservation?: Reservation;
  /** Preselected room for a new reservation, e.g. from the Book button on the rooms page. */
  initialRoomId?: number;
  onClose: () => void;
  onSaved: (reservation: Reservation) => void;
}

interface FormState {
  roomId: number | '';
  organizerId: number | '';
  title: string;
  start: Dayjs | null;
  end: Dayjs | null;
  attendees: string;
}

function initialState(reservation?: Reservation, initialRoomId?: number): FormState {
  if (reservation) {
    return {
      roomId: reservation.roomId,
      organizerId: reservation.organizerId,
      title: reservation.title,
      start: dayjs(reservation.start),
      end: dayjs(reservation.end),
      attendees: String(reservation.attendees),
    };
  }

  const nextHour = dayjs().add(1, 'hour').startOf('hour');
  return {
    roomId: initialRoomId ?? '',
    organizerId: '',
    title: '',
    start: nextHour,
    end: nextHour.add(1, 'hour'),
    attendees: '2',
  };
}

export function ReservationDialog({ open, reservation, initialRoomId, onClose, onSaved }: ReservationDialogProps) {
  const rooms = useRooms();
  const users = useUsers();
  const settings = useBookingSettings();
  const save = useSaveReservation();
  const [form, setForm] = useState<FormState>(() => initialState(reservation, initialRoomId));

  // Start from a fresh form every time the dialog opens.
  useEffect(() => {
    if (open) {
      setForm(initialState(reservation, initialRoomId));
      save.reset();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open, reservation, initialRoomId]);

  const activeRooms = rooms.data?.filter((room) => room.isActive || room.id === form.roomId) ?? [];
  const selectedRoom = rooms.data?.find((room) => room.id === form.roomId);
  const isEdit = reservation !== undefined;

  function update<K extends keyof FormState>(key: K, value: FormState[K]) {
    setForm((current) => ({ ...current, [key]: value }));
  }

  function changeStart(value: Dayjs | null) {
    // Moving the start moves the end too, keeping the meeting length.
    setForm((current) => {
      if (!value || !current.start || !current.end) return { ...current, start: value };
      const length = current.end.diff(current.start);
      return { ...current, start: value, end: value.add(length, 'millisecond') };
    });
  }

  function submit(event: FormEvent) {
    event.preventDefault();
    if (form.roomId === '' || form.organizerId === '' || !form.start || !form.end) return;

    save.mutate(
      {
        id: reservation?.id,
        body: {
          roomId: form.roomId,
          organizerId: form.organizerId,
          title: form.title,
          start: form.start.toISOString(),
          end: form.end.toISOString(),
          attendees: Number(form.attendees),
        },
      },
      { onSuccess: onSaved },
    );
  }

  const errors = save.error instanceof ApiError ? save.error.errors : save.error ? [{ code: 'NETWORK', message: 'Could not reach the server.' }] : [];
  const canSubmit = form.roomId !== '' && form.organizerId !== '' && form.title.trim() !== '' && form.start && form.end && form.attendees !== '';

  return (
    <Dialog open={open} onClose={onClose} maxWidth="sm" fullWidth component="form" onSubmit={submit}>
      <DialogTitle>{isEdit ? 'Edit reservation' : 'New reservation'}</DialogTitle>
      <DialogContent>
        <Stack spacing={2.5} sx={{ pt: 1 }}>
          {errors.length > 0 && (
            <Alert severity="error">
              <Typography variant="subtitle2" gutterBottom>
                The reservation could not be saved:
              </Typography>
              <Stack component="ul" spacing={0.5} sx={{ m: 0, pl: 2 }}>
                {errors.map((error) => (
                  <li key={error.code + error.message}>{error.message}</li>
                ))}
              </Stack>
            </Alert>
          )}

          <TextField
            select
            label="Room"
            value={form.roomId}
            onChange={(e) => update('roomId', Number(e.target.value))}
            helperText={selectedRoom ? `Fits ${selectedRoom.capacity} people, floor ${selectedRoom.floor}` : 'Only active rooms can be booked'}
            required
          >
            {activeRooms.map((room) => (
              <MenuItem key={room.id} value={room.id}>
                {room.name} ({room.capacity} people)
              </MenuItem>
            ))}
          </TextField>

          <TextField
            select
            label="Organizer"
            value={form.organizerId}
            onChange={(e) => update('organizerId', Number(e.target.value))}
            required
          >
            {users.data?.map((user) => (
              <MenuItem key={user.id} value={user.id}>
                {user.fullName}
              </MenuItem>
            ))}
          </TextField>

          <TextField
            label="Meeting title"
            value={form.title}
            onChange={(e) => update('title', e.target.value)}
            inputProps={{ maxLength: 200 }}
            required
          />

          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
            <DateTimePicker label="Start" value={form.start} onChange={changeStart} ampm={false} sx={{ flex: 1 }} />
            <DateTimePicker label="End" value={form.end} onChange={(value) => update('end', value)} ampm={false} sx={{ flex: 1 }} />
          </Stack>

          <TextField
            label="Attendees"
            type="number"
            value={form.attendees}
            onChange={(e) => update('attendees', e.target.value)}
            inputProps={{ min: 1 }}
            required
          />

          {settings.data && (
            <Typography variant="body2" color="text.secondary">
              A reservation can last up to {settings.data.maxDurationDays} days and start up to{' '}
              {settings.data.maxDaysAhead} days from now. All rules are checked when you save.
            </Typography>
          )}
        </Stack>
      </DialogContent>
      <DialogActions sx={{ px: 3, pb: 2 }}>
        <Button onClick={onClose}>Close</Button>
        <Button type="submit" variant="contained" disabled={!canSubmit || save.isPending}>
          {save.isPending ? 'Saving…' : isEdit ? 'Save changes' : 'Book room'}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
