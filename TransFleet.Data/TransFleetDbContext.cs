using System.Data.Entity;
using TransFleet.Data.Entities;

namespace TransFleet.Data
{
    public class TransFleetDbContext : DbContext
    {
        public TransFleetDbContext() : base("name=TransFleetConnection")
        {
            Configuration.LazyLoadingEnabled = true;
            Configuration.ProxyCreationEnabled = true;
        }

        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Fleet> Fleets { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<MaintenanceSchedule> MaintenanceSchedules { get; set; }
        public DbSet<FuelTransaction> FuelTransactions { get; set; }
        public DbSet<GPSPosition> GPSPositions { get; set; }
        public DbSet<HOSLog> HOSLogs { get; set; }
        public DbSet<Geofence> Geofences { get; set; }
        public DbSet<WorkOrder> WorkOrders { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure decimal precision for financial fields
            modelBuilder.Entity<FuelTransaction>()
                .Property(f => f.Amount)
                .HasPrecision(18, 2);

            modelBuilder.Entity<FuelTransaction>()
                .Property(f => f.Gallons)
                .HasPrecision(18, 3);

            modelBuilder.Entity<FuelTransaction>()
                .Property(f => f.PricePerGallon)
                .HasPrecision(18, 3);

            modelBuilder.Entity<Vehicle>()
                .Property(v => v.PurchasePrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<WorkOrder>()
                .Property(w => w.EstimatedCost)
                .HasPrecision(18, 2);

            modelBuilder.Entity<WorkOrder>()
                .Property(w => w.ActualCost)
                .HasPrecision(18, 2);

            // Configure GPS coordinate precision
            modelBuilder.Entity<GPSPosition>()
                .Property(g => g.Latitude)
                .HasPrecision(18, 8);

            modelBuilder.Entity<GPSPosition>()
                .Property(g => g.Longitude)
                .HasPrecision(18, 8);

            modelBuilder.Entity<GPSPosition>()
                .Property(g => g.Speed)
                .HasPrecision(18, 2);

            modelBuilder.Entity<GPSPosition>()
                .Property(g => g.Heading)
                .HasPrecision(18, 2);

            modelBuilder.Entity<HOSLog>()
                .Property(h => h.Latitude)
                .HasPrecision(18, 8);

            modelBuilder.Entity<HOSLog>()
                .Property(h => h.Longitude)
                .HasPrecision(18, 8);
        }
    }
}
