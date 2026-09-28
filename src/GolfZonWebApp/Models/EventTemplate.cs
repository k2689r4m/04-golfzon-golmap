using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfZonWebApp.Models
{
    public class EventTemplate : BaseEntity
    {
        // 템플릿 상태
        public static int STATUS_NORMAL = 1;
        public static int STATUS_DELETED = 0;

        public int Id { get; set; }
        [ForeignKey("TemplateId")]
        public Template Template { get; set; }
        public int TemplateId { get; set; }
        [StringLength(255)]
        public string Title { get; set; }
        //[StringLength(255)]
        public string Content { get; set; }
        [StringLength(255)]
        public string Link { get; set; }
        [Required]
        [DefaultValue(1)]
        public int Status { get; set; } // 1: 정상 0: 삭제
        public bool IsPush { get; set; }
        public bool IsMainExposure { get; set; }
        public bool IsTemp { get; set; }
        [StringLength(255)]
        public string TFileOriginalName { get; set; }
        [StringLength(255)]
        public string TFileName { get; set; }
        [StringLength(255)]
        public string TFileUri { get; set; }
        public ICollection<EventTemplateGolfClub> EventTemplateGolfClubs  { get; set; }

        [NotMapped]
        public int? ImageIndex { get; set; }
    }
}