using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace GolfZonWebApp.Models
{
    public class GeoList : BaseEntity
    {
        public int Id { get; set; }
        public Geometry Position { get; set; }
        public int Code { get; set; }
        public int Group { get; set; }
        public ICollection<GolfClub> GolfClubs { get; set; }
        public ICollection<Hole> Holes { get; set; }
        public ICollection<ClubHouse> ClubHouses { get; set; }
        public ICollection<ShadeHouse> ShadeHouses { get; set; }
        public ICollection<Convenience> Conveniences { get; set; }
    }
}