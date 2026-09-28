using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using NetTopologySuite.Geometries;

namespace GolfZonWebApp.Models
{
    public class Convenience: BaseEntity
    {
        // 매장 상태 상수
        public static int STATUS_NORMAL = 1;
        public static int STATUS_DELETED = 0;

        //[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        //public string Id { get; set; }
        public int Id { get; set; }
        public int? KaKaoId { get; set; }

        //[ForeignKey("GolfClubId")]
        //public GolfClub GolfClub { get; set; }
        //public int? GolfClubId { get; set; }

        [ForeignKey("GeoListId")]
        public GeoList GeoList { get; set; }
        public int GeoListId { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }
        public int Views { get; set; }
        public string ConNum { get; set; }
        [StringLength(255)]
        public string Type { get; set; }
        [StringLength(255)]
        public string Hours { get; set; }
        [StringLength(255)]
        public string Contact { get; set; }
        [StringLength(255)]
        public string Address { get; set; }
        [StringLength(255)]
        public string TFileName { get; set; }
        [StringLength(255)]
        public string TFileOriginalName { get; set; }
        [StringLength(255)]
        public string TFileUri { get; set; }
        [StringLength(255)]
        public string MFileName { get; set; }
        [StringLength(255)]
        public string MFileOriginalName { get; set; }
        [StringLength(255)]
        public string MFileUri { get; set; }
        //public double? Lat { get; set; }
        //public double? Lng { get; set; }
        [DefaultValue(1)]
        public int Status { get; set; } // 1: 정상 0: 삭제
        //public Point Location { get; set; }
        public ICollection<ConvenienceMenu> ConvenienceMenus { get; set; }
        public ICollection<ConvenienceImage> ConvenienceImages { get; set; }
        public ICollection<WrongInfo> WrongInfos { get; set; }
        public ICollection<ConUpdateList> ConUpdateLists { get; set; }

        [NotMapped]
        public int? TImageIndex { get; set; }

        [NotMapped]
        public int? MImageIndex { get; set; }
    }
}
