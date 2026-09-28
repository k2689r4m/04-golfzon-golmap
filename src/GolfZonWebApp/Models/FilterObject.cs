using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GolfZonWebApp.Models
{
    public class FilterObject : BaseEntity
    {
        public int Id { get; set; }
        [StringLength(255)]
        public string Name { get; set; }
        public ICollection<FilterList> FilterLists { get; set; }
    }
}