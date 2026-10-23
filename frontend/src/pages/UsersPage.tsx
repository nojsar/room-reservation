import { Button, Chip, Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow } from '@mui/material';
import { Link as RouterLink } from 'react-router-dom';
import { useUsers } from '../api/queries';
import { PageHeader } from '../components/PageHeader';
import { QueryState } from '../components/QueryState';

export function UsersPage() {
  const users = useUsers();

  return (
    <>
      <PageHeader title="Users" subtitle="People who organize meetings, with their active (upcoming or ongoing) reservations." />

      <QueryState isLoading={users.isLoading} error={users.error}>
        <TableContainer component={Paper}>
          <Table>
            <TableHead>
              <TableRow>
                <TableCell>ID</TableCell>
                <TableCell>First name</TableCell>
                <TableCell>Last name</TableCell>
                <TableCell align="right">Active reservations</TableCell>
                <TableCell />
              </TableRow>
            </TableHead>
            <TableBody>
              {users.data?.map((user) => (
                <TableRow key={user.id} hover>
                  <TableCell>{user.id}</TableCell>
                  <TableCell>{user.firstName}</TableCell>
                  <TableCell>{user.lastName}</TableCell>
                  <TableCell align="right">
                    <Chip
                      size="small"
                      label={user.activeReservationCount}
                      color={user.activeReservationCount > 0 ? 'primary' : 'default'}
                    />
                  </TableCell>
                  <TableCell align="right">
                    <Button size="small" component={RouterLink} to={`/reservations?organizer=${user.id}&status=Booked`}>
                      View reservations
                    </Button>
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
