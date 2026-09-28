using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace GolfZonWebApp.ViewModels
{
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "아이디를 입력해 주세요.")]
        //[Remote(action: "isUsernameNullOrEmpty", controller: "Account")]
        public string Username { get; set; }

        [Required(ErrorMessage = "비밀번호를 입력해 주세요.")]
        [DataType(DataType.Password)]
        //[Remote(action: "isPasswordNullOrEmpty", controller: "Account")]
        public string Password { get; set; }
    }
}
