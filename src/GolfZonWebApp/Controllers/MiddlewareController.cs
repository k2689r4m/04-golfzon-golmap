using GolfZonWebApp.Data;
using GolfZonWebApp.Models;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Security.Cryptography;
using GolfZonWebApp.Lib;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Dynamic;

namespace GolfZonWebApp.Controllers
{
    [ApiController]
    public class MiddlewareController : Controller
    {
        private readonly GolfzonContext context;
        private readonly IAntiforgery antiforgery;
        //private readonly ISessionRepository sessionRepository;
        private readonly AppSettings appSettings;

        public MiddlewareController (GolfzonContext context, IAntiforgery antiforgery, IOptions<AppSettings> appSettings)//, ISessionRepository sessionRepository)
        {
            this.context = context;
            this.antiforgery = antiforgery;
            //this.sessionRepository = sessionRepository;
            this.appSettings = appSettings.Value;
        }

        [HttpGet("[controller]/index")]
        public IActionResult Index()
        {
            return View();
        }

        //[HttpPost("[controller]/index")]
        //public IActionResult Index([FromForm]int id)
        //{
        //    try
        //    {
        //        User user = context.Users.Find(id);

        //        if (user == null)
        //        {
        //            user = new User() { Username = $"User-{id}", Fullname = $"User={id}", Nickname = $"User={id}", UserNum = $"UserNum=${id}", Phone = $"Phone-{id}", Email = $"Email-{id}", Setting = new Setting() { } };
        //            context.Users.Add(user);
        //            context.SaveChanges();
        //        }

        //        //key generate algorithm
        //        //string accessTokenKey = "";
        //        //Session session = new Session() { AccessTokenKey = accessTokenKey };
        //        //session = sessionRepository.Add(session);
        //        //end

        //        var tokenHandler = new JwtSecurityTokenHandler();
        //        //var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings.Secret));
        //        var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings.Secret));
        //        //key.KeyId = "1";
        //        //key.KeyId = session.Id.ToString();
        //        //var refreshKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings.Refresh));
        //        //key.KeyId = "1";
        //        //key.KeyId = session.Id.ToString();
        //        var tokenDescriptor = new SecurityTokenDescriptor
        //        {
        //            Subject = new ClaimsIdentity(new[] { new Claim("id", user.Id.ToString()) }),
        //            //Expires = DateTime.UtcNow.AddSeconds(20),//.AddHours(9).AddMinutes(10),
        //            Expires = DateTime.UtcNow.AddDays(7),
        //            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature)
        //            //SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
        //        };
        //        //var refreshTokenDescriptor = new SecurityTokenDescriptor
        //        //{
        //        //    Subject = new ClaimsIdentity(new[] { new Claim("id", user.Id.ToString()) }),
        //        //    Expires = DateTime.UtcNow.AddHours(9).AddMinutes(20),
        //        //    SigningCredentials = new SigningCredentials(refreshKey, SecurityAlgorithms.HmacSha512Signature)
        //        //};

        //        var token = tokenHandler.CreateToken(tokenDescriptor);
        //        //var refreshToken = tokenHandler.CreateToken(refreshTokenDescriptor);

        //        //user.RefreshToken = tokenHandler.WriteToken(refreshToken);
        //        //context.SaveChanges();

        //        //Session session = new Session() { AccessToken = tokenHandler.WriteToken(token) };

        //        //session = sessionRepository.Add(session);

        //        //return Json(new string[] { session.AccessToken, user.RefreshToken });
        //        return Json(tokenHandler.WriteToken(token));
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(false);
        //    }
        //}


        public class UserInfoDto
        {
            public int userNo { get; set; }
            public string nickName { get; set; }
            public string userId { get; set; }
        }

        [HttpPost("exist")]
        public IActionResult Exsit([FromBody] UserInfoDto user)
        {
            try
            {
                User _user = context.Users.Where(u => u.UserNum == user.userNo).FirstOrDefault();
                bool result = false;
                //User user = context.Users.Find(id);

                if (_user == null)
                {
                    result = true;
                }

                return Json(result);
            }
            catch
            {
                return BadRequest();
            }
        }

        [HttpPost("login")]

