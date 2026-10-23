import { Chip } from '@mui/material';
import type { ReservationStatus } from '../api/types';

const COLORS: Record<ReservationStatus, 'primary' | 'success' | 'default'> = {
  Booked: 'primary',
  Completed: 'success',
  Cancelled: 'default',
};

export function StatusChip({ status }: { status: ReservationStatus }) {
  return (
    <Chip
      size="small"
      label={status}
      color={COLORS[status]}
      variant={status === 'Booked' ? 'filled' : 'outlined'}
    />
  );
}
