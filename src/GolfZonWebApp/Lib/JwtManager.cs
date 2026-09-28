using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using GolfZonWebApp.Data;
using GolfZonWebApp.Models;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;

namespace GolfZonWebApp.Lib
{
    public class JwtManager
    {
        private readonly TimeSpan expireTime;
        private readonly GolfzonContext context;
        private readonly HttpContext httpContext;
        private readonly AppSettings appSettings;

        public JwtManager (TimeSpan expireTime, GolfzonContext context, IOptions<AppSettings> appSettings, HttpContext httpContext)
        {
            this.expireTime = expireTime;
            this.context = context;
            this.httpContext = httpContext;
            this.appSettings = appSettings.Value;
        }

        public string GenerateJwt(string secretKey, int userId)
        {
            try
            {
                User user = context.Users.Find(userId);

                if (user != null)
                {
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));
                    var tokenDescriptor = new SecurityTokenDescriptor
                    {
                        Subject = new ClaimsIdentity(new[] { new Claim("id", user.Id.ToString()) }),
                        Expires = DateTime.UtcNow + expireTime,
                        SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature)
                    };

                    var token = tokenHandler.CreateToken(tokenDescriptor);

                    return tokenHandler.WriteToken(token);
                }
            }
            catch (Exception e) { }

            return null;
        }

        public bool ValidateJwt (string jwt)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(appSettings.Secret);
                tokenHandler.ValidateToken(jwt, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                return true;
            }
            catch (SecurityTokenExpiredException stee)
            {
                //token expired
            }
            catch (Exception e)
            {
                //validate error
                Console.WriteLine(e.GetType().Name);
                Console.WriteLine(e.StackTrace);
            }

            return false;
        }

        public static int getJwtId(HttpContext httpContext)
        {
            try
            {
                if (httpContext.Request.Headers.ContainsKey("access-token"))
                {
                    string jwt = httpContext.Request.Headers["access-token"];

                    var tokenHandler = new JwtSecurityTokenHandler();

                    if (tokenHandler.CanReadToken(jwt))
                    {
                        JwtSecurityToken jwtToken = tokenHandler.ReadJwtToken(jwt);
                        return int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);
                    }
                }
            }
            catch (Exception e) { }

            return 0;
        }

        public static int getJwtNo(HttpContext httpContext)
        {
            try
            {
                if (httpContext.Request.Headers.ContainsKey("access-token"))
                {
                    string jwt = httpContext.Request.Headers["access-token"];

                    var tokenHandler = new JwtSecurityTokenHandler();
                    Console.WriteLine(0);
                    if (tokenHandler.CanReadToken(jwt))
                    {
                        Console.WriteLine(1);
                        JwtSecurityToken jwtToken = tokenHandler.ReadJwtToken(jwt);
                        Console.WriteLine(2);

                        var dd = int.Parse(jwtToken.Claims.First(x => x.Type == "n").Value);
                        Console.WriteLine(dd);

                        return dd;
                    }
                }
            }
            catch (Exception) { }

            return 0;
        }

        public string ValidateAndGenerateJwt (string jwt)
        {
            return "";
        }
    }
}
