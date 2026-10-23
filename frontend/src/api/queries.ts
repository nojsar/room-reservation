import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { request, toQueryString } from './client';
import type { Room, RoomFilter, User } from './types';

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
