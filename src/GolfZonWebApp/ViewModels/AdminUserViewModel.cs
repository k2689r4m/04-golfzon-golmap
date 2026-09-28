using GolfZonWebApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace GolfZonWebApp.ViewModels
{
    public class AdminUserViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "아이디는 필수 입력값입니다.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "username length validation error")]
        public string UserName { get; set; }

        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required(ErrorMessage = "이름은 필수 입력값입니다.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "name length validation error")]
        public string Name { get; set; }
        public string Department { get; set; }

        [Range(0, 2)]
        public int Status { get; set; }

        public DateTime? LoggedAt { get; set; }

        public ICollection<AdminUserRole> AdminUserRoles { get; set; }
    }
}
