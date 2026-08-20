import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { useParams } from "react-router-dom";
import { ParkingSquare, CheckCircle2, IndianRupee, Plus, ReceiptText } from "lucide-react";
import { bookingsApi, parkingLotsApi } from "../../api/endpoints";
import { getErrorMessage } from "../../api/errors";
import { useToast } from "../../context/ToastContext";
import SlotSeatMap from "../../components/SlotSeatMap";
import { LotPageShell } from "../../components/LotPageShell";
import { BookingsTable } from "../../components/BookingsTable";
import {
  Button,
  Card,
  EmptyState,
  ErrorMessage,
  Field,
  PageContainer,
  StatCard,
  TextInput,
} from "../../components/ui";
import type { BookingDto, ParkingLotDto, ParkingSlotDto } from "../../types";

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
  const [addError, setAddError] = useState("");
  const [adding, setAdding] = useState(false);

  async function loadAll() {
    setLoading(true);
    setError("");
    try {
      const [lotRes, slotsRes, bookingsRes] = await Promise.all([
        parkingLotsApi.get(lotId),
        parkingLotsApi.listSlots(lotId),
        bookingsApi.forLot(lotId),
      ]);
      setLot(lotRes.data);
      setSlots(slotsRes.data);
      setBookings(bookingsRes.data);
    } catch (err) {
      setError(getErrorMessage(err, "Could not load lot details."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (lotId) loadAll();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [lotId]);

  async function handleAddSlot(e: FormEvent) {
    e.preventDefault();
    setAddError("");
    setAdding(true);
    try {
      await parkingLotsApi.addSlot(lotId, { slotNumber, hourlyRate: Number(hourlyRate) });
      toast.success(`Added slot ${slotNumber}.`);
      setSlotNumber("");
      setHourlyRate("");
      await loadAll();
    } catch (err) {
      setAddError(getErrorMessage(err, "Could not add slot."));
    } finally {
      setAdding(false);
    }
  }

  const occupied = lot ? lot.totalSlots - lot.availableSlots : 0;

  return (
    <PageContainer>
      <LotPageShell backTo="/operator/lots" lot={lot} lotId={lotId} loading={loading} error={error}>
        {lot && (
          <div className="mb-6 grid grid-cols-2 gap-4 sm:grid-cols-3">
            <StatCard icon={<ParkingSquare size={18} />} label="Total Slots" value={lot.totalSlots} tone="slate" />
            <StatCard icon={<CheckCircle2 size={18} />} label="Available" value={lot.availableSlots} tone="emerald" />
            <StatCard icon={<IndianRupee size={18} />} label="Occupied" value={occupied} tone="rose" />
          </div>
        )}

        <Card className="mb-6">
          <h2 className="mb-3 flex items-center gap-1.5 text-base font-semibold text-slate-900">
            <Plus size={16} /> Add a slot
          </h2>
          <form onSubmit={handleAddSlot} className="flex flex-wrap items-end gap-3">
            <div className="min-w-[160px]">
              <Field label="Slot Number" htmlFor="slotNumber">
                <TextInput
                  id="slotNumber"
                  required
                  value={slotNumber}
                  onChange={(e) => setSlotNumber(e.target.value)}
                  placeholder="A1"
                />
              </Field>
            </div>
            <div className="min-w-[160px]">
              <Field label="Hourly Rate (₹)" htmlFor="hourlyRate">
                <TextInput
                  id="hourlyRate"
                  type="number"
                  min={0}
                  step="0.01"
                  required
                  value={hourlyRate}
                  onChange={(e) => setHourlyRate(e.target.value)}
                  placeholder="50"
                />
              </Field>
            </div>
            <Button type="submit" loading={adding} className="mb-4">
              {adding ? "Adding…" : "Add Slot"}
            </Button>
          </form>
          <ErrorMessage>{addError}</ErrorMessage>
        </Card>

        <Card className="mb-6">
          <h2 className="mb-4 text-base font-semibold text-slate-900">Slots</h2>
          {slots.length === 0 ? (
            <EmptyState icon={<ParkingSquare size={20} />} title="No slots yet" description="Add your first slot above." />
          ) : (
            <SlotSeatMap slots={slots} />
          )}
        </Card>

        <Card className="!p-0 overflow-hidden">
          <h2 className="flex items-center gap-1.5 px-6 pt-6 pb-3 text-base font-semibold text-slate-900">
            <ReceiptText size={16} /> Bookings
          </h2>
          {bookings.length === 0 ? (
            <EmptyState icon={<ReceiptText size={20} />} title="No bookings for this lot yet" />
          ) : (
            <BookingsTable bookings={bookings} variant="operator" />
          )}
        </Card>
      </LotPageShell>
    </PageContainer>
  );
}
