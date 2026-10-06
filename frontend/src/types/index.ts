
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

export type VehicleType = "Car" | "Bike";

export type SlotStatus = "Available" | "Reserved" | "Occupied" | "Maintenance";

export type BookingStatus = "Reserved" | "Active" | "Completed" | "Overstayed" | "Cancelled" | "Expired";

export interface ParkingSlotDto {
  id: number;
  slotNumber: string;
  status: SlotStatus;
  vehicleType: VehicleType;
  hourlyRate: number;
  parkingLotId: number;
}

export interface CreateParkingSlotDto {
  slotNumber: string;
  hourlyRate: number;
  vehicleType: VehicleType;
}

export interface VehicleDto {
  id: number;
  vehicleNumber: string;
  type: VehicleType;
  createdAt: string;
}

export interface CreateVehicleDto {
  vehicleNumber: string;
  type: VehicleType;
}

export interface BookingDto {
  id: number;
  status: BookingStatus;
  vehicleId: number;
  vehicleNumber: string;
  vehicleType: VehicleType;
  driverName: string;
  parkingSlotId: number;
  slotNumber: string;
  parkingLotId: number;
  lotName: string;
  hourlyRate: number;
  reservedAt: string;
  reservationExpiresAt: string | null;
  entryTime: string | null; // check-in: billing starts here
  exitTime: string | null;
  cancelledAt: string | null; // cancelled or expired reservations
  totalFee: number | null;
  penaltyFee: number | null;
}

export interface CreateBookingDto {
  vehicleId: number;
  parkingSlotId: number;
}

export type NotificationChannel = "Sms" | "Email";
export type NotificationStatus = "Sent" | "Simulated" | "Failed" | "Pending";

export interface NotificationDto {
  id: number;
  bookingId: number;
  type: string;
  channel: NotificationChannel;
  recipient: string;
  subject: string | null;
  message: string;
  status: NotificationStatus;
  provider: string;
  error: string | null;
  createdAt: string;
}

export interface LotUtilizationDto {
  lotId: number;
  lotName: string;
  totalSlots: number;
  occupiedSlots: number;
  reservedSlots: number;
  occupancyRatePercent: number;
  revenue: number;
}

export interface ReportOverviewDto {
  totalLots: number;
  totalSlots: number;
  occupiedSlots: number;
  reservedSlots: number;
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
