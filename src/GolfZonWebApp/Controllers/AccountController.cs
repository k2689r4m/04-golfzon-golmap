using GolfZonWebApp.Data;
using GolfZonWebApp.Models;
using GolfZonWebApp.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Dynamic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using GolfZonWebApp.Lib;

namespace GolfZonWebApp.Controllers
{
    [ApiController]
    public class AccountController : Controller
    {
        private readonly GolfzonContext context;
        private readonly UserManager<AdminUser> userManager;
        private readonly RoleManager<AdminRole> roleManager;
        private readonly SignInManager<AdminUser> signInManager;
        private readonly IHttpContextAccessor httpContext;
        private readonly IOptions<AppSettings> appSettings;

        public AccountController(GolfzonContext context, UserManager<AdminUser> userManager, RoleManager<AdminRole> roleManager
            , SignInManager<AdminUser> signInManager, IHttpContextAccessor httpContext, IOptions<AppSettings> appSettings)
        {
            this.roleManager = roleManager;
            this.context = context;
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.httpContext = httpContext;
            this.appSettings = appSettings;
        }

        //[HttpGet]
        //[AllowAnonymous]
        //public IActionResult Login()
        //{
        //    return View();
        //}

        //[AllowAnonymous]
        public class AdminUserLoginModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        [HttpPost("[controller]/login")]
        //public async Task<IActionResult> Login(AdminLoginViewModel model, string returnUrl)
        public async Task<IActionResult> Login([FromBody] AdminUserLoginModel model, string returnUrl)
        {
            string validError = "ID또는 PW가 올바르지 않습니다. 5회 실패 시 10분간 로그인 할 수 없습니다.";

            if (ModelState.IsValid)
            {
                var user = context.AdminUsers.Where(au => au.UserName == model.Username).FirstOrDefault();
                if (user == null)
                {
                    return BadRequest(validError);
                }
                else if (user.LockDateTime != null && user.LockDateTime > DateTime.UtcNow.AddHours(9))
                {
                    return BadRequest("로그인 실패 횟수 초과로 일정 시간 동안 로그인할 수 없습니다.");
                }

                var result = await signInManager.PasswordSignInAsync(model.Username, model.Password, false, false);

                if (result.Succeeded)
                {
                    var roles = from role in context.AdminRoles
                                join rel in context.AdminUserRoles on role.Id equals rel.RoleId
                                where rel.UserId == user.Id
                                select role;

                    var Subject = new ClaimsIdentity(new[] {
                        new Claim("i", user.Id.ToString()), new Claim("n", user.Name)
                    });
                    foreach (var role in roles.Select(r => r.Order))
                    {
                        Subject.AddClaim(new Claim("r", $"{role}"));
                    }

                    //Console.WriteLine("================================================");
                    //Console.WriteLine("Claim value");
                    //foreach (var t in Subject.Claims.Where(c => c.Type == "r").Select(c => c.Value))
                    //{
                    //    Console.WriteLine(t);
                    //}
                    //Console.WriteLine("================================================");

                    var tokenHandler = new JwtSecurityTokenHandler();

                    var newKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings.Value.AdminSecret));
                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = Subject,//new ClaimsIdentity(new[] { new Claim("id", user.Id.ToString()) }),
                        //Expires = DateTime.UtcNow.AddSeconds(20),//.AddHours(9).AddMinutes(10),
                        Expires = DateTime.UtcNow.AddMinutes(10),
                        SigningCredentials = new SigningCredentials(newKey, SecurityAlgorithms.HmacSha512Signature)
                    };

                    var newToken = tokenHandler.CreateToken(tokenDescriptor);

                    httpContext.HttpContext.Response.Headers.Append("access-token", tokenHandler.WriteToken(newToken));

                    var userIp = httpContext.HttpContext.Connection.RemoteIpAddress.ToString();

                    Console.WriteLine(httpContext.HttpContext.Connection.RemoteIpAddress.ToString());

                    using (var cmd = context.Database.GetDbConnection().CreateCommand())
                    {
                        cmd.CommandText = "Admin_Login_History_Insert";
                        cmd.CommandType = CommandType.StoredProcedure;
                        // set some parameters of the stored procedure
                        cmd.Parameters.Add(new SqlParameter("@P_IP",
                            SqlDbType.VarChar)
                        { Value = userIp });
                        cmd.Parameters.Add(new SqlParameter("@P_ADMIN_ID",
                            SqlDbType.NVarChar)
                        { Value = user.Id.ToString() });

                        if (cmd.Connection.State != ConnectionState.Open)
                            cmd.Connection.Open();

                        cmd.ExecuteReader();
                    }


                    return Ok();
                }
                else
                {
                    var userIp = httpContext.HttpContext.Connection.RemoteIpAddress.ToString();

                    using (var cmd = context.Database.GetDbConnection().CreateCommand())
                    {
                        cmd.CommandText = "Admin_Login_Fail_History_Insert";
                        cmd.CommandType = CommandType.StoredProcedure;
                        // set some parameters of the stored procedure
                        cmd.Parameters.Add(new SqlParameter("@P_IP",
                            SqlDbType.VarChar)
                        { Value = userIp });
                        cmd.Parameters.Add(new SqlParameter("@P_ADMIN_ID",
                            SqlDbType.NVarChar)
                        { Value = user.Id.ToString() });

                        if (cmd.Connection.State != ConnectionState.Open)
                            cmd.Connection.Open();

                        cmd.ExecuteReader();
                    }
                }

                //ModelState.AddModelError(string.Empty, "계정 정보를 확인해 주세요.");
            }

            //return View(model);
            return BadRequest(validError);
        }

        [HttpGet("api/usergolf/event/log/{section}/{type}")]
        public async Task<IActionResult> EventLog(int section, int type)
        {
            int tokenUserId = JwtManager.getJwtId(httpContext.HttpContext);
            if (tokenUserId == 0) return Unauthorized();

            Console.WriteLine($"EventLog: section[{section}], type[{type}], userId[{tokenUserId}]");

            if (section < 1 || type < 1) return BadRequest();

            using (var cmd = context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "SetConnectUserHistory";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_HistorySection",
                    SqlDbType.SmallInt)
                { Value = section });
                cmd.Parameters.Add(new SqlParameter("@P_HistoryType",
                    SqlDbType.SmallInt)
                { Value = type });
                cmd.Parameters.Add(new SqlParameter("@P_UserId",
                    SqlDbType.Int)
                { Value = tokenUserId });

                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();

                cmd.ExecuteReader();
            }

            return Ok();
        }

        //[AcceptVerbs("Get", "Post")]
        //[AllowAnonymous]
        //public IActionResult isUsernameNullOrEmpty(string username)
        //{
        //    return Json("아이디를 입력해 주세요.");

        //    if (string.IsNullOrEmpty(username))
        //    {
        //        return Json("아이디를 입력해 주세요.");
        //    }
        //    else
        //    {
        //        return Json(true);
        //    }
        //}

        //[AcceptVerbs("Get", "Post")]
        //[AllowAnonymous]
        //public IActionResult isPasswordNullOrEmpty(string password)
        //{
        //    if (string.IsNullOrEmpty(password))
        //    {
        //        return Json("비밀번호를 입력해 주세요.");
        //    }
        //    else
        //    {
        //        return Json(true);
        //    }
        //}
    }
}
