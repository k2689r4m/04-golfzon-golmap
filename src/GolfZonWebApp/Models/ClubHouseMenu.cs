using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfZonWebApp.Models
{
    public class ClubHouseMenu : BaseEntity
    {
        public int Id { get; set; }
        [ForeignKey("ClubHouseId")]
        public ClubHouse ClubHouse { get; set; }
        public int ClubHouseId { get; set; }
        [StringLength(255)]
        public string Type { get; set; }
        [StringLength(255)]
        public string MenuName { get; set; }

        [Column(TypeName = "varchar(32)")]
        public string Price { get; set; } = "0¿ø";

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