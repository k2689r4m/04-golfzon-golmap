using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using GolfZonWebApp.Types;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using NetTopologySuite.Geometries;

namespace GolfZonWebApp.Models
{
    public class Hole : BaseEntity
    {
        public int Id { get; set; }
        [ForeignKey("GeoListId")]
        public GeoList GeoList { get; set; }
        public int? GeoListId { get; set; }

        [ForeignKey("CourseId")]
        public Course Course { get; set; }
        public int CourseId { get; set; }
        [StringLength(255)]
        public string Name { get; set; }
        [StringLength(255)]
        public string Par { get; set; }
        [StringLength(255)]
        public string Video { get; set; }
        [StringLength(255)]
        public string Image { get; set; }
        [StringLength(255)]
        public string Detail { get; set; }
        //public Polygon Position { get; set; }
    }
}