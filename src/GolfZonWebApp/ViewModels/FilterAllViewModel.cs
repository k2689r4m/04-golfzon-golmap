using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using GolfZonWebApp.Models;

namespace GolfZonWebApp.ViewModels
{
    public class FilterAllViewModel
    {
        public int Id { get; set; }
        public int GolfClubId { get; set; }
        public double? Length { get; set; }
        public ICollection<Hole> Holes
        {
            get { return null; }
            set { HolesCount = value.Count; }
        }

        public int HolesCount { get; set; }
    }
}