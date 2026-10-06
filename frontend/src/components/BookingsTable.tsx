import { ArrowRight, LogIn, LogOut, X } from "lucide-react";
import { Button } from "./ui";
import { amountDue, BookingStatusBadge, HoldCountdown, LiveEstimate, VehicleIcon } from "./BookingParts";
import { formatDateTime, rupees } from "../lib/booking";
import type { BookingDto } from "../types";

export type BookingAction = "checkIn" | "exit" | "cancel";

export interface BookingActions {
  onAction: (action: BookingAction, booking: BookingDto) => void;
  busy: { id: number; action: BookingAction } | null;
}

function RowActions({ booking, actions }: { booking: BookingDto; actions: BookingActions }) {
  const busy = (a: BookingAction) => actions.busy?.id === booking.id && actions.busy.action === a;
  const anyBusy = actions.busy?.id === booking.id;

  if (booking.status === "Reserved") {
    return (
      <div className="flex justify-end gap-2">
        <Button
          icon={<LogIn size={14} />}
          loading={busy("checkIn")}
          disabled={anyBusy}
          onClick={() => actions.onAction("checkIn", booking)}
        >
          Check in
        </Button>
        <Button
          variant="secondary"
          icon={<X size={14} />}
          loading={busy("cancel")}
          disabled={anyBusy}
          onClick={() => actions.onAction("cancel", booking)}
        >
          Cancel
        </Button>
      </div>
    );
  }
  if (booking.status === "Active") {
    return (
      <div className="flex justify-end">
        <Button
          variant="danger"
          icon={<LogOut size={14} />}
          loading={busy("exit")}
          disabled={anyBusy}
          onClick={() => actions.onAction("exit", booking)}
        >
          Exit
        </Button>
      </div>
    );
  }
  return null;
}

function Fee({ booking }: { booking: BookingDto }) {
  const due = amountDue(booking);
  if (due != null) {
    return (
      <span className="text-slate-800">
        {rupees(due)}
        {booking.penaltyFee != null && booking.penaltyFee > 0 && (
          <span className="block text-xs text-rose-600">incl. {rupees(booking.penaltyFee)} overstay</span>
        )}
      </span>
    );
  }
  if (booking.status === "Active" && booking.entryTime) {
    return <LiveEstimate entryTime={booking.entryTime} hourlyRate={booking.hourlyRate} />;
  }
  if (booking.status === "Reserved") return <span className="whitespace-nowrap text-slate-400">Starts at check-in</span>;
  return <span className="text-slate-400">—</span>;
}

export function BookingsTable({
  bookings,
  showLot = false,
  showDriver = false,
  actions,
}: {
  bookings: BookingDto[];
  showLot?: boolean;
  showDriver?: boolean;
  actions?: BookingActions;
}) {
  return (
    <div className="overflow-x-auto">
      <table className="w-full text-left text-sm">
        <thead>
          <tr className="border-b border-slate-200 bg-slate-50/80 text-xs uppercase tracking-wide text-slate-500">
            <th className="px-6 py-3 font-medium">Vehicle → Slot</th>
            {showDriver && <th className="px-6 py-3 font-medium">Driver</th>}
            {showLot && <th className="px-6 py-3 font-medium">Lot</th>}
            <th className="px-6 py-3 font-medium">Reserved</th>
            <th className="px-6 py-3 font-medium">Checked in</th>
            <th className="px-6 py-3 font-medium">Exited</th>
            <th className="px-6 py-3 font-medium">Fee</th>
            <th className="px-6 py-3 font-medium">Status</th>
            {actions && <th className="px-6 py-3" />}
          </tr>
        </thead>
        <tbody>
          {bookings.map((b) => (
            <tr key={b.id} className="border-b border-slate-100 last:border-0 hover:bg-slate-50/60">
              <td className="px-6 py-3.5">
                <span className="inline-flex items-center gap-2 whitespace-nowrap font-medium text-slate-900">
                  <span className="text-slate-400">
                    <VehicleIcon type={b.vehicleType} size={15} />
                  </span>
                  {b.vehicleNumber}
                  <ArrowRight size={13} className="text-slate-300" />
                  <span className="rounded-md bg-slate-100 px-1.5 py-0.5 font-mono text-xs">{b.slotNumber}</span>
                </span>
              </td>
              {showDriver && <td className="px-6 py-3.5 whitespace-nowrap text-slate-700">{b.driverName}</td>}
              {showLot && <td className="px-6 py-3.5 whitespace-nowrap text-slate-700">{b.lotName}</td>}
              <td className="px-6 py-3.5 whitespace-nowrap text-slate-700">
                {formatDateTime(b.reservedAt)}
                {b.status === "Reserved" && b.reservationExpiresAt && (
                  <span className="block text-xs">
                    <HoldCountdown expiresAt={b.reservationExpiresAt} />
                  </span>
                )}
              </td>
              <td className="px-6 py-3.5 whitespace-nowrap text-slate-700">{formatDateTime(b.entryTime)}</td>
              <td className="px-6 py-3.5 whitespace-nowrap text-slate-700">{formatDateTime(b.exitTime)}</td>
              <td className="px-6 py-3.5">
                <Fee booking={b} />
              </td>
              <td className="px-6 py-3.5">
                <BookingStatusBadge status={b.status} />
              </td>
              {actions && (
                <td className="px-6 py-3.5">
                  <RowActions booking={b} actions={actions} />
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
