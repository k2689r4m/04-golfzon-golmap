using System.ComponentModel.DataAnnotations;

namespace GolfZonWebApp.Models
{
    public class AdminLoginHistory : BaseEntity
    {
        public int Id { get; set; }
        [Required]
        [StringLength(450)]
        public string AdminId { get; set; }
        [StringLength(15)]
        public string Ip{ get; set; }
    }
}
