using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using NetTopologySuite.Geometries;

namespace GolfZonWebApp.Models
{
    public class GolfClub : BaseEntity
    {
        // 골프장 상태
        public static int STATUS_NORMAL = 1;
        public static int STATUS_DELETED = 0;

        // 골프장 등급
        public static int GRADE_GENERAL = 1;
        public static int GRADE_PREMIUM = 2;

        // 한강 이남, 이북 여부
        public static int DIRECTION_SOUTH = 1;
        public static int DIRECTION_NORTH = 2;

        // 셀프 라운드
        public static int SELF_ROUND_ALL = 1;
        public static int SELF_ROUND_DAYTIME = 2;
        public static int SELF_ROUND_NIGHTTIME = 3;

        // 전장
        public static int HOLE_LENGTH_LONG = 1;
        public static int HOLE_LENGTH_SHORT = 2;

        public int Id { get; set; }

        [ForeignKey("GeoListId")]
        public GeoList GeoList { get; set; }
        public int? GeoListId { get; set; }

        [StringLength(255)]
        public string GcNum { get; set; }

        [StringLength(255)]
        public string GcNums { get; set; }

        public bool IsField { get; set; }
        public bool IsScreen { get; set; }
        [Required]
        [StringLength(255)]
        public string Name { get; set; }
        [StringLength(255)]
        public string Contact { get; set; }
        [Required]
        [DefaultValue(1)]
        public int Status { get; set; } // 1: 정상 0: 삭제
        [StringLength(255)]
        public string Address { get; set; }
        public int? Direction { get; set; } // 1: 한강이남 2: 한강이북
        public int? Since { get; set; }
        [StringLength(255)]
        public string Hours { get; set; }
        public double? CaddieFee { get; set; }
        public double? CartFee { get; set; }
        public double? GreenFee { get; set; }
        public int Grade { get; set; } // 1 일반 2 프리미엄
        public int SelfRound { get; set; } // 주/야간: 1 주간: 2 야간: 3
        //[StringLength(255)]
        //public string Filter { get; set; }
        [StringLength(255)]
        public string Note { get; set; }
        [StringLength(255)]
        public string TFileName { get; set; }
        [StringLength(255)]
        public string TFileOriginalName { get; set; }
        [StringLength(255)]
        public string TFileUri { get; set; }
        public string MFileName { get; set; }
        [StringLength(255)]
        public string MFileOriginalName { get; set; }
        [StringLength(255)]
        public string MFileUri { get; set; }

        [StringLength(255)]
        public string FairwayGrass { get; set; }
        [StringLength(255)]
        public string GreenGrass { get; set; }
        [Range(0.5, 5.0)]
        public double? DifficultyLevel { get; set; }
        [Range(0.5, 5.0)]
        public double? GreenDifficultyLevel { get; set; }
        [StringLength(255)]
        public string SignatureHole { get; set; }
        [Range(1, 2)]
        public int HoleLength { get; set; } // 1 긴 2 짧은
        [StringLength(32)]
        public string CoursesType { get; set; }
        //[StringLength(32)]
        //public string Length { get; set; }
        //public Polygon Position { get; set; } 

        public ICollection<LifeBestMonth> LifeBestMonths { get; set; }
        public ICollection<GolfClubImage> GolfClubImages { get; set; }
        public ICollection<Course> Courses { get; set; }
        public ICollection<ShadeHouse> ShadeHouses { get; set; }
        public ICollection<ShadeHouseMenu> ShadeHouseMenus { get; set; }
        public ClubHouse ClubHouse { get; set; }
        public ICollection<FilterList> FilterLists { get; set; }
        public ICollection<Review> Reviews { get; set; }
        public ICollection<Weather> Weathers { get; set; }
        public ICollection<WrongInfo> WrongInfos { get; set; }
        //public GeoList GeoLists { get; set; }

        [NotMapped]
        public int? ImageIndex { get; set; }

        //[NotMapped]
        //public int? ImageIndex2 { get; set; }
    }
}

