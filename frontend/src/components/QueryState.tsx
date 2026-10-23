import { Alert, Box, CircularProgress } from '@mui/material';
import type { ReactNode } from 'react';
import { ApiError } from '../api/client';

interface QueryStateProps {
  isLoading: boolean;
  error: unknown;
  children: ReactNode;
}

/** Shows a spinner while loading and the API error messages when a request fails. */
export function QueryState({ isLoading, error, children }: QueryStateProps) {
  if (isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (error) {
    return <ErrorAlert error={error} />;
  }

  return <>{children}</>;
}

export function ErrorAlert({ error }: { error: unknown }) {
  const messages =
    error instanceof ApiError
      ? error.errors.map((e) => e.message)
      : ['Could not reach the server. Is the back end running?'];

  return (
    <Alert severity="error">
      {messages.length === 1 ? (
        messages[0]
      ) : (
        <Box component="ul" sx={{ m: 0, pl: 2 }}>
          {messages.map((message) => (
            <li key={message}>{message}</li>
          ))}
        </Box>
      )}
    </Alert>
  );
}
