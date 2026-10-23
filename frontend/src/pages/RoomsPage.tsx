import {
  Button,
  Chip,
  FormControl,
  InputLabel,
  ListItemText,
  MenuItem,
  OutlinedInput,
  Paper,
  Select,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  TextField,
  Typography,
} from '@mui/material';
import Checkbox from '@mui/material/Checkbox';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useFloors, useRooms } from '../api/queries';
import { EQUIPMENT, type Equipment } from '../api/types';
import { EQUIPMENT_LABELS, EquipmentChips } from '../components/EquipmentChips';
import { PageHeader } from '../components/PageHeader';
import { QueryState } from '../components/QueryState';

export function RoomsPage() {
  const [minCapacity, setMinCapacity] = useState('');
  const [equipment, setEquipment] = useState<Equipment[]>([]);
  const [floor, setFloor] = useState<number | ''>('');

  const rooms = useRooms({
    minCapacity: minCapacity === '' ? undefined : Number(minCapacity),
    equipment,
    floor: floor === '' ? undefined : floor,
  });
  const floors = useFloors();
  const navigate = useNavigate();

  const hasFilters = minCapacity !== '' || equipment.length > 0 || floor !== '';

  function clearFilters() {
    setMinCapacity('');
    setEquipment([]);
    setFloor('');
  }

  return (
    <>
      <PageHeader title="Rooms" subtitle="All meeting rooms in the office. Inactive rooms cannot be booked." />

      <Paper sx={{ p: 2, mb: 2 }}>
        <Stack direction={{ xs: 'column', md: 'row' }} spacing={2} alignItems={{ md: 'center' }}>
          <TextField
            label="Minimum capacity"
            type="number"
            size="small"
            value={minCapacity}
            onChange={(e) => setMinCapacity(e.target.value)}
            inputProps={{ min: 1 }}
            sx={{ width: { md: 170 } }}
          />

          <FormControl size="small" sx={{ minWidth: 260 }}>
            <InputLabel id="equipment-label">Equipment</InputLabel>
            <Select
              labelId="equipment-label"
              multiple
              value={equipment}
              onChange={(e) => setEquipment(e.target.value as Equipment[])}
              input={<OutlinedInput label="Equipment" />}
              renderValue={(selected) => selected.map((item) => EQUIPMENT_LABELS[item]).join(', ')}
            >
              {EQUIPMENT.map((item) => (
                <MenuItem key={item} value={item}>
                  <Checkbox checked={equipment.includes(item)} size="small" />
                  <ListItemText primary={EQUIPMENT_LABELS[item]} />
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <FormControl size="small" sx={{ minWidth: 140 }}>
            <InputLabel id="floor-label">Floor</InputLabel>
            <Select
              labelId="floor-label"
              label="Floor"
              value={floor}
              onChange={(e) => setFloor(e.target.value === '' ? '' : Number(e.target.value))}
            >
              <MenuItem value="">All floors</MenuItem>
              {floors.data?.map((f) => (
                <MenuItem key={f} value={f}>
                  Floor {f}
                </MenuItem>
              ))}
            </Select>
          </FormControl>

          <Button onClick={clearFilters} disabled={!hasFilters}>
            Clear filters
          </Button>
        </Stack>
      </Paper>

      <QueryState isLoading={rooms.isLoading} error={rooms.error}>
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>ID</TableCell>
                <TableCell>Name</TableCell>
                <TableCell align="right">Capacity</TableCell>
                <TableCell>Floor / location</TableCell>
                <TableCell>Equipment</TableCell>
                <TableCell>Status</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {rooms.data?.length === 0 && (
                <TableRow>
                  <TableCell colSpan={7}>
                    <Typography color="text.secondary">No rooms match these filters.</Typography>
                  </TableCell>
                </TableRow>
              )}
              {rooms.data?.map((room) => (
                <TableRow key={room.id} hover>
                  <TableCell>{room.id}</TableCell>
                  <TableCell>
                    <Typography fontWeight={600}>{room.name}</Typography>
                  </TableCell>
                  <TableCell align="right">{room.capacity}</TableCell>
                  <TableCell>
                    Floor {room.floor}
                    <Typography variant="body2" color="text.secondary">
                      {room.location}
                    </Typography>
                  </TableCell>
                  <TableCell>
                    <EquipmentChips equipment={room.equipment} />
                  </TableCell>
                  <TableCell>
                    <Chip
                      size="small"
                      label={room.isActive ? 'Active' : 'Inactive'}
                      color={room.isActive ? 'success' : 'default'}
                      variant={room.isActive ? 'filled' : 'outlined'}
                    />
                  </TableCell>
                  <TableCell align="right">
                    {room.isActive && (
                      <Button size="small" variant="outlined" onClick={() => navigate(`/reservations?new=1&roomId=${room.id}`)}>
                        Book
                      </Button>
                    )}
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      </QueryState>
    </>
  );
}
