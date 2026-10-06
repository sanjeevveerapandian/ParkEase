import { useEffect, useState } from "react";
import { Bell, Mail, MessageSquare } from "lucide-react";
import { notificationsApi } from "../../api/endpoints";
import { getErrorMessage } from "../../api/errors";
import { formatDateTime } from "../../lib/booking";
import { Badge, Card, EmptyState, ErrorMessage, PageContainer, PageHeader, Skeleton } from "../../components/ui";
import type { NotificationDto, NotificationStatus } from "../../types";

const typeLabels: Record<string, string> = {
  BookingConfirmation: "Slot reserved",
  CheckIn: "Checked in",
  ExitReceipt: "Exit receipt",
  ReservationCancelled: "Reservation cancelled",
  ReservationExpired: "Reservation expired",
  OverstayWarning: "Overstay warning",
};

const statusTone: Record<NotificationStatus, "green" | "indigo" | "red" | "slate"> = {
  Sent: "green",
  Simulated: "indigo",
  Failed: "red",
  Pending: "slate",
};

const statusLabel: Record<NotificationStatus, string> = {
  Sent: "Delivered to provider",
  Simulated: "Simulated (not delivered)",
  Failed: "Failed",
  Pending: "Pending",
};

export default function NotificationsPage() {
  const [items, setItems] = useState<NotificationDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    notificationsApi
      .mine()
      .then((res) => setItems(res.data))
      .catch((err) => setError(getErrorMessage(err, "Could not load notifications.")))
      .finally(() => setLoading(false));
  }, []);

  return (
    <PageContainer>
      <PageHeader
        title="Notifications"
        description="Every SMS and email we send about your bookings, and whether it went out."
      />
      <ErrorMessage>{error}</ErrorMessage>

      <Card className="!p-0 overflow-hidden">
        {loading ? (
          <div className="space-y-3 p-6">
            {Array.from({ length: 4 }).map((_, i) => (
              <Skeleton key={i} className="h-16" />
            ))}
          </div>
        ) : items.length === 0 ? (
          <EmptyState
            icon={<Bell size={22} />}
            title="Nothing sent yet"
            description="You'll get an SMS and an email when you reserve, check in, exit or a hold expires."
          />
        ) : (
          <ul className="divide-y divide-slate-100">
            {items.map((n) => (
              <li key={n.id} className="flex gap-4 px-6 py-4">
                <span className="mt-0.5 flex h-9 w-9 shrink-0 items-center justify-center rounded-full bg-slate-100 text-slate-500">
                  {n.channel === "Sms" ? <MessageSquare size={16} /> : <Mail size={16} />}
                </span>
                <div className="min-w-0 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <p className="text-sm font-semibold text-slate-900">{typeLabels[n.type] ?? n.type}</p>
                    <Badge tone={statusTone[n.status]} dot>
                      {statusLabel[n.status]}
                    </Badge>
                    <span className="text-xs text-slate-400">Booking #{n.bookingId}</span>
                  </div>
                  <p className="mt-1 whitespace-pre-line break-words text-sm text-slate-600">
                    {n.channel === "Email" && n.subject ? n.subject : n.message}
                  </p>
                  <p className="mt-1 text-xs text-slate-400">
                    {n.channel === "Sms" ? "SMS" : "Email"} to {n.recipient} · {n.provider} · {formatDateTime(n.createdAt)}
                  </p>
                  {n.error && <p className="mt-1 text-xs text-rose-600">{n.error}</p>}
                </div>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </PageContainer>
  );
}
