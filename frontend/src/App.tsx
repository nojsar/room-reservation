import MeetingRoomIcon from '@mui/icons-material/MeetingRoom';
import { AppBar, Box, Button, Container, Toolbar, Typography } from '@mui/material';
import { NavLink, Navigate, Route, Routes } from 'react-router-dom';
import { RoomsPage } from './pages/RoomsPage';
import { UsersPage } from './pages/UsersPage';

const NAV_ITEMS = [
  { to: '/rooms', label: 'Rooms' },
  { to: '/users', label: 'Users' },
];

export function App() {
  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default' }}>
      <AppBar position="sticky" elevation={0}>
        <Toolbar sx={{ gap: 3 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
            <MeetingRoomIcon />
            <Typography variant="h6" component="span" noWrap>
              Room Reservation
            </Typography>
          </Box>
          <Box component="nav" sx={{ display: 'flex', gap: 1 }}>
            {NAV_ITEMS.map((item) => (
              <Button
                key={item.to}
                component={NavLink}
                to={item.to}
                color="inherit"
                sx={{ opacity: 0.8, '&.active': { opacity: 1, bgcolor: 'rgba(255,255,255,0.15)' } }}
              >
                {item.label}
              </Button>
            ))}
          </Box>
        </Toolbar>
      </AppBar>

      <Container maxWidth="lg" sx={{ py: 4 }}>
        <Routes>
          <Route path="/" element={<Navigate to="/rooms" replace />} />
          <Route path="/rooms" element={<RoomsPage />} />
          <Route path="/users" element={<UsersPage />} />
          <Route path="*" element={<Navigate to="/rooms" replace />} />
        </Routes>
      </Container>
    </Box>
  );
}
