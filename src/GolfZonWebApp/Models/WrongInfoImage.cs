using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfZonWebApp.Models
{
    public class WrongInfoImage : BaseEntity
    {
        public int Id { get; set; }
        [ForeignKey("WrongInfoId")]
        public WrongInfo WrongInfo { get; set; }
        public int WrongInfoId { get; set; }
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
