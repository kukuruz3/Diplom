using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SanatoriumIS.Models;

namespace SanatoriumIS.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Procedure> Procedures { get; set; }
        public DbSet<ProcedureRoom> ProcedureRooms { get; set; }
        public DbSet<ProcedureAssignment> ProcedureAssignments { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Employee> Employees { get; set; }
        public DbSet<RoomPrice> RoomPrices { get; set; }
        public DbSet<BookingService> BookingServices { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Связь ApplicationUser с Employee
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Employee)
                .WithMany()
                .HasForeignKey(u => u.EmployeeId)
                .OnDelete(DeleteBehavior.SetNull);

            // Настройка точности для decimal
            builder.Entity<Procedure>()
                .Property(p => p.Price)
                .HasPrecision(18, 2);

            builder.Entity<Service>()
                .Property(s => s.Price)
                .HasPrecision(18, 2);

            builder.Entity<Employee>()
                .Property(e => e.Salary)
                .HasPrecision(18, 2);

            builder.Entity<RoomPrice>()
                .Property(rp => rp.PricePerNight)
                .HasPrecision(18, 2);

            builder.Entity<BookingService>()
                .Property(bs => bs.PriceAtTime)
                .HasPrecision(18, 2);

            // Связь Booking с BookingService (один ко многим)
            builder.Entity<BookingService>()
                .HasOne(bs => bs.Booking)
                .WithMany(b => b.BookingServices)
                .HasForeignKey(bs => bs.BookingId)
                .OnDelete(DeleteBehavior.Cascade);

            // Связь Service с BookingService (один ко многим)
            builder.Entity<BookingService>()
                .HasOne(bs => bs.Service)
                .WithMany()
                .HasForeignKey(bs => bs.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Связь ProcedureAssignment с Employee
            builder.Entity<ProcedureAssignment>()
                .HasOne(p => p.Employee)
                .WithMany(e => e.ProcedureAssignments)
                .HasForeignKey(p => p.EmployeeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Уникальные индексы
            builder.Entity<Room>()
                .HasIndex(r => r.Number)
                .IsUnique();

            builder.Entity<ProcedureRoom>()
                .HasIndex(p => p.RoomNumber)
                .IsUnique();

            builder.Entity<Service>()
                .HasIndex(s => s.Name)
                .IsUnique();

            builder.Entity<RoomPrice>()
                .HasIndex(rp => new { rp.Capacity, rp.Category })
                .IsUnique();
        }
    }
}