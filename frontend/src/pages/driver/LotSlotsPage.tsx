import { useEffect, useState } from "react";
import { Link, useLocation, useNavigate, useParams } from "react-router-dom";
import { ArrowRight, Car, Clock, IndianRupee, ParkingSquare } from "lucide-react";
import { bookingsApi, parkingLotsApi, vehiclesApi } from "../../api/endpoints";
import { getErrorMessage } from "../../api/errors";
import { useToast } from "../../context/ToastContext";
import SlotSeatMap from "../../components/SlotSeatMap";
import { LotPageShell } from "../../components/LotPageShell";
import { VehicleIcon } from "../../components/BookingParts";
import { slotFitsVehicle } from "../../lib/booking";
import { Button, Card, EmptyState, LinkButton, PageContainer } from "../../components/ui";
import type { ParkingLotDto, ParkingSlotDto, VehicleDto } from "../../types";

const HOLD_MINUTES = 15;

function StepHeading({ n, title, done }: { n: number; title: string; done?: boolean }) {
  return (
    <h2 className="mb-4 flex items-center gap-2 text-base font-semibold text-slate-900">
      <span
        className={`flex h-6 w-6 items-center justify-center rounded-full text-xs font-bold ${
          done ? "bg-indigo-600 text-white" : "bg-slate-200 text-slate-600"
        }`}
      >
        {n}
      </span>
      {title}
    </h2>
  );
}

