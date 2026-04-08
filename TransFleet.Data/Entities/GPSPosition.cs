using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransFleet.Data.Entities
{
    [Table("GPSPositions")]
    [Index("VehicleId", "Timestamp")]
    public class GPSPosition
    {
        [Key]
        public long PositionId { get; set; }

        public int VehicleId { get; set; }

        public decimal Latitude { get; set; }

        public decimal Longitude { get; set; }

        public decimal? Speed { get; set; }

        public decimal? Heading { get; set; }

        public decimal? Altitude { get; set; }

        public DateTime Timestamp { get; set; }

        public int? Satellites { get; set; }

        [StringLength(20)]
        public string GPSQuality { get; set; }

        public DateTime CreatedDate { get; set; }

        [ForeignKey("VehicleId")]
        public virtual Vehicle Vehicle { get; set; }
    }
}
