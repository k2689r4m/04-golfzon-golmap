using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GolfZonWebApp.Types;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using NetTopologySuite.Geometries;

namespace GolfZonWebApp.Models
{
    public class SettingFilter : BaseEntity
    {
        public int Id { get; set; }
        [ForeignKey("SettingId")]
        public Setting Setting { get; set; }
        public int SettingId { get; set; }
        

        [ForeignKey("FilterObjectId")]
        public FilterObject FilterObject { get; set; }
        public int FilterObjectId { get; set; }
    }
}