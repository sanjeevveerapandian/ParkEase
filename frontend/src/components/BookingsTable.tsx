import { useEffect, useState } from "react";
import { LogOut as ExitIcon } from "lucide-react";
import { Badge, Button } from "./ui";
import type { BookingDto } from "../types";

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

// Shared bookings table for both the driver's "My Bookings" view and the operator's
// per-lot bookings view — previously two near-identical hand-copies. "driver" adds the
// Penalty column, the live running-cost estimate on Active rows, and the Exit action;
// "operator" is the same base columns, read-only.
export function BookingsTable({
  bookings,
  variant,
  onExit,
  exitingId,
}: {
  bookings: BookingDto[];
  variant: "driver" | "operator";
  onExit?: (booking: BookingDto) => void;
  exitingId?: number | null;
}) {
  const isDriver = variant === "driver";

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50/80 text-xs uppercase tracking-wide text-slate-500">
            <th className="px-6 py-3 font-medium">Slot</th>
            <th className="px-6 py-3 font-medium">Vehicle</th>
            <th className="px-6 py-3 font-medium">Entry</th>
            <th className="px-6 py-3 font-medium">Exit</th>
            <th className="px-6 py-3 font-medium">Fee</th>
            {isDriver && <th className="px-6 py-3 font-medium">Penalty</th>}
            <th className="px-6 py-3 font-medium">Status</th>
            {isDriver && <th className="px-6 py-3 font-medium"></th>}
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
                ) : isDriver && b.status === "Active" ? (
                  <LiveEstimate entryTime={b.entryTime} hourlyRate={b.hourlyRate} />
                ) : (
                  "—"
                )}
              </td>
              {isDriver && (
                <td className="px-6 py-3.5 text-slate-700">
                  {b.penaltyFee != null && b.penaltyFee > 0 ? (
                    <span className="text-rose-600">₹{b.penaltyFee.toFixed(2)}</span>
                  ) : (
                    "—"
                  )}
                </td>
              )}
              <td className="px-6 py-3.5">
                <Badge tone={b.status === "Active" ? "green" : "slate"} dot>
                  {b.status}
                </Badge>
              </td>
              {isDriver && (
                <td className="px-6 py-3.5">
                  {b.status === "Active" && onExit && (
                    <Button
                      variant="danger"
                      icon={<ExitIcon size={14} />}
                      loading={exitingId === b.id}
                      onClick={() => onExit(b)}
                    >
                      {exitingId === b.id ? "Exiting…" : "Exit"}
                    </Button>
                  )}
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
