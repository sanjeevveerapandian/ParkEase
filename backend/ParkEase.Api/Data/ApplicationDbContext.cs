using Microsoft.EntityFrameworkCore;
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
        }
    }
}