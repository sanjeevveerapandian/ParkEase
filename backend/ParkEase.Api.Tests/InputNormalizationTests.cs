using System.ComponentModel.DataAnnotations;
using ParkEase.Api.DTOs;
using ParkEase.Api.Services;
using ParkEase.Api.Services.Notifications;

namespace ParkEase.Api.Tests
{
    public class PhoneNumberTests
    {
        [Theory]
        [InlineData("9876543210", "+919876543210")]
        [InlineData("09876543210", "+919876543210")]
        [InlineData("919876543210", "+919876543210")]
        [InlineData("+919876543210", "+919876543210")]
        [InlineData("+91 98765 43210", "+919876543210")]
        [InlineData("98765-43210", "+919876543210")]
        [InlineData("(+91) 98765 43210", "+919876543210")]
        [InlineData("+14155552671", "+14155552671")]   // other countries pass through as E.164
        public void Valid_numbers_are_normalized_to_E164(string raw, string expected)
        {
            Assert.True(PhoneNumber.TryNormalize(raw, out var e164));
            Assert.Equal(expected, e164);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("12345")]
        [InlineData("5876543210")]        // Indian mobiles start 6-9
        [InlineData("98765432100")]       // 11 digits without a leading 0
        [InlineData("+915876543210")]
        [InlineData("98765+43210")]       // '+' only at the start
        [InlineData("not a number")]
        public void Invalid_numbers_are_rejected(string? raw)
        {
            Assert.False(PhoneNumber.TryNormalize(raw, out var e164));
            Assert.Equal(string.Empty, e164);
        }

        [Fact]
        public void Normalizing_twice_changes_nothing()
        {
            PhoneNumber.TryNormalize("9876543210", out var once);
            PhoneNumber.TryNormalize(once, out var twice);

            Assert.Equal(once, twice);
        }
    }

    public class VehicleNumberTests
    {
        [Theory]
        [InlineData("tn 09-ab 1234", "TN09AB1234")]
        [InlineData("TN09AB1234", "TN09AB1234")]
        [InlineData(" mh12de1433 ", "MH12DE1433")]
        public void Plates_are_normalized(string raw, string expected) =>
            Assert.Equal(expected, VehicleNumber.Normalize(raw));

        [Theory]
        [InlineData("TN09AB1234")]
        [InlineData("DL1CAB1234")]    // 3-letter series
        [InlineData("tn 09 ab 1234")] // normalized before checking
        public void Valid_plates_pass(string raw) => Assert.True(VehicleNumber.IsValid(raw));

        [Theory]
        [InlineData("")]
        [InlineData("garbage")]
        [InlineData("TN09AB12")]       // too few digits
        [InlineData("TN09AB12345")]    // too many digits
        [InlineData("T09AB1234")]      // one-letter state code
        [InlineData("TN09 1234")]      // no series letters
        public void Invalid_plates_fail(string raw) => Assert.False(VehicleNumber.IsValid(raw));
    }

    public class DtoValidationTests
    {
        private static IList<ValidationResult> Validate(object model)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(model, new ValidationContext(model), results, validateAllProperties: true);
            return results;
        }

        private static RegisterDto ValidRegistration() => new()
        {
            FullName = "Dana Driver",
            Email = "dana@example.com",
            Password = "Passw0rd!",
            PhoneNumber = "9876543210",
            Role = "Driver"
        };

        [Fact]
        public void A_complete_registration_is_valid() => Assert.Empty(Validate(ValidRegistration()));

        [Fact]
        public void Registration_rejects_a_bad_phone_number()
        {
            var dto = ValidRegistration();
            dto.PhoneNumber = "12345";

            Assert.Contains(Validate(dto), r => r.MemberNames.Contains(nameof(RegisterDto.PhoneNumber)));
        }

        [Fact]
        public void Registration_rejects_short_passwords_bad_emails_and_unknown_roles()
        {
            var dto = ValidRegistration();
            dto.Password = "short";
            dto.Email = "not-an-email";
            dto.Role = "Superuser";

            var members = Validate(dto).SelectMany(r => r.MemberNames).ToList();
            Assert.Contains(nameof(RegisterDto.Password), members);
            Assert.Contains(nameof(RegisterDto.Email), members);
            Assert.Contains(nameof(RegisterDto.Role), members);
        }

        [Theory]
        [InlineData("Car")]
        [InlineData("bike")]
        public void Vehicle_types_are_case_insensitive(string type) =>
            Assert.Empty(Validate(new CreateVehicleDto { VehicleNumber = "TN09AB1234", Type = type }));

        [Fact]
        public void Vehicles_need_a_real_plate_and_a_known_type()
        {
            var members = Validate(new CreateVehicleDto { VehicleNumber = "nope", Type = "Spaceship" })
                .SelectMany(r => r.MemberNames).ToList();

            Assert.Contains(nameof(CreateVehicleDto.VehicleNumber), members);
            Assert.Contains(nameof(CreateVehicleDto.Type), members);
        }

        [Fact]
        public void Slots_need_a_positive_rate_and_a_known_type()
        {
            var members = Validate(new CreateParkingSlotDto { SlotNumber = "A1", HourlyRate = 0, VehicleType = "Truck" })
                .SelectMany(r => r.MemberNames).ToList();

            Assert.Contains(nameof(CreateParkingSlotDto.HourlyRate), members);
            Assert.Contains(nameof(CreateParkingSlotDto.VehicleType), members);
        }

        [Fact]
        public void Operators_can_only_set_a_slot_to_Available_or_Maintenance()
        {
            Assert.Empty(Validate(new UpdateSlotStatusDto { Status = "Maintenance" }));
            Assert.NotEmpty(Validate(new UpdateSlotStatusDto { Status = "Occupied" }));
        }
    }
}
