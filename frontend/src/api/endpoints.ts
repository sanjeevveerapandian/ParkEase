import { apiClient } from "./client";
import type {
  AuthResponseDto,
  BookingDto,
  CreateBookingDto,
  CreateParkingLotDto,
  CreateParkingSlotDto,
  CreateVehicleDto,
  LoginDto,
  NotificationDto,
  ParkingLotDto,
  ParkingSlotDto,
  RegisterDto,
  ReportOverviewDto,
  VehicleDto,
} from "../types";

export const authApi = {
  login: (data: LoginDto) =>
    apiClient.post<AuthResponseDto>("/api/auth/login", data),
  register: (data: RegisterDto) =>
    apiClient.post<AuthResponseDto>("/api/auth/register", data),
};

export const parkingLotsApi = {
  list: () => apiClient.get<ParkingLotDto[]>("/api/parkinglots"),
  browse: () => apiClient.get<ParkingLotDto[]>("/api/parkinglots/browse"),
  get: (id: number) => apiClient.get<ParkingLotDto>(`/api/parkinglots/${id}`),
  create: (data: CreateParkingLotDto) =>
    apiClient.post<ParkingLotDto>("/api/parkinglots", data),
  remove: (id: number) => apiClient.delete(`/api/parkinglots/${id}`),
  listSlots: (lotId: number) =>
    apiClient.get<ParkingSlotDto[]>(`/api/parkinglots/${lotId}/slots`),
  addSlot: (lotId: number, data: CreateParkingSlotDto) =>
    apiClient.post<ParkingSlotDto>(`/api/parkinglots/${lotId}/slots`, data),
  setSlotStatus: (lotId: number, slotId: number, status: "Available" | "Maintenance") =>
    apiClient.patch<ParkingSlotDto>(`/api/parkinglots/${lotId}/slots/${slotId}/status`, { status }),
  removeSlot: (lotId: number, slotId: number) =>
    apiClient.delete(`/api/parkinglots/${lotId}/slots/${slotId}`),
};

export const vehiclesApi = {
  mine: () => apiClient.get<VehicleDto[]>("/api/vehicles"),
  add: (data: CreateVehicleDto) => apiClient.post<VehicleDto>("/api/vehicles", data),
  remove: (id: number) => apiClient.delete(`/api/vehicles/${id}`),
};

export const bookingsApi = {
  reserve: (data: CreateBookingDto) =>
    apiClient.post<BookingDto>("/api/bookings", data),
  checkIn: (id: number) => apiClient.post<BookingDto>(`/api/bookings/${id}/check-in`),
  exit: (id: number) => apiClient.post<BookingDto>(`/api/bookings/${id}/exit`),
  cancel: (id: number) => apiClient.post<BookingDto>(`/api/bookings/${id}/cancel`),
  mine: () => apiClient.get<BookingDto[]>("/api/bookings/mine"),
  forLot: (lotId: number) =>
    apiClient.get<BookingDto[]>(`/api/bookings/lot/${lotId}`),
};

export const notificationsApi = {
  mine: () => apiClient.get<NotificationDto[]>("/api/notifications/mine"),
};

export const reportsApi = {
  overview: () => apiClient.get<ReportOverviewDto>("/api/reports/overview"),
};
