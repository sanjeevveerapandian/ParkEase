using Microsoft.Extensions.Logging.Abstractions;
using ParkEase.Api.Models;
using ParkEase.Api.Services.Notifications;
using ParkEase.Api.Tests.Support;

namespace ParkEase.Api.Tests
{
    public class NotificationTests
    {
        private sealed class FakeSms : ISmsSender
        {
            public string ProviderName => "FakeSms";
            public List<(string To, string Message)> Sent { get; } = new();
            public Func<SendResult>? Result { get; set; }
            public Exception? Throw { get; set; }

            public Task<SendResult> SendAsync(string toE164, string message, CancellationToken ct = default)
            {
                if (Throw != null) throw Throw;
                Sent.Add((toE164, message));
                return Task.FromResult(Result?.Invoke() ?? new SendResult(NotificationStatus.Sent, ProviderMessageId: "SM1"));
            }
        }

        private sealed class FakeEmail : IEmailSender
        {
            public string ProviderName => "FakeEmail";
            public List<(string To, string Subject, string Html)> Sent { get; } = new();
            public Exception? Throw { get; set; }

            public Task<SendResult> SendAsync(string toEmail, string toName, string subject, string htmlBody, CancellationToken ct = default)
            {
                if (Throw != null) throw Throw;
                Sent.Add((toEmail, subject, htmlBody));
                return Task.FromResult(new SendResult(NotificationStatus.Sent));
            }
        }

        private readonly TestWorld _w = new();
        private readonly FakeSms _sms = new();
        private readonly FakeEmail _email = new();

        private NotificationService Service() =>
            new(_w.Db, _sms, _email, _w.Clock, NullLogger<NotificationService>.Instance);

        private List<Notification> Rows(int bookingId) => _w.Db.Notifications.Where(n => n.VehicleBookingId == bookingId && n.Provider != "Test").ToList();

        // ------------------------------------------------------------- dispatching ----

        [Fact]
        public async Task Each_event_sends_one_sms_and_one_email_and_records_both()
        {
            var booking = await _w.ReserveAsync();

            await Service().NotifyAsync(NotificationType.BookingConfirmation, booking.Id);

            var sms = Assert.Single(_sms.Sent);
            Assert.Equal("+919876500001", sms.To);
            Assert.Contains("Slot A1", sms.Message);
            Assert.Contains("TN09AB1234", sms.Message);

            var email = Assert.Single(_email.Sent);
            Assert.Equal("dana@test.com", email.To);

            var rows = Rows(booking.Id);
            Assert.Equal(2, rows.Count);
            var smsRow = rows.Single(r => r.Channel == NotificationChannel.Sms);
            Assert.Equal(NotificationStatus.Sent, smsRow.Status);
            Assert.Equal("FakeSms", smsRow.Provider);
            Assert.Equal("+919876500001", smsRow.Recipient);
            var emailRow = rows.Single(r => r.Channel == NotificationChannel.Email);
            Assert.Equal("dana@test.com", emailRow.Recipient);
            Assert.NotNull(emailRow.Subject);
        }

        [Fact]
        public async Task A_raw_phone_number_is_normalized_before_sending()
        {
            var booking = await _w.ReserveAsync();
            _w.Driver.PhoneNumber = "98765 00001";
            _w.Db.SaveChanges();

            await Service().NotifyAsync(NotificationType.CheckIn, booking.Id);

            Assert.Equal("+919876500001", Assert.Single(_sms.Sent).To);
        }

        [Fact]
        public async Task An_invalid_phone_number_fails_the_sms_but_still_sends_the_email()
        {
            var booking = await _w.ReserveAsync();
            _w.Driver.PhoneNumber = "12345";
            _w.Db.SaveChanges();

            await Service().NotifyAsync(NotificationType.BookingConfirmation, booking.Id);

            Assert.Empty(_sms.Sent);
            Assert.Single(_email.Sent);
            var smsRow = Rows(booking.Id).Single(r => r.Channel == NotificationChannel.Sms);
            Assert.Equal(NotificationStatus.Failed, smsRow.Status);
            Assert.Contains("not a valid mobile number", smsRow.Error);
        }

        [Fact]
        public async Task A_provider_failure_is_recorded_with_its_reason()
        {
            var booking = await _w.ReserveAsync();
            _sms.Result = () => new SendResult(NotificationStatus.Failed, "Twilio 21608: unverified number");

            await Service().NotifyAsync(NotificationType.BookingConfirmation, booking.Id);

            var row = Rows(booking.Id).Single(r => r.Channel == NotificationChannel.Sms);
            Assert.Equal(NotificationStatus.Failed, row.Status);
            Assert.Equal("Twilio 21608: unverified number", row.Error);
        }

        [Fact]
        public async Task A_simulated_send_is_recorded_as_simulated()
        {
            var booking = await _w.ReserveAsync();
            _sms.Result = () => new SendResult(NotificationStatus.Simulated);

            await Service().NotifyAsync(NotificationType.BookingConfirmation, booking.Id);

            Assert.Equal(NotificationStatus.Simulated, Rows(booking.Id).Single(r => r.Channel == NotificationChannel.Sms).Status);
        }

        [Fact]
        public async Task A_sender_that_throws_never_breaks_the_caller()
        {
            var booking = await _w.ReserveAsync();
            _sms.Throw = new InvalidOperationException("sms exploded");
            _email.Throw = new InvalidOperationException("email exploded");

            await Service().NotifyAsync(NotificationType.BookingConfirmation, booking.Id);   // must not throw

            var rows = Rows(booking.Id);
            Assert.All(rows, r => Assert.Equal(NotificationStatus.Failed, r.Status));
            Assert.Contains(rows, r => r.Error == "sms exploded");
            Assert.Contains(rows, r => r.Error == "email exploded");
        }

        [Fact]
        public async Task Overlong_errors_are_trimmed_to_fit_the_column()
        {
            var booking = await _w.ReserveAsync();
            _sms.Result = () => new SendResult(NotificationStatus.Failed, new string('x', 900));

            await Service().NotifyAsync(NotificationType.BookingConfirmation, booking.Id);

            Assert.Equal(500, Rows(booking.Id).Single(r => r.Channel == NotificationChannel.Sms).Error!.Length);
        }

        [Fact]
        public async Task Notifying_for_a_missing_booking_does_nothing()
        {
            await Service().NotifyAsync(NotificationType.CheckIn, 9999);

            Assert.Empty(_sms.Sent);
            Assert.Empty(_email.Sent);
        }

        // ----------------------------------------------------------------- inbox ----

        [Fact]
        public async Task A_driver_sees_only_their_own_notifications_newest_first()
        {
            var mine = await _w.ReserveAsync();
            await Service().NotifyAsync(NotificationType.BookingConfirmation, mine.Id);
            _w.Clock.Advance(TimeSpan.FromMinutes(1));
            await Service().NotifyAsync(NotificationType.CheckIn, mine.Id);

            var theirVehicle = _w.AddVehicleFor(_w.OtherDriver, "TN10CD5678");
            var theirs = await _w.Bookings().ReserveAsync(
                new ParkEase.Api.DTOs.CreateBookingDto { VehicleId = theirVehicle.Id, ParkingSlotId = _w.CarSlot2.Id }, _w.OtherDriver.Id);
            await Service().NotifyAsync(NotificationType.BookingConfirmation, theirs.Value!.Id);

            var inbox = await Service().GetForDriverAsync(_w.Driver.Id);

            Assert.All(inbox, n => Assert.Equal(mine.Id, n.BookingId));
            Assert.Equal("CheckIn", inbox.First().Type);
            Assert.Contains(inbox, n => n.Channel == "Sms");
            Assert.Contains(inbox, n => n.Channel == "Email");
        }

        [Fact]
        public async Task The_inbox_respects_the_take_limit()
        {
            var booking = await _w.ReserveAsync();
            await Service().NotifyAsync(NotificationType.BookingConfirmation, booking.Id);
            await Service().NotifyAsync(NotificationType.CheckIn, booking.Id);

            Assert.Equal(3, (await Service().GetForDriverAsync(_w.Driver.Id, take: 3)).Count);
        }
    }

    public class NotificationTemplateTests
    {
        private static readonly DateTime Reserved = new(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc);   // 13:30 IST

        private static NotificationContext Context(
            DateTime? entry = null, DateTime? exit = null, decimal? total = null, decimal? penalty = null,
            DateTime? now = null, int holdMinutes = 15) =>
            new(BookingId: 42, VehicleNumber: "TN09AB1234", SlotNumber: "A1", LotName: "Mylapore Lot",
                HourlyRate: 50m, ReservedAt: Reserved, ReservationExpiresAt: Reserved.AddMinutes(holdMinutes),
                EntryTime: entry, ExitTime: exit, TotalFee: total, PenaltyFee: penalty,
                DriverName: "Dana", DriverEmail: "dana@test.com", DriverPhone: "+919876500001",
                NowUtc: now ?? Reserved);

