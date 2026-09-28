using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using NetTopologySuite.Geometries;

namespace GolfZonWebApp.Models
{
    public class ConUpdateList : BaseEntity
    {
        public int Id { get; set; }

        [ForeignKey("ConvenienceId")]
        public int ConvenienceId { get; set; }
        public Convenience Convenience { get; set; }

        [StringLength(255)]
        public string BName { get; set; }
        [StringLength(255)]
        public string BType { get; set; }
        [StringLength(255)]
        public string BAddress { get; set; }
        [StringLength(255)]
        public string BContact { get; set; }
        public Geometry BPosition { get; set; }


        [StringLength(255)]
        public string AName { get; set; }
        [StringLength(255)]
        public string AType { get; set; }
        [StringLength(255)]
        public string AAddress { get; set; }
        [StringLength(255)]
        public string AContact { get; set; }
        public Geometry APosition { get; set; }
        public int Code { get; set; }
    }
}
