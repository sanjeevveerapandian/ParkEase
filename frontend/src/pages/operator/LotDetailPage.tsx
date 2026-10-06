import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { useParams } from "react-router-dom";
import { CheckCircle2, Clock, ParkingSquare, Plus, ReceiptText, Trash2, Wrench, XCircle } from "lucide-react";
import { bookingsApi, parkingLotsApi } from "../../api/endpoints";
import { getErrorMessage } from "../../api/errors";
import { useToast } from "../../context/ToastContext";
import SlotSeatMap from "../../components/SlotSeatMap";
import { LotPageShell } from "../../components/LotPageShell";
import { BookingsTable } from "../../components/BookingsTable";
import type { BookingAction } from "../../components/BookingsTable";
import { BookingStatusBadge, VehicleIcon } from "../../components/BookingParts";
import { isLive } from "../../lib/booking";
import {
  Badge,
  Button,
  Card,
  EmptyState,
  ErrorMessage,
  Field,
  PageContainer,
  Select,
  StatCard,
  TextInput,
} from "../../components/ui";
import type { BookingDto, ParkingLotDto, ParkingSlotDto, VehicleType } from "../../types";

const doneMessages: Record<BookingAction, string> = {
  checkIn: "Vehicle checked in. Billing has started.",
  exit: "Vehicle exited. Receipt sent to the driver.",
  cancel: "Reservation cancelled. Slot released.",
};

export default function LotDetailPage() {
  const { id } = useParams<{ id: string }>();
  const lotId = Number(id);
  const toast = useToast();

  const [lot, setLot] = useState<ParkingLotDto | null>(null);
  const [slots, setSlots] = useState<ParkingSlotDto[]>([]);
  const [bookings, setBookings] = useState<BookingDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [slotNumber, setSlotNumber] = useState("");
  const [hourlyRate, setHourlyRate] = useState("");
  const [vehicleType, setVehicleType] = useState<VehicleType>("Car");
  const [addError, setAddError] = useState("");
  const [adding, setAdding] = useState(false);

  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [busy, setBusy] = useState<{ id: number; action: BookingAction } | null>(null);
  const [slotBusy, setSlotBusy] = useState(false);

  async function refresh() {
    const [lotRes, slotsRes, bookingsRes] = await Promise.all([
      parkingLotsApi.get(lotId),
      parkingLotsApi.listSlots(lotId),
      bookingsApi.forLot(lotId),
    ]);
    setLot(lotRes.data);
    setSlots(slotsRes.data);
    setBookings(bookingsRes.data);
  }

  async function loadAll() {
    setLoading(true);
    setError("");
    try {
      await refresh();
    } catch (err) {
      setError(getErrorMessage(err, "Could not load lot details."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (!lotId) return;
    loadAll();
    // Live console: keep occupancy and holds current.
    const timer = window.setInterval(() => refresh().catch(() => undefined), 15_000);
    return () => window.clearInterval(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [lotId]);

  async function handleAddSlot(e: FormEvent) {
    e.preventDefault();
    setAddError("");
    setAdding(true);
    try {
      await parkingLotsApi.addSlot(lotId, { slotNumber, hourlyRate: Number(hourlyRate), vehicleType });
      toast.success(`Added ${vehicleType.toLowerCase()} slot ${slotNumber.toUpperCase()}.`);
      setSlotNumber("");
      await refresh();
    } catch (err) {
      setAddError(getErrorMessage(err, "Could not add slot."));
    } finally {
      setAdding(false);
    }
  }

  async function handleBookingAction(action: BookingAction, booking: BookingDto) {
    setBusy({ id: booking.id, action });
    try {
      if (action === "checkIn") await bookingsApi.checkIn(booking.id);
      else if (action === "exit") await bookingsApi.exit(booking.id);
      else await bookingsApi.cancel(booking.id);
      toast.success(doneMessages[action]);
    } catch (err) {
      toast.error(getErrorMessage(err, "That didn't work. Please try again."));
    } finally {
      setBusy(null);
      await refresh().catch(() => undefined);
    }
  }

  async function toggleMaintenance(slot: ParkingSlotDto) {
    setSlotBusy(true);
    try {
      await parkingLotsApi.setSlotStatus(lotId, slot.id, slot.status === "Maintenance" ? "Available" : "Maintenance");
      await refresh();
    } catch (err) {
      toast.error(getErrorMessage(err, "Could not change slot status."));
    } finally {
      setSlotBusy(false);
    }
  }

  async function deleteSlot(slot: ParkingSlotDto) {
    if (!window.confirm(`Delete slot ${slot.slotNumber}?`)) return;
    setSlotBusy(true);
    try {
      await parkingLotsApi.removeSlot(lotId, slot.id);
      toast.success(`Deleted slot ${slot.slotNumber}.`);
      setSelectedId(null);
      await refresh();
    } catch (err) {
      toast.error(getErrorMessage(err, "Could not delete slot."));
    } finally {
      setSlotBusy(false);
    }
  }

  const count = (s: ParkingSlotDto["status"]) => slots.filter((x) => x.status === s).length;
  const selected = slots.find((s) => s.id === selectedId) ?? null;
  const selectedBooking = selected ? bookings.find((b) => b.parkingSlotId === selected.id && isLive(b)) : undefined;
  const canEditSlot = selected && (selected.status === "Available" || selected.status === "Maintenance");

  return (
    <PageContainer>
      <LotPageShell backTo="/operator/lots" lot={lot} lotId={lotId} loading={loading} error={error}>
        <div className="mb-6 grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-5">
          <StatCard icon={<ParkingSquare size={18} />} label="Total slots" value={slots.length} tone="slate" />
          <StatCard icon={<CheckCircle2 size={18} />} label="Available" value={count("Available")} tone="emerald" />
          <StatCard icon={<Clock size={18} />} label="Reserved" value={count("Reserved")} tone="amber" />
          <StatCard icon={<XCircle size={18} />} label="Occupied" value={count("Occupied")} tone="rose" />
          <StatCard icon={<Wrench size={18} />} label="Maintenance" value={count("Maintenance")} tone="slate" />
        </div>

        <Card className="mb-6">
          <h2 className="mb-3 flex items-center gap-1.5 text-base font-semibold text-slate-900">
            <Plus size={16} /> Add a slot
          </h2>
          <form onSubmit={handleAddSlot} className="flex flex-wrap items-end gap-3">
            <div className="min-w-[140px]">
              <Field label="Slot number" htmlFor="slotNumber">
                <TextInput
                  id="slotNumber"
                  required
                  value={slotNumber}
                  onChange={(e) => setSlotNumber(e.target.value.toUpperCase())}
                  placeholder="A1"
                />
              </Field>
            </div>
            <div className="min-w-[140px]">
              <Field label="Vehicle type" htmlFor="slotType">
                <Select id="slotType" value={vehicleType} onChange={(e) => setVehicleType(e.target.value as VehicleType)}>
                  <option value="Car">Car</option>
                  <option value="Bike">Bike</option>
                </Select>
              </Field>
            </div>
            <div className="min-w-[140px]">
              <Field label="Hourly rate (₹)" htmlFor="hourlyRate">
                <TextInput
                  id="hourlyRate"
                  type="number"
                  min={1}
                  step="0.01"
                  required
                  value={hourlyRate}
                  onChange={(e) => setHourlyRate(e.target.value)}
                  placeholder="50"
                />
              </Field>
            </div>
            <Button type="submit" loading={adding} className="mb-4">
              {adding ? "Adding…" : "Add slot"}
            </Button>
          </form>
          <ErrorMessage>{addError}</ErrorMessage>
        </Card>

        <div className="mb-6 grid gap-6 lg:grid-cols-[1fr_320px]">
          <Card>
            <h2 className="mb-4 text-base font-semibold text-slate-900">Slots</h2>
            {slots.length === 0 ? (
              <EmptyState icon={<ParkingSquare size={20} />} title="No slots yet" description="Add your first slot above." />
            ) : (
              <SlotSeatMap
                slots={slots}
                selectedId={selectedId}
                onSelect={(s) => setSelectedId((prev) => (prev === s.id ? null : s.id))}
                canSelect={() => true}
              />
            )}
          </Card>

          <Card>
            <h2 className="mb-4 text-base font-semibold text-slate-900">Slot details</h2>
            {!selected ? (
              <p className="text-sm text-slate-500">Select a slot to see who it's mapped to and manage it.</p>
            ) : (
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <p className="text-lg font-bold text-slate-900">Slot {selected.slotNumber}</p>
                  <Badge tone={selected.status === "Available" ? "green" : selected.status === "Maintenance" ? "slate" : "amber"} dot>
                    {selected.status}
                  </Badge>
                </div>
                <p className="flex items-center gap-1.5 text-sm text-slate-500">
                  <VehicleIcon type={selected.vehicleType} size={14} /> {selected.vehicleType} slot · ₹{selected.hourlyRate}/hr
                </p>

                {selectedBooking && (
                  <div className="space-y-3 rounded-xl bg-slate-50 p-4">
                    <div className="flex items-center justify-between">
                      <p className="text-sm font-semibold text-slate-900">{selectedBooking.vehicleNumber}</p>
                      <BookingStatusBadge status={selectedBooking.status} />
                    </div>
                    <p className="text-sm text-slate-500">Driver: {selectedBooking.driverName}</p>
                    <div className="flex flex-wrap gap-2">
                      {selectedBooking.status === "Reserved" && (
                        <>
                          <Button
                            loading={busy?.id === selectedBooking.id && busy.action === "checkIn"}
                            disabled={!!busy}
                            onClick={() => handleBookingAction("checkIn", selectedBooking)}
                          >
                            Check in
                          </Button>
                          <Button
                            variant="secondary"
                            loading={busy?.id === selectedBooking.id && busy.action === "cancel"}
                            disabled={!!busy}
                            onClick={() => handleBookingAction("cancel", selectedBooking)}
                          >
                            Cancel
                          </Button>
                        </>
                      )}
                      {selectedBooking.status === "Active" && (
                        <Button
                          variant="danger"
                          loading={busy?.id === selectedBooking.id && busy.action === "exit"}
                          disabled={!!busy}
                          onClick={() => handleBookingAction("exit", selectedBooking)}
                        >
                          Exit vehicle
                        </Button>
                      )}
                    </div>
                  </div>
                )}

                {canEditSlot && (
                  <div className="flex flex-wrap gap-2">
                    <Button
                      variant="secondary"
                      icon={<Wrench size={14} />}
                      loading={slotBusy}
                      onClick={() => toggleMaintenance(selected)}
                    >
                      {selected.status === "Maintenance" ? "Back in service" : "Set maintenance"}
                    </Button>
                    <Button variant="ghost" icon={<Trash2 size={14} />} disabled={slotBusy} onClick={() => deleteSlot(selected)}>
                      Delete
                    </Button>
                  </div>
                )}
              </div>
            )}
          </Card>
        </div>

        <Card className="!p-0 overflow-hidden">
          <h2 className="flex items-center gap-1.5 px-6 pt-6 pb-3 text-base font-semibold text-slate-900">
            <ReceiptText size={16} /> Bookings
          </h2>
          {bookings.length === 0 ? (
            <EmptyState icon={<ReceiptText size={20} />} title="No bookings for this lot yet" />
          ) : (
            <BookingsTable bookings={bookings} showDriver actions={{ onAction: handleBookingAction, busy }} />
          )}
        </Card>
      </LotPageShell>
    </PageContainer>
  );
}
