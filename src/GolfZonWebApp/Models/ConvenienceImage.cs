using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfZonWebApp.Models
{
    public class ConvenienceImage : BaseEntity
    {
        public int Id { get; set; }
        [ForeignKey("ConvenienceId")]
        public Convenience Convenience { get; set; }
        public int ConvenienceId { get; set; }
        [StringLength(255)]
        public string Name { get; set; }
        [StringLength(255)]
        public string OriginalName { get; set; }
        [StringLength(255)]
        public string Uri { get; set; }
        public int? Next { get; set; }

        [NotMapped]
        public int? ImageIndex { get; set; }
    }
}
