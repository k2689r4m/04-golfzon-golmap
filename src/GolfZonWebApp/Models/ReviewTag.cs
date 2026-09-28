using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfZonWebApp.Models
{
    public class ReviewTag : BaseEntity
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("TagId")]
        public Tag Tag { get; set; }

        public int TagId { get; set; }

        [Required]
        [ForeignKey("ReviewId")]
        public Review Review { get;  set; }

        public int ReviewId { get; set; }
    }
}
