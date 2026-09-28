using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GolfZonWebApp.Types;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using NetTopologySuite.Geometries;

namespace GolfZonWebApp.Models
{
    public class Setting : BaseEntity
    {
        public int Id { get; set; }
        [ForeignKey("UserId")]
        public int UserId { get; set; }

        public bool PrivateAgree { get; set; } = true;
        public bool FriendReview { get; set; } = true;
        public bool ReviewLike { get; set; } = true;
        public bool NewEvent { get; set; } = true;
        public bool GolfclubName { get; set; } = true;
        public bool Weather { get; set; } = true;
        public bool ScreenIcon { get; set; } = true;
        public bool FieldIcon  { get; set; } = true;
        public bool PremiumIcon { get; set; } = true;

        public ICollection<SettingFilter> SettingFilters { get; set; }
    }
}