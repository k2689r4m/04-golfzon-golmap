using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Antiforgery;
using GolfZonWebApp.Data;
using GolfZonWebApp.Models;

using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using GolfZonWebApp.Lib;

namespace GolfZonWebApp.Middlewares
{
    class UnAuthorizedUserWhiteList
    {

        //asdasadasdasdasadas
        private static string[][] PATH = new string[][] {
            //new string[] { "GET", "/middleware/index" },
            //new string[] { "POST", "/middleware/index" } ,
            new string[] { "POST", "/account/login" } ,
            new string[] { "POST", "/account/login" } ,
            //new string[] { "GET", "/api/user/maintenance" },
        };

        private static string[][] NOT_CREATE_TOKEN_PATH = new string[][]
        {
            new string[] { "GET", "/middleware/index" },
        };

        private static string API_PATH = "/api/usergolf/";

        private static string[] SHARE_API_PATH = new string[] {
            "/api/usergolf/detail_sp/",
            "/api/usergolf/golf/event/",
            "/api/usergolf/holeInfo/",
            "/api/usergolf/search_together/",
            "/api/usergolf/review/avg/",
            "/api/usergolf/convenience/all",
            "/api/usergolf/review/all",
            "/api/usergolf/detail/picture/golfclub/images",
            "/api/usergolf/detail/picture/golfclub/",
            //"/api/usergolf/detail/yadage",
            "/api/usergolf/convenience/detail/",
            "/api/usergolf/convenience/other/reviews/",
            "/api/usergolf/review/get/",
            "/api/usergolf/review/get",
            "/api/usergolf/convenience/around/conveniences",
            "/review/image/download"
        };

        private static string ADMIN_API_PATH = "/api";

        private static string[] ADMIN_PATH = new string[]
        {
            "", 
            "/api/users",
            "/api/golfclub",
            "/api/convenience",
            "/api/wronginfo",
            "/api/event",
            "/api/template",
            "/api/review",
            "/api/point",
            "/api/push",
            "/api/statistics",
            "/api/adminrole",
            "/api/tag",
            "/api/roulette_winner",// 바꿔야함
            "/api/roulette_setting",// 바꿔야함
            "/api/maintenance"
        };

        public static bool IsInWhiteList(string path, string method)
        {
            foreach (string[] _path in PATH)
            {
                if (string.Equals(path, _path[1], StringComparison.OrdinalIgnoreCase) && 
                    string.Equals(method, _path[0], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsInShareList(string path)
        {
            foreach (string _path in SHARE_API_PATH)
            {
                if (path.Contains(_path, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsNotNeedCreateToken(string path, string method)
        {
            foreach (string[] _path in NOT_CREATE_TOKEN_PATH)
            {
                if (string.Equals(path, _path[1], StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(method, _path[0], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsApiPath (string path)
        {
            return path.StartsWith(API_PATH);
        }

        public static bool IsAdminApiPath(string path)
        {
            return path.StartsWith(ADMIN_API_PATH);
        }

        public static bool TEST_AUTH(string path)
        {
            return path.StartsWith("/api/users", StringComparison.OrdinalIgnoreCase);
        }

        public static bool AdminAuthCheck (int order, string path, string method)
        {
            if (order < 0 || order > 28) return false;
            if (order == 0) return true;
            else if (order % 2 == 1)
            {
                if (method == "GET")
                {
                    if (path.StartsWith(ADMIN_PATH[(order + 1) / 2], StringComparison.OrdinalIgnoreCase)) return true;
                }
                return false;
            }
            else
            {
                if (method == "POST")
                {
                    if (path.StartsWith(ADMIN_PATH[order / 2], StringComparison.OrdinalIgnoreCase)) return true;
                }
                return false;
            }
        }

        public static bool AdminAuthListCheck(List<int> orders, string path, string method)
        {
            return orders.Where(order => AdminAuthCheck(order, path, method)).Count() > 0;
        }
    }

    // You may need to install the Microsoft.AspNetCore.Http.Abstractions package into your project
    public class UserSessionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly int userTokenTimeout = 20;
        private readonly int adminTokenTimeout = 20;

        public UserSessionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task Invoke(HttpContext httpContext, IAntiforgery antiforgery, GolfzonContext context, IOptions<AppSettings> appSettings)//, ISessionRepository sessionRepository)
        {
            // you can write here whatever you want
            string path = httpContext.Request.Path.Value;
            string method = httpContext.Request.Method;

            try
            {
                var token = httpContext.Request.Headers["access-token"];
                Console.WriteLine($"Request URL: {path}, Access-Token: {token}");

                if (token.Any())
                {
                    if (path.StartsWith("/api/usergolf/filter/all_sp", StringComparison.OrdinalIgnoreCase))
                    {
                        //1,6
                    }
                }

                if (path.StartsWith("/api/user/maintenance", StringComparison.OrdinalIgnoreCase))
                {
                    return _next(httpContext);
                }

                if (!token.Any() && UnAuthorizedUserWhiteList.IsInShareList(path))
                {
                    var checkResult = GzApi.GetSingleParamSP(context, "MaintenanceUserCheck", "@UserId", 0);

                    if (checkResult.Count == 0)
                    {
                        if (path.StartsWith("/review/image/download", StringComparison.OrdinalIgnoreCase))
                        {
                            httpContext.Response.Redirect("https://golmap.golfzon.com/user/maintenance");
                            return Task.FromResult(0);
                        }

                        httpContext.Response.StatusCode = 503;
                        return Task.FromResult(0);
                    }
                }
                else  if (UnAuthorizedUserWhiteList.IsApiPath(path))
                {
                    
                    var tokenHandler = new JwtSecurityTokenHandler();
                    byte[] key = null;
                    key = Encoding.ASCII.GetBytes(appSettings.Value.Secret);

                    tokenHandler.ValidateToken(token, new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                        ClockSkew = TimeSpan.Zero
                    }, out SecurityToken validatedToken);

                    var jwtToken = (JwtSecurityToken)validatedToken;
                    int userId = 0;
                    string adminUserId = null;
                    List<int> orders = new List<int>();
                    User user = null;
                    if (UnAuthorizedUserWhiteList.IsApiPath(path))
                    {
                        userId = int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);
                        user = context.Users.Find(userId);
                        if (user == null) throw new Exception();
                    }

                    var checkResult = GzApi.GetSingleParamSP(context, "MaintenanceUserCheck", "@UserId", user.Id);

                    if (checkResult.Count == 0)
                    {
                        httpContext.Response.StatusCode = 503;
                        return Task.FromResult(0);
                    }
                }
            } catch
            {
                httpContext.Response.StatusCode = 401;
                return Task.FromResult(0);
            }
            

            //if (UnAuthorizedUserWhiteList.IsAdminApiPath(path) && !UnAuthorizedUserWhiteList.IsApiPath(path))
            //{
            //    return _next(httpContext);
            //}

            if (!UnAuthorizedUserWhiteList.IsInWhiteList(path, method) && UnAuthorizedUserWhiteList.IsAdminApiPath(path))
            {
                var token = httpContext.Request.Headers["access-token"];

                if (!token.Any() && UnAuthorizedUserWhiteList.IsInShareList(path))
                {
                    return _next(httpContext);
                }

                try
                {
                    var tokenHandler = new JwtSecurityTokenHandler();

                    byte[] key = null;
                    if (UnAuthorizedUserWhiteList.IsApiPath(path))
                    {
                        key = Encoding.ASCII.GetBytes(appSettings.Value.Secret);
                    }
                    else
                    {
                        key = Encoding.ASCII.GetBytes(appSettings.Value.AdminSecret);
                    }

                    //var key = Encoding.ASCII.GetBytes(appSettings.Value.Secret);
                    tokenHandler.ValidateToken(token, new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                        ClockSkew = TimeSpan.Zero
                    }, out SecurityToken validatedToken);

                    var jwtToken = (JwtSecurityToken)validatedToken;
                    int userId = 0;
                    string adminUserId = null;
                    List<int> orders = new List<int>();
                    User user = null;
                    if (UnAuthorizedUserWhiteList.IsApiPath(path))
                    {
                        userId = int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);
                        user = context.Users.Find(userId);
                        if (user == null) throw new Exception();
                    }
                    else
                    {
                        adminUserId = jwtToken.Claims.First(x => x.Type == "i").Value;
                        orders = context.AdminUserRoles.Where(x => x.UserId == adminUserId).Join(context.AdminRoles, x => x.RoleId, y => y.Id, (x, y) => y.Order).Select(x => x).ToList();
                        //var roles = jwtToken.Claims.Where(x => x.Type == "r").Select(x => x.Value);
                        if (!path.StartsWith("/api/filters", StringComparison.OrdinalIgnoreCase) || method == "POST")
                        {
                            bool result = false;
                            if (path.StartsWith("/api/maintenance", StringComparison.OrdinalIgnoreCase))
                            {
                                result = true;
                            }
                            foreach (var o in orders)
                            {
                                result = result | UnAuthorizedUserWhiteList.AdminAuthCheck(o, path, method);
                            }

                            if (!result)
                            {
                                throw new Exception("innerAuth");
                            }
                        }
                    }

                    // attach user to context on successful jwt validation
                    //User user = context.Users.Find(session.UserId);

                    SymmetricSecurityKey newKey = null;
                    if (UnAuthorizedUserWhiteList.IsApiPath(path))
                    {
                        newKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings.Value.Secret));
                    }
                    else
                    {
                        newKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings.Value.AdminSecret));
                    }

                    ClaimsIdentity ci = new ClaimsIdentity();
                    if (UnAuthorizedUserWhiteList.IsApiPath(path))
                    {
                        ci.AddClaim(new Claim("id", userId.ToString()));
                        ci.AddClaim(new Claim("u", user.Nickname));
                        ci.AddClaim(new Claim("n", user.UserNum.ToString()));
                    }
                    else
                    {
                        ci.AddClaim(new Claim("n", jwtToken.Claims.First(x => x.Type == "n").Value));
                        ci.AddClaim(new Claim("i", adminUserId));
                        //var orders = context.AdminUserRoles.Where(x => x.UserId == adminUserId).Join(context.AdminRoles, x => x.RoleId, y => y.Id, (x, y) => y.Order).Select(x => x);
                        foreach (var o in orders)
                        {
                            ci.AddClaim(new Claim("r", o.ToString()));
                        }
                    }

                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = ci,
                        //Expires = DateTime.UtcNow.AddSeconds(20),//.AddHours(9).AddMinutes(10),
                        Expires = DateTime.UtcNow.AddMinutes(10),
                        SigningCredentials = new SigningCredentials(newKey, SecurityAlgorithms.HmacSha512Signature)
                    };

                    var newToken = tokenHandler.CreateToken(tokenDescriptor);

                    httpContext.Response.Headers.Append("access-token", tokenHandler.WriteToken(newToken));
                }
                catch (SecurityTokenExpiredException stee)
                {
                    //token expired
                    httpContext.Response.StatusCode = 401;
                    return Task.FromResult(0);
                }
                catch (Exception e)
                {
                    if (e.Message == "innerAuth")
                    {
                        httpContext.Response.StatusCode = 403;
                        return Task.FromResult(0);
                    }

                    httpContext.Response.StatusCode = 401; 
                    return Task.FromResult(0);
                }

                return _next(httpContext);
            }
            else
            {
                Console.WriteLine($"Request URL: {path}, UnauthorizedAccess");
            }

            return _next(httpContext);
        }
    }

    // Extension method used to add the middleware to the HTTP request pipeline.
    public static class UserSessionMiddlewareExtensions
    {
        public static IApplicationBuilder UseUserSessionMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UserSessionMiddleware>();
        }
    }
}