        [Fact]
        public void Times_are_shown_in_IST()
        {
            Assert.Equal("05 Oct 2026, 01:30 PM IST", NotificationTemplates.FormatIst(Reserved));
        }

        [Theory]
        [InlineData(0, 45, "45m")]
        [InlineData(1, 5, "1h 5m")]
        [InlineData(26, 0, "26h 0m")]
        public void Durations_are_compact(int hours, int minutes, string expected) =>
            Assert.Equal(expected, NotificationTemplates.FormatDuration(new TimeSpan(hours, minutes, 0)));

        [Fact]
        public void The_confirmation_names_the_vehicle_slot_lot_and_deadline()
        {
            var content = NotificationTemplates.Build(NotificationType.BookingConfirmation, Context());

            Assert.Contains("Slot A1", content.Sms);
            Assert.Contains("Mylapore Lot", content.Sms);
            Assert.Contains("TN09AB1234", content.Sms);
            Assert.Contains("01:45 PM IST", content.Sms);   // 15-min hold from 01:30 PM
            Assert.Contains("#42", content.Sms);
            Assert.Contains("Check in by", content.EmailText);
        }

        [Fact]
        public void The_exit_receipt_totals_fee_and_penalty()
        {
            var entry = Reserved;
            var exit = Reserved.AddHours(26);
            var content = NotificationTemplates.Build(NotificationType.ExitReceipt, Context(entry: entry, exit: exit, total: 1200m, penalty: 150m));

            Assert.Contains("Total Rs.1350.00", content.Sms);
            Assert.Contains("incl. Rs.150.00 overstay", content.Sms);
            Assert.Contains("26h 0m", content.Sms);
            Assert.Contains("Overstay penalty", content.EmailText);
        }

        [Fact]
        public void A_receipt_without_a_penalty_omits_the_penalty_line()
        {
            var content = NotificationTemplates.Build(NotificationType.ExitReceipt,
                Context(entry: Reserved, exit: Reserved.AddMinutes(61), total: 100m, penalty: 0m));

            Assert.Contains("Total Rs.100.00", content.Sms);
            Assert.DoesNotContain("overstay", content.Sms);
            Assert.DoesNotContain("Overstay penalty", content.EmailText);
        }

        [Fact]
        public void Cancellation_and_expiry_say_there_is_no_charge()
        {
            var cancelled = NotificationTemplates.Build(NotificationType.ReservationCancelled, Context());
            var expired = NotificationTemplates.Build(NotificationType.ReservationExpired, Context(holdMinutes: 15));

            Assert.Contains("No charge", cancelled.Sms);
            Assert.Contains("No charge", expired.Sms);
            Assert.Contains("15 min", expired.Sms);
        }

        [Fact]
        public void The_overstay_warning_states_hours_parked_and_the_penalty_rate()
        {
            var content = NotificationTemplates.Build(NotificationType.OverstayWarning,
                Context(entry: Reserved, now: Reserved.AddHours(23).AddMinutes(10)));

            Assert.Contains("23h", content.Sms);
            Assert.Contains("1.5x", content.Sms);
            Assert.Contains("Please exit soon", content.Sms);
        }

        [Theory]
        [InlineData(NotificationType.BookingConfirmation)]
        [InlineData(NotificationType.CheckIn)]
        [InlineData(NotificationType.ExitReceipt)]
        [InlineData(NotificationType.ReservationCancelled)]
        [InlineData(NotificationType.ReservationExpired)]
        [InlineData(NotificationType.OverstayWarning)]
        public void Every_sms_stays_in_the_cheap_GSM7_alphabet_and_html_is_encoded(NotificationType type)
        {
            var ctx = Context(entry: Reserved, exit: Reserved.AddHours(2), total: 100m, penalty: 0m, now: Reserved.AddHours(23))
                with { LotName = "Tom & Jerry <Lot>" };

            var content = NotificationTemplates.Build(type, ctx);

            Assert.DoesNotContain("₹", content.Sms);
            Assert.All(content.Sms, ch => Assert.True(ch < 128, $"non-ASCII '{ch}' in SMS for {type}"));
            Assert.DoesNotContain("<Lot>", content.EmailHtml);   // encoded, not injected
        }
    }
}
