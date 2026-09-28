//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//using System.IdentityModel.Tokens.Jwt;
//using Microsoft.Extensions.Options;
//using Microsoft.IdentityModel.Tokens;
//using System.Text;

//namespace GolfZonWebApp.Models
//{
//    public class MockSessionRepository : ISessionRepository
//    {
//        private List<Session> _sessionList;
//        private readonly AppSettings appSettings;

//        public MockSessionRepository (IOptions<AppSettings> appSettings)
//        {
//            _sessionList = new List<Session>();
//            this.appSettings = appSettings.Value;
//        }

//        public Session Add(Session session)
//        {
//            if (_sessionList.Count == 0)
//            {
//                session.Id = 1;
//            }
//            else
//            {
//                session.Id = _sessionList.Max(s => s.Id) + 1;
//            }

//            var utcNow = DateTime.UtcNow.AddHours(9);
//            session.CreatedAt = utcNow;
//            session.UpdatedAt = utcNow;

//            _sessionList.Add(session);

//            return session;
//        }

//        public Session GetSession(string accessToken)
//        {
//            return _sessionList.FirstOrDefault(s => s.AccessToken == accessToken);
//        }

//        public Session Update(Session sessionChange)
//        {
//            Session session = _sessionList.FirstOrDefault(s => s.Id == sessionChange.Id);

//            if (session != null)
//            {
//                var utcNow = DateTime.UtcNow.AddHours(9);
//                session.UpdatedAt = utcNow;
//                session.AccessToken = sessionChange.AccessToken;
//            }

//            return session;
//        }

//        public int Delete(string accessToken)
//        {
//            try
//            {
//                var tokenHandler = new JwtSecurityTokenHandler();

//                if (!tokenHandler.CanReadToken(accessToken))
//                {
//                    throw new Exception();
//                }

//                Session session = _sessionList.FirstOrDefault(s => s.AccessToken == accessToken);
//                if (session != null)
//                {
//                    JwtSecurityToken jst = tokenHandler.ReadJwtToken(accessToken);
//                    Console.WriteLine(jst.Header.First(x => x.Key == "kid").Value);

//                    var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(appSettings.Secret));
//                    tokenHandler.ValidateToken(accessToken, new TokenValidationParameters
//                    {
//                        ValidateIssuerSigningKey = true,
//                        IssuerSigningKey = key,
//                        ValidateIssuer = false,
//                        ValidateAudience = false,
//                        // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
//                        ClockSkew = TimeSpan.Zero,
//                        //AuthenticationType = SecurityAlgorithms.HmacSha512Signature,
//                        //AlgorithmValidator = SecurityAlgorithms.HmacSha512Signature,
//                        //ValidAlgorithms = SecurityAlgorithms.HmacSha512Signature,
//                        //ValidateLifetime = false,
//                    }, out SecurityToken validatedToken);

//                    var jwtToken = (JwtSecurityToken)validatedToken;
//                    Console.WriteLine($"userId: {int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value)}");

//                    return int.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);
//                }
//            }
//            catch (SecurityTokenExpiredException stee)
//            {
//                //token expired
//            }
//            catch (Exception e)
//            {
//                //validate error
//                Console.WriteLine(e.GetType().Name);
//                Console.WriteLine(e.StackTrace);
//            }

//            return 0;
//        }
//    }
//}
