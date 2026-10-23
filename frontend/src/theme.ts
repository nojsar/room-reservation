import { createTheme } from '@mui/material';

export const theme = createTheme({
  palette: {
    primary: { main: '#1d4f91' },
    secondary: { main: '#00897b' },
    background: { default: '#f3f5f9' },
  },
  shape: { borderRadius: 8 },
  typography: {
    fontFamily: '"Segoe UI", system-ui, -apple-system, Roboto, Arial, sans-serif',
    h4: { fontWeight: 700 },
  },
  components: {
    MuiTableCell: {
      styleOverrides: {
        head: { fontWeight: 600, color: '#4a5568' },
      },
    },
    MuiPaper: {
      defaultProps: { variant: 'outlined' },
    },
  },
});
