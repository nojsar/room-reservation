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

export const RESERVATION_STATUSES = ['Booked', 'Completed', 'Cancelled'] as const;
export type ReservationStatus = (typeof RESERVATION_STATUSES)[number];

export interface Reservation {
  id: number;
  roomId: number;
  roomName: string;
  organizerId: number;
  organizerName: string;
  title: string;
  start: string;
  end: string;
  endedEarlyAt: string | null;
  cancelledAt: string | null;
  attendees: number;
  status: ReservationStatus;
  canEdit: boolean;
  canCancel: boolean;
  canEndEarly: boolean;
}

export interface ReservationFilter {
  roomId?: number;
  organizerId?: number;
  status?: ReservationStatus;
  /** ISO date-times. */
  from?: string;
  to?: string;
}

export interface SaveReservationRequest {
  roomId: number;
  organizerId: number;
  title: string;
  start: string;
  end: string;
  attendees: number;
}

export interface BookingSettings {
  maxDurationDays: number;
  maxDaysAhead: number;
}

export interface RoomUsage {
  roomId: number;
  roomName: string;
  floor: number;
  isActive: boolean;
  bookings: number;
  cancellations: number;
  endedEarly: number;
  bookedHours: number;
  usedHours: number;
  usagePercent: number | null;
  isOftenBookedButRarelyUsed: boolean;
}

export interface RoomUsageReport {
  from: string;
  to: string;
  lowUsagePercent: number;
  minBookings: number;
  rooms: RoomUsage[];
}

export interface ApiErrorItem {
  code: string;
  message: string;
}
