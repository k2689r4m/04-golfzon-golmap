using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using Newtonsoft.Json;

namespace GolfZonWebApp.Models
{
    public class Notification : BaseEntity
    {
        public int Id { get; set; }

        [ForeignKey("AuthorUserId ")]
        public User AuthorUser { get; set; }
        public int? AuthorUserId { get; set; }

        [ForeignKey("TargetUserId")]
        public User TargetUser { get; set; }
        public int? TargetUserId { get; set; }

        [ForeignKey("GolfClubId")]
        public GolfClub GolfClub { get; set; }
        public int? GolfClubId { get; set; }

        [ForeignKey("ConvenienceId")]
        public Convenience Convenience { get; set; }
        public int? ConvenienceId { get; set; }

        [ForeignKey("EventTemplateId")]
        public EventTemplate EventTemplate { get; set; }
        public int? EventTemplateId { get; set; }

        [ForeignKey("ReviewId")]
        public Review Review { get; set; }
        public int? ReviewId { get; set; }

        public string Contents { get; set; }

        public bool IsRead { get; set; } = false;

        /**
         * Types List
         * 
         * 10: 친구가 리뷰 작성한 경우
         * 11: 친구가 골프클럽 리뷰 작성한 경우
         * 12: 친구가 편의시설 리뷰 작성한 경우
         * 20: 내가 작성한 리뷰에 좋아요 달린 경우
         * 21: 내가 작성한 골프클럽 리뷰에 좋아요 달린 경우
         * 22: 내가 작성한 편의시설 리뷰에 좋아요 달린 경우
         * 30: 내가 작성한 리뷰가 관리자에 의해 삭제된 경우
         * 40: 관리자가 새로운 전체 이벤트/공지 등록한 경우
         **/
        public int Type { get; set; }

        [NotMapped]
        public bool State { get; set; }      
    }
}
