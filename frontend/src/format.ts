import dayjs from 'dayjs';

/** "26 Oct 2026, 09:00" in the viewer's local time. */
export function formatDateTime(iso: string): string {
  return dayjs(iso).format('D MMM YYYY, HH:mm');
}

/** The end time: only the time when it is on the same day as the start. */
export function formatEnd(startIso: string, endIso: string): string {
  return dayjs(startIso).isSame(endIso, 'day') ? dayjs(endIso).format('HH:mm') : formatDateTime(endIso);
}

export function formatHours(hours: number): string {
  return `${hours.toFixed(1)} h`;
}
