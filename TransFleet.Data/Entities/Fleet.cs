using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransFleet.Data.Entities
{
    [Table("Fleets")]
    public class Fleet
    {
        [Key]
        public int FleetId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }

        public int ClientId { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(20)]
        public string Status { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }

        [ForeignKey("ClientId")]
        public virtual Client Client { get; set; }
    }
}
