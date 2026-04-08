using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TransFleet.Data.Entities
{
    [Table("Drivers")]
    public class Driver
    {
        [Key]
        public int DriverId { get; set; }

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(100)]
        public string LastName { get; set; }

        [Required]
        [StringLength(50)]
        public string LicenseNumber { get; set; }

        public DateTime LicenseExpiry { get; set; }

        [StringLength(10)]
        public string CDLClass { get; set; } // A, B, C

        public DateTime? MedicalCertExpiry { get; set; }

        [StringLength(20)]
        public string Status { get; set; } // Active, OnLeave, Terminated, Suspended

        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(20)]
        public string Phone { get; set; }

        public DateTime HireDate { get; set; }

        public DateTime? TerminationDate { get; set; }

        [StringLength(200)]
        public string Address { get; set; }

        [StringLength(100)]
        public string City { get; set; }

        [StringLength(50)]
        public string State { get; set; }

        [StringLength(20)]
        public string ZipCode { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? ModifiedDate { get; set; }
    }
}
