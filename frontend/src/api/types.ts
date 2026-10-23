// Types that mirror the back end DTOs.

export const EQUIPMENT = ['Projector', 'TvScreen', 'VideoConferencing', 'Whiteboard'] as const;
export type Equipment = (typeof EQUIPMENT)[number];

export interface Room {
  id: number;
  name: string;
  capacity: number;
  floor: number;
  location: string;
  equipment: Equipment[];
  isActive: boolean;
}

export interface RoomFilter {
  minCapacity?: number;
  equipment?: Equipment[];
  floor?: number;
}

export interface User {
  id: number;
  firstName: string;
  lastName: string;
  fullName: string;
  activeReservationCount: number;
}

export interface ApiErrorItem {
  code: string;
  message: string;
}
