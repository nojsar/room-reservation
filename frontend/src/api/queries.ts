import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { request, toQueryString } from './client';
import type {
  BookingSettings,
  Reservation,
  ReservationFilter,
  Room,
  RoomFilter,
  RoomUsageReport,
  SaveReservationRequest,
  User,
} from './types';

// React Query keeps server data in a cache, tracks loading and error states,
// and refetches when a query key (for example the filters) changes.

export function useRooms(filter: RoomFilter = {}) {
  return useQuery({
    queryKey: ['rooms', filter],
    queryFn: () =>
      request<Room[]>(
        `/rooms${toQueryString({
          minCapacity: filter.minCapacity,
          floor: filter.floor,
          equipment: filter.equipment,
        })}`,
      ),
    // Keep showing the previous list while new filter results load, so the table does not flicker.
    placeholderData: keepPreviousData,
  });
}

export function useFloors() {
  return useQuery({ queryKey: ['floors'], queryFn: () => request<number[]>('/rooms/floors') });
}

export function useUsers() {
  return useQuery({ queryKey: ['users'], queryFn: () => request<User[]>('/users') });
}

export function useReservations(filter: ReservationFilter) {
  return useQuery({
    queryKey: ['reservations', filter],
    queryFn: () => request<Reservation[]>(`/reservations${toQueryString({ ...filter })}`),
    placeholderData: keepPreviousData,
  });
}

export function useBookingSettings() {
  return useQuery({
    queryKey: ['settings', 'booking'],
    queryFn: () => request<BookingSettings>('/settings/booking'),
    staleTime: Infinity,
  });
}

export function useRoomUsage(from?: string, to?: string) {
  return useQuery({
    queryKey: ['room-usage', from, to],
    queryFn: () => request<RoomUsageReport>(`/reports/room-usage${toQueryString({ from, to })}`),
    placeholderData: keepPreviousData,
  });
}

/** After any change to a reservation, lists that depend on reservations are loaded again. */
function useReservationMutation<TVariables>(mutationFn: (variables: TVariables) => Promise<Reservation>) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['reservations'] });
      queryClient.invalidateQueries({ queryKey: ['users'] });
      queryClient.invalidateQueries({ queryKey: ['room-usage'] });
    },
  });
}

export function useSaveReservation() {
  return useReservationMutation(({ id, body }: { id?: number; body: SaveReservationRequest }) =>
    request<Reservation>(id ? `/reservations/${id}` : '/reservations', {
      method: id ? 'PUT' : 'POST',
      body: JSON.stringify(body),
    }),
  );
}

export function useCancelReservation() {
  return useReservationMutation((id: number) => request<Reservation>(`/reservations/${id}/cancel`, { method: 'POST' }));
}

export function useEndReservation() {
  return useReservationMutation((id: number) => request<Reservation>(`/reservations/${id}/end`, { method: 'POST' }));
}
