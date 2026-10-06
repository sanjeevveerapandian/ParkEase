import { useEffect, useState } from "react";
import { Bike, Car, Check } from "lucide-react";
import { Badge } from "./ui";
import { estimateFee, formatDateTime, formatDuration, rupees } from "../lib/booking";
import type { BookingDto, BookingStatus, VehicleType } from "../types";

const statusTone: Record<BookingStatus, "indigo" | "green" | "slate" | "amber" | "red"> = {
  Reserved: "amber",
  Active: "green",
  Completed: "slate",
  Overstayed: "red",
  Cancelled: "slate",
  Expired: "slate",
};

export function BookingStatusBadge({ status }: { status: BookingStatus }) {
  return (
    <Badge tone={statusTone[status]} dot>
      {status === "Active" ? "Parked" : status}
    </Badge>
  );
}

export function VehicleIcon({ type, size = 16 }: { type: VehicleType; size?: number }) {
  return type === "Bike" ? <Bike size={size} /> : <Car size={size} />;
}

// Re-renders every second while mounted.
function useNow(intervalMs = 1000) {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const id = window.setInterval(() => setNow(Date.now()), intervalMs);
    return () => window.clearInterval(id);
  }, [intervalMs]);
  return now;
}

export function HoldCountdown({ expiresAt }: { expiresAt: string }) {
  const now = useNow();
  const remaining = new Date(expiresAt).getTime() - now;
  if (remaining <= 0) {
    return <span className="text-rose-600">Hold ended — releasing slot…</span>;
  }
  const totalSeconds = Math.floor(remaining / 1000);
  const m = Math.floor(totalSeconds / 60);
  const s = String(totalSeconds % 60).padStart(2, "0");
  return (
    <span className={`font-mono tabular-nums ${remaining < 120_000 ? "text-rose-600" : "text-amber-700"}`}>
      {m}:{s} left to check in
    </span>
  );
}

export function LiveEstimate({ entryTime, hourlyRate }: { entryTime: string; hourlyRate: number }) {
  const now = useNow();
  const elapsed = now - new Date(entryTime).getTime();
  return (
    <span className="inline-flex items-center gap-1.5 whitespace-nowrap text-indigo-600">
      <span className="h-1.5 w-1.5 shrink-0 animate-pulse rounded-full bg-indigo-500" />
      {formatDuration(elapsed)} · ~{rupees(estimateFee(entryTime, hourlyRate, now))}
    </span>
  );
}

// What the driver owes once the booking is closed out.
export const amountDue = (b: BookingDto) =>
  b.totalFee == null ? null : b.totalFee + (b.penaltyFee ?? 0);

const STEPS = ["Reserved", "Checked in", "Exited"] as const;

function currentStep(b: BookingDto) {
  switch (b.status) {
    case "Reserved":
      return 0;
    case "Active":
      return 1;
    case "Completed":
    case "Overstayed":
      return 2;
    default:
      return -1; // cancelled / expired never got going
  }
}

// Reserved → Checked in → Exited, with the time each step happened.
export function BookingTimeline({ booking }: { booking: BookingDto }) {
  const step = currentStep(booking);
  const times = [booking.reservedAt, booking.entryTime, booking.exitTime];

  if (step === -1) {
    return (
      <p className="text-sm text-slate-500">
        {booking.status === "Cancelled" ? "Cancelled" : "Expired — no check-in in time"}{" "}
        {formatDateTime(booking.cancelledAt)} · not charged
      </p>
    );
  }

  return (
    <ol className="flex items-start">
      {STEPS.map((label, i) => {
        const done = i <= step;
        return (
          <li key={label} className="relative flex flex-1 flex-col items-center text-center">
            {i > 0 && (
              <span
                className={`absolute right-1/2 top-3 h-0.5 w-full -translate-y-1/2 ${i <= step ? "bg-indigo-500" : "bg-slate-200"}`}
              />
            )}
            <span
              className={`relative z-10 flex h-6 w-6 items-center justify-center rounded-full text-[11px] font-bold ${
                done ? "bg-indigo-600 text-white" : "bg-slate-200 text-slate-400"
              }`}
            >
              {done ? <Check size={13} /> : i + 1}
            </span>
            <span className={`mt-1.5 text-xs font-medium ${done ? "text-slate-800" : "text-slate-400"}`}>{label}</span>
            <span className="text-[11px] text-slate-400">{done ? formatDateTime(times[i]) : "—"}</span>
          </li>
        );
      })}
    </ol>
  );
}
