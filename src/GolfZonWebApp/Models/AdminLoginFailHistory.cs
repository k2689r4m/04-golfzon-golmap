using System.ComponentModel.DataAnnotations;

namespace GolfZonWebApp.Models
{
    public class AdminLoginFailHistory : BaseEntity
    {
        public int Id { get; set; }
        [Required]
        [StringLength(450)]
        public string AdminId { get; set; }
        [StringLength(15)]
        public string Ip { get; set; }
        public bool State { get; set; }
    }
}
