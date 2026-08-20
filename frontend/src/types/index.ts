// Mirrors backend/ParkEase.Api/DTOs — property names are camelCase because
// ASP.NET Core's default System.Text.Json serializer lowercases the first letter.

export type Role = "Driver" | "Operator" | "Admin";

export interface AuthResponseDto {
  token: string;
  fullName: string;
  role: Role;
  expiresAt: string;
}

export interface LoginDto {
  email: string;
  password: string;
}

export interface RegisterDto {
  fullName: string;
  email: string;
  password: string;
  phoneNumber: string;
  role: Role;
}

export interface ParkingLotDto {
  id: number;
  name: string;
  address: string;
  operatorId: number;
  totalSlots: number;
  availableSlots: number;
}

export interface CreateParkingLotDto {
  name: string;
  address: string;
}

export interface ParkingSlotDto {
  id: number;
  slotNumber: string;
  status: string; // "Available" | "Occupied" (backend enum serialized as string)
  hourlyRate: number;
  parkingLotId: number;
}

export interface CreateParkingSlotDto {
  slotNumber: string;
  hourlyRate: number;
}

export interface BookingDto {
  id: number;
  vehicleNumber: string;
  parkingSlotId: number;
  slotNumber: string;
  hourlyRate: number;
  entryTime: string;
  exitTime: string | null;
  totalFee: number | null;
  penaltyFee: number | null;
  status: string; // "Active" | "Completed"
}

export interface CreateBookingDto {
  parkingSlotId: number;
  vehicleNumber: string;
}

export interface LotUtilizationDto {
  lotId: number;
  lotName: string;
  totalSlots: number;
  occupiedSlots: number;
  occupancyRatePercent: number;
  revenue: number;
}

export interface ReportOverviewDto {
  totalLots: number;
  totalSlots: number;
  occupiedSlots: number;
  availableSlots: number;
  overallOccupancyRatePercent: number;
  totalRevenue: number;
  lotBreakdown: LotUtilizationDto[];
}

export interface ApiProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  [key: string]: unknown;
}
