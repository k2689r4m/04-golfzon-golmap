using System;
using System.ComponentModel.DataAnnotations;

namespace GolfZonWebApp.Models
{
    public class Admin : BaseEntity
    {
        public int Id { get; set; }

        [StringLength(50)]
        [Required()]
        public string Username { get; set; }

        [StringLength(50)]
        [Required()]
        public string Password { get; set; }

        [StringLength(20)]
        [Required()]
        public string Name { get; set; }

        [Required]
        public int Level { get; set; }
    }
}
