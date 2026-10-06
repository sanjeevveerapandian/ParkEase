import { useEffect, useState } from "react";
import { ArrowRight, LogIn, LogOut, ReceiptText, X } from "lucide-react";
import { bookingsApi } from "../../api/endpoints";
import { getErrorMessage } from "../../api/errors";
import { useToast } from "../../context/ToastContext";
import { BookingsTable } from "../../components/BookingsTable";
import type { BookingAction } from "../../components/BookingsTable";
import {
  amountDue,
  BookingStatusBadge,
  BookingTimeline,
  HoldCountdown,
  LiveEstimate,
  VehicleIcon,
} from "../../components/BookingParts";
import { isLive, rupees } from "../../lib/booking";
import { Button, Card, EmptyState, ErrorMessage, PageContainer, PageHeader, Skeleton } from "../../components/ui";
import type { BookingDto } from "../../types";

const doneMessages: Record<BookingAction, (b: BookingDto) => string> = {
  checkIn: (b) => `Checked in at slot ${b.slotNumber}. Billing has started.`,
  exit: (b) => `Exited slot ${b.slotNumber}. Receipt sent by SMS and email.`,
  cancel: (b) => `Reservation for slot ${b.slotNumber} cancelled. No charge.`,
};

export default function MyBookingsPage() {
  const toast = useToast();
  const [bookings, setBookings] = useState<BookingDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [busy, setBusy] = useState<{ id: number; action: BookingAction } | null>(null);

  async function loadBookings() {
    setError("");
    try {
      const res = await bookingsApi.mine();
      setBookings(res.data);
    } catch (err) {
      setError(getErrorMessage(err, "Could not load your bookings."));
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadBookings();
    // Pick up holds the server has expired in the meantime.
    const id = window.setInterval(loadBookings, 30_000);
    return () => window.clearInterval(id);
  }, []);

  async function handleAction(action: BookingAction, booking: BookingDto) {
    setBusy({ id: booking.id, action });
    try {
      if (action === "checkIn") await bookingsApi.checkIn(booking.id);
      else if (action === "exit") await bookingsApi.exit(booking.id);
      else await bookingsApi.cancel(booking.id);
      toast.success(doneMessages[action](booking));
    } catch (err) {
      toast.error(getErrorMessage(err, "That didn't work. Please try again."));
    } finally {
      setBusy(null);
      await loadBookings();
    }
  }

  const live = bookings.filter(isLive);
  const history = bookings.filter((b) => !isLive(b));

  return (
    <PageContainer>
      <PageHeader title="My Bookings" description="Reserve, check in when you arrive, exit when you leave." />
      <ErrorMessage>{error}</ErrorMessage>

      {loading ? (
        <div className="space-y-3">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-24" />
          ))}
        </div>
      ) : bookings.length === 0 ? (
        <Card>
          <EmptyState
            icon={<ReceiptText size={22} />}
            title="No bookings yet"
            description="Pick a lot, choose your vehicle and a slot to reserve one."
          />
        </Card>
      ) : (
        <>
          {live.length > 0 && (
            <div className="mb-8 space-y-4">
              <h2 className="text-sm font-semibold uppercase tracking-wide text-slate-500">Active</h2>
              {live.map((b) => {
                const isBusy = busy?.id === b.id;
                return (
                  <Card key={b.id}>
                    <div className="mb-5 flex flex-wrap items-start justify-between gap-3">
                      <div>
                        <p className="flex flex-wrap items-center gap-2 text-lg font-bold text-slate-900">
                          <span className="text-indigo-600">
                            <VehicleIcon type={b.vehicleType} size={18} />
                          </span>
                          {b.vehicleNumber}
                          <ArrowRight size={16} className="text-slate-300" />
                          Slot {b.slotNumber}
                        </p>
                        <p className="mt-0.5 text-sm text-slate-500">
                          {b.lotName} · ₹{b.hourlyRate}/hr
                        </p>
                      </div>
                      <BookingStatusBadge status={b.status} />
                    </div>

                    <BookingTimeline booking={b} />

                    <div className="mt-5 flex flex-wrap items-center justify-between gap-3 border-t border-slate-100 pt-4">
                      <div className="text-sm">
                        {b.status === "Reserved" && b.reservationExpiresAt && <HoldCountdown expiresAt={b.reservationExpiresAt} />}
                        {b.status === "Active" && b.entryTime && (
                          <LiveEstimate entryTime={b.entryTime} hourlyRate={b.hourlyRate} />
                        )}
                      </div>
                      <div className="flex gap-2">
                        {b.status === "Reserved" && (
                          <>
                            <Button
                              icon={<LogIn size={14} />}
                              loading={busy?.id === b.id && busy.action === "checkIn"}
                              disabled={isBusy}
                              onClick={() => handleAction("checkIn", b)}
                            >
                              Check in
                            </Button>
                            <Button
                              variant="secondary"
                              icon={<X size={14} />}
                              loading={busy?.id === b.id && busy.action === "cancel"}
                              disabled={isBusy}
                              onClick={() => handleAction("cancel", b)}
                            >
                              Cancel
                            </Button>
                          </>
                        )}
                        {b.status === "Active" && (
                          <Button
                            variant="danger"
                            icon={<LogOut size={14} />}
                            loading={busy?.id === b.id && busy.action === "exit"}
                            disabled={isBusy}
                            onClick={() => handleAction("exit", b)}
                          >
                            Exit &amp; pay
                          </Button>
                        )}
                      </div>
                    </div>
                  </Card>
                );
              })}
            </div>
          )}

          {history.length > 0 && (
            <div>
              <h2 className="mb-3 text-sm font-semibold uppercase tracking-wide text-slate-500">History</h2>
              <Card className="!p-0 overflow-hidden">
                <BookingsTable bookings={history} showLot />
              </Card>
              <p className="mt-3 text-xs text-slate-400">
                Total paid: {rupees(history.reduce((sum, b) => sum + (amountDue(b) ?? 0), 0))}
              </p>
            </div>
          )}
        </>
      )}
    </PageContainer>
  );
}
