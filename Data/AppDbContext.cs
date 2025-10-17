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
        public DbSet<DriverDetails> DriverDetails { get; set; }
        public DbSet<PricingSettings> PricingSettings { get; set; }
        public DbSet<TelegramUser> TelegramUser { get; set; }
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


            // Настройка связи один-к-одному Driver - DriverDetails
            modelBuilder.Entity<Driver>()
                .HasOne(d => d.DriverDetails)
                .WithOne(dd => dd.Driver)
                .HasForeignKey<DriverDetails>(dd => dd.DriverId)
                .OnDelete(DeleteBehavior.Cascade);

            // Конвертация enum DriverStatus в строку
            modelBuilder.Entity<DriverDetails>()
                .Property(dd => dd.Status)
                .HasConversion<string>();


            modelBuilder.Entity<PricingSettings>().HasData(
                   new PricingSettings
                   {
                       Id = 1,
                       BasePrice = 50m,
                       PricePerKm = 20m,
                       NightMultiplier = 1.2m,
                       BadWeatherMultiplier = 1.3m
                   }
               );
            base.OnModelCreating(modelBuilder);
        }
    }
}