export default function LotSlotsPage() {
  const { id } = useParams<{ id: string }>();
  const lotId = Number(id);
  const location = useLocation();
  const navigate = useNavigate();
  const toast = useToast();

  const [lot, setLot] = useState<ParkingLotDto | null>((location.state as { lot?: ParkingLotDto } | null)?.lot ?? null);
  const [slots, setSlots] = useState<ParkingSlotDto[]>([]);
  const [vehicles, setVehicles] = useState<VehicleDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  const [vehicleId, setVehicleId] = useState<number | null>(null);
  const [selected, setSelected] = useState<ParkingSlotDto | null>(null);
  const [bookingError, setBookingError] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const vehicle = vehicles.find((v) => v.id === vehicleId) ?? null;

  async function loadSlots() {
    const res = await parkingLotsApi.listSlots(lotId);
    setSlots(res.data);
  }

  async function loadAll() {
    setLoading(true);
    setError("");
    try {
      const tasks: Promise<unknown>[] = [
        loadSlots(),
        vehiclesApi.mine().then((res) => {
          setVehicles(res.data);
          if (res.data.length === 1) setVehicleId(res.data[0].id);
        }),
      ];
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

  function chooseVehicle(v: VehicleDto) {
    setVehicleId(v.id);
    setBookingError("");
    // A slot picked for the previous vehicle may not fit this one.
    if (selected && !slotFitsVehicle(selected, v)) setSelected(null);
  }

  function handleSelect(slot: ParkingSlotDto) {
    setBookingError("");
    setSelected((prev) => (prev?.id === slot.id ? null : slot));
  }

  async function handleReserve() {
    if (!selected || !vehicle) return;
    setBookingError("");
    setSubmitting(true);
    try {
      await bookingsApi.reserve({ vehicleId: vehicle.id, parkingSlotId: selected.id });
      toast.success(`Slot ${selected.slotNumber} reserved for ${vehicle.vehicleNumber}. Check in within ${HOLD_MINUTES} minutes.`);
      navigate("/driver/bookings");
    } catch (err) {
      setBookingError(getErrorMessage(err, "Could not reserve this slot."));
      await loadSlots().catch(() => undefined);
      setSelected(null);
    } finally {
      setSubmitting(false);
    }
  }

  const noVehicles = !loading && !error && vehicles.length === 0;

  return (
    <PageContainer>
      <LotPageShell backTo="/driver/lots" lot={lot} lotId={lotId} loading={loading} error={error}>
        {noVehicles && (
          <Card className="mb-6">
            <EmptyState
              icon={<Car size={22} />}
              title="Add a vehicle first"
              description="A booking maps one of your vehicles to one slot, so we need to know what you're parking."
              action={<LinkButton to="/driver/vehicles">Add a vehicle</LinkButton>}
            />
          </Card>
        )}

        {!error && !noVehicles && (
          <>
            <Card className="mb-6">
              <StepHeading n={1} title="Choose your vehicle" done={!!vehicle} />
              <div className="flex flex-wrap gap-3">
                {vehicles.map((v) => (
                  <button
                    key={v.id}
                    type="button"
                    onClick={() => chooseVehicle(v)}
                    className={`flex items-center gap-3 rounded-xl border px-4 py-3 text-left transition-all ${
                      v.id === vehicleId
                        ? "border-indigo-600 bg-indigo-50 ring-2 ring-indigo-600/20"
                        : "border-slate-200 bg-white hover:border-slate-300"
                    }`}
                  >
                    <span className="text-indigo-600">
                      <VehicleIcon type={v.type} size={18} />
                    </span>
                    <span>
                      <span className="block text-sm font-semibold tracking-wide text-slate-900">{v.vehicleNumber}</span>
                      <span className="block text-xs text-slate-500">{v.type}</span>
                    </span>
                  </button>
                ))}
                <Link
                  to="/driver/vehicles"
                  className="flex items-center rounded-xl border border-dashed border-slate-300 px-4 py-3 text-sm font-medium text-slate-500 hover:border-indigo-400 hover:text-indigo-600"
                >
                  + Add another
                </Link>
              </div>
            </Card>

            <Card className="mb-6">
              <StepHeading n={2} title={vehicle ? `Pick a ${vehicle.type.toLowerCase()} slot` : "Pick a slot"} done={!!selected} />
              {slots.length === 0 ? (
                <EmptyState icon={<ParkingSquare size={22} />} title="No slots in this lot yet" />
              ) : (
                <>
                  {!vehicle && <p className="mb-4 text-sm text-slate-500">Choose a vehicle above to see which slots fit it.</p>}
                  <SlotSeatMap
                    slots={slots}
                    selectedId={selected?.id ?? null}
                    onSelect={handleSelect}
                    canSelect={(s) => !!vehicle && slotFitsVehicle(s, vehicle)}
                    dimmed={(s) => s.status === "Available" && !!vehicle && s.vehicleType !== vehicle.type}
                  />
                </>
              )}
            </Card>

            <Card>
              <StepHeading n={3} title="Reserve" />
              {vehicle && selected ? (
                <div className="space-y-4">
                  <div className="flex flex-wrap items-center gap-3 text-sm">
                    <span className="inline-flex items-center gap-2 rounded-lg bg-slate-100 px-3 py-2 font-semibold text-slate-900">
                      <VehicleIcon type={vehicle.type} size={15} /> {vehicle.vehicleNumber}
                    </span>
                    <ArrowRight size={16} className="text-slate-400" />
                    <span className="inline-flex items-center gap-2 rounded-lg bg-indigo-50 px-3 py-2 font-semibold text-indigo-700">
                      <ParkingSquare size={15} /> Slot {selected.slotNumber}
                      <span className="inline-flex items-center gap-0.5 font-normal text-indigo-500">
                        · <IndianRupee size={12} />
                        {selected.hourlyRate}/hr
                      </span>
                    </span>
                  </div>
                  <p className="flex items-start gap-2 text-sm text-slate-500">
                    <Clock size={15} className="mt-0.5 shrink-0" />
                    We hold the slot for {HOLD_MINUTES} minutes. Billing starts only when you check in, and cancelling or
                    letting the hold lapse is free. You'll get an SMS and an email.
                  </p>
                  {bookingError && <p className="text-sm text-rose-600">{bookingError}</p>}
                  <Button onClick={handleReserve} loading={submitting}>
                    {submitting ? "Reserving…" : "Reserve slot"}
                  </Button>
                </div>
              ) : (
                <>
                  <p className="text-sm text-slate-500">Select a vehicle and a slot to continue.</p>
                  {bookingError && <p className="mt-2 text-sm text-rose-600">{bookingError}</p>}
                </>
              )}
            </Card>
          </>
        )}
      </LotPageShell>
    </PageContainer>
  );
}
