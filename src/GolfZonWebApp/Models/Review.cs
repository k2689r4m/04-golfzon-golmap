using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using Newtonsoft.Json;
using NetTopologySuite.Geometries;

namespace GolfZonWebApp.Models
{
    public class Review : BaseEntity
    {
        // 좋아연 상태 상수
        public static int STATE_NORMAL = 0;
        public static int STATE_DELETE = 1;
        public static int STATE_FORCED_DELETE = 2;

        public int Id { get; set; }
        [ForeignKey("GolfClubId")]
        public GolfClub GolfClub { get; set; }
        public int? GolfClubId { get; set; }
        [ForeignKey("ConvenienceId")]
        public Convenience Convenience { get; set; }
        public int? ConvenienceId { get; set; }
        [ForeignKey("UserId")]
        public User User { get; set; }
        public int UserId { get; set; }
        public int VisitCount { get; set; }
        public string ReviewContent { get; set; }
        public DateTime? PlayDate { get; set; }
        public int Score { get; set; } = 0;
        public double SCourse { get; set; } = 0.0;
        public double SFacility { get; set; } = 0.0;
        public double SCaddy { get; set; } = 0.0;
        public double SFoodAndDrink { get; set; } = 0.0;
        public string DCourse { get; set; }
        public string DFacility { get; set; }
        public string DCaddy { get; set; }
        public string DFoodAndDrink { get; set; }
        [StringLength(255)]
        public string CaddyName { get; set; }
        public bool Representation { get; set; } = false;       //대표여부
        public int OpenState { get; set; } = 0;         //공개여부
        public int State { get; set; } = 0;         //상태
        public int Views { get; set; } = 0;     //조회수
        public int SkinType { get; set; } = 0;     //스킨 타입
        //public int? ReCourseId { get; set; }     //다녀온 코스         =>  RefCourseLists
        public ICollection<RefCourseList> RefCourseLists { get; set; }
        public ICollection<ReviewGood> ReviewGoods { get; set; }
        public ICollection<ReviewImage> ReviewImages { get; set; }
        public ICollection<ReviewGrade> ReviewGrades { get; set; }
        public ICollection<ReviewReport> ReviewReports { get; set; }
        public ICollection<ReviewTag> ReviewTags { get; set; }
    }
}