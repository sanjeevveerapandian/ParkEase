import { useEffect, useState } from "react";
import type { FormEvent } from "react";
import { Link, useLocation, useParams } from "react-router-dom";
import { ArrowLeft, Car, IndianRupee, MapPin, ParkingSquare, X } from "lucide-react";
import { parkingLotsApi, bookingsApi } from "../../api/endpoints";
import { getErrorMessage } from "../../api/errors";
import { useToast } from "../../context/ToastContext";
import SlotSeatMap from "../../components/SlotSeatMap";
import {
  Button,
  Card,
  EmptyState,
  ErrorMessage,
  PageContainer,
  Skeleton,
  SkeletonGrid,
  TextInput,
} from "../../components/ui";
import type { ParkingLotDto, ParkingSlotDto } from "../../types";

// Standard Indian registration format: 2-letter state code, 1-2 digit RTO code,
// 1-2 letter series, 4 digit number (e.g. TN01AB1234). Deliberately lenient on the
// series length rather than matching every real-world exception (BH-series, defense
// plates, etc.) — this catches obvious typos/garbage without rejecting valid input.
const VEHICLE_NUMBER_PATTERN = /^[A-Z]{2}\d{1,2}[A-Z]{1,2}\d{4}$/;

export default function LotSlotsPage() {
  const { id } = useParams<{ id: string }>();
  const lotId = Number(id);
  const location = useLocation();
  const toast = useToast();

  // Prefer the lot summary handed off from BrowseLotsPage (no extra round trip);
  // fall back to fetching + filtering the open browse list on a direct link/refresh.
  const [lot, setLot] = useState<ParkingLotDto | null>((location.state as { lot?: ParkingLotDto } | null)?.lot ?? null);
  const [slots, setSlots] = useState<ParkingSlotDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [selected, setSelected] = useState<ParkingSlotDto | null>(null);
  const [vehicleNumber, setVehicleNumber] = useState("");
  const [bookingError, setBookingError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  async function loadSlots() {
    const res = await parkingLotsApi.listSlots(lotId);
    setSlots(res.data);
  }

  async function loadAll() {
    setLoading(true);
    setError("");
    try {
      const tasks: Promise<unknown>[] = [loadSlots()];
      if (!lot) {
        tasks.push(
          parkingLotsApi.browse().then((res) => {
            const found = res.data.find((l) => l.id === lotId);
            if (!found) throw new Error("Lot not found.");
            setLot(found);
          })
        );
      }
      await Promise.all(tasks);
    } catch (err) {
      setError(getErrorMessage(err, "Could not load this lot."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    if (lotId) loadAll();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [lotId]);

  function handleSelect(slot: ParkingSlotDto) {
    setBookingError("");
    setSelected((prev) => (prev?.id === slot.id ? null : slot));
  }

  async function handleBook(e: FormEvent) {
    e.preventDefault();
    if (!selected) return;
    setBookingError("");
    if (!VEHICLE_NUMBER_PATTERN.test(vehicleNumber)) {
      setBookingError("Enter a valid vehicle number, e.g. TN01AB1234.");
      return;
    }
    setSubmitting(true);
    try {
      await bookingsApi.create({ parkingSlotId: selected.id, vehicleNumber });
      toast.success(`Booked slot ${selected.slotNumber}. Check "My Bookings" for details.`);
      setSelected(null);
      setVehicleNumber("");
      await loadSlots();
    } catch (err) {
      setBookingError(getErrorMessage(err, "Could not create booking."));
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <PageContainer>
      <Link
        to="/driver/lots"
        className="mb-4 inline-flex items-center gap-1.5 text-sm font-medium text-slate-500 hover:text-slate-900"
      >
        <ArrowLeft size={15} /> Back to lots
      </Link>

      <ErrorMessage>{error}</ErrorMessage>

      {loading ? (
        <div className="space-y-6">
          <Skeleton className="h-8 w-64" />
          <SkeletonGrid count={4} />
        </div>
      ) : (
        <>
          <div className="mb-6">
            <h1 className="text-2xl font-bold tracking-tight text-slate-900">{lot?.name ?? `Lot #${lotId}`}</h1>
            {lot && (
              <p className="mt-1 flex items-center gap-1.5 text-sm text-slate-500">
                <MapPin size={14} /> {lot.address}
              </p>
            )}
          </div>

          {/* Suppress the slots section entirely when loadAll failed (e.g. the lot was
              deleted) — otherwise an empty slots array renders a misleading "No slots
              yet" card alongside the "Lot not found" error above. */}
          {!error &&
            (slots.length === 0 ? (
              <Card>
                <EmptyState icon={<ParkingSquare size={22} />} title="No slots in this lot yet" />
              </Card>
            ) : (
              <Card className={selected ? "pb-28" : undefined}>
                <SlotSeatMap slots={slots} selectedId={selected?.id ?? null} onSelect={handleSelect} />
              </Card>
            ))}

          {/* Sticky checkout bar — appears once a slot is picked, like confirming a seat. */}
          {selected && (
            <div className="fixed inset-x-0 bottom-0 z-30 border-t border-slate-200 bg-white/95 backdrop-blur-md shadow-[0_-4px_16px_rgba(0,0,0,0.06)] animate-fade-slide-in">
              <form
                onSubmit={handleBook}
                className="mx-auto flex max-w-6xl flex-wrap items-center gap-3 px-4 py-3 sm:px-6 lg:px-8"
              >
                <div className="flex items-center gap-2 rounded-lg bg-indigo-50 px-3 py-2 text-sm font-semibold text-indigo-700">
                  <ParkingSquare size={15} />
                  Slot {selected.slotNumber}
                  <span className="flex items-center gap-0.5 font-normal text-indigo-500">
                    · <IndianRupee size={12} />
                    {selected.hourlyRate}/hr
                  </span>
                </div>

                <div className="relative min-w-[180px] flex-1">
                  <Car size={16} className="pointer-events-none absolute left-3.5 top-1/2 -translate-y-1/2 text-slate-400" />
                  <TextInput
                    required
                    autoFocus
                    value={vehicleNumber}
                    onChange={(e) => setVehicleNumber(e.target.value.toUpperCase())}
                    placeholder="Vehicle number, e.g. TN01AB1234"
                    className="pl-10 uppercase placeholder:normal-case"
                  />
                </div>

                <Button type="submit" loading={submitting}>
                  {submitting ? "Booking…" : "Confirm Booking"}
                </Button>
                <button
                  type="button"
                  onClick={() => {
                    setSelected(null);
                    setVehicleNumber("");
                    setBookingError("");
                  }}
                  className="rounded-lg p-2.5 text-slate-400 hover:bg-slate-100 hover:text-slate-600"
                  aria-label="Cancel selection"
                >
                  <X size={18} />
                </button>

                {bookingError && (
                  <p className="w-full text-sm text-rose-600">{bookingError}</p>
                )}
              </form>
            </div>
          )}
        </>
      )}
    </PageContainer>
  );
}
