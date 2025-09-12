using Microsoft.EntityFrameworkCore;
using server.Models;

namespace server.Data
{
    public class AppDbContext : DbContext
    {
        public DbSet<Passenger> Passengers { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Ride> Rides { get; set; }
        public DbSet<RideHistory> RideHistories { get; set; }
        public DbSet<VerificationCode> VerificationCodes { get; set; }
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Настройка связей для Ride
            modelBuilder.Entity<Ride>()
                .HasOne(r => r.Passenger)
                .WithMany(p => p.Rides)
                .HasForeignKey(r => r.PassengerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ride>()
                .HasOne(r => r.Driver)
                .WithMany(d => d.Rides)
                .HasForeignKey(r => r.DriverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Конвертация enum в строку для БД
            modelBuilder.Entity<Ride>()
                .Property(r => r.PaymentMethod)
                .HasConversion<string>();

            modelBuilder.Entity<Ride>()
                .Property(r => r.Status)
                .HasConversion<string>();

            //конвертер для enum PaymentMethod RideHistory
            modelBuilder.Entity<RideHistory>()
                .Property(r => r.PaymentMethod)
                .HasConversion<string>();

            base.OnModelCreating(modelBuilder);
        }
    }
}
