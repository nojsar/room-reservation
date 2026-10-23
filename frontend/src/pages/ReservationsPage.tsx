import AddIcon from '@mui/icons-material/Add';
import CancelOutlinedIcon from '@mui/icons-material/CancelOutlined';
import EditOutlinedIcon from '@mui/icons-material/EditOutlined';
import StopCircleOutlinedIcon from '@mui/icons-material/StopCircleOutlined';
import {
  Alert,
  Button,
  IconButton,
  MenuItem,
  Paper,
  Snackbar,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TablePagination,
  TableRow,
  TextField,
  Tooltip,
  Typography,
} from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers';
import dayjs from 'dayjs';
import { useEffect, useMemo, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { ApiError } from '../api/client';
import { useCancelReservation, useEndReservation, useReservations, useRooms, useUsers } from '../api/queries';
import { RESERVATION_STATUSES, type Reservation, type ReservationFilter, type ReservationStatus } from '../api/types';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { PageHeader } from '../components/PageHeader';
import { QueryState } from '../components/QueryState';
import { ReservationDialog } from '../components/ReservationDialog';
import { StatusChip } from '../components/StatusChip';
import { formatDateTime, formatEnd } from '../format';

type PendingAction = { kind: 'cancel' | 'end'; reservation: Reservation } | null;
type Notice = { severity: 'success' | 'error'; message: string } | null;

const DATE_FORMAT = 'YYYY-MM-DD';

export function ReservationsPage() {
  // Filters live in the URL, so a filtered view can be bookmarked or linked to (e.g. from the users page).
  const [params, setParams] = useSearchParams();
  const rooms = useRooms();
  const users = useUsers();
  const cancel = useCancelReservation();
  const end = useEndReservation();

  const [dialog, setDialog] = useState<{ open: boolean; reservation?: Reservation; roomId?: number }>({ open: false });
  const [pending, setPending] = useState<PendingAction>(null);
  const [notice, setNotice] = useState<Notice>(null);
  const [page, setPage] = useState(0);
  const [rowsPerPage, setRowsPerPage] = useState(25);

  // First visit without any filters: show today and later.
  useEffect(() => {
    if (params.size === 0) {
      setParams({ from: dayjs().format(DATE_FORMAT) }, { replace: true });
    }
  }, [params, setParams]);

  // "Book" on the rooms page links here with ?new=1&roomId=..
  useEffect(() => {
    if (params.get('new') === '1') {
      setDialog({ open: true, roomId: Number(params.get('roomId')) || undefined });
      const next = new URLSearchParams(params);
      next.delete('new');
      next.delete('roomId');
      setParams(next, { replace: true });
    }
  }, [params, setParams]);

  const filter = useMemo<ReservationFilter>(() => {
    // Until the effect above has written the default to the URL, use the same default here.
    const from = params.get('from') ?? (params.size === 0 ? dayjs().format(DATE_FORMAT) : null);
    const to = params.get('to');
    return {
      roomId: params.get('room') ? Number(params.get('room')) : undefined,
      organizerId: params.get('organizer') ? Number(params.get('organizer')) : undefined,
      status: (params.get('status') as ReservationStatus | null) ?? undefined,
      from: from ? dayjs(from).startOf('day').toISOString() : undefined,
      // The "to" date includes that whole day.
      to: to ? dayjs(to).add(1, 'day').startOf('day').toISOString() : undefined,
    };
  }, [params]);

  const reservations = useReservations(filter);

  function setFilter(key: string, value: string | number | undefined) {
    const next = new URLSearchParams(params);
    if (value === undefined || value === '') next.delete(key);
    else next.set(key, String(value));
    // Keep at least one parameter so clearing a filter does not bring back the default date.
    if (next.size === 0) next.set('all', '1');
    setParams(next);
    setPage(0);
  }

  function clearFilters() {
    setParams({ all: '1' });
    setPage(0);
  }

  function runPendingAction() {
    if (!pending) return;
    const mutation = pending.kind === 'cancel' ? cancel : end;
    mutation.mutate(pending.reservation.id, {
      onSuccess: () =>
        setNotice({
          severity: 'success',
          message: pending.kind === 'cancel' ? 'Reservation cancelled. The room is free again.' : 'Meeting ended. The room is free from now.',
        }),
      onError: (error) =>
        setNotice({ severity: 'error', message: error instanceof ApiError ? error.message : 'Could not reach the server.' }),
      onSettled: () => setPending(null),
    });
  }

  const rows = reservations.data ?? [];
  const visibleRows = rows.slice(page * rowsPerPage, page * rowsPerPage + rowsPerPage);
  const hasFilters = ['room', 'organizer', 'status', 'from', 'to'].some((key) => params.get(key));

  return (
    <>
      <PageHeader
        title="Reservations"
        subtitle="Book rooms, cancel upcoming meetings or end a meeting early."
        action={
          <Button variant="contained" startIcon={<AddIcon />} onClick={() => setDialog({ open: true })}>
            New reservation
          </Button>
        }
      />

      <Paper sx={{ p: 2, mb: 2 }}>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems={{ md: 'center' }} useFlexGap flexWrap="wrap">
          <TextField
            select
            size="small"
            label="Room"
            value={params.get('room') ?? ''}
            onChange={(e) => setFilter('room', e.target.value)}
            sx={{ minWidth: 150 }}
          >
            <MenuItem value="">All rooms</MenuItem>
            {rooms.data?.map((room) => (
              <MenuItem key={room.id} value={room.id}>
                {room.name}
              </MenuItem>
            ))}
          </TextField>

          <TextField
            select
            size="small"
            label="Organizer"
            value={params.get('organizer') ?? ''}
            onChange={(e) => setFilter('organizer', e.target.value)}
            sx={{ minWidth: 190 }}
          >
            <MenuItem value="">Everyone</MenuItem>
            {users.data?.map((user) => (
              <MenuItem key={user.id} value={user.id}>
                {user.fullName}
              </MenuItem>
            ))}
          </TextField>

          <TextField
            select
            size="small"
            label="Status"
            value={params.get('status') ?? ''}
            onChange={(e) => setFilter('status', e.target.value)}
            sx={{ minWidth: 140 }}
          >
            <MenuItem value="">Any status</MenuItem>
            {RESERVATION_STATUSES.map((status) => (
              <MenuItem key={status} value={status}>
                {status}
              </MenuItem>
            ))}
          </TextField>

          <DatePicker
            label="From"
            value={params.get('from') ? dayjs(params.get('from')) : null}
            onChange={(value) => setFilter('from', value?.isValid() ? value.format(DATE_FORMAT) : undefined)}
            slotProps={{ textField: { size: 'small', sx: { width: 165 } }, field: { clearable: true } }}
          />
          <DatePicker
            label="To"
            value={params.get('to') ? dayjs(params.get('to')) : null}
            onChange={(value) => setFilter('to', value?.isValid() ? value.format(DATE_FORMAT) : undefined)}
            slotProps={{ textField: { size: 'small', sx: { width: 165 } }, field: { clearable: true } }}
          />

          <Button onClick={clearFilters} disabled={!hasFilters}>
            Clear filters
          </Button>
        </Stack>
      </Paper>

      <QueryState isLoading={reservations.isLoading} error={reservations.error}>
        <TableContainer component={Paper}>
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Room</TableCell>
                <TableCell>Organizer</TableCell>
                <TableCell>Title</TableCell>
                <TableCell>Start</TableCell>
                <TableCell>End</TableCell>
                <TableCell align="right">Attendees</TableCell>
                <TableCell>Status</TableCell>
                <TableCell align="right">Actions</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {rows.length === 0 && (
                <TableRow>
                  <TableCell colSpan={8}>
                    <Typography color="text.secondary" sx={{ py: 2 }}>
                      No reservations match these filters.
                    </Typography>
                  </TableCell>
                </TableRow>
              )}
              {visibleRows.map((reservation) => (
                <TableRow key={reservation.id} hover>
                  <TableCell>{reservation.roomName}</TableCell>
                  <TableCell>{reservation.organizerName}</TableCell>
                  <TableCell>{reservation.title}</TableCell>
                  <TableCell sx={{ whiteSpace: 'nowrap' }}>{formatDateTime(reservation.start)}</TableCell>
                  <TableCell sx={{ whiteSpace: 'nowrap' }}>
                    {formatEnd(reservation.start, reservation.end)}
                    {reservation.endedEarlyAt && (
                      <Typography variant="caption" color="text.secondary" display="block">
                        ended {formatEnd(reservation.start, reservation.endedEarlyAt)}
                      </Typography>
                    )}
                  </TableCell>
                  <TableCell align="right">{reservation.attendees}</TableCell>
                  <TableCell>
                    <StatusChip status={reservation.status} />
                  </TableCell>
                  <TableCell align="right" sx={{ whiteSpace: 'nowrap' }}>
                    {reservation.canEdit && (
                      <Tooltip title="Edit">
                        <IconButton size="small" onClick={() => setDialog({ open: true, reservation })}>
                          <EditOutlinedIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    )}
                    {reservation.canCancel && (
                      <Tooltip title="Cancel reservation">
                        <IconButton size="small" color="error" onClick={() => setPending({ kind: 'cancel', reservation })}>
                          <CancelOutlinedIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    )}
                    {reservation.canEndEarly && (
                      <Tooltip title="End meeting now">
                        <IconButton size="small" color="warning" onClick={() => setPending({ kind: 'end', reservation })}>
                          <StopCircleOutlinedIcon fontSize="small" />
                        </IconButton>
                      </Tooltip>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          <TablePagination
            component="div"
            count={rows.length}
            page={Math.min(page, Math.max(0, Math.ceil(rows.length / rowsPerPage) - 1))}
            onPageChange={(_, next) => setPage(next)}
            rowsPerPage={rowsPerPage}
            onRowsPerPageChange={(e) => {
              setRowsPerPage(Number(e.target.value));
              setPage(0);
            }}
            rowsPerPageOptions={[10, 25, 50, 100]}
          />
        </TableContainer>
      </QueryState>

      <ReservationDialog
        open={dialog.open}
        reservation={dialog.reservation}
        initialRoomId={dialog.roomId}
        onClose={() => setDialog({ open: false })}
        onSaved={(saved) => {
          setDialog({ open: false });
          setNotice({ severity: 'success', message: `${saved.roomName} is booked for "${saved.title}".` });
        }}
      />

      <ConfirmDialog
        open={pending !== null}
        title={pending?.kind === 'cancel' ? 'Cancel this reservation?' : 'End this meeting now?'}
        message={
          pending
            ? pending.kind === 'cancel'
              ? `"${pending.reservation.title}" in ${pending.reservation.roomName} will be cancelled and the room becomes free.`
              : `"${pending.reservation.title}" in ${pending.reservation.roomName} will end now and the rest of the time becomes free.`
            : ''
        }
        confirmLabel={pending?.kind === 'cancel' ? 'Cancel reservation' : 'End meeting'}
        busy={cancel.isPending || end.isPending}
        onConfirm={runPendingAction}
        onClose={() => setPending(null)}
      />

      {notice && (
        <Snackbar open autoHideDuration={5000} onClose={() => setNotice(null)} anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}>
          <Alert severity={notice.severity} variant="filled" onClose={() => setNotice(null)}>
            {notice.message}
          </Alert>
        </Snackbar>
      )}
    </>
  );
}
