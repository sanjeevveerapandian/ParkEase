import { useEffect, useState } from "react";
import { ReceiptText, LogOut as ExitIcon } from "lucide-react";
import { bookingsApi } from "../../api/endpoints";
import { getErrorMessage } from "../../api/errors";
import { useToast } from "../../context/ToastContext";
import {
  Badge,
  Button,
  Card,
  EmptyState,
  ErrorMessage,
  PageContainer,
  PageHeader,
  Skeleton,
} from "../../components/ui";
import type { BookingDto } from "../../types";

// Live elapsed-time + running-cost estimate for an Active booking. Uses the same
// round-up-to-the-hour rule the backend bills with at exit, so the number here is
// a genuine preview of the final fee, not just a decorative timer — it just can't
// know the final minute until you actually exit.
function LiveEstimate({ entryTime, hourlyRate }: { entryTime: string; hourlyRate: number }) {
  const [now, setNow] = useState(() => Date.now());

  useEffect(() => {
    const id = window.setInterval(() => setNow(Date.now()), 1000);
    return () => window.clearInterval(id);
  }, []);

  const elapsedMs = Math.max(0, now - new Date(entryTime).getTime());
  const hours = Math.floor(elapsedMs / 3_600_000);
  const minutes = Math.floor((elapsedMs % 3_600_000) / 60_000);
  const billableHours = Math.max(1, Math.ceil(elapsedMs / 3_600_000));
  const estimate = billableHours * hourlyRate;

  return (
    <span className="inline-flex items-center gap-1.5 whitespace-nowrap text-indigo-600">
      <span className="h-1.5 w-1.5 shrink-0 animate-pulse rounded-full bg-indigo-500" />
      {hours}h {minutes}m · ~₹{estimate.toFixed(2)}
    </span>
  );
}

export default function MyBookingsPage() {
  const toast = useToast();
  const [bookings, setBookings] = useState<BookingDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [exitingId, setExitingId] = useState<number | null>(null);

  async function loadBookings() {
    setLoading(true);
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
  }, []);

  async function handleExit(booking: BookingDto) {
    setError("");
    setExitingId(booking.id);
    try {
      await bookingsApi.exit(booking.id);
      toast.success(`Exited slot ${booking.slotNumber}. Fee calculated.`);
      await loadBookings();
    } catch (err) {
      toast.error(getErrorMessage(err, "Could not close out this booking."));
    } finally {
      setExitingId(null);
    }
  }

  return (
    <PageContainer>
      <PageHeader title="My Bookings" description="Your vehicle check-in history and active sessions." />
      <ErrorMessage>{error}</ErrorMessage>

      <Card className="!p-0 overflow-hidden">
        {loading ? (
          <div className="space-y-3 p-6">
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className="h-12" />
            ))}
          </div>
        ) : bookings.length === 0 ? (
          <EmptyState
            icon={<ReceiptText size={22} />}
            title="No bookings yet"
            description="Browse slots and book one to see it show up here."
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50/80 text-xs uppercase tracking-wide text-slate-500">
                  <th className="px-6 py-3 font-medium">Slot</th>
                  <th className="px-6 py-3 font-medium">Vehicle</th>
                  <th className="px-6 py-3 font-medium">Entry</th>
                  <th className="px-6 py-3 font-medium">Exit</th>
                  <th className="px-6 py-3 font-medium">Fee</th>
                  <th className="px-6 py-3 font-medium">Penalty</th>
                  <th className="px-6 py-3 font-medium">Status</th>
                  <th className="px-6 py-3 font-medium"></th>
                </tr>
              </thead>
              <tbody>
                {bookings.map((b) => (
                  <tr key={b.id} className="border-b border-slate-100 last:border-0 hover:bg-slate-50/60">
                    <td className="px-6 py-3.5 font-medium text-slate-900">{b.slotNumber}</td>
                    <td className="px-6 py-3.5 text-slate-700">{b.vehicleNumber}</td>
                    <td className="px-6 py-3.5 whitespace-nowrap text-slate-700">
                      {new Date(b.entryTime).toLocaleString()}
                    </td>
                    <td className="px-6 py-3.5 whitespace-nowrap text-slate-700">
                      {b.exitTime ? new Date(b.exitTime).toLocaleString() : "—"}
                    </td>
                    <td className="px-6 py-3.5 text-slate-700">
                      {b.totalFee != null ? (
                        `₹${b.totalFee.toFixed(2)}`
                      ) : b.status === "Active" ? (
                        <LiveEstimate entryTime={b.entryTime} hourlyRate={b.hourlyRate} />
                      ) : (
                        "—"
                      )}
                    </td>
                    <td className="px-6 py-3.5 text-slate-700">
                      {b.penaltyFee != null && b.penaltyFee > 0 ? (
                        <span className="text-rose-600">₹{b.penaltyFee.toFixed(2)}</span>
                      ) : (
                        "—"
                      )}
                    </td>
                    <td className="px-6 py-3.5">
                      <Badge tone={b.status === "Active" ? "green" : "slate"} dot>
                        {b.status}
                      </Badge>
                    </td>
                    <td className="px-6 py-3.5">
                      {b.status === "Active" && (
                        <Button
                          variant="danger"
                          icon={<ExitIcon size={14} />}
                          loading={exitingId === b.id}
                          onClick={() => handleExit(b)}
                        >
                          {exitingId === b.id ? "Exiting…" : "Exit"}
                        </Button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </PageContainer>
  );
}
