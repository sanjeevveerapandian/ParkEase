using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ParkEase.Api.Models;

namespace ParkEase.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<ParkingLot> ParkingLots { get; set; }
        public DbSet<ParkingSlot> ParkingSlots { get; set; }
        public DbSet<VehicleBooking> VehicleBookings { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<VehicleBooking>()
                .HasOne(vb => vb.User)
                .WithMany(u => u.Bookings)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VehicleBooking>()
                .HasOne(vb => vb.ParkingSlot)
                .WithMany(ps => ps.Bookings)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<VehicleBooking>()
                .HasOne(vb => vb.Vehicle)
                .WithMany(v => v.Bookings)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ParkingLot>()
                .HasOne(pl => pl.Operator)
                .WithMany(u => u.ManagedLots)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ParkingSlot>()
                .HasOne(ps => ps.ParkingLot)
                .WithMany(pl => pl.Slots)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vehicle>()
                .HasOne(v => v.Owner)
                .WithMany(u => u.Vehicles)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Vehicle>(e =>
            {
                e.Property(v => v.VehicleNumber).HasMaxLength(15);
                e.HasIndex(v => new { v.OwnerId, v.VehicleNumber }).IsUnique();
            });

            modelBuilder.Entity<ParkingSlot>(e =>
            {
                e.Property(ps => ps.SlotNumber).HasMaxLength(20);
                e.Property(ps => ps.HourlyRate).HasColumnType("decimal(10,2)");
                e.HasIndex(ps => new { ps.ParkingLotId, ps.SlotNumber }).IsUnique();
            });

            modelBuilder.Entity<VehicleBooking>(e =>
            {
                e.Property(vb => vb.VehicleNumber).HasMaxLength(15);
                e.Property(vb => vb.TotalFee).HasColumnType("decimal(10,2)");
                e.Property(vb => vb.PenaltyFee).HasColumnType("decimal(10,2)");

                // "Live" = Active (0) or Reserved (4). These two filtered unique indexes make the
                // database itself refuse a double-booked slot or a vehicle in two slots at once,
                // so two simultaneous requests can't both win even if both pass the service checks.
                e.HasIndex(vb => vb.ParkingSlotId)
                    .IsUnique()
                    .HasFilter("[Status] IN (0, 4)")
                    .HasDatabaseName("UX_VehicleBookings_OneLiveBookingPerSlot");
                e.HasIndex(vb => vb.VehicleId)
                    .IsUnique()
                    .HasFilter("[Status] IN (0, 4)")
                    .HasDatabaseName("UX_VehicleBookings_OneLiveBookingPerVehicle");
            });

            modelBuilder.Entity<Notification>(e =>
            {
                e.Property(n => n.Recipient).HasMaxLength(256);
                e.Property(n => n.Subject).HasMaxLength(200);
                e.Property(n => n.Message).HasMaxLength(2000);
                e.Property(n => n.Provider).HasMaxLength(50);
                e.Property(n => n.Error).HasMaxLength(500);
                e.HasIndex(n => new { n.VehicleBookingId, n.Type });
            });

            // SQL Server's datetime2 stores no timezone, so EF reads every DateTime back as
            // Kind=Unspecified; the JSON then loses its "Z" and browsers parse it as local time.
            // Tagging them as UTC here fixes it for every DateTime property in the model.
            var utcConverter = new ValueConverter<DateTime, DateTime>(
                v => v,
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableUtcConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(utcConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableUtcConverter);
                    }
                }
            }
        }
    }
}
