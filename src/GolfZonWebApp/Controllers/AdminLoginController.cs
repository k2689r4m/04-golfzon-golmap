using GolfZonWebApp.Data;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace GolfZonWebApp.Controllers
{
    public class AdminLoginController : Controller
    {
        private readonly GolfzonContext _context;

        public AdminLoginController(GolfzonContext context)
        {
            this._context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Index(string username, string password)
        {
            try
            {
                if (username == null || password == null)
                {
                    throw new Exception("입력값을 확인해주세요.");
                }

                var admin = _context.Admins.Where(v => v.Username == username).SingleOrDefault();

                if (admin == null)
                {
                    throw new Exception("존재하지 않는 계정입니다.");
                }
                else if (admin.Password != password)
                {
                    throw new Exception("잘못된 비밀번호입니다.");
                }

                // Put User Information into session.
                HttpContext.Session.SetString("admin", JsonSerializer.Serialize(admin));
                
                ViewBag.Message = string.Format("{0}님 환영합니다.\n접속일시: {1}", admin.Name, DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss"));
            } 
            catch (Exception e)
            {
                ViewBag.Message = e.Message;
            }
             
            return Redirect("/admin");
        }
    }
}
