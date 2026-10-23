import WarningAmberIcon from '@mui/icons-material/WarningAmber';
import {
  Alert,
  Box,
  Button,
  Chip,
  LinearProgress,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import { DatePicker } from '@mui/x-date-pickers';
import dayjs, { type Dayjs } from 'dayjs';
import { useState } from 'react';
import { useRoomUsage } from '../api/queries';
import { PageHeader } from '../components/PageHeader';
import { QueryState } from '../components/QueryState';
import { formatHours } from '../format';

export function UsagePage() {
  const [from, setFrom] = useState<Dayjs | null>(null);
  const [to, setTo] = useState<Dayjs | null>(null);

  const report = useRoomUsage(
    from?.isValid() ? from.startOf('day').toISOString() : undefined,
    to?.isValid() ? to.add(1, 'day').startOf('day').toISOString() : undefined,
  );

  const data = report.data;
  const flagged = data?.rooms.filter((room) => room.isOftenBookedButRarelyUsed) ?? [];

  return (
    <>
      <PageHeader
        title="Room usage"
        subtitle="Booked time compared with the time rooms were actually used. Cancelled bookings count as unused, and meetings that ended early count only until they ended."
      />

      <Paper sx={{ p: 2, mb: 2 }}>
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ sm: 'center' }}>
          <DatePicker
            label="From"
            value={from}
            onChange={setFrom}
            slotProps={{ textField: { size: 'small', sx: { width: 170 } } }}
          />
          <DatePicker
            label="To"
            value={to}
            onChange={setTo}
            disableFuture
            slotProps={{ textField: { size: 'small', sx: { width: 170 } } }}
          />
          <Button
            onClick={() => {
              setFrom(null);
              setTo(null);
            }}
            disabled={!from && !to}
          >
            Last 30 days
          </Button>
          {data && (
            <Typography variant="body2" color="text.secondary">
              Period: {dayjs(data.from).format('D MMM YYYY')} to {dayjs(data.to).format('D MMM YYYY, HH:mm')}
            </Typography>
          )}
        </Stack>
      </Paper>

      <QueryState isLoading={report.isLoading} error={report.error}>
        {data && (
          <>
            {flagged.length > 0 ? (
              <Alert severity="warning" icon={<WarningAmberIcon />} sx={{ mb: 2 }}>
                <strong>{flagged.map((room) => room.roomName).join(', ')}</strong>{' '}
                {flagged.length === 1 ? 'is' : 'are'} often booked but rarely used: less than {data.lowUsagePercent}% of the
                booked time was used (rooms with at least {data.minBookings} bookings).
              </Alert>
            ) : (
              <Alert severity="success" sx={{ mb: 2 }}>
                No room is flagged in this period. Every room with at least {data.minBookings} bookings used at least{' '}
                {data.lowUsagePercent}% of its booked time.
              </Alert>
            )}

            <TableContainer component={Paper}>
              <Table>
                <TableHead>
                  <TableRow>
                    <TableCell>Room</TableCell>
                    <TableCell align="right">Bookings</TableCell>
                    <TableCell align="right">Cancelled</TableCell>
                    <TableCell align="right">Ended early</TableCell>
                    <TableCell align="right">Booked</TableCell>
                    <TableCell align="right">Used</TableCell>
                    <TableCell sx={{ width: 220 }}>Usage</TableCell>
                  </TableRow>
                </TableHead>
                <TableBody>
                  {data.rooms.map((room) => (
                    <TableRow key={room.roomId} hover>
                      <TableCell>
                        <Typography fontWeight={600}>{room.roomName}</Typography>
                        <Typography variant="body2" color="text.secondary">
                          Floor {room.floor}
                          {!room.isActive && ' · inactive'}
                        </Typography>
                      </TableCell>
                      <TableCell align="right">{room.bookings}</TableCell>
                      <TableCell align="right">{room.cancellations}</TableCell>
                      <TableCell align="right">{room.endedEarly}</TableCell>
                      <TableCell align="right">{formatHours(room.bookedHours)}</TableCell>
                      <TableCell align="right">{formatHours(room.usedHours)}</TableCell>
                      <TableCell>
                        {room.usagePercent === null ? (
                          <Typography variant="body2" color="text.secondary">
                            No bookings
                          </Typography>
                        ) : (
                          <Stack spacing={0.5}>
                            <Stack direction="row" justifyContent="space-between" alignItems="center">
                              <Typography variant="body2">{room.usagePercent}%</Typography>
                              {room.isOftenBookedButRarelyUsed && (
                                <Chip size="small" color="warning" label="Rarely used" />
                              )}
                            </Stack>
                            <Box>
                              <LinearProgress
                                variant="determinate"
                                value={room.usagePercent}
                                color={room.isOftenBookedButRarelyUsed ? 'warning' : 'primary'}
                                sx={{ height: 8, borderRadius: 4 }}
                              />
                            </Box>
                          </Stack>
                        )}
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            </TableContainer>
          </>
        )}
      </QueryState>
    </>
  );
}
