import type { BookingDto, BookingStatus, ParkingSlotDto, VehicleDto } from "../types";

export const LIVE_STATUSES: BookingStatus[] = ["Reserved", "Active"];

export const isLive = (b: BookingDto) => LIVE_STATUSES.includes(b.status);

// A slot can take a vehicle when it is free and built for that vehicle type.
export const slotFitsVehicle = (slot: ParkingSlotDto, vehicle: VehicleDto | null) =>
  slot.status === "Available" && (!vehicle || slot.vehicleType === vehicle.type);

export function formatDateTime(iso: string | null): string {
  if (!iso) return "—";
  return new Date(iso).toLocaleString(undefined, {
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export function formatDuration(ms: number): string {
  const totalMinutes = Math.max(0, Math.floor(ms / 60_000));
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  return hours > 0 ? `${hours}h ${minutes}m` : `${minutes}m`;
}

export const rupees = (amount: number) => `₹${amount.toFixed(2)}`;

// Same rule as the backend FeeCalculator, for the live "so far" estimate only.
// The authoritative fee is the one the server returns on exit.
export function estimateFee(entryTime: string, hourlyRate: number, now: number): number {
  const elapsedMs = Math.max(0, now - new Date(entryTime).getTime());
  const hours = Math.max(1, Math.ceil(elapsedMs / 3_600_000));
  const normal = Math.min(hours, 24);
  const overstay = Math.max(0, hours - 24);
  return normal * hourlyRate + overstay * hourlyRate * 1.5;
}

export const isValidIndianMobile = (raw: string) => {
  const digits = raw.replace(/[\s()-]/g, "");
  return /^(\+91|91|0)?[6-9]\d{9}$/.test(digits) || /^\+[1-9]\d{7,14}$/.test(digits);
};
