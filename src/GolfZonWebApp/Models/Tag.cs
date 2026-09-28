using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace GolfZonWebApp.Models
{
    public class Tag : BaseEntity
    {
        public int Id { get; set; }

        [StringLength(50)]
        [Required]
        public string Name { get; set; }

        [StringLength(255)]
        public string TFileName { get; set; }

        [StringLength(255)]
        public string TFileOriginalName { get; set; }

        [StringLength(255)]
        public string TFileUri { get; set; }

        [Required]
        public bool IsActive { get; set; } = false;

        public int? Sequence { get; set; }
    }
}
