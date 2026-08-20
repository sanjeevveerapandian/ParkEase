import { apiClient } from "./client";
import type {
  AuthResponseDto,
  BookingDto,
  CreateBookingDto,
  CreateParkingLotDto,
  CreateParkingSlotDto,
  LoginDto,
  ParkingLotDto,
  ParkingSlotDto,
  RegisterDto,
  ReportOverviewDto,
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
};

export const bookingsApi = {
  create: (data: CreateBookingDto) =>
    apiClient.post<BookingDto>("/api/bookings", data),
  exit: (id: number) => apiClient.post<BookingDto>(`/api/bookings/${id}/exit`),
  mine: () => apiClient.get<BookingDto[]>("/api/bookings/mine"),
  forLot: (lotId: number) =>
    apiClient.get<BookingDto[]>(`/api/bookings/lot/${lotId}`),
};

export const reportsApi = {
  overview: () => apiClient.get<ReportOverviewDto>("/api/reports/overview"),
};
