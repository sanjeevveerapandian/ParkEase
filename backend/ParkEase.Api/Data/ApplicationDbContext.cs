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

            modelBuilder.Entity<ParkingLot>()
                .HasOne(pl => pl.Operator)
                .WithMany(u => u.ManagedLots)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ParkingSlot>()
                .HasOne(ps => ps.ParkingLot)
                .WithMany(pl => pl.Slots)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ParkingSlot>()
                .Property(ps => ps.HourlyRate)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<VehicleBooking>()
                .Property(vb => vb.TotalFee)
                .HasColumnType("decimal(10,2)");

            modelBuilder.Entity<VehicleBooking>()
                .Property(vb => vb.PenaltyFee)
                .HasColumnType("decimal(10,2)");

            // Root-cause fix for a whole class of bugs: SQL Server's datetime2 columns carry
            // no timezone info, so EF Core reads every DateTime back with Kind=Unspecified even
            // though every write in this app is DateTime.UtcNow. An Unspecified-kind DateTime
            // serializes to JSON without a "Z"/offset, and JS's Date parser then treats that as
            // *local* time — silently shifting every timestamp by the client's UTC offset. Rather
            // than re-tagging DateTimes by hand at each call site (fragile — easy for a new DTO
            // to forget), apply one converter to every DateTime/DateTime? property in the model
            // so this is correct everywhere, including fields no DTO exposes yet.
            var utcConverter = new ValueConverter<DateTime, DateTime>(
                v => v, // writing: already UTC (DateTime.UtcNow) — pass through
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc)); // reading: re-tag as UTC

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