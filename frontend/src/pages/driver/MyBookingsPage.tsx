import { useEffect, useState } from "react";
import { ReceiptText } from "lucide-react";
import { bookingsApi } from "../../api/endpoints";
import { getErrorMessage } from "../../api/errors";
import { useToast } from "../../context/ToastContext";
import { BookingsTable } from "../../components/BookingsTable";
import { Card, EmptyState, ErrorMessage, PageContainer, PageHeader, Skeleton } from "../../components/ui";
import type { BookingDto } from "../../types";

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
          <BookingsTable bookings={bookings} variant="driver" onExit={handleExit} exitingId={exitingId} />
        )}
      </Card>
    </PageContainer>
  );
}
