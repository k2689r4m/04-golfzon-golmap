using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfZonWebApp.Models
{
    public class ConvenienceMenu : BaseEntity
    {
        public int Id { get; set; }
        [ForeignKey("ConvenienceId")]
        public Convenience Convenience { get; set; }
        public int ConvenienceId { get; set; }
        [StringLength(255)]
        public string Type { get; set; }
        [StringLength(255)]
        public string MenuName { get; set; }
        [StringLength(32)]
        public string Price { get; set; }
        [StringLength(255)]
        public string Description { get; set; }
        public bool Representative { get; set; }
        public int? Next { get; set; }
        [StringLength(255)]
        public string MFileName { get; set; }
        [StringLength(255)]
        public string MFileOriginalName { get; set; }
        [StringLength(255)]
        public string MFileUri { get; set; }

        [NotMapped]
        public int? ImageIndex { get; set; }
    }
}