using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace GolfZonWebApp.Models
{
    public class RefCourseList : BaseEntity
    {
        public int Id { get; set; }

        [Required]
        [ForeignKey("ReviewId")]
        public int ReviewId { get; set; }

        [Required]
        [ForeignKey("CourseId")]
        public int CourseId { get; set; }
    }
}