        public IActionResult Login([FromBody] UserInfoDto user)
        {
            try
            {
                User _user = context.Users.Where(u => u.UserNum == user.userNo).FirstOrDefault();
                bool result = false;
                //User user = context.Users.Find(id);

                if (_user == null)
                {
                    _user = new User()
                    {
                        Username = user.userId,
                        Fullname = user.nickName,
                        Nickname = user.nickName,
                        UserNum = user.userNo,
                        Phone = "",
                        Email = "",
                        Setting = new Setting() { }
                    };
                    context.Users.Add(_user);
                    context.SaveChanges();

                    result = true;
                    ////////////////////
                    /// 골프존 가입일자 넣기
                    /// 

                    var reg = GzApi.GetRegist(_user.UserNum);

                    if (reg != null)
                    {
                        _user.Regdt = reg.Regdt;
                        context.Users.Update(_user);
                        context.SaveChanges();
                    }

                }
                else
                {
                    _user.Fullname = user.nickName;
                    _user.Nickname = user.nickName;
                    context.SaveChanges();
                }

                var checkResult = GzApi.GetSingleParamSP(context, "MaintenanceUserCheck", "@UserId", _user.Id);

                if (checkResult.Count == 0)
                {
                    return StatusCode(503);
                }

                //key generate algorithm
                //string accessTokenKey = "";
                //Session session = new Session() { AccessTokenKey = accessTokenKey };
                //session = sessionRepository.Add(session);
                //end

                var tokenHandler = new JwtSecurityTokenHandler();
                //var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings.Secret));
                var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings.Secret));
                //key.KeyId = "1";
                //key.KeyId = session.Id.ToString();
                //var refreshKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings.Refresh));
                //key.KeyId = "1";
                //key.KeyId = session.Id.ToString();
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new[] { new Claim("id", _user.Id.ToString()), new Claim("u", _user.Nickname), new Claim("n", _user.UserNum.ToString()) }),
                    //Expires = DateTime.UtcNow.AddSeconds(20),//.AddHours(9).AddMinutes(10),
                    Expires = DateTime.UtcNow.AddMinutes(10),
                    SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature)
                    //SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256Signature)
                };

                //var refreshTokenDescriptor = new SecurityTokenDescriptor
                //{
                //    Subject = new ClaimsIdentity(new[] { new Claim("id", user.Id.ToString()) }),
                //    Expires = DateTime.UtcNow.AddHours(9).AddMinutes(20),
                //    SigningCredentials = new SigningCredentials(refreshKey, SecurityAlgorithms.HmacSha512Signature)
                //};

                var token = tokenHandler.CreateToken(tokenDescriptor);
                //var refreshToken = tokenHandler.CreateToken(refreshTokenDescriptor);

                //user.RefreshToken = tokenHandler.WriteToken(refreshToken);
                //context.SaveChanges();

                //Session session = new Session() { AccessToken = tokenHandler.WriteToken(token) };

                //session = sessionRepository.Add(session);

                //return Json(new string[] { session.AccessToken, user.RefreshToken });

                HttpContext.Response.Headers.Append("access-token", tokenHandler.WriteToken(token));

                /////////////////////////
                /// 유저 로긴 히스토리 추가
                context.UserLoginHistorys.Add(new UserLoginHistory { UserId = _user.Id });
                context.SaveChanges();
                /////////////////////////
                ///
                ////////////////////////////
                /// 스크린 데이터 업데이트
                GzApi.UpdateScreenData(context, _user.Id, _user.UserNum);
                /////////////////////////
                try
                {
                    using (var cmd = context.Database.GetDbConnection().CreateCommand())
                    {
                        cmd.CommandText = "Roulette_Add_Stamp";
                        cmd.CommandType = CommandType.StoredProcedure;
                        // set some parameters of the stored procedure
                        cmd.Parameters.Add(new SqlParameter("@UserId",
                            SqlDbType.Int)
                        { Value = _user.Id });
                        cmd.Parameters.Add(new SqlParameter("@State",
                            SqlDbType.VarChar)
                        { Value = "방문" });

                        if (cmd.Connection.State != ConnectionState.Open)
                            cmd.Connection.Open();

                        var retObject = new List<dynamic>();
                        using (var dataReader = cmd.ExecuteReader())
                        {
                            while (dataReader.Read())
                            {
                                var dataRow = new ExpandoObject() as IDictionary<string, object>;
                                for (var iFiled = 0; iFiled < dataReader.FieldCount; iFiled++)
                                {
                                    dataRow.Add(
                                        dataReader.GetName(iFiled),
                                        dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled]
                                    );
                                }

                                retObject.Add((ExpandoObject)dataRow);
                            }
                        }

                        var retStamp = retObject[0].type;
                        string sRetStamp;
                        switch (Convert.ToString(retStamp))
                        {
                            case "첫 방문":
                                sRetStamp = "1";
                                break;
                            case "재방문":
                                sRetStamp = "2";
                                break;
                            case "쉐이킹":
                                sRetStamp = "3";
                                break;
                            case "리뷰 작성":
                                sRetStamp = "4";
                                break;
                            default:
                                sRetStamp = null;
                                break;
                        }

                        if (sRetStamp != null)
                        {
                            Console.WriteLine("===============================");
                            HttpContext.Response.Headers.Append("stamp", sRetStamp);
                            Console.WriteLine("===============================");
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                /////////////////////////
                /// 유저 로긴 이벤트
                var loginHistory = context.UserLoginHistorys.Where(u => u.UserId == _user.Id).ToList();

                Console.WriteLine(loginHistory.Count);
                if (loginHistory.Any())
                {
                    if (loginHistory.Count == 2)
                    {
                        MileageManager.SetEvent(context, _user.UserNum, _user.Id,
                            MileageManager.EVENT_TYPE_RELOGIN, MileageManager.EVENT_GROUP_LOGIN);
                    }
                }
                /////////////////////////
                /// 사전 예약
                MileageManager.SetReservationEvent(context, _user.UserNum, _user.Id,
                            MileageManager.EVENT_TYPE_RESERVATION, MileageManager.EVENT_GROUP_ETC);
                /////////////////////////
                ///


                /////////////////////////
                /// 룰렛 이벤트
                /// 
                

                return Json(result);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        [HttpPost("logout")]
        public IActionResult Logout([FromForm] string accessToken)
        {
            //int userId= sessionRepository.Delete(accessToken);
            //if (userId == 0)
            //{
            //    return Ok();
            //}

            //User user = context.Users.Find(userId);

            //if (user != null)
            //{
            //    user.RefreshToken = null;
            //    context.SaveChanges();
            //}

            HttpContext.Response.Headers.Remove("access-token");

            return Ok();
        }
    }
}
