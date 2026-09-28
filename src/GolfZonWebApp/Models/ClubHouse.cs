using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using GolfZonWebApp.Types;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace GolfZonWebApp.Models
{
    public class ClubHouse : BaseEntity
    {
        public int Id { get; set; }

        [ForeignKey("GeoListId")]
        public GeoList GeoList { get; set; }
        public int? GeoListId { get; set; }

        [ForeignKey("GolfClubId")]
        public GolfClub GolfClub { get; set; }
        public int GolfClubId { get; set; }
        [StringLength(255)]
        public string MFileName { get; set; }
        [StringLength(255)]
        public string MFileOriginalName { get; set; }
        [StringLength(255)]
        public string MFileUri { get; set; }
        //public Point Position { get; set; }
        public ICollection<ClubHouseMenu> ClubHouseMenus { get; set; }

        [NotMapped]
        public int? ImageIndex { get; set; }
    }
}