using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfZonWebApp.Models
{
    public class ReviewImage : BaseEntity
    {
        public int Id { get; set; }
        [ForeignKey("ReviewId")]
        public Review Review { get; set; }
        public int ReviewId { get; set; }
        [StringLength(255)]
        public string Name { get; set; }
        [StringLength(255)]
        public string OriginalName { get; set; }
        [StringLength(255)]
        public string Uri { get; set; }
        public int? Next { get; set; }
        public bool Representative { get; set; } = false;

        
        [NotMapped]
        public int? ImageIndex { get; set; }
    }
}
