using GolfZonWebApp.Data;
using GolfZonWebApp.Models;
using GolfZonWebApp.Lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using GolfZonWebApp.Types;
using Newtonsoft.Json;
using NetTopologySuite.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Net;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Types;
using System.Text.RegularExpressions;
using System.Data;
using System.Dynamic;
using static System.Net.Mime.MediaTypeNames;
using System.Xml;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace GolfZonWebApp.Controllers
{
    [ApiController]
    public class UserGolfController : Controller
    {
        private readonly int STATE_NORMAL = 0;
        private readonly int STATE_DELETE = 1;
        private readonly int STATE_FORCED_DELETE = 2;
        private readonly GolfzonContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpContext _httpContext;
        private readonly IOptions<Geo> _geo;
        private readonly IOptions<GeoMore> _geoMore;

        public UserGolfController(GolfzonContext context, IOptions<Geo> geo, IOptions<GeoMore> geoMore, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _httpContext = httpContextAccessor.HttpContext;
            _geo = geo;
            _geoMore = geoMore;
        }

        //###############################
        [HttpGet("api/[controller]/convenience/other/reviews/{id}")]
        public async Task<ActionResult> GetConvenienceOtherReviewAll(int id)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            //if (tokenUserId == 0) return Unauthorized();

            List<int> blackUsers = _context.ReviewBlackUsers.Where(e => e.UserId == tokenUserId).Select(b => b.BlackUserId).ToList();

            var reviews = from review in _context.Reviews
                          where review.ConvenienceId == id && review.OpenState == 1 && review.State == 0
                          join convenience in _context.Conveniences on review.ConvenienceId equals convenience.Id
                          join user in _context.Users on review.UserId equals user.Id 
                          where user.Blinded != null
                          select new
                          {
                              review.Id,
                              name = convenience.Name,
                              review.ReviewContent,
                              review.CreatedAt,
                              User = new
                              {
                                  user.Id,
                                  user.Nickname,
                                  user.UserNum,
                                  Setting = _context.Settings.Where((s) => s.UserId == user.Id).Select((s) => new { s.PrivateAgree }).FirstOrDefault()
                              },
                              Images = _context.ReviewImages.Where(i => i.ReviewId == review.Id).Select(i => new {
                                  i.Uri,
                                  i.Representative
                                }).ToList(),
                              LikeCnt = _context.ReviewGoods.Count(r => r.State == 1 && r.ReviewId == review.Id),
                              HateCnt = _context.ReviewGoods.Count(r => r.State == 2 && r.ReviewId == review.Id),
                              MyGood = _context.ReviewGoods.Where(r => r.UserId == tokenUserId && r.ReviewId == review.Id).Select(e => new { e.Id, e.State }).FirstOrDefault(),
                              AvgScore = _context.ReviewGrades.Where(g => g.ReviewId == review.Id).Count() > 0 ? _context.ReviewGrades.Where(g => g.ReviewId == review.Id).Average(g => g.Score) : 0,
                          };

            var reviews2 = reviews.Where(review => !blackUsers.Contains(review.User.Id)).OrderByDescending(r => r.AvgScore).Take(3);

            return Json(reviews2);
        }

        [HttpGet("api/[controller]/convenience/all")]
        public async Task<ActionResult> GetConvenienceAll(int id, string type = null)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Mappage_Around_All";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@P_GOLF_ID",
                    SqlDbType.Int)
                { Value = id });
                cmd.Parameters.Add(new SqlParameter("@P_CON_TYPE",
                    SqlDbType.VarChar)
                { Value = type });
                

                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();

                var retObject = new List<Dictionary<string, object>>();
                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var dataRow = new Dictionary<string, object>();

                        for (var iFiled = 0; iFiled < dataReader.FieldCount; iFiled++)
                        {
                            if (dataReader.GetName(iFiled) == "position")
                            {
                                var geoStr = dataReader[iFiled].ToString();
                                SqlGeography s = SqlGeography.STGeomFromText(new System.Data.SqlTypes.SqlChars(geoStr), 4326);

                                if (geoStr.Contains("POINT"))
                                {
                                    dataRow.Add(
                                        dataReader.GetName(iFiled),
                                        new Point((double)s.Long, (double)s.Lat)
                                    );
                                }
                                else
                                {
                                    dataRow.Add(
                                        dataReader.GetName(iFiled),
                                        new Point((double)s.EnvelopeCenter().Long, (double)s.EnvelopeCenter().Lat)
                                    );
                                }
                            }
                            else
                            {
                                dataRow.Add(
                                    dataReader.GetName(iFiled),
                                    dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
                                );
                            }
                        }

                        retObject.Add(dataRow);
                    }
                }

                return Json(retObject);
            }
        }

        [HttpGet("api/[controller]/convenience/around/golfclubs")]
        public async Task<ActionResult> GetAroundGolfClubs(int id)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "ReviewDetailpage_Around_GolfClubs";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@P_CONV_ID",
                    SqlDbType.Int)
                { Value = id });


                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();

                var retObject = new List<Dictionary<string, object>>();
                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var dataRow = new Dictionary<string, object>();

                        for (var iFiled = 0; iFiled < dataReader.FieldCount; iFiled++)
                        {
                            dataRow.Add(
                                dataReader.GetName(iFiled),
                                dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
                            );
                        }

                        retObject.Add(dataRow);
                    }
                }

                return Json(retObject);
            }
        }

        [HttpGet("api/[controller]/keeping_alive")]
        public ActionResult KeepingAlive()
        {
            return Ok();
        }

        [HttpGet("api/[controller]/convenience/around/conveniences")]
        public async Task<ActionResult> GetAroundConveniences(int id)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            //if (tokenUserId == 0) return Unauthorized();

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "ReviewDetailpage_Around_Conveniences";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@P_GOLF_ID",
                    SqlDbType.Int)
                { Value = id });

                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();

                var retObject = new List<Dictionary<string, object>>();
                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var dataRow = new Dictionary<string, object>();

                        for (var iFiled = 0; iFiled < dataReader.FieldCount; iFiled++)
                        {
                            dataRow.Add(
                                dataReader.GetName(iFiled),
                                dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
                            );
                        }

                        retObject.Add(dataRow);
                    }
                }

                return Json(retObject);
            }
        }

        [HttpPost("api/[controller]/filter/all_sp")]
        public async Task<ActionResult<List<GolfClub>>> GetAllSp([FromBody] FilterAllDto param)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            DateTime date = DateTime.Now.Date;
            DateTime date2 = param.Date.Date;

            if (date.AddDays(-1) >= date2 || date.AddDays(14) <= date2)
            {
                return BadRequest();
            }

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Mappage_GolfClub_All";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@userId",
                    SqlDbType.Int)
                { Value = tokenUserId });
                cmd.Parameters.Add(new SqlParameter("@pox1",
                    SqlDbType.VarChar)
                { Value = param.Po1x.ToString() });
                cmd.Parameters.Add(new SqlParameter("@poy1",
                    SqlDbType.VarChar)
                { Value = param.Po1y.ToString() });
                cmd.Parameters.Add(new SqlParameter("@pox2",
                    SqlDbType.VarChar)
                { Value = param.Po2x.ToString() });
                cmd.Parameters.Add(new SqlParameter("@poy2",
                    SqlDbType.VarChar)
                { Value = param.Po2y.ToString() });
                cmd.Parameters.Add(new SqlParameter("@myX",
                    SqlDbType.Float)
                { Value = param.myX});
                cmd.Parameters.Add(new SqlParameter("@myY",
                    SqlDbType.Float)
                { Value = param.myY });
                cmd.Parameters.Add(new SqlParameter("@date",
                    SqlDbType.Date)
                { Value = param.Date });

                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();

                var retObject = new List<Dictionary<string, object>>();
                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var dataRow = new Dictionary<string, object>();

                        for (var iFiled = 0; iFiled < dataReader.FieldCount; iFiled++)
                        {
                            if (dataReader.GetName(iFiled) == "position")
                            {
                                var geoStr = dataReader[iFiled].ToString();
                                SqlGeography s = SqlGeography.STGeomFromText(new System.Data.SqlTypes.SqlChars(geoStr), 4326);

                                if (geoStr.Contains("POINT"))
                                {
                                    dataRow.Add(
                                        dataReader.GetName(iFiled),
                                        new Point((double)s.Long, (double)s.Lat)
                                    );
                                }
                                else
                                {
                                    dataRow.Add(
                                        dataReader.GetName(iFiled),
                                        new Point((double)s.EnvelopeCenter().Long, (double)s.EnvelopeCenter().Lat)
                                    );
                                }
                            }
                            else
                            {
                                dataRow.Add(
                                    dataReader.GetName(iFiled),
                                    dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
                                );
                            }
                        }

                        retObject.Add(dataRow);
                    }
                }

                return Json(retObject.Select(e => new
                {
                    id = e["id"],
                    holeCount = e["holeCount"],
                    courseCount = e["courseCount"],
                    filterObjectIdList = e["filterObjectIdList"].ToString() == "" ? new List<int>() : e["filterObjectIdList"].ToString().Split(",").Select(e => int.Parse(e)).ToList(),
                    filterName = e["filterName"],
                    golfClub = new
                    {
                        id = e["id"],
                        address = e["golfClub.address"],
                        grade = e["golfClub.grade"],
                        isField = e["golfClub.isField"],
                        isScreen = e["golfClub.isScreen"],
                        name = e["golfClub.name"],
                        tFileUri = e["golfClub.tFileUri"],
                        signatureHole = e["golfClub.signatureHole"],
                        fairwayGrass = e["golfClub.fairwayGrass"],
                        gcNum = e["golfClub.gcNum"],
                    },
                    position = e["position"],
                    visitCount = e["visitCount"],
                    screenVisitCount = e["screenVisitCount"],
                    weather = new
                    {
                        deg = e["weather.deg"],
                        main = e["weather.main"],
                        max = e["weather.max"],
                        min = e["weather.min"],
                        speed = e["weather.speed"],
                        now = e["weather.now"]
                    },
                    reviewAvgScore = e["avg"],
                    reviewCount = e["reviewCount"],
                    length = e["length"],
                    lengthAvg = e["lengthAvg"],
                    distance = e["distance"]
                })) ;
            }
        }


        //###############################
        [HttpPost("api/[controller]/filter/allLinq")]
        public async Task<ActionResult<List<GolfClub>>> GetAllLinq([FromBody] FilterAllDto param)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            if (param.Level != 1 && param.Level != 1.5) return Ok();


            var _golfClubsLinq = from golfClub in _context.GolfClubs
                                 .Where(g => g.Status == 1)
                                 .Select(g => new { g.Id, g.Name, g.Address, g.IsField, g.SignatureHole, g.TFileUri, g.IsScreen, g.Grade })
                                 join clubHouse in _context.ClubHouses.Where(c => c.GeoListId != null).Select(ch => new { ch.GolfClubId, ch.GeoListId }) on golfClub.Id equals clubHouse.GolfClubId
                                 join geoList in _context.GeoLists.Where(g => g.Group == 4).Select(g => new { g.Id, g.Position }) on clubHouse.GeoListId equals geoList.Id
                                 where param.GetPosition().Contains(geoList.Position)
                                 join weather in _context.Weathers.Where(w => w.Dt.Date == DateTime.Today.Date).Select(w => new
                                 {
                                     w.Main,
                                     w.Max,
                                     w.Min,
                                     w.Speed,
                                     w.Deg,
                                     w.Icon,
                                     w.GolfClubId
                                 }) on golfClub.Id equals weather.GolfClubId
                                 select new
                                 {
                                     //Id = golfClub.Id, // 여기추가함
                                     GolfClub = golfClub,
                                     Position = geoList.Position,
                                     Weather = weather,
                                     //Visit = _context.Reviews.Where(r => r.GolfClubId == golfClub.Id).Where(r => golfClub.IsField).Where(r => r.UserId == tokenUserId).Count() > 0
                                 };

            var _golfClubs = _golfClubsLinq.ToList();

            var _golfClubSubLinq = from GolfClubId in _golfClubs.Select(g => g.GolfClub.Id)
                                   join course in _context.Courses.Select(c => new { c.Id, c.Length, c.GolfClubId }) on GolfClubId equals course.GolfClubId
                                   join hole in _context.Holes.Select(h => new { h.Id, h.CourseId, h.Par }) on course.Id equals hole.CourseId
                                   group new { GolfClubId = GolfClubId, Course = course, Par = hole.Par } by hole.CourseId into holeGroup
                                   group new {
                                       GolfClubId = holeGroup.First().GolfClubId,
                                       HoleCount = holeGroup.Count(),
                                       Par = holeGroup.Sum(hg => int.TryParse(hg.Par, out int result) ? result : 0),
                                       Length = holeGroup.First().Course.Length,
                                   } by holeGroup.First().GolfClubId into courseGroup
                                   select new
                                   {
                                       GolfClubId = courseGroup.Key,
                                       CourseCount = courseGroup.Count(),
                                       HoleCount = courseGroup.Sum(cg => cg.HoleCount),
                                       Par = courseGroup.Sum(cg => cg.Par),
                                       Length = courseGroup.Sum(cg => cg.Length),
                                   };

            var _golfClubSub = _golfClubSubLinq.ToList();



            var _reviewsLinq = from golfClubId in _golfClubs.Select(g => g.GolfClub.Id)
                               join review in _context.Reviews.Where(r => r.GolfClubId != null && r.State == 0).Where(r => r.UserId == tokenUserId || r.OpenState == 1).Select(r => new
                               {
                                   r.Id,
                                   r.UserId,
                                   r.GolfClubId,
                                   AvgScore = (r.SCaddy + r.SCourse + r.SFacility + r.SFoodAndDrink) / 4,
                                   r.State,
                                   r.OpenState
                               }) on golfClubId equals review.GolfClubId
                               group review by review.GolfClubId into reviews
                               select new
                               {
                                   GolfClubId = reviews.Key,
                                   ReviewAvgScore = reviews.Average(r => r.AvgScore),
                                   VisitCount = reviews.Where(r => r.UserId == tokenUserId).Count(),
                               };

            var _reviews = _reviewsLinq.ToList();

            var data = from golfClub in _golfClubs
                       join golfClubSub in _golfClubSub on golfClub.GolfClub.Id equals golfClubSub.GolfClubId
                       join reviews in _reviews on golfClub.GolfClub.Id equals reviews.GolfClubId into reviewGroup
                       from reviews in reviewGroup.DefaultIfEmpty()
                       select new
                       {
                           Id = golfClub.GolfClub.Id, // 여기추가함
                           GolfClub = golfClub.GolfClub,
                           Position = golfClub.Position,
                           Weather = golfClub.Weather,
                           CourseCount = golfClubSub.CourseCount,
                           HoleCount = golfClubSub.HoleCount,
                           Par = golfClubSub.Par,
                           Length = golfClubSub.Length,
                           ReviewAvgScore = reviews == null ? 0 : reviews.ReviewAvgScore,
                           VisitCount = reviews == null ? 0 : reviews.VisitCount,
                       };

            var data2 = from d in (from d in data
                                   join filterList in _context.FilterLists on d.Id equals filterList.GolfClubId into filterListGroup
                                   from filterList in filterListGroup.DefaultIfEmpty()
                                   select new
                                   {
                                       Id = d.Id, // 여기추가함
                                       GolfClub = d.GolfClub,
                                       Position = d.Position,
                                       Weather = d.Weather,
                                       CourseCount = d.CourseCount,
                                       HoleCount = d.HoleCount,
                                       Par = d.Par,
                                       Length = d.Length,
                                       ReviewAvgScore = d.ReviewAvgScore,
                                       VisitCount = d.VisitCount,
                                       FilterObjectId = filterList == null ? 0 : filterList.FilterObjectId,
                                   })
                        join filterObject in _context.FilterObjects on d.FilterObjectId equals filterObject.Id into filterObjectGroup
                        from filterObject in filterObjectGroup.DefaultIfEmpty()
                        group filterObject by new
                        {
                            d.Id,
                            d.GolfClub,
                            d.Position,
                            d.Weather,
                            d.CourseCount,
                            d.HoleCount,
                            d.Par,
                            d.Length,
                            d.ReviewAvgScore,
                            d.VisitCount
                        } into filterObjectGroup
                        select new
                        {
                            Id = filterObjectGroup.Key.Id, // 여기추가함
                            GolfClub = filterObjectGroup.Key.GolfClub,
                            Position = filterObjectGroup.Key.Position,
                            Weather = filterObjectGroup.Key.Weather,
                            CourseCount = filterObjectGroup.Key.CourseCount,
                            HoleCount = filterObjectGroup.Key.HoleCount,
                            Par = filterObjectGroup.Key.Par,
                            Length = filterObjectGroup.Key.Length,
                            ReviewAvgScore = filterObjectGroup.Key.ReviewAvgScore,
                            VisitCount = filterObjectGroup.Key.VisitCount,
                            FilterObjectIdList = filterObjectGroup != null && filterObjectGroup.Count() > 0 ? filterObjectGroup.Where(f => f != null).Select(f => f.Id) : null,
                        };

            return Json(data2);

            //var _golfClubSub = _golfClubSubLinq.ToList();
            //var golfClubs = from golfClub in _golfClubSub
            //                group new
            //                {
            //                    GolfClub = golfClub.GolfClub.GolfClub,
            //                    Position = golfClub.GolfClub.Position,
            //                    Par = golfClub.Hole.Par,
            //                    Weather = golfClub.Weather,
            //                } by new
            //                {
            //                    CourseId = golfClub.Course.Id,
            //                } into holes
            //                group new
            //                {
            //                    GolfClub = holes.First().GolfClub,
            //                    Position = holes.First().Position,
            //                    Holes = holes.Count(),
            //                    //Par = holes.Sum(p => String.IsNullOrEmpty(p.Par) || p.Par == "-" ? 0 : Convert.ToInt32(p.Par)),
            //                    Par = holes.Sum(h => int.TryParse(h.Par, out int result) ? result : 0),
            //                    Weather = holes.First().Weather,
            //                } by holes.First().GolfClub.Id into courses
            //                select new
            //                {
            //                    GolfClub = courses.First().GolfClub,
            //                    Position = courses.First().Position,
            //                    HoleCount = courses.Sum(c => c.Holes),
            //                    CourseCount = courses.Count(),
            //                    Par = courses.Sum(c => c.Par),
            //                    Weather = courses.First().Weather,
            //                };

            ////_golfClubsLinq.ToList().GroupBy(g => g.Id)

            //    return Json(golfClubs);
        }

        [HttpGet("api/[controller]/filter/all2_sp")]
        public async Task<ActionResult> GetAllLevel2Sp(DateTime date, string foIds)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            DateTime date2 = DateTime.Now.Date;
            date = date.Date;

            if (date2.AddDays(-1) >= date || date2.AddDays(14) <= date)
            {
                return BadRequest();
            }

            

            DataTable tvp = new DataTable();
            tvp.Columns.Add(new DataColumn("code", typeof(int)));

            if (foIds != null)
            {
                string[] intFoIds = foIds.Split(new char[] { ',' });
                foreach (var item in intFoIds)
                {
                    tvp.Rows.Add(int.Parse(item));
                }
            }

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Mappage_WeatherArea_All";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@date",
                    SqlDbType.Date)
                { Value = date });
                cmd.Parameters.Add(new SqlParameter("@P_FO_ID_LIST", "dbo.CiCodeList") { Value = tvp });

                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();

                var retObject = new List<Dictionary<string, object>>();
                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var dataRow = new Dictionary<string, object>();

                        for (var iFiled = 0; iFiled < dataReader.FieldCount; iFiled++)
                        {
                            dataRow.Add(
                                dataReader.GetName(iFiled),
                                dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
                            );
                        }

                        retObject.Add(dataRow);
                    }
                }

                return Json(retObject.Select(e => new
                {
                    key = e["key"],
                    count = e["count"],
                    weather = new
                    {
                        deg = e["deg"],
                        main = e["main"],
                        min = e["min"],
                        max = e["max"],
                        speed = e["speed"],
                        now = e["now"]
                    }
                }));
            }
        }

        //###############################
        [HttpGet("api/[controller]/filter/all2")]
        public async Task<ActionResult> GetAllLevel2()
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            //if (param.Level != 2) return Ok();

            DateTime now = DateTime.UtcNow.AddHours(9);

            var areas = _context.ClubHouses.Join(_context.GeoLists, c => c.GeoListId, g => g.Id, (c, g) => new
            {
                ClubHouseId = c.Id,
                g.Code,
            })
            .GroupBy(c => c.Code)
            .Select(c => new
            {
                Key = c.Key,
                Count = c.Count(),
            })
            .Join(_context.WeatherAreas, c => c.Key, w => w.Code, (c, w) => new
            {
                Key = c.Key,
                Count = c.Count,
                Weather = new
                {
                    w.Dt,
                    w.Min,
                    w.Max,
                    w.Speed,
                    w.Deg,
                    w.Main,
                    w.Description,
                    w.Icon
                },
            })
            .ToList()
            .GroupBy(c => c.Key)
            .Select(c => new
            {
                Key = c.Key,
                Count = c.First().Count,
                Weather = c.Where(_c => _c.Weather.Dt.Date == now.Date).Select(_c => _c.Weather).FirstOrDefault(),
            })
            .ToList();

            return Json(areas);

            //var golfClubs = _context.ClubHouses
            //        .Join(_context.GeoLists, c => c.GeoListId, geo => geo.Id, (c, geo) => new {
            //            Code = geo.Code,
            //            Id = c.Id,
            //        })
            //        .GroupBy(x => x.Code)
            //        .Select(x => new {
            //            x.Key,
            //            Count = x.Count(),
            //            Weather = _context.WeatherAreas.Where(wt => wt.Code == x.Key)
            //                    .Where(wt => wt.Dt.Year == param.Date.Year && wt.Dt.Month == param.Date.Month && wt.Dt.Day == param.Date.Day).FirstOrDefault()
            //        }).ToList();

            //return Json(golfClubs);
        }


        //###############################
        [HttpPost("api/[controller]/filter/all")]
        public async Task<ActionResult<List<GolfClub>>> GetAll([FromBody] FilterAllDto param)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (param.Level == 1 || param.Level == 1.5)
            {
                //var golfClubs = await _context.GolfClubs
                //    .Where(gc => param.GetPosition().Contains(gc.GeoList.Position))
                //    .Include(gc => gc.GeoList)
                //    .ToListAsync();

                var golfClubs = _context.GolfClubs
                   //.Where(gc =>param.GetPosition().Contains(gc.GeoList.Position))
                   .Include(gc => gc.GeoList)
                   .Include(gc => gc.Courses)
                   .ThenInclude(gcc => gcc.Holes)
                   .Include(gc => gc.Reviews)
                   .Include(gc => gc.Weathers)
                   .Include(gc => gc.FilterLists)
                   .AsSplitQuery()
                   .AsNoTracking()
                   .AsEnumerable()
                   .Where(gc => gc.Status == 1 && (gc.GeoList == null ? false : param.GetPosition().Contains(gc.GeoList.Position.Centroid)))
                   .Select(golfClub =>
                   new
                   {
                       golfClub.Id,
                       golfClub.Name,
                       golfClub.Address,
                       golfClub.TFileUri,
                       golfClub.SignatureHole,
                       golfClub.IsField,
                       golfClub.IsScreen,
                       golfClub.FilterLists,
                       golfClub.Grade,

                       FieldRoundCount = _context.Reviews.Count(r => r.GolfClubId == golfClub.Id),
                       FieldMinScore = _context.Reviews.Where(r => r.GolfClubId == golfClub.Id).Count() == 0 ? 0 : _context.Reviews.Where(r => r.GolfClubId == golfClub.Id).Select(r => r.Score).Min(),
                       Length = golfClub.Courses.Sum(co => co.Length),
                       Par = golfClub.Courses.Join(_context.Holes, c => c.Id, h => h.CourseId, (c, h) => new
                       {
                           Par = h.Par
                       }).Sum(p => String.IsNullOrEmpty(p.Par) || p.Par == "-" || String.IsNullOrEmpty(p.Par) ? 0 : Convert.ToInt32(p.Par)),
                       Weather = golfClub.Weathers.Where(wt => wt.Dt.Year == param.Date.Year && wt.Dt.Month == param.Date.Month && wt.Dt.Day == param.Date.Day).FirstOrDefault(),
                       ReviewsCount = golfClub.Reviews.Count,
                       CoursesCount = golfClub.Courses.Count,
                       CourseLen = golfClub.Courses.Select(co => co.Length).Count() == 0 ? 0 : golfClub.Courses.Select(co => co.Length).Max(),
                       HolesCount = golfClub.Courses.Sum(co => co.Holes.Count()),
                       ReviewScoreAverage = golfClub.Reviews.Count() == 0 ? 0 : golfClub.Reviews.Average(re => (re.SCourse + re.SFacility + re.SFoodAndDrink + re.SCaddy) / 4),
                       Position = golfClub.GeoList.Position.Centroid
                   })
                   .ToList();




                //var golfClubs = await _context.GolfClubs
                //   //.Where(gc => param.GetPosition().Contains(gc.GeoList.Position))
                //   .Include(gc => gc.GeoList).Where(gc => gc.GeoList.Group == 1)
                //   .GroupBy(gc => gc.GeoList.Code)
                //   .Select(gg => new
                //   {
                //       gg.Key,
                //       Count = gg.Count(),
                //       Weather = _context.WeatherAreas
                //       .Where(wt => wt.Code == gg.Key)
                //       .Where(wt => wt.Dt.Year == param.Date.Year && wt.Dt.Month == param.Date.Month && wt.Dt.Day == param.Date.Day).FirstOrDefault()
                //   })
                //   .ToListAsync();

                //return Json(golfClubs);
                return await Task.FromResult(Json(golfClubs));
            }
            else if (param.Level == 2)
            {
                var golfClubs = _context.ClubHouses
                    .Join(_context.GeoLists, c => c.GeoListId, geo => geo.Id, (c, geo) => new {
                        Code = geo.Code,
                        Id = c.Id,
                    })
                    .GroupBy(x => x.Code)
                    .Select(x => new {
                        x.Key,
                        Count = x.Count(),
                        Weather = _context.WeatherAreas.Where(wt => wt.Code == x.Key)
                                .Where(wt => wt.Dt.Year == param.Date.Year && wt.Dt.Month == param.Date.Month && wt.Dt.Day == param.Date.Day).FirstOrDefault()
                    }).ToList();

                return Json(golfClubs);

                //var golfClubs = await _context.GolfClubs
                //    //.Where(gc => param.GetPosition().Contains(gc.GeoList.Position))
                //    .Include(gc => gc.GeoList).Where(gc => gc.GeoList.Group == 1)
                //    .GroupBy(gc => gc.GeoList.Code)
                //    .Select(gg => new
                //    {
                //        gg.Key,
                //        Count = gg.Count(),
                //        Weather = _context.WeatherAreas
                //        .Where(wt => wt.Code == gg.Key)
                //        .Where(wt => wt.Dt.Year == param.Date.Year && wt.Dt.Month == param.Date.Month && wt.Dt.Day == param.Date.Day).FirstOrDefault()
                //        //Position = _geo.Value.features.Where(geo => geo.properties.CODE == gg.Key).FirstOrDefault() != null ? 1: 0
                //    })
                //    .ToListAsync();

                //var golfClubs_ = golfClubs.Select(gc => new
                //{
                //    gc.Key,
                //    gc.Count,
                //    gc.Weather,
                //    Position = _geo.Value.features.Where(geo => geo.properties.CODE == gc.Key).FirstOrDefault().geometry.Centroid
                //});


                //return Json(golfClubs);
            }
            else
            {
                return Ok();
            }

        }

        [HttpGet("api/[controller]/weather/{code}")]
        public async Task<ActionResult<List<WeatherArea>>> GetWeather(int code)
        {
            return await _context.WeatherAreas.Where((wa) => wa.Code == code).ToListAsync();
        }

        [HttpGet("api/[controller]/weather")]
        public async Task<ActionResult<List<WeatherArea>>> GetWeatherGeo(double x, double y)
        {
            var po = new Point(x, y);
            foreach (var item in _geo.Value.features)
            {
                if (item.geometry.Contains(po))
                {
                    return await _context.WeatherAreas.Where((wa) => wa.Code == item.properties.CODE).ToListAsync();
                }
            }

            return Json(false);
        }


        //###############################
        [HttpGet("api/[controller]/review/image/download")]
        public async Task<ActionResult> ImageDownloadOtp (int id)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            var reviewImage = (from ri in _context.ReviewImages
                              join r in _context.Reviews on ri.ReviewId equals r.Id
                              where r.UserId == tokenUserId || (r.OpenState == 1 && r.State == 0) && ri.Id == id
                              select new
                              {
                                  ri.Name,
                                  ri.OriginalName,
                              }).FirstOrDefault();

            if (reviewImage == null)
            {
                return BadRequest();
            }

            string _key = Guid.NewGuid().ToString("N");

            var user = _context.Users.Find(tokenUserId);
            if (user == null)
            {
                return BadRequest();
            }

            user.RefreshToken = _key;
            _context.SaveChanges();

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(_key));
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("i", tokenUserId.ToString()), new Claim("r", id.ToString()) }),
                Expires = DateTime.UtcNow.AddHours(30),
                SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            string _token = tokenHandler.WriteToken(token);

            return Json(_token);
        }


        //###############################
        [HttpGet("review/image/download2")]
        public async Task<IActionResult> ImageDownload2(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();

                if (tokenHandler.CanReadToken(token))
                {
                    JwtSecurityToken jwtToken = tokenHandler.ReadJwtToken(token);
                    int id = int.Parse(jwtToken.Claims.First(x => x.Type == "i").Value);
                    int imageId = int.Parse(jwtToken.Claims.First(x => x.Type == "r").Value);

                    var user = _context.Users.Find(id);
                    if (user == null)
                    {
                        return BadRequest();
                    }

                    byte[] key = Encoding.ASCII.GetBytes(user.RefreshToken);

                    tokenHandler.ValidateToken(token, new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        // set clockskew to zero so tokens expire exactly at token expiration time (instead of 5 minutes later)
                        ClockSkew = TimeSpan.Zero
                    }, out SecurityToken validatedToken);

                    var ri = _context.ReviewImages.Find(imageId);

                    if (ri == null)
                    {
                        return BadRequest();
                    }

                    var r = _context.Reviews.Find(ri.ReviewId);

                    if (r == null)
                    {
                        return BadRequest();
                    }

                    ViewBag.uri = ri.Uri.Replace("images\\", "images/");
                    ViewBag.type = r.SkinType;
                    //ViewBag.name = ri.OriginalName;
                }
            }
            catch (Exception e) {
                return BadRequest();
            }

            return View();
        }

        //###############################
        [HttpGet("review/image/download")]
        public async Task<IActionResult> ImageDownload(string uri, string code, string score="", string golfClubName="", string playDate="")
        {
            try
            {
                uri = uri.Replace("images/", "images\\");

                var ri = _context.ReviewImages.Where(_ri => _ri.Uri == uri).FirstOrDefault();

                if (ri == null)
                {
                    return BadRequest();
                }

                var r = _context.Reviews.Find(ri.ReviewId);

                if (r == null)
                {
                    return BadRequest();
                }

                ViewBag.uri = ri.Uri.Replace("images\\", "images/");
                ViewBag.type = r.SkinType;
                ViewBag.code = code;
                ViewBag.golfClubName = golfClubName;
                ViewBag.score = score;
                ViewBag.playDate = playDate;
                //ViewBag.name = ri.OriginalName;
            }
            catch (Exception e)
            {
                return BadRequest();
            }

            return View();
        }

        [HttpGet("seon")]
        public async Task<ActionResult> GetSeon(string type)
        {
            var pathToSave = @"D:\wwwroot\zxzx\Upload\Temp\test.txt";
            string textValue = "asdasdasd";
            System.IO.File.WriteAllText(pathToSave, textValue, Encoding.Default);
            try { 
            
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            

            return Json("false");
            //GzApi.UpdateScreenData(_context, 80, 1793295);
            //return Ok();
            //var pathToSave = @"\\nas.golfzon.local\golmap_wwwimg\Images";


            //var pathToSave = Path.Combine(@"\\nas.golfzon.local\golmap_wwwimg", type);
            //string textValue = "asdasdsadas";
            //var fullPath = Path.Combine(pathToSave, "aaaaaa");

            //return Json(System.Security.Principal.WindowsIdentity.GetCurrent().Name);

            //try
            //{
            //    //System.IO.File.WriteAllText(fullPath, textValue, Encoding.Default);
            //    //return Json(Directory.GetDirectories(Directory.GetCurrentDirectory()));
            //    //using (var stream = new FileStream(fullPath, FileMode.Create))
            //    //{
            //    //    //await file.CopyToAsync(stream);
            //    //}




            //    int eventType = 0;

            //    bool reconSt = true;
               

            //    Console.WriteLine("리뷰 사용자 아이디 ::: {0}", 438887);
            //    if (MileageManager.SetEvent(_context, 438887, 242, 101, MileageManager.EVENT_GROUP_REVIEW))
            //    {
            //        //리뷰이벤트 마일리지 적립 성공
            //        Console.WriteLine("리뷰적립!!!");
            //    }
            //    else
            //    {
            //        Console.WriteLine("리뷰 적립 싈패");
            //    }
            //    /////////////////////////////////////////////////////////

            //    return Json(true);
            //}
            //catch (Exception e)
            //{
            //    return Json(e.ToString());
            //}

            //return Json("asdasd");

          


        }



        [HttpGet("api/[controller]/golf/event/{id}")]
        public async Task<ActionResult<List<EventTemplate>>> GetEventByGolfId(int id)
        {
            var now = DateTime.Now.Date;

            var _eventTemplatesLinq = from eventTemplateGolfClub in _context.EventTemplatesGolfClubs
                                          where eventTemplateGolfClub.GolfClubId == id
                                          join eventTemplate in _context.EventTemplates on eventTemplateGolfClub.EventTemplateId equals eventTemplate.Id
                                          where eventTemplate.Status == 1 && eventTemplate.IsMainExposure
                                          join template in _context.Templates on eventTemplate.TemplateId equals template.Id
                                          where template.IsActive == true
                                          where now >= template.StartDate.Date
                                          where now <= template.EndDate.Date
                                      orderby template.Sequence ascending, eventTemplate.Id ascending
                                          select eventTemplate;

            var _eventTemplates = _eventTemplatesLinq.Take(2).ToList();

            return Json(_eventTemplates);
        }

        [HttpGet("api/[controller]/convenience/detail/{id}")]
        public async Task<ActionResult<Convenience>> GetConvenienceItem(int id)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            //if (tokenUserId == 0) return Unauthorized();

            var c = _context.Conveniences.Where(c => c.Id == id).FirstOrDefault();

            if (c == null)
            {
                return NotFound();
            }

            c.Views += 1;
            await _context.SaveChangesAsync();

            var convenience = await _context.Conveniences
                                        .Include(c => c.ConvenienceMenus)
                                        .Include(c => c.ConvenienceImages)
                                        .Include(c => c.GeoList)
                                        .Select(c => new
                                        {
                                            Id = c.Id,
                                            Name = c.Name,
                                            Type = c.Type,
                                            Hours = c.Hours,
                                            Contact = c.Contact,
                                            Address = c.Address,
                                            Status = c.Status,
                                            TFileUri = c.TFileUri,
                                            MFileUri = c.MFileUri,
                                            Favorite = _context.ConvenienceFavorites.Where(cf => cf.ConvenienceId == c.Id && cf.UserId == tokenUserId).Select(f => new { f.Id }).FirstOrDefault(),
                                            ConvenienceMenus = c.ConvenienceMenus,
                                            ConvenienceImages = c.ConvenienceImages,
                                            Position = c.GeoList.Position.Centroid
                                        })
                                        .AsNoTracking()
                                        .AsSplitQuery()
                                        .SingleOrDefaultAsync(c => c.Status == 1 && c.Id == id);

            return Json(convenience);
        }

        [HttpGet("api/[controller]/holeInfo/{id}")]
        public async Task<ActionResult> GetHoleInfo(int id)
        {
            var golfClub = _context.GolfClubs.Select(g => new
            {
                g.Id,
                g.DifficultyLevel,
                g.GreenDifficultyLevel
            }).SingleOrDefault(g => g.Id == id);

            if (golfClub == null) return NotFound();

            var courses = _context.Courses
                               .Where(c => c.GolfClubId == id)
                               .Include(c => c.Holes)
                               .Select(c => new
                               {
                                     c.Id,
                                     c.CourseName,
                                     Holes = c.Holes.OrderBy(h => h.Name).Select(h => new
                                     {
                                          h.Id,
                                          h.Video,
                                          h.Image,
                                          h.Par,
                                     })
                               }).ToList();

            return Json(new
            {
                golfClub,
                courses,
            });
        }

        [HttpGet("api/[controller]/detail_sp/{id}")]
        public async Task<ActionResult> GetItemSP(int id)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            // if (tokenUserId == 0) return Unauthorized();

            var golfclub = new List<dynamic>();
            var clubHouseMenus = new List<dynamic>();
            var shadeHouseMenus = new List<dynamic>();
            var weathers = new List<dynamic>();
            var reviewImages = new List<dynamic>();
            var representativeReview = new List<dynamic>();
            var courses = new List<dynamic>();

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "GolfClubDetailpage_Main_Info";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@userId", SqlDbType.Int) { Value = tokenUserId });
                cmd.Parameters.Add(new SqlParameter("@golfClubId", SqlDbType.Int) { Value = id });

                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();

                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var dataRow = new ExpandoObject() as IDictionary<string, object>;

                        for (var iFiled = 0; iFiled < dataReader.FieldCount; iFiled++)
                        {
                            if (dataReader.GetName(iFiled) == "position")
                            {
                                var geoStr = dataReader[iFiled].ToString();
                                SqlGeography s = SqlGeography.STGeomFromText(new System.Data.SqlTypes.SqlChars(geoStr), 4326);

                                if (geoStr.Contains("POINT"))
                                {
                                    dataRow.Add(
                                        dataReader.GetName(iFiled),
                                        new Point((double)s.Long, (double)s.Lat)
                                    );
                                }
                                else
                                {
                                    dataRow.Add(
                                        dataReader.GetName(iFiled),
                                        new Point((double)s.EnvelopeCenter().Long, (double)s.EnvelopeCenter().Lat)
                                    );
                                }
                            }
                            else
                            {
                                dataRow.Add(
                                    dataReader.GetName(iFiled),
                                    dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
                                );
                            }
                        }

                        golfclub.Add((ExpandoObject)dataRow);
                    }
                }
            }

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "GolfClubDetailpage_Main_ClubHouseMenus";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@golfClubId", SqlDbType.Int) { Value = id });

                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();

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

                        clubHouseMenus.Add((ExpandoObject)dataRow);
                    }
                }
            }

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "GolfClubDetailpage_Main_ShadeHouseMenus";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@golfClubId", SqlDbType.Int) { Value = id });

                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();

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

                        shadeHouseMenus.Add((ExpandoObject)dataRow);
                    }
                }
            }

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "GolfClubDetailpage_Main_Weather";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@golfClubId", SqlDbType.Int) { Value = id });

                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();

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

                        weathers.Add((ExpandoObject)dataRow);
                    }
                }
            }

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "GolfClubDetailpage_Review_Info";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@golfclubId", SqlDbType.Int) { Value = id });
                cmd.Parameters.Add(new SqlParameter("@userId", SqlDbType.Int) { Value = tokenUserId });

                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();

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

                        representativeReview.Add((ExpandoObject)dataRow);
                    }
                }
            }

            var r = representativeReview.Count > 0 ? (IDictionary<string, object>)representativeReview[0] : null;
            
            if (r != null)
            {
                using (var cmd = _context.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = "GolfClubDetailpage_Review_Good";
                    cmd.CommandType = CommandType.StoredProcedure;
                    // set some parameters of the stored procedure
                    cmd.Parameters.Add(new SqlParameter("@reviewId", SqlDbType.Int) { Value = (int)r["id"] });
                    cmd.Parameters.Add(new SqlParameter("@userId", SqlDbType.Int) { Value = tokenUserId });

                    if (cmd.Connection.State != ConnectionState.Open)
                        cmd.Connection.Open();

                    using (var dataReader = cmd.ExecuteReader())
                    {
                        if (dataReader.Read())
                        {
                            for (var iFiled = 0; iFiled < dataReader.FieldCount; iFiled++)
                            {
                                r.Add(
                                    dataReader.GetName(iFiled),
                                    dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled]
                                );
                            }
                        }
                    }
                }

                using (var cmd = _context.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = "Get_Review_images";
                    cmd.CommandType = CommandType.StoredProcedure;
                    // set some parameters of the stored procedure
                    cmd.Parameters.Add(new SqlParameter("@reviewId", SqlDbType.Int) { Value = (int)r["id"] });

                    if (cmd.Connection.State != ConnectionState.Open)
                        cmd.Connection.Open();

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

                            reviewImages.Add((ExpandoObject)dataRow);
                        }
                    }

                    r.Add(
                        "images",
                        reviewImages
                    );
                }
            }


            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "GolfClubDetailpage_Courses";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_GOLF_ID", SqlDbType.Int) { Value = id });

                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();

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

                        courses.Add((ExpandoObject)dataRow);
                    }
                }
            }

            if (golfclub.Count == 0) return NotFound();

            var g = (IDictionary<string, object>)golfclub[0];

            return Json(new
            {
                courseCount = g["courseCount"],
                filterList = g["filterList"].ToString() == "" ? new List<string>().ToArray() : g["filterList"].ToString().Split(","),
                courses = courses,
                images = g["images"].ToString() == "" ? new List<string>().ToArray() : g["images"].ToString().Split(","),
                golfClub = new
                {
                    id = g["id"],
                    address = g["golfClub.address"],
                    grade = g["golfClub.grade"],
                    isField = g["golfClub.isField"],
                    isScreen = g["golfClub.isScreen"],
                    name = g["golfClub.name"],
                    signatureHole = g["golfClub.signatureHole"],
                    tFileUri = g["golfClub.tFileUri"],
                    caddieFee = g["golfClub.caddieFee"],
                    cartFee = g["golfClub.cartFee"],
                    since = g["golfClub.since"],
                    selfRound = g["golfClub.selfRound"],
                    gcNum = g["golfClub.gcNum"],
                    contact = g["golfClub.contact"],
                    fairwayGrass = g["golfClub.fairwayGrass"],
                    greenGrass = g["golfClub.greenGrass"],
                    mFileUri = g["golfClub.mFileUri"],
                    note = g["golfClub.note"],
                    coursesType = g["golfClub.coursesType"]
                },
                clubHouse = new
                {
                    mFileUri = g["clubHouse.mFileUri"],
                    clubHouseMenus = clubHouseMenus
                },
                representation = r == null ? null : r,
                holeCount = g["holeCount"],
                length = g["length"],
                lengthAvg = g["lengthAvg"],
                par = g["par"],
                favorite = g["favorite"],
                position = g["position"],
                visitCount = g["visitCount"],
                screenVisitCount = g["screenVisitCount"],
                fieldReviewAvgScore = g["fieldReviewAvgScore"],
                fieldReviewCount = g["fieldReviewCount"],
                fieldAvgScore = g["fieldAvgScore"],
                minScore = g["minScore"],
                screenMinScore = g["screenMinScore"],
                shadeHouseMenus = shadeHouseMenus,
                weathers = weathers
            });
        }

        [HttpGet("api/[controller]/detailLinq/{id}")]
        public async Task<ActionResult> GetItemLinq (int id)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            //var _t = from a in _context.GolfClubs
            //         join b in _context.GolfClubImages on a.Id equals b.GolfClubId into _b
            //         from b in _b.DefaultIfEmpty()
            //         join c in _context.Reviews on a.Id equals c.GolfClubId into _c
            //         from c in _c.DefaultIfEmpty()
            //         select new { a, b, c };

            //return Json(_t.ToList());

            var _golfClubsLinq = from golfClub in _context.GolfClubs
                                 .Where(g => g.Id == id)
                                 .Where(g => g.Status == 1)
                                 .Select(g => new { g.Id, g.Name, g.Address, g.IsField, g.SignatureHole, g.TFileUri, g.IsScreen, g.Since, g.SelfRound, g.Contact, g.FairwayGrass, g.GreenGrass, g.CartFee, g.CaddieFee, g.Note, g.Grade, g.GcNum, g.MFileUri })
                                 join clubHouse in _context.ClubHouses on golfClub.Id equals clubHouse.GolfClubId into clubHouseGroup
                                 from clubHouse in clubHouseGroup.DefaultIfEmpty()
                                 join geoList in _context.GeoLists on clubHouse.GeoListId equals geoList.Id into geoListGroup
                                 from geoList in geoListGroup.DefaultIfEmpty()
                                 join favorite in _context.GolfFavorites
                                 on new { GolfClubId = golfClub.Id, UserId = tokenUserId } equals new { GolfClubId = favorite.GolfClubId, UserId = favorite.UserId } into favoriteGroup
                                 from favorite in favoriteGroup.DefaultIfEmpty()
                                 join image in _context.GolfClubImages on golfClub.Id equals image.GolfClubId into imageGroup
                                 from image in imageGroup.DefaultIfEmpty()
                                 select new
                                 {
                                     GolfClub = golfClub,
                                     Images = image == null ? null : new { image.Uri }, //_context.GolfClubImages.Where(gi => gi.GolfClubId == id).Select(gi => new { gi.Id, gi.Uri, gi.GolfClubId }).ToList(),
                                     Favorite = favorite == null ? null : new { favorite.Id },//_context.GolfFavorites.Where(gf => gf.UserId == tokenUserId).Where(gf => gf.GolfClubId == id).Select(gf => new { gf.GolfClubId, gf.Id }).FirstOrDefault(),
                                     Position = geoList == null ? null : geoList.Position,
                                 };

            var _golfClubs = (from golfClub in _golfClubsLinq.ToList()
                             group golfClub.Images by new { golfClub.GolfClub, golfClub.Favorite, golfClub.Position } into images
                             select new
                             {
                                 GolfClub = images.Key.GolfClub,
                                 Favorite = images.Key.Favorite,
                                 Images = images.Where(i => i != null),
                                 Position = images.Key.Position
                             }).FirstOrDefault();

            if (_golfClubs == null)
            {
                return NotFound();
            }

            var _golfClubWeatherLinq = from w in _context.Weathers
                                       where w.GolfClubId == id
                                       orderby w.Dt ascending
                                       select new
                                       { w.Main, w.Max, w.Min, w.Speed, w.Deg, w.Icon, w.GolfClubId, w.Dt };

            var _golfClubWeather = _golfClubWeatherLinq.ToList();

            var _golfClubSubLinq = from course in _context.Courses
                                   where course.GolfClubId == id
                                   join hole in _context.Holes on course.Id equals hole.CourseId
                                   select new
                                   {
                                       Par = hole.Par,
                                       Length = course.Length,
                                       CourseName = course.CourseName,
                                       CourseId = course.Id,
                                       GolfClubId = course.GolfClubId
                                   };

            var _golfClubSub = (from golfClubSub in _golfClubSubLinq.ToList()
                               group golfClubSub.Par by new { golfClubSub.CourseId, golfClubSub.Length, golfClubSub.CourseName, golfClubSub.GolfClubId } into holeGroup
                               group new
                               {
                                   Length = holeGroup.Key.Length,
                                   CourseName = holeGroup.Key.CourseName,
                                   Par = holeGroup.Sum(p => int.TryParse(p, out int n) ? n : 0),
                                   HoleCount = holeGroup.Count()
                               } by holeGroup.Key.GolfClubId into courseGroup
                               select new
                               {
                                   Length = courseGroup.Sum(c => c.Length),
                                   Par = courseGroup.Sum(c => c.Par),
                                   CourseName = courseGroup.Select(c => c.CourseName),
                                   HoleCount = courseGroup.Sum(c => c.HoleCount),
                                   CourseCount = courseGroup.Count()
                               }).FirstOrDefault();

            var _reviewsLinq = from review in _context.Reviews
                               where review.GolfClubId == id && (review.UserId == tokenUserId || review.OpenState == 1)
                               group review by review.GolfClubId into reviews
                               select new
                               {
                                   GolfClubId = reviews.Key,
                                   VisitCount = reviews.Where(r => r.UserId == tokenUserId).Count(),
                                   MyFieldMinScore = reviews.Where(r => r.UserId == tokenUserId).Count() > 0 ? reviews.Where(r => r.UserId == tokenUserId).Min(r => r.Score) : 0,
                                   FieldAvgScore = reviews.Count() > 0 ? reviews.Average(r => r.Score) : 0,
                                   FieldReviewCount = reviews.Count(),
                                   FieldAvgS = reviews.Count() > 0 ? reviews.Average(r => (r.SCourse + r.SFacility + r.SFoodAndDrink + r.SCaddy) / 4) : 0,
                               };


            var _reviews = _reviewsLinq.FirstOrDefault();

            var _representationReviewLinq = from review in _context.Reviews
                                            where review.GolfClubId == id && review.Representation && review.State == 0 && review.OpenState == 1
                                            //.Select(r => new { r.CaddyName, r.UserId, r.CreatedAt, r.Id, r.PlayDate, r.ReviewContent, r.SCaddy, r.SCourse, r.SFacility, r.SFoodAndDrink, r.Score, r.SkinType })
                                            join user in _context.Users on review.UserId equals user.Id
                                            where user.Blinded == null
                                            join blackUser in _context.ReviewBlackUsers on new { UserId = tokenUserId, BlackUserId = user.Id } equals new { UserId = blackUser.UserId, BlackUserId = blackUser.BlackUserId } into blackUserGroup
                                            from blackUser in blackUserGroup.DefaultIfEmpty()
                                            where blackUser == null
                                            join setting in _context.Settings on user.Id equals setting.UserId
                                            select new
                                            {
                                                Review = new { 
                                                    review.CaddyName,
                                                    review.UserId,
                                                    review.CreatedAt,
                                                    review.Id,
                                                    review.PlayDate,
                                                    review.ReviewContent,
                                                    review.SCaddy,
                                                    review.SCourse,
                                                    review.SFacility,
                                                    review.SFoodAndDrink,
                                                    review.Score,
                                                    review.SkinType },
                                                User = new { user.Id, user.Nickname },
                                                Setting = new { setting.PrivateAgree }
                                            };

            var _representationReviewFirst = _representationReviewLinq.FirstOrDefault();

            var _goodLinq = _representationReviewFirst == null ?
                (from reviewGood in new List<ReviewGood>()
                 where (_representationReviewFirst.Review.Id == reviewGood.ReviewId)
                 select new
                 {
                     reviewGood.Id,
                     reviewGood.State,
                     reviewGood.UserId
                 })
                : 
                (from reviewGood in _context.ReviewGoods
                       where (_representationReviewFirst.Review.Id == reviewGood.ReviewId)
                       select new
                       {
                           reviewGood.Id,
                           reviewGood.State,
                           reviewGood.UserId
                       });

            var _good = _goodLinq.ToList();

            var _imageLinq = _representationReviewFirst == null ? new List<String>() : (from reviewImage in _context.ReviewImages
                             where _representationReviewFirst.Review.Id == reviewImage.ReviewId
                             select reviewImage.Uri).ToList();

            var _visitCount = _representationReviewFirst == null ? 0 : (from review in _context.Reviews
                             where review.State == 0
                             where review.UserId == _representationReviewFirst.User.Id
                             where review.GolfClubId == id
                             select new { review.Id }).Count();

            var _representationReview = new
                                         {
                                             //GolfClubId = goods.Key.Review.GolfClubId,
                                             Review = _representationReviewFirst == null ? null : _representationReviewFirst.Review,
                                             Images = _imageLinq,
                                             VisitCount = _visitCount,
                                             User = _representationReviewFirst == null ? null : _representationReviewFirst.User,
                                             Setting = _representationReviewFirst == null ? null : _representationReviewFirst.Setting,
                                             GoodCount = _good.Where(g => g.State == 1).Count(),
                                             DislikeCount = _good.Where(g => g.State == 2).Count(),
                                             MyGoodState = _good.Where(g => g.UserId == tokenUserId).Select(g => new { g.Id, g.State }).FirstOrDefault(),
                                         };

            var _clubHouseMenuLinq = from clubHouse in _context.ClubHouses
                                     where clubHouse.GolfClubId == id
                                     join clubHouseMenu in _context.ClubHouseMenus on clubHouse.Id equals clubHouseMenu.ClubHouseId into clubHouseMenuGroup
                                     from clubHouseMenu in clubHouseMenuGroup.DefaultIfEmpty()
                                     select new
                                     {
                                         MFileUri = clubHouse.MFileUri,
                                         ClubHouseMenu = clubHouseMenu
                                         //new
                                         //{
                                         //    clubHouseMenu.MenuName,
                                         //    clubHouseMenu.Price,
                                         //    clubHouseMenu.Representative,
                                         //    clubHouseMenu.Type,
                                         //    clubHouseMenu.Description,
                                         //    clubHouseMenu.MFileUri
                                         //}
                                     };

            var _clubHouseMenu = (from tuple in _clubHouseMenuLinq.ToList()
                                             group tuple.ClubHouseMenu
                                             by tuple.MFileUri into newGroup
                                             select new { MFileUri = newGroup.Key, ClubHouseMenus = newGroup })
                                             .FirstOrDefault();

            var __clubHouseMenu =
                new
                {
                    MFileUri = _clubHouseMenu.MFileUri,
                    ClubHouseMenus = _clubHouseMenu.ClubHouseMenus.First() == null ? null :_clubHouseMenu.ClubHouseMenus.Select(clubHouseMenu => new
                    {
                        clubHouseMenu.MenuName,
                        clubHouseMenu.Price,
                        clubHouseMenu.Representative,
                        clubHouseMenu.Type,
                        clubHouseMenu.Description,
                        clubHouseMenu.MFileUri
                    }).ToList()
                };
                


            var _shadeHouseMenuLinq = from shadeHouseMenu in _context.ShadeHouseMenus
                                      where shadeHouseMenu.GolfClubId == id
                                      select new
                                      {
                                          shadeHouseMenu.MenuName,
                                          shadeHouseMenu.Price,
                                          shadeHouseMenu.Representative,
                                          shadeHouseMenu.Type,
                                          shadeHouseMenu.MFileUri,
                                          shadeHouseMenu.Description
                                      };

            var _shadeHouseMenu = _shadeHouseMenuLinq.ToList();

            var _filterListLinq = from filterList in _context.FilterLists
                                  where filterList.GolfClubId == id
                                  join filter in _context.FilterObjects on filterList.FilterObjectId equals filter.Id
                                  select filter.Name;

            var _filterList = _filterListLinq.ToList();



            //bool isSub = _golfClubSub.Count() > 0 ? true : false;
            var data = new
                       {
                           GolfClubId = id,
                           GolfClub = _golfClubs.GolfClub,
                           Images = _golfClubs.Images,
                           Favorite = _golfClubs.Favorite,
                           Position = _golfClubs.Position,
                           MyFieldMinScore = _reviews == null ? 0 : _reviews.MyFieldMinScore,
                           VisitCount =  _reviews == null ? 0 : _reviews.VisitCount,
                           FieldAvgScore = _reviews == null ? 0 : _reviews.FieldAvgScore,
                           FieldReviewCount = _reviews == null ? 0 : _reviews.FieldReviewCount,
                           FieldAvgS = _reviews == null ? 0 : _reviews.FieldAvgS,
                           CourseCount = _golfClubSub == null ? 0 :  _golfClubSub.CourseCount,
                           HoleCount = _golfClubSub == null ? 0 : _golfClubSub.HoleCount,
                           Par = _golfClubSub == null ? 0 : _golfClubSub.Par,
                           Length = _golfClubSub == null ? 0 : _golfClubSub.Length,
                           CourseNames = _golfClubSub == null ? new List<string>() : _golfClubSub.CourseName,
                           Weather = _golfClubWeather,
                           Representation = _representationReviewFirst == null ? null : _representationReview,
                           ClubHouse = __clubHouseMenu,
                           ShadeHouseMenus = _shadeHouseMenu,
                           FilterList = _filterList
            };

            return Json(data);
        }

        [HttpGet("api/[controller]/detail/{id}")]
        public async Task<ActionResult<List<GolfClub>>> GetItem(int id)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            DateTime dateNow = DateTime.Now.Date;
            DateTime dateAfterAWeek = dateNow.AddDays(7);

            var golfClub = await _context.GolfClubs
               //.Include(gc => gc.GeoList)
               .Where(gc => gc.Status == 1)
               .Include(gc => gc.GolfClubImages.OrderBy(gc => gc.Id))
               .Include(gc => gc.Courses)
               .ThenInclude(gc => gc.Holes)
               //.ThenInclude(ho => ho.GeoList)
               .Include(gc => gc.ClubHouse)
               .ThenInclude(gc => gc.ClubHouseMenus)
               //.Include(gc => gc.ClubHouse.ClubHouseMenus)
               .Include(gc => gc.ShadeHouses)
               //.Include(gc => gc.ShadeHouseMenus)
               .Include(gc => gc.Weathers)
               .Include(gc => gc.Reviews)
               .ThenInclude(re => re.User)
               .Include(gc => gc.Reviews)
               .ThenInclude(re => re.ReviewImages)
               .Include(gc => gc.Reviews)
               .ThenInclude(re => re.User)
               .ThenInclude(u => u.Setting)
               .Include(gc => gc.Reviews)
               .ThenInclude(re => re.ReviewGoods)
               //.Include(gc => gc.Reviews)
               //.ThenInclude(dd => dd.Review.User)

               .Include(gc => gc.FilterLists)
               .ThenInclude(f => f.FilterObject)
               .Select(golfClub => new
               {
                   golfClub.Id,
                   golfClub.GcNum,
                   golfClub.IsField,
                   golfClub.IsScreen,
                   golfClub.Name,
                   golfClub.Since,
                   golfClub.Address,
                   golfClub.SignatureHole,
                   golfClub.FairwayGrass,
                   golfClub.GreenGrass,
                   golfClub.Contact,
                   golfClub.CartFee,
                   golfClub.CaddieFee,
                   golfClub.SelfRound,
                   golfClub.Note,
                   //golfClub.TFileName,
                   //golfClub.TFileOriginalName,
                   golfClub.TFileUri,
                   golfClub.MFileUri,
                   golfClub.GolfClubImages,
                   golfClub.FilterLists,
                   golfClub.DifficultyLevel,
                   golfClub.GreenDifficultyLevel,
                   golfClub.Grade,
                   favorite = _context.GolfFavorites.Where(gf => gf.GolfClubId == golfClub.Id && gf.UserId == tokenUserId).Select(f => new { f.Id }).FirstOrDefault(),
                   FieldRoundCount = _context.Reviews.Count(r => r.GolfClubId == golfClub.Id),
                   BestScore = _context.Reviews.Where(r => r.GolfClubId == golfClub.Id && r.UserId == tokenUserId).Count() == 0 ? 0 : _context.Reviews.Where(r => r.GolfClubId == golfClub.Id && r.UserId == tokenUserId).Select(r => r.Score).Min(),
                   AverageScore = _context.Reviews.Where(r => r.GolfClubId == golfClub.Id).Count() == 0 ? 0 : _context.Reviews.Where(r => r.GolfClubId == golfClub.Id).Average(r => r.Score),
                   Position = golfClub.GeoList.Position.Centroid,
                   ClubHouse = golfClub.ClubHouse,
                   ShadeHouses = golfClub.ShadeHouses,
                   ShadeHouseMenus = golfClub.ShadeHouseMenus,
                   Courses = golfClub.Courses,
                   ReviewsCount = golfClub.Reviews.Count,
                   Weathers = golfClub.Weathers,
                   Review = golfClub.Reviews.Where(re => re.Representation).FirstOrDefault(),
                   ReviewUserCount = golfClub.Reviews.Where(
                       re => re.UserId == golfClub.Reviews.Where(re => re.Representation).FirstOrDefault().UserId).Count(),
                   ReviewGoodCount = golfClub.Reviews.Where(re => re.Representation).FirstOrDefault()
                  .ReviewGoods.Count(rg => rg.State == 0),
                   ReviewNotGoodCount = golfClub.Reviews.Where(re => re.Representation).FirstOrDefault()
                  .ReviewGoods.Count(rg => rg.State == 1),
                   //CoursesCount = golfClub.Courses.Count,x
                   //CourseLen = golfClub.Courses.Select(co => co.Length).Count() == 0 ? 0 : golfClub.Courses.Select(co => co.Length).Max(),
                   //HolesCount = golfClub.Courses.Select(co => co.Holes).Count(),
                   //ReviewSCourseAverage = 
                   //(golfClub.Reviews.Average(re => re.SCourse) 
                   //+ golfClub.Reviews.Average(re => re.SFacility) 
                   //+ golfClub.Reviews.Average(re => re.SCaddy)
                   //+ golfClub.Reviews.Average(re => re.SFoodAndDrink)) / 4,

                   ReviewSCourseAverage = golfClub.Reviews.Count() != 0 ? golfClub.Reviews.Average(re => re.SCourse) : 0,
                   ReviewDCourseCount = golfClub.Reviews.Count(re => re.DCourse != null && re.DCourse != ""),
                   ReviewSFacilityAverage = golfClub.Reviews.Count() != 0 ? golfClub.Reviews.Average(re => re.SFacility) : 0,
                   ReviewDFacilityCount = golfClub.Reviews.Count(re => re.DFacility != null && re.DFacility != ""),
                   ReviewSCaddyAverage = golfClub.Reviews.Count() != 0 ? golfClub.Reviews.Average(re => re.SCaddy) : 0,
                   ReviewDCaddyCount = golfClub.Reviews.Count(re => re.DCaddy != null && re.DCaddy != ""),
                   ReviewSFoodAndDrinkAverage = golfClub.Reviews.Count() != 0 ? golfClub.Reviews.Average(re => re.SFoodAndDrink) : 0,
                   ReviewDFoodAndDrinkCount = golfClub.Reviews.Count(re => re.DFoodAndDrink != null && re.DFoodAndDrink != ""),
               })
               .AsNoTracking()
               .AsSingleQuery()

               //.AsSplitQuery()
               .SingleOrDefaultAsync(gc => gc.Id == id);

            return Json(golfClub);
        }


        [HttpGet("api/[controller]/detail/yadage")]
        public async Task<ActionResult<List<WeatherArea>>> GetYadage(int hNo, long ci, int gNo, int cNo)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            int tokenUserNo = JwtManager.getJwtNo(_httpContext);
            if (tokenUserId == 0 && tokenUserNo == 0) return Unauthorized();
            //if (tokenUserId == 0 && tokenUserNo == 0) return BadRequest();

            return Json(GzApi.GetYadage(hNo, tokenUserNo, ci, gNo, cNo));
        }

        [HttpGet("api/[controller]/golfclub/type/count")]
        public async Task<ActionResult<List<GolfClub>>> GetTypeCount()
        {
            var dd = new
            {
                field = await _context.GolfClubs.CountAsync(c => c.IsField && c.GeoList != null),
                screen = await _context.GolfClubs.CountAsync(c => c.IsScreen && c.GeoList != null)
            };


            return Json(dd);
        }

        [HttpGet("api/[controller]/golfclub/type/countLinq")]
        public async Task<IActionResult> GetTypeCountLinq()
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            var _fieldVisitCountLinq = from review in _context.Reviews
                                  where review.UserId == tokenUserId
                                  where review.GolfClubId != null
                                  select review.GolfClubId;

            var _fieldVisitCount = from golfClubId in _fieldVisitCountLinq.ToList()
                                   group golfClubId by golfClubId into golfClubIdGroup
                                   select golfClubIdGroup;

            return Json(new
            {
                field = _fieldVisitCount.Count(),
                screen = 0
            });
        }

        [HttpGet("api/[controller]/golfclub/type/countLinq_sp")]
        public async Task<IActionResult> GetTypeCountLinqSP()
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            return Json(GzApi.GetSingleParamSP(_context, "Mappage_Field_Screen_Visit", "@P_USER_ID", tokenUserId));
        }



        [HttpGet("api/[controller]/filter/count")]
        public async Task<ActionResult<List<GolfClub>>> GetCount()
        {
            var golfClubs = await _context.GolfClubs
                .GroupBy(gc => gc.Address.Substring(0,  2))
                .Select(gg => new
                {
                    gg.Key,
                    Count = gg.Count(),
                })
                .ToListAsync();

            return Json(golfClubs);
        }



        [HttpPost("api/[controller]/filter/review")]
        public async Task<ActionResult<List<GolfClub>>> GetReview([FromForm] FilterAllDto param)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var golfClubs = await _context.GolfClubs
                //.Where(gc => param.GetPosition().Contains(gc.Position))
                .Include(gc => gc.Courses)
                .ThenInclude(gcc => gcc.Holes)
                .Include(gc => gc.Reviews)
                .ThenInclude(r => r.User)
                .Select(golfClub => new
                {
                    golfClub.Id,
                    golfClub.Name,
                    golfClub.Address,
                    golfClub.TFileName,
                    golfClub.TFileOriginalName,
                    golfClub.TFileUri,
                    ReviewsCount = golfClub.Reviews.Count(r => r.User.Blinded != null),
                    CoursesCount = golfClub.Courses.Count,
                    CourseLen = golfClub.Courses.Select(co => co.Length).Count() == 0 ? 0 : golfClub.Courses.Select(co => co.Length).Max(),
                    HolesCount = golfClub.Courses.Sum(co => co.Holes.Count()),
                    ReviewScoreAverage = golfClub.Reviews.Count() == 0 ? 0 : golfClub.Reviews.Where(r => r.User.Blinded != null).Average(re => (re.SCourse + re.SFacility + re.SFoodAndDrink + re.SCaddy) / 4),
                    //Position = golfClub.Position.Centroid
                }).OrderByDescending(g => g.ReviewsCount)
                .ToListAsync();

            return Json(golfClubs);
        }

        [HttpGet("api/[controller]/sort/{type}")]
        public async Task<ActionResult<List<GolfClub>>> GetSortedGolfClubList(string type)
        {
            /**
             * type == 1 필드 성적 좋은
             * * type == 2 스크린 성적 좋은
             * * type == 3 필드 많이친
             * * type == 4 스크린 많이친
             * * type == 5 긴코스
             * * type == 6 짧은코스
             * * type == 7 평점 좋은
             * * type == 8 리뷰 많은
             */
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            DateTime now = DateTime.UtcNow.AddHours(9);

            var _golfClubLinq = _context.GolfClubs
                                    .GroupJoin(_context.Reviews, g => g.Id, r => r.GolfClubId, (g, r) => new
                                    {
                                        GolfClub = g,
                                        Review = r
                                    })
                                    .SelectMany(
                                        tuple => tuple.Review.DefaultIfEmpty(),
                                        (golfclub, review) => new
                                        {
                                            golfclub = golfclub.GolfClub,
                                            visit = review.UserId == tokenUserId ? 1 : 0,
                                            RC = review == null ? 0 : 1,
                                            AS = review == null ? 0 : (review.SCourse + review.SFacility + review.SFoodAndDrink + review.SCaddy) / 4,
                                            Score = review == null ? 0 : review.Score,
                                        }
                                    )
                                    .Join(_context.Courses, tuple => tuple.golfclub.Id, course => course.GolfClubId, (tuple, course) => new
                                    {
                                        golfclub = tuple.golfclub,
                                        tuple.AS,
                                        tuple.RC,
                                        tuple.visit,
                                        tuple.Score,
                                        course,
                                    })
                                    .Select(
                                        (tuple) => new
                                        {
                                            tuple.golfclub,
                                            tuple.AS,
                                            tuple.RC,
                                            tuple.visit,
                                            tuple.Score,
                                            courseId = tuple.course.Id,
                                            length = tuple.course.Length
                                        }
                                    )
                                    .Join(_context.Holes, tuple => tuple.courseId, hole => hole.CourseId, (tuple, hole) => new
                                    {
                                        tuple.golfclub,
                                        tuple.AS,
                                        tuple.RC,
                                        tuple.visit,
                                        tuple.Score,
                                        tuple.courseId,
                                        tuple.length,
                                        holeId = hole.Id,
                                        par = hole.Par,
                                    })
                                    .Select(
                                        (tuple) => new
                                        {
                                            tuple.golfclub,
                                            tuple.AS,
                                            tuple.RC,
                                            tuple.visit,
                                            tuple.Score,
                                            tuple.courseId,
                                            tuple.length,
                                            tuple.holeId,
                                            par = tuple.par == "-" ? 0 : Convert.ToInt32(tuple.par)
                                        }
                                    )
                                    .GroupBy(tuple => new
                                    {
                                        tuple.golfclub.Id,
                                        tuple.golfclub.Address,
                                        tuple.golfclub.Name,
                                        tuple.golfclub.IsField,
                                        tuple.golfclub.IsScreen,
                                        tuple.golfclub.TFileUri,
                                        tuple.golfclub.SignatureHole,
                                        tuple.courseId,
                                        tuple.length,
                                        tuple.par,
                                        tuple.holeId
                                    }).Select(x => new
                                    {
                                        x.Key.Id,
                                        x.Key.Address,
                                        x.Key.Name,
                                        x.Key.IsField,
                                        x.Key.IsScreen,
                                        x.Key.TFileUri,
                                        x.Key.SignatureHole,
                                        x.Key.courseId,
                                        x.Key.length,
                                        x.Key.par,
                                        FieldAvgS = x.Average(rg => rg.AS),
                                        VisitCount = x.Sum(_x => _x.visit),
                                        ReviewCount = x.Sum(_x => _x.RC),
                                        MinScore = x.Min(_x => _x.Score)
                                    })
                                    .GroupBy(tuple => new
                                    {
                                        tuple.Id,
                                        tuple.Address,
                                        tuple.Name,
                                        tuple.IsField,
                                        tuple.MinScore,
                                        tuple.IsScreen,
                                        tuple.TFileUri,
                                        tuple.SignatureHole,
                                        tuple.courseId,
                                        tuple.length,
                                        tuple.FieldAvgS,
                                        tuple.VisitCount,
                                        tuple.ReviewCount,
                                    })
                                    .Select(x => new
                                    {
                                        x.Key.Id,
                                        x.Key.Address,
                                        x.Key.Name,
                                        x.Key.IsField,
                                        x.Key.IsScreen,
                                        x.Key.TFileUri,
                                        x.Key.SignatureHole,
                                        x.Key.courseId,
                                        x.Key.length,
                                        x.Key.FieldAvgS,
                                        x.Key.VisitCount,
                                        x.Key.ReviewCount,
                                        x.Key.MinScore,
                                        par = x.Sum(_x => _x.par),
                                        holeCount = x.Count(),
                                    })
                                    .GroupBy(tuple => new
                                    {
                                        tuple.Id,
                                        tuple.Address,
                                        tuple.Name,
                                        tuple.IsField,
                                        tuple.IsScreen,
                                        tuple.TFileUri,
                                        tuple.SignatureHole,
                                        tuple.FieldAvgS,
                                        tuple.VisitCount,
                                        tuple.ReviewCount,
                                        tuple.MinScore
                                    })
                                    .Select(x => new
                                    {
                                        x.Key.Id,
                                        x.Key.Address,
                                        x.Key.Name,
                                        x.Key.IsField,
                                        x.Key.IsScreen,
                                        x.Key.TFileUri,
                                        x.Key.SignatureHole,
                                        x.Key.FieldAvgS,
                                        x.Key.VisitCount,
                                        x.Key.ReviewCount,
                                        x.Key.MinScore,
                                        Par = x.Sum(_x => _x.par),
                                        Length = x.Sum(_x => _x.length),
                                        HoleCount = x.Sum(_x => _x.holeCount),
                                        CourseCount = x.Count()
                                    })
                                    .Join(_context.ClubHouses, tuple => tuple.Id, ch => ch.GolfClubId, (tuple, ch) => new 
                                    {
                                        tuple.Id,
                                        tuple.Address,
                                        tuple.Name,
                                        tuple.IsField,
                                        tuple.IsScreen,
                                        tuple.TFileUri,
                                        tuple.SignatureHole,
                                        tuple.FieldAvgS,
                                        tuple.VisitCount,
                                        tuple.ReviewCount,
                                        tuple.MinScore,
                                        tuple.Par,
                                        tuple.Length,
                                        tuple.HoleCount,
                                        tuple.CourseCount,
                                        ch.GeoListId
                                    })
                                    .Select(x => new
                                    {
                                        x.Id,
                                        x.Address,
                                        x.Name,
                                        x.IsField,
                                        x.IsScreen,
                                        x.TFileUri,
                                        x.SignatureHole,
                                        x.FieldAvgS,
                                        x.VisitCount,
                                        x.ReviewCount,
                                        x.MinScore,
                                        x.Par,
                                        x.Length,
                                        x.HoleCount,
                                        x.CourseCount,
                                        Position = _context.GeoLists.SingleOrDefault(geo => geo.Id == x.GeoListId) != null ? _context.GeoLists.SingleOrDefault(geo => geo.Id == x.GeoListId).Position : null
                                        //Position = x.Geo == null ? null : x.Geo.Position
                                    });
            //.Join(_context.GeoLists, tuple => tuple.GeoListId, geo => geo.Id, (tuple, geo) => new
            //{
            //    tuple.Id,
            //    tuple.Address,
            //    tuple.Name,
            //    tuple.IsField,
            //    tuple.IsScreen,
            //    tuple.TFileUri,
            //    tuple.SignatureHole,
            //    tuple.FieldAvgS,
            //    tuple.VisitCount,
            //    tuple.ReviewCount,
            //    tuple.MinScore,
            //    tuple.Par,
            //    tuple.Length,
            //    tuple.HoleCount,
            //    tuple.CourseCount,
            //    Geo = geo
            //}
            //)
            //.Select(x => new
            //{
            //    x.Id,
            //    x.Address,
            //    x.Name,
            //    x.IsField,
            //    x.IsScreen,
            //    x.TFileUri,
            //    x.SignatureHole,
            //    x.FieldAvgS,
            //    x.VisitCount,
            //    x.ReviewCount,
            //    x.MinScore,
            //    x.Par,
            //    x.Length,
            //    x.HoleCount,
            //    x.CourseCount,
            //    Position = x.Geo == null ? null : x.Geo.Position
            //});

            switch (type)
            {
                case "1":
                    _golfClubLinq = _golfClubLinq.Where(g => g.IsField && g.ReviewCount > 0).OrderBy(g => g.MinScore);
                    break;
                case "2":
                    _golfClubLinq = _golfClubLinq.Where(g => g.IsScreen);
                    break;
                case "3":
                    _golfClubLinq = _golfClubLinq.Where(g => g.IsField).OrderByDescending(g => g.ReviewCount);
                    break;
                case "4":
                    _golfClubLinq = _golfClubLinq.Where(g => g.IsScreen);
                    break;
                case "5":
                    _golfClubLinq = _golfClubLinq.Where(g => g.IsField).OrderByDescending(g => g.Length);
                    break;
                case "6":
                    _golfClubLinq = _golfClubLinq.Where(g => g.IsField).OrderBy(g => g.Length);
                    break;
                case "7":
                    _golfClubLinq = _golfClubLinq.OrderByDescending(g => g.FieldAvgS);
                    break;
                case "8":
                    _golfClubLinq = _golfClubLinq.OrderByDescending(g => g.ReviewCount);
                    break;
            }

            //var _golfClubLinq = from golfClub in _context.GolfClubs
            //                    join review in _context.Reviews on golfClub.Id equals review.GolfClubId// into reviewGroup
            //                    //from review in reviewGroup.DefaultIfEmpty()
            //                    group review by golfClub into reviewGroup
            //                    let myReview = reviewGroup.Where(rg => rg.UserId == tokenUserId)
            //                    select new
            //                    {
            //                        GolfClub = reviewGroup.Key,
            //                        FieldAvgS = reviewGroup.Average(rg => (rg.SCourse + rg.SFacility + rg.SFoodAndDrink + rg.SCaddy) / 4),
            //                        VisitCount = myReview.Count(),
            //                        ReviewCount = reviewGroup.Count()
            //                    };

            return Json(_golfClubLinq.Take(20).ToList());
            
            


            //var _golfClubSubLinq = from course in _context.Courses
            //                    where course.GolfClubId == id
            //                    join hole in _context.Holes on course.Id equals hole.CourseId
            //                    select new
            //                    {
            //                        Par = hole.Par,
            //                        Length = course.Length,
            //                        CourseName = course.CourseName,
            //                        CourseId = course.Id,
            //                        GolfClubId = course.GolfClubId
            //                    };

            //var _golfClubSub = (from golfClubSub in _golfClubSubLinq.ToList()
            //                    group golfClubSub.Par by new { golfClubSub.CourseId, golfClubSub.Length, golfClubSub.CourseName, golfClubSub.GolfClubId } into holeGroup
            //                    group new
            //                    {
            //                        Length = holeGroup.Key.Length,
            //                        CourseName = holeGroup.Key.CourseName,
            //                        Par = holeGroup.Sum(p => int.TryParse(p, out int n) ? n : 0),
            //                        HoleCount = holeGroup.Count()
            //                    } by holeGroup.Key.GolfClubId into courseGroup
            //                    select new
            //                    {
            //                        Length = courseGroup.Sum(c => c.Length),
            //                        Par = courseGroup.Sum(c => c.Par),
            //                        CourseName = courseGroup.Select(c => c.CourseName),
            //                        HoleCount = courseGroup.Sum(c => c.HoleCount),
            //                        CourseCount = courseGroup.Count()
            //                    }).FirstOrDefault();

            //var query = _context.GolfClubs
            //    .Where(gc => gc.GeoList != null)
            //    .Include(gc => gc.GeoList)
            //    .Include(gc => gc.Weathers)
            //    .Include(gc => gc.Courses)
            //        .ThenInclude(gcc => gcc.Holes)
            //    .Include(gc => gc.Reviews)
            //    .AsSplitQuery()
            //    .AsNoTracking();

            //switch (type)
            //{
            //    case "1":
            //        query = query.Where(g => g.IsField && g.Reviews.Count > 0).OrderBy(g => g.Reviews.Min(r => r.Score));
            //        break;
            //    case "2":
            //        query = query.Where(g => g.IsScreen);
            //        break;
            //    case "3":
            //        query = query.Where(g => g.IsField).OrderByDescending(g => g.Reviews.Count);
            //        break;
            //    case "4":
            //        query = query.Where(g => g.IsScreen);
            //        break;
            //    case "5":
            //        query = query.Where(g => g.IsField).OrderByDescending(g => g.Courses.Sum(co => co.Length));
            //        break;
            //    case "6":
            //        query = query.Where(g => g.IsField).OrderBy(g => g.Courses.Sum(co => co.Length));
            //        break;
            //    case "7":
            //        query = query.OrderByDescending(g => g.Reviews.Count() == 0 ? 0 : g.Reviews.Average(re => (re.SCourse + re.SFacility + re.SFoodAndDrink + re.SCaddy) / 4));
            //        break;
            //}

            //var golfClubs = await query.Select(golfClub => new
            //{
            //    golfClub.Id,
            //    golfClub.Name,
            //    golfClub.Address,
            //    golfClub.TFileUri,
            //    golfClub.SignatureHole,
            //    golfClub.IsField,
            //    golfClub.IsScreen,
            //    golfClub.Grade,
            //    Length = golfClub.Courses.Sum(co => co.Length),
            //    Par = golfClub.Courses.Join(_context.Holes, c => c.Id, h => h.CourseId, (c, h) => new
            //    {
            //        Par = h.Par
            //    }).Sum(p => String.IsNullOrEmpty(p.Par) || p.Par == "-" ? 0 : Convert.ToInt32(p.Par)),
            //    FieldRoundCount = _context.Reviews.Count(r => r.GolfClubId == golfClub.Id),
            //    FieldMinScore = _context.Reviews.Where(r => r.GolfClubId == golfClub.Id).Count() == 0 ? 0 : _context.Reviews.Where(r => r.GolfClubId == golfClub.Id).Select(r => r.Score).Min(),
            //    ReviewsCount = golfClub.Reviews.Count,
            //    CoursesCount = golfClub.Courses.Count,
            //    CourseLen = golfClub.Courses.Select(co => co.Length).Count() == 0 ? 0 : golfClub.Courses.Select(co => co.Length).Max(),
            //    HolesCount = golfClub.Courses.Join(_context.Holes, c => c.Id, h => h.CourseId, (c, h) => new
            //    {
            //        Id = h.Id
            //    }).Count(),
            //    ReviewScoreAverage = golfClub.Reviews.Count() == 0 ? 0 : golfClub.Reviews.Average(re => (re.SCourse + re.SFacility + re.SFoodAndDrink + re.SCaddy) / 4),
            //    Position = golfClub.GeoList.Position.Centroid
            //})
            //            .Take(20)
            //            .ToListAsync();

            //return Json(golfClubs);
        }

        [HttpGet("api/[controller]/sort_sp/{type}")]
        public async Task<ActionResult<List<GolfClub>>> GetSortedGolfClubListSp(string type, string myX = "0", string myY = "0")
        {
            /**
             * type == 1 필드 성적 좋은
             * * type == 2 스크린 성적 좋은
             * * type == 3 필드 많이친
             * * type == 4 스크린 많이친
             * * type == 5 긴코스
             * * type == 6 짧은코스
             * * type == 7 평점 좋은
             * * type == 8 리뷰 많은
             */
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();


            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Mappage_GolfClub_Sort";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@userId",
                    SqlDbType.Int)
                { Value = tokenUserId });
                cmd.Parameters.Add(new SqlParameter("@sortNum",
                    SqlDbType.Int)
                { Value = type });
                cmd.Parameters.Add(new SqlParameter("@myX",
                    SqlDbType.VarChar)
                { Value = myX });
                cmd.Parameters.Add(new SqlParameter("@myY",
                    SqlDbType.VarChar)
                { Value = myY });

                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();

                var retObject = new List<Dictionary<string, object>>();
                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var dataRow = new Dictionary<string, object>();

                        for (var iFiled = 0; iFiled < dataReader.FieldCount; iFiled++)
                        {
                            if (dataReader.GetName(iFiled) == "position")
                            {
                                var geoStr = dataReader[iFiled].ToString();
                                SqlGeography s = SqlGeography.STGeomFromText(new System.Data.SqlTypes.SqlChars(geoStr), 4326);

                                if (geoStr.Contains("POINT"))
                                {
                                    dataRow.Add(
                                        dataReader.GetName(iFiled),
                                        new Point((double)s.Long, (double)s.Lat)
                                    );
                                }
                                else
                                {
                                    dataRow.Add(
                                        dataReader.GetName(iFiled),
                                        new Point((double)s.EnvelopeCenter().Long, (double)s.EnvelopeCenter().Lat)
                                    );
                                }
                            }
                            else
                            {
                                dataRow.Add(
                                    dataReader.GetName(iFiled),
                                    dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
                                );
                            }
                        }

                        retObject.Add(dataRow);
                    }
                }

                return Json(retObject.Select(e => new
                {
                    id = e["id"],
                    filterName = e["filterName"],
                    holeCount = e["holeCount"],
                    courseCount = e["courseCount"],
                    golfClub = new
                    {
                        id = e["id"],
                        address = e["golfClub.address"],
                        grade = e["golfClub.grade"],
                        isField = e["golfClub.isField"],
                        isScreen = e["golfClub.isScreen"],
                        name = e["golfClub.name"],
                        tFileUri = e["golfClub.tFileUri"],
                        signatureHole = e["golfClub.signatureHole"],
                        fairwayGrass = e["golfClub.fairwayGrass"],
                        gcNum = e["golfClub.gcNum"],
                    },
                    position = e["position"],
                    visitCount = e["visitCount"],
                    minScore = e["minScore"],
                    avgScore = e["avgScore"],
                    screenVisitCount = e["screenVisitCount"],
                    screenMinScore = e["screenMinScore"],
                    screenAvgScore = e["screenAvgScore"],
                    reviewAvgScore = e["avg"],
                    reviewCount = e["reviewCount"],
                    length = e["length"],
                    lengthAvg = e["lengthAvg"],
                    golfClubAvgScore = e["golfClubAvgScore"],
                    golfClubScreenVisitCount = e["golfClubScreenVisitCount"],
                    golfClubScreenAvgScore = e["golfClubScreenAvgScore"],
                    distance = e["distance"],
                }));
            }

        }

        [HttpGet("api/[controller]/search/{search}/{date}")]
        public async Task<ActionResult<List<GolfClub>>> GetGolfClubList(string search = "", DateTime date  = new DateTime())
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            string _search = search;
            string regex = RegexConvert.Regex(search);

            var _golfClubLinq = _context.GolfClubs
                                    .Where(gc => EF.Functions.Like(gc.Name, regex + "%"))
                                    .GroupJoin(_context.Reviews, g => g.Id, r => r.GolfClubId, (g, r) => new
                                    {
                                        GolfClub = g,
                                        Review = r
                                    })
                                    .SelectMany(
                                        tuple => tuple.Review.DefaultIfEmpty(),
                                        (golfclub, review) => new
                                        {
                                            golfclub = golfclub.GolfClub,
                                            visit = review.UserId == tokenUserId ? 1 : 0,
                                            RC = review == null ? 0 : 1,
                                            AS = review == null ? 0 : (review.SCourse + review.SFacility + review.SFoodAndDrink + review.SCaddy) / 4,
                                            Score = review == null ? 0 : review.Score,
                                        }
                                    )
                                    .Join(_context.Courses, tuple => tuple.golfclub.Id, course => course.GolfClubId, (tuple, course) => new
                                    {
                                        golfclub = tuple.golfclub,
                                        tuple.AS,
                                        tuple.RC,
                                        tuple.visit,
                                        tuple.Score,
                                        course,
                                    })
                                    .Select(
                                        (tuple) => new
                                        {
                                            tuple.golfclub,
                                            tuple.AS,
                                            tuple.RC,
                                            tuple.visit,
                                            tuple.Score,
                                            courseId = tuple.course.Id,
                                            length = tuple.course.Length
                                        }
                                    )
                                    .Join(_context.Holes, tuple => tuple.courseId, hole => hole.CourseId, (tuple, hole) => new
                                    {
                                        tuple.golfclub,
                                        tuple.AS,
                                        tuple.RC,
                                        tuple.visit,
                                        tuple.Score,
                                        tuple.courseId,
                                        tuple.length,
                                        holeId = hole.Id,
                                        par = hole.Par,
                                    })
                                    .Select(
                                        (tuple) => new
                                        {
                                            tuple.golfclub,
                                            tuple.AS,
                                            tuple.RC,
                                            tuple.visit,
                                            tuple.Score,
                                            tuple.courseId,
                                            tuple.length,
                                            tuple.holeId,
                                            par = tuple.par == "-" ? 0 : Convert.ToInt32(tuple.par)
                                        }
                                    )
                                    .GroupBy(tuple => new
                                    {
                                        tuple.golfclub.Id,
                                        tuple.golfclub.Address,
                                        tuple.golfclub.Name,
                                        tuple.golfclub.IsField,
                                        tuple.golfclub.IsScreen,
                                        tuple.golfclub.Grade,
                                        tuple.golfclub.TFileUri,
                                        tuple.golfclub.SignatureHole,
                                        tuple.courseId,
                                        tuple.length,
                                        tuple.par,
                                        tuple.holeId
                                    }).Select(x => new
                                    {
                                        x.Key.Id,
                                        x.Key.Address,
                                        x.Key.Name,
                                        x.Key.IsField,
                                        x.Key.IsScreen,
                                        x.Key.Grade,
                                        x.Key.TFileUri,
                                        x.Key.SignatureHole,
                                        x.Key.courseId,
                                        x.Key.length,
                                        x.Key.par,
                                        FieldAvgS = x.Average(rg => rg.AS),
                                        VisitCount = x.Sum(_x => _x.visit),
                                        ReviewCount = x.Sum(_x => _x.RC),
                                        MinScore = x.Min(_x => _x.Score)
                                    })
                                    .GroupBy(tuple => new
                                    {
                                        tuple.Id,
                                        tuple.Address,
                                        tuple.Name,
                                        tuple.IsField,
                                        tuple.MinScore,
                                        tuple.IsScreen,
                                        tuple.Grade,
                                        tuple.TFileUri,
                                        tuple.SignatureHole,
                                        tuple.courseId,
                                        tuple.length,
                                        tuple.FieldAvgS,
                                        tuple.VisitCount,
                                        tuple.ReviewCount
                                    })
                                    .Select(x => new
                                    {
                                        x.Key.Id,
                                        x.Key.Address,
                                        x.Key.Name,
                                        x.Key.IsField,
                                        x.Key.IsScreen,
                                        x.Key.TFileUri,
                                        x.Key.Grade,
                                        x.Key.SignatureHole,
                                        x.Key.courseId,
                                        x.Key.length,
                                        x.Key.FieldAvgS,
                                        x.Key.VisitCount,
                                        x.Key.ReviewCount,
                                        x.Key.MinScore,
                                        par = x.Sum(_x => _x.par),
                                        holeCount = x.Count(),
                                    })
                                    .GroupBy(tuple => new
                                    {
                                        tuple.Id,
                                        tuple.Address,
                                        tuple.Name,
                                        tuple.IsField,
                                        tuple.IsScreen,
                                        tuple.Grade,
                                        tuple.TFileUri,
                                        tuple.SignatureHole,
                                        tuple.FieldAvgS,
                                        tuple.VisitCount,
                                        tuple.ReviewCount,
                                        tuple.MinScore
                                    })
                                    .Select(x => new
                                    {
                                        x.Key.Id,
                                        x.Key.Address,
                                        x.Key.Name,
                                        x.Key.IsField,
                                        x.Key.IsScreen,
                                        x.Key.TFileUri,
                                        x.Key.Grade,
                                        x.Key.SignatureHole,
                                        x.Key.FieldAvgS,
                                        x.Key.VisitCount,
                                        x.Key.ReviewCount,
                                        x.Key.MinScore,
                                        Par = x.Sum(_x => _x.par),
                                        Length = x.Sum(_x => _x.length),
                                        holeCount = x.Sum(_x => _x.holeCount),
                                        courseCount = x.Count()
                                    })
                                    .Join(_context.Weathers, x => x.Id, w=> w.GolfClubId, (x, w) => new
                                    {
                                        x,
                                        w
                                    })
                                    .Where(w => w.w.Dt.Date == date.Date) 
                                    .Select(x => new
                                    {
                                        x.x.Id,
                                        x.x.Address,
                                        x.x.Name,
                                        x.x.IsField,
                                        x.x.IsScreen,
                                        x.x.Grade,
                                        x.x.TFileUri,
                                        x.x.SignatureHole,
                                        x.x.FieldAvgS,
                                        x.x.VisitCount,
                                        x.x.ReviewCount,
                                        x.x.MinScore,
                                        x.x.Par,
                                        x.x.Length,
                                        x.x.holeCount,
                                        x.x.courseCount,
                                        x.w,
                                    })
                                    .Join(_context.ClubHouses, g => g.Id, c => c.GolfClubId, (x, c) => new {
                                        x.Id,
                                        x.Address,
                                        x.Name,
                                        x.IsField,
                                        x.IsScreen,
                                        x.Grade,
                                        x.TFileUri,
                                        x.SignatureHole,
                                        x.FieldAvgS,
                                        x.VisitCount,
                                        x.ReviewCount,
                                        x.MinScore,
                                        x.Par,
                                        x.Length,
                                        x.holeCount,
                                        x.courseCount,
                                        x.w,
                                        c.GeoListId,
                                    })
                                    .Where(x => x.GeoListId != null)
                                    .Join(_context.GeoLists, x => x.GeoListId, geo => geo.Id, (x, geo) => new {
                                        x.Id,
                                        x.Address,
                                        x.Name,
                                        x.IsField,
                                        x.IsScreen,
                                        x.Grade,
                                        x.TFileUri,
                                        x.SignatureHole,
                                        x.FieldAvgS,
                                        x.VisitCount,
                                        x.ReviewCount,
                                        x.MinScore,
                                        x.Par,
                                        x.Length,
                                        x.holeCount,
                                        x.courseCount,
                                        x.w,
                                        geo.Position,
                                    })
                                    .OrderBy(x => x.Name).Take(5);

            var __golfClubs = _golfClubLinq.ToList();

            if (0 < __golfClubs.Count())
            {
                _context.GolfClubSearchHistories.Add(new GolfClubSearchHistory
                {
                    GolfClubId = __golfClubs[0].Id,
                    UserId = tokenUserId,
                    KeyWord = search,
                    Dt = date
                });

                await _context.SaveChangesAsync();
            }

            return Json(__golfClubs);

            //var golfClubs = await _context.GolfClubs
            //.Where(gc => EF.Functions.Like(gc.Name, "%" + regex + "%"))
            //.Include(gc => gc.GeoList)
            //.Include(gc => gc.Weathers)
            //.Include(gc => gc.Courses)
            //    .ThenInclude(gcc => gcc.Holes)
            //.Include(gc => gc.Reviews)
            //.Select(golfClub => new
            //{
            //    golfClub.Id,
            //    golfClub.Name,
            //    golfClub.Address,
            //    golfClub.TFileUri,
            //    golfClub.SignatureHole,
            //    golfClub.IsField,
            //    golfClub.IsScreen,
            //    golfClub.Grade,
            //    Length = golfClub.Courses.Sum(co => co.Length),
            //    Par = golfClub.Courses.Join(_context.Holes, c => c.Id, h => h.CourseId, (c, h) => new
            //    {
            //        Par = h.Par
            //    }).Sum(p => String.IsNullOrEmpty(p.Par) || p.Par == "-" ? 0 : Convert.ToInt32(p.Par)),
            //    Weather = golfClub.Weathers.Where(wt => wt.Dt.Year == date.Year && wt.Dt.Month == date.Month && wt.Dt.Day == date.Day).FirstOrDefault(),
            //    ReviewsCount = golfClub.Reviews.Count,
            //    CoursesCount = golfClub.Courses.Count,
            //    CourseLen = golfClub.Courses.Select(co => co.Length).Count() == 0 ? 0 : golfClub.Courses.Select(co => co.Length).Max(),
            //    HolesCount = golfClub.Courses.Join(_context.Holes, c => c.Id, h => h.CourseId, (c, h) => new
            //    {
            //        Id = h.Id
            //    }).Count(),
            //    ReviewScoreAverage = golfClub.Reviews.Count() == 0 ? 0 : golfClub.Reviews.Average(re => (re.SCourse + re.SFacility + re.SFoodAndDrink + re.SCaddy) / 4),
            //    Position = golfClub.GeoList.Position.Centroid
            //})
            //.AsSplitQuery()
            //.AsNoTracking()
            ////.OrderBy(gc => gc.Name)
            //.Take(5)
            //.ToListAsync();


            //if (0 < golfClubs.Count())
            //{
            //    _context.GolfClubSearchHistories.Add(new GolfClubSearchHistory {
            //        GolfClubId = golfClubs[0].Id,
            //        UserId = tokenUserId,
            //        KeyWord = search,
            //        Dt = date
            //    });

            //    await _context.SaveChangesAsync();
            //}

            //return Json(golfClubs);
        }

        [HttpGet("api/[controller]/search_sp/{search}/{date}")]
        public async Task<ActionResult<List<GolfClub>>> GetGolfClubListSp(string date = "", string search = "", string myX = "0", string myY = "0")
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Mappage_GolfClub_Search";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@userId",
                    SqlDbType.Int)
                { Value = tokenUserId });
                cmd.Parameters.Add(new SqlParameter("@date",
                    SqlDbType.VarChar)
                { Value = date });
                cmd.Parameters.Add(new SqlParameter("@search",
                    SqlDbType.VarChar)
                { Value = search });
                cmd.Parameters.Add(new SqlParameter("@myX",
                    SqlDbType.VarChar)
                { Value = myX });
                cmd.Parameters.Add(new SqlParameter("@myY",
                    SqlDbType.VarChar)
                { Value = myY});

                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();

                var retObject = new List<Dictionary<string, object>>();
                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var dataRow = new Dictionary<string, object>();

                        for (var iFiled = 0; iFiled < dataReader.FieldCount; iFiled++)
                        {
                            if (dataReader.GetName(iFiled) == "position")
                            {
                                var geoStr = dataReader[iFiled].ToString();
                                SqlGeography s = SqlGeography.STGeomFromText(new System.Data.SqlTypes.SqlChars(geoStr), 4326);

                                if (geoStr.Contains("POINT"))
                                {
                                    dataRow.Add(
                                        dataReader.GetName(iFiled),
                                        new Point((double)s.Long, (double)s.Lat)
                                    );
                                }
                                else
                                {
                                    dataRow.Add(
                                        dataReader.GetName(iFiled),
                                        new Point((double)s.EnvelopeCenter().Long, (double)s.EnvelopeCenter().Lat)
                                    );
                                }
                            }
                            else
                            {
                                dataRow.Add(
                                    dataReader.GetName(iFiled),
                                    dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
                                );
                            }
                        }

                        retObject.Add(dataRow);
                    }
                }

                if (retObject.Count > 0)
                {
                    int.TryParse(retObject[0]["id"].ToString(), out int gid);

                    if (gid != 0)
                    {
                        _context.GolfClubSearchHistories.Add(new GolfClubSearchHistory
                        {
                            GolfClubId = gid,
                            UserId = tokenUserId,
                            KeyWord = search,
                            Dt = DateTime.Now
                        });

                        await _context.SaveChangesAsync();
                    }

                   
                }

                return Json(retObject.Select(e => new
                {
                    id = e["id"],
                    filterName = e["filterName"],
                    holeCount = e["holeCount"],
                    courseCount = e["courseCount"],
                    filterObjectIdList = e["filterObjectIdList"].ToString() == "" ? new List<int>() : e["filterObjectIdList"].ToString().Split(",").Select(e => int.Parse(e)).ToList(),
                    golfClub = new
                    {
                        id = e["id"],
                        address = e["golfClub.address"],
                        grade = e["golfClub.grade"],
                        isField = e["golfClub.isField"],
                        isScreen = e["golfClub.isScreen"],
                        name = e["golfClub.name"],
                        tFileUri = e["golfClub.tFileUri"],
                        signatureHole = e["golfClub.signatureHole"],
                        fairwayGrass = e["golfClub.fairwayGrass"],
                        gcNum = e["golfClub.gcNum"],
                    },
                    position = e["position"],
                    visitCount = e["visitCount"],
                    screenVisitCount = e["screenVisitCount"],
                    weather = new
                    {
                        deg = e["weather.deg"],
                        main = e["weather.main"],
                        max = e["weather.max"],
                        min = e["weather.min"],
                        speed = e["weather.speed"],
                        now = e["weather.now"]
                    },
                    reviewAvgScore = e["avg"],
                    reviewCount = e["reviewCount"],
                    length = e["length"],
                    lengthAvg = e["lengthAvg"],
                    distance = e["distance"]
                }
                ));
            }
        }

        [HttpGet("api/[controller]/search/layer")]
        public async Task<ActionResult> SearchLayer(string search = "")
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            string _search = search;
            string regex = RegexConvert.Regex(search);

            if (_search.Trim() == "")
            {
                return BadRequest();
            }

            var golfClubs = _context.GolfClubs
            .Where(gc => EF.Functions.Like(gc.Name, regex + "%"))
            .Join(_context.ClubHouses, g => g.Id, c => c.GolfClubId, (g, c) => new
            {
                g,
                c
            })
            .Where((e) => e.c.GeoListId != null)
            .Select(e => new
            {
                e.g.Id,
                e.g.Name
            }).AsNoTracking().OrderBy(g => g.Name).Take(5).ToList();

            var conveniences = _context.Conveniences
                .Where(c => EF.Functions.Like(c.Name, regex + "%"))
                .Select(c => new
                {
                    c.Id,
                    c.Name
                }).AsNoTracking().OrderBy(c => c.Name).Take(5).ToList();

            var nicknames = _context.Users
                .Where(u => EF.Functions.Like(u.Nickname, regex + "%"))
                .Select(u => new
                {
                    u.Id,
                    u.Nickname
                }).AsNoTracking().OrderBy(u => u.Nickname).Take(5).ToList();

            var tags = _context.Tags
                .Where(t => t.IsActive).Where(t => EF.Functions.Like(t.Name, regex + "%"))
                .Select(t => new
                {
                    t.Id,
                    t.Name
                }).AsNoTracking().OrderBy(t => t.Name).Take(5).ToList();

            var data = new
            {
                GolfClubs = golfClubs,
                Conveniences = conveniences,
                Nicknames = nicknames,
                Tags = tags,
            };

            return Json(data);
        }

        [HttpGet("api/[controller]/search/golf_sp/{search}")]
        public async Task<ActionResult<List<GolfClub>>> SearchGolfClubListSp(string search = "")
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            string _search = search;
            string regex = RegexConvert.Regex(search);

            if (_search.Trim() == "")
            {
                return Json(new List<GolfClub>());
            }

            return Json(GzApi.GetSingleParamSP(_context, "Mappage_GolfClub_Keyword_Search", "@search", search));
        }

        [HttpGet("api/[controller]/search/golf/{search}")]
        public async Task<ActionResult<List<GolfClub>>> SearchGolfClubList(string search = "")
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            string _search = search;
            string regex = RegexConvert.Regex(search);

            if (_search.Trim() == "")
            {
                return Json(new List<GolfClub>());
            }

            var golfClubs = _context.GolfClubs
            .Where(gc => EF.Functions.Like(gc.Name, regex + "%"))
            .Join(_context.ClubHouses, g => g.Id, c => c.GolfClubId, (g, c) => new
            {
                g,
                c
            })
            .Where((e) => e.c.GeoListId != null)
            .Select(e => new
            {
                e.g.Id,
                e.g.Name
            }).AsNoTracking().OrderBy(g => g.Name).Take(5).ToList();

            return Json(golfClubs);
        }

        [HttpGet("api/[controller]/page/search")]
        public async Task<ActionResult<List<Template>>> ViewSearch()
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();
            //var dbTems = await _context.Templates
            //    .Where(te => te.IsActive)
            //    .Include(te => te.EventTemplates)
            //    .ThenInclude(et => et.EventTemplateGolfClubs)
            //    .ThenInclude(etg => etg.GolfClub)
            //    .Include(te => te.GolfClubTemplates)
            //    .ThenInclude(gc => gc.GolfClub)
            //    .Include(te => te.ConvenienceTemplates)
            //    .ThenInclude(co => co.Convenience)
            //    .OrderBy(te => te.Sequence)
            //    .AsSplitQuery()
            //    .AsNoTracking()
            //    .ToListAsync();

            var golfClubs = GzApi.GetSingleParamSP(_context, "Searchpage_Golfclub_Templates","@P_USER_ID", tokenUserId);
            var conveniences = GzApi.GetDefaultSP(_context, "Searchpage_Convenience_Templates");
            var events = GzApi.GetDefaultSP(_context, "Searchpage_Event_Templates");

            List<IDictionary<string, object>> result = new List<IDictionary<string, object>>();
            for (int i = 1;i <= 10; i++)
            {
                var dataRow = new ExpandoObject() as IDictionary<string, object>;

                var g = golfClubs.Where(g => g.templateSequence == i).ToList();
                if (g.Count == 0)
                {
                    dataRow.Add("golfClubTemplate", null);
                }
                else
                {
                    dataRow.Add("golfClubTemplate", g);
                }

                var c = conveniences.Where(c => c.templateSequence == i).ToList();
                if (c.Count == 0)
                {
                    dataRow.Add("convenienceTemplate", null);
                }
                else
                {
                    dataRow.Add("convenienceTemplate", c);
                }

                var e = events.Where(e => e.templateSequence == i).ToList();
                if (e.Count == 0)
                {
                    dataRow.Add("eventTemplate", null);
                }
                else
                {
                    dataRow.Add("eventTemplate", e);
                }

                result.Add(dataRow);
            }

            return Json(result);
        }

        [HttpGet("api/[controller]/template/event/all")]
        public async Task<ActionResult<List<EventTemplate>>> GetEventTemplateAll(int? id)
        {
            var now = DateTime.Now.Date;

            if (id == null)
            {
                var _eventTemplateLinq = from templates in _context.Templates
                                         where templates.IsActive == true
                                         orderby templates.Sequence
                                         join eventTemplates in _context.EventTemplates on templates.Id equals eventTemplates.TemplateId
                                         where eventTemplates.Status == 1 && now >= templates.StartDate && now <= templates.EndDate && !eventTemplates.IsTemp && templates.IsActive
                                         orderby templates.Sequence ascending
                                         select eventTemplates;

                var _eventTemplates = await _eventTemplateLinq.ToListAsync();

                return Json(_eventTemplates);
            }
            else
            {
                var _eventTemplateLinq = from templates in _context.Templates
                                         where templates.IsActive == true
                                         orderby templates.Sequence
                                         join eventTemplates in _context.EventTemplates on templates.Id equals eventTemplates.TemplateId
                                         join etf in _context.EventTemplatesGolfClubs on eventTemplates.Id equals etf.EventTemplateId
                                         where eventTemplates.Status == 1 && etf.GolfClubId == id && !eventTemplates.IsTemp && templates.IsActive
                                         select eventTemplates;

                var _eventTemplates = await _eventTemplateLinq.ToListAsync();

                return Json(_eventTemplates);
            }
        }

        [HttpGet("api/[controller]/template/event/item/{id}")]
        public async Task<ActionResult<EventTemplate>> GetEventTemplateItem(int id)
        {
            var _eventTemplateLinq = from eventTemplate in _context.EventTemplates
                                     where eventTemplate.Id == id
                                     where eventTemplate.Status == 1
                                     join template in _context.Templates on eventTemplate.TemplateId equals template.Id
                                     where template.IsActive == true
                                     select eventTemplate;

            var _eventTemplate = _eventTemplateLinq.FirstOrDefault();

            return Json(_eventTemplate);
        }

        [HttpGet("api/[controller]/get/favorite/golf/{id}")]
        public async Task<ActionResult<GolfFavorites>> GetGolfFavorite(int id)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            var favorite = _context.GolfFavorites.Where(g => g.GolfClubId == id && g.UserId == tokenUserId).FirstOrDefault();

            return Json(favorite);
        }

        [HttpGet("api/[controller]/search/history_sp")]
        public async Task<ActionResult<SearchHistoryResult>> GetSearchHistory_SP()
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            string error = "no result";

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Mypage_Search_Histories";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_USER_ID",
                    SqlDbType.Int)
                { Value = tokenUserId });

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

                return Json(retObject);
            }
        }

        [HttpGet("api/[controller]/search/history")]
        public async Task<ActionResult> GetSearchHistory()
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Mappage_Search_Histories";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_USER_ID",
                    SqlDbType.Int)
                { Value = tokenUserId });

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

                return Json(retObject);
            }
        }

        [HttpGet("api/[controller]/recommended_tags")]
        public async Task<ActionResult<List<Tag>>> GetRecommendedTags()
        {
            var tags = _context.Tags
                .Where(t => t.IsActive)
                .OrderBy(t => t.Sequence)
                .Select(t => new
                {
                    Id = t.Id,
                    Name = t.Name,
                    Uri = t.TFileUri,
                    ReviewCount = _context.Reviews.Include(r=> r.User).Where(r=> r.User.Blinded == null && r.State == 0 && r.OpenState == 1).Join(_context.ReviewTags, r => r.Id, rt => rt.ReviewId, (r, rt) => new
                    {
                        ReviewId = r.Id,
                        TagId = rt.TagId
                    }).Count(e => e.TagId == t.Id)
                })
                .ToList();
            return Json(tags);
        }

        [HttpGet("api/[controller]/find/tag/id")]
        public async Task<ActionResult<List<Tag>>> FindTagId(string name)
        {
            var tag = _context.Tags
                .Where(t => t.Name == name)
                .Select(t => new
                {
                    Id = t.Id,
                })
                .FirstOrDefault();
            return Json(tag);
        }

        [HttpGet("api/[controller]/search/tags")]
        public async Task<ActionResult<List<Tag>>> SearchTags(string word)
        {
            string regex = RegexConvert.Regex(word);

            var tags = _context.Tags
                .Select(t => new {  t.Name  })
                .Where(t => EF.Functions.Like(t.Name, regex + "%"))
                .OrderBy(t => t.Name)
                .Take(10)
                .ToList();

            return Json(tags);
        }

        [HttpGet("api/[controller]/review/block/all")]
        public async Task<ActionResult> GetBlockUsers()
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            var blockUsers = await _context.ReviewBlackUsers.Where(u => u.UserId == tokenUserId).Select(u => new
            {
                BlockUser = _context.Users.SingleOrDefault(s => s.Id == u.BlackUserId),
                CreatedAt = u.CreatedAt
            }).ToListAsync();

            return Json(blockUsers);
        }

        [HttpGet("api/[controller]/review/block/{userId}")]
        public async Task<ActionResult> BlockUserReview(int userId)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            var targetUser = await _context.Users.FindAsync(userId);

            if (targetUser == null)
            {
                return NotFound("User not found");
            }

            var alreadyBlock = await _context.ReviewBlackUsers.Where(u => u.UserId == tokenUserId && u.BlackUserId == userId).CountAsync() > 0;

            if (alreadyBlock)
            {
                return Ok("Already blocked");
            }

            var newReviewBlackUser = new ReviewBlackUser
            {
                UserId = tokenUserId,
                BlackUserId = userId
            };

            _context.ReviewBlackUsers.Add(newReviewBlackUser);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("api/[controller]/review/unblock/{userId}")]
        public async Task<ActionResult> UnblockUserReview(int userId)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            var targetUser = await _context.ReviewBlackUsers.Where(u => u.BlackUserId == userId && u.UserId == tokenUserId).AsNoTracking().FirstOrDefaultAsync();

            if (targetUser != null)
            {
                _context.ReviewBlackUsers.Remove(targetUser);
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpGet("api/[controller]/review/search/all_sp/{search?}")]
        public async Task<ActionResult<List<GolfClub>>> GetReviewAllSearchSP(string search = "")
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();
            string error = "no result";

            if (search == "") return BadRequest(); 

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Reviewpage_Review_Search";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_SEARCH",
                    SqlDbType.NVarChar)
                { Value = search });
                cmd.Parameters.Add(new SqlParameter("@P_USER_ID",
                    SqlDbType.Int)
                { Value = tokenUserId });

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

                return Json(retObject);
            }
        }

        [HttpGet("api/[controller]/review/search/all/{search?}")]
        public async Task<ActionResult<List<GolfClub>>> GetReviewAllSearch(string search = "")
        {
            string _search = search;
            string regex = RegexConvert.Regex(search);

            if (search == "")
            {
                return Json(new ReviewAllResult { GolfClubs = new List<GolfClub>(), Conveniences = new List<Convenience>(), Users = new List<User>() });
            }
            else
            {
                var golfClubs = await _context.GolfClubs
                    .Where(gc => EF.Functions.Like(gc.Name,  regex + "%"))
                    .AsSplitQuery()
                    .AsNoTracking()
                    .OrderBy(gc => gc.Name)
                    .Take(5)
                    .ToListAsync();

                var conveniences = await _context.Conveniences
                    .Where(con => EF.Functions.Like(con.Name,  regex + "%"))
                    .AsSplitQuery()
                    .AsNoTracking()
                    .OrderBy(con => con.Name)
                    .Take(5)
                    .ToListAsync();

                var users = await _context.Users
                    .Where(user  => EF.Functions.Like(user.Nickname, regex + "%"))
                    .AsSplitQuery()
                    .AsNoTracking()
                    .OrderBy(user => user.Nickname)
                    .Take(5)
                    .ToListAsync();

                var tags = await _context.Tags
                    .Where(tag => EF.Functions.Like(tag.Name, regex + "%"))
                    .AsSplitQuery()
                    .AsNoTracking()
                    .OrderBy(tag => tag.Name)
                    .Take(5)
                    .ToListAsync();

                return Json(new ReviewAllResult { GolfClubs = golfClubs, Conveniences = conveniences, Users = users, Tags = tags });
            }
        }

        [HttpGet("api/[controller]/review/search_sp/{search?}")]
        public async Task<ActionResult> GetReviewSearchSP(string search = "")
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            int tokenUserNo = JwtManager.getJwtNo(_httpContext);
            if (tokenUserId == 0 || tokenUserNo == 0) return Unauthorized();
            string error = "no result";

            if (search == "") 
            {
                var recommends = GzApi.GetRecommend(tokenUserNo);
                List<int> ciList = new List<int>();

                foreach (var item in recommends)
                {
                    ciList.Add(item.CiCode);
                }


                DataTable tvp = new DataTable();
                tvp.Columns.Add(new DataColumn("code", typeof(int)));

                foreach (var item in ciList)
                {
                    tvp.Rows.Add(item);
                }

                using (var cmd = _context.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = "Screen_Suggestion";
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.Add(new SqlParameter("@List", "dbo.CiCodeList") { Value = tvp });

                    if (cmd.Connection.State != ConnectionState.Open)
                        cmd.Connection.Open();

                    var retObject = new List<IDictionary<string, object>>();
                    using (var dataReader = cmd.ExecuteReader())
                    {
                        while (dataReader.Read())
                        {
                            var dataRow = new ExpandoObject() as IDictionary<string, object>;
                            for (var iFiled = 0; iFiled < dataReader.FieldCount; iFiled++)
                            {
                                dataRow.Add(
                                    dataReader.GetName(iFiled),
                                    dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
                                );
                            }

                            retObject.Add(dataRow);
                        }
                    }
                    return Json(retObject.Take(5));
                }

                return BadRequest();
            }

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Reviewpage_GC_Search";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_SEARCH",
                    SqlDbType.NVarChar)
                { Value = search });

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

                return Json(retObject);
            }
        }


        [HttpGet("api/[controller]/review/search/{search?}")]
        public async Task<ActionResult<List<GolfClub>>> GetReviewSearch(string search = "")
        {
            string _search = search;
            string regex = RegexConvert.Regex(search);

            if (search == "")
            {
                var golfClubs = await _context.GolfClubs
                    .AsSplitQuery()
                    .AsNoTracking()
                    .OrderBy(gc => gc.Name)
                    .Take(5)
                    .ToListAsync();

                var conveniences = await _context.Conveniences
                    .AsSplitQuery()
                    .AsNoTracking()
                    .OrderBy(con => con.Name)
                    .Take(5)
                    .ToListAsync();

                //var conveniences = new List<Convenience>();

                return Json(new ReviewResult { GolfClubs = golfClubs, Conveniences = conveniences });
            }
            else
            {
                var golfClubs = await _context.GolfClubs
                    .Where(gc => EF.Functions.Like(gc.Name, "%" + regex + "%"))
                    .AsSplitQuery()
                    .AsNoTracking()
                    .OrderBy(gc => gc.Name)
                    .ToListAsync();

                golfClubs = golfClubs.OrderBy(e =>
                {
                    return Regex.Match(e.Name, regex).Index;
                }).Take(5).ToList();

                var conveniences = await _context.Conveniences
                    .Where(con => EF.Functions.Like(con.Name, "%" + regex + "%"))
                    .AsSplitQuery()
                    .AsNoTracking()
                    .OrderBy(con => con.Name)
                    .ToListAsync();

                conveniences = conveniences.OrderBy(e =>
                {
                    return Regex.Match(e.Name, regex).Index;
                }).Take(5).ToList();

                return Json(new ReviewResult { GolfClubs = golfClubs, Conveniences = conveniences });
            }
        }

        [HttpGet("api/[controller]/favorites/golfclub_sp")]
        public async Task<ActionResult> GetGolfFavoritesSP(int page = 1)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();
            string error = "no result";

            if (page < 1) page = 1;

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Favoritepage_Golfclub";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_USER_ID",
                    SqlDbType.Int)
                { Value = tokenUserId });
                cmd.Parameters.Add(new SqlParameter("@P_PAGE",
                    SqlDbType.TinyInt)
                { Value = page });

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

                return Json(retObject);
            }
        }

        [HttpGet("api/[controller]/favorites/golfclub")]
        public async Task<ActionResult<List<GolfClub>>> GetGolfFavorites(int? id)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();
            string error = "no result";

            if (id != null)
            {
                IQueryable<GolfFavorites> data = null;

                data =
                       from golffavorite in _context.GolfFavorites
                                                           .OrderByDescending(fa => fa.Id)
                                                           .Include(fa => fa.GolfClub)
                                                           .ThenInclude(fa => fa.Reviews)
                                                           .Include(fa => fa.GolfClub)
                                                           .ThenInclude(fa => fa.Courses)
                                                           .ThenInclude(co => co.Holes)
                                                           .AsNoTracking()
                                                           .AsSplitQuery()
                       where golffavorite.UserId == tokenUserId
                       where golffavorite.Id < id
                       select golffavorite;

                if (data == null)
                {
                    return Json(PaginatedList<int>.nothing(error));
                }

                PaginatedList<GolfFavorites> _golffavorite = await PaginatedList<GolfFavorites>.CreateAsync(data, 1, 10);


                return Json(_golffavorite.Select(s => new
                {
                    id = s.Id,
                    s.GolfClub,

                    CoursesCount = s.GolfClub.Courses.Count(),
                    HolesCount = s.GolfClub.Courses.Sum(co => co.Holes.Count()),
                    Length = s.GolfClub.Courses.Sum(co => co.Length),
                    Par = s.GolfClub.Courses.Join(_context.Holes, c => c.Id, h => h.CourseId, (c, h) => new
                    {
                        Par = h.Par
                    }).Sum(p => String.IsNullOrEmpty(p.Par) || p.Par == "-" ? 0 : Convert.ToInt32(p.Par)),
                    FieldRoundCount = _context.Reviews.Where(r => r.UserId == tokenUserId).Count(r => r.GolfClubId == s.GolfClubId),
                    FieldMinScore = _context.Reviews.Where(r => r.GolfClubId == s.GolfClubId).Count() == 0 ? 0 : _context.Reviews.Where(r => r.GolfClubId == s.GolfClubId && r.UserId == tokenUserId).Select(r => r.Score).Min(),
                    ScreenRoundCount = _context.LifeBestMonths.Where(l => l.UserId == tokenUserId).Count(l => l.GolfClubId == s.GolfClubId),
                    ScreenMinScore = _context.LifeBestMonths.Where(l => l.GolfClubId == s.GolfClubId).Count() == 0 ? 0 : _context.LifeBestMonths.Where(l => l.GolfClubId == s.GolfClubId && l.UserId == tokenUserId).Select(l => l.BestScore).Min(),
                    revewUserCount = _context.Reviews.Count(re => re.GolfClubId == s.GolfClubId),
                    reviewScoreAverage =
                    s.GolfClub.Reviews.Count() != 0 ?
                    ((s.GolfClub.Reviews.Average(re => re.SCourse)
                    + s.GolfClub.Reviews.Average(re => re.SFacility)
                    + s.GolfClub.Reviews.Average(re => re.SCaddy)
                    + s.GolfClub.Reviews.Average(re => re.SFoodAndDrink)) / 4) : 0,
                    favoriteUserAllCount = _context.GolfFavorites.Where(gf => gf.GolfClubId == s.GolfClubId).Count(),
                    createdAt = s.CreatedAt
                }
                ));
            }
            else
            {
                var golffavorite = _context.GolfFavorites
                    .Where(fa => fa.UserId == tokenUserId)
                    .OrderByDescending(fa => fa.Id)
                    .Include(fa => fa.GolfClub)
                    .ThenInclude(fa => fa.Reviews)
                    .Include(fa => fa.GolfClub)
                    .ThenInclude(fa => fa.Courses)
                    .ThenInclude(co => co.Holes)
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Take(10)
                    .ToList();

                return Json(golffavorite.Select(s => new
                {
                    id = s.Id,

                    golfClub = s.GolfClub,
                    CoursesCount = s.GolfClub.Courses.Count(),
                    HolesCount = s.GolfClub.Courses.Sum(co => co.Holes.Count()),
                    Length = s.GolfClub.Courses.Sum(co => co.Length),
                    Par = s.GolfClub.Courses.Join(_context.Holes, c => c.Id, h => h.CourseId, (c, h) => new
                    {
                        Par = h.Par
                    }).Sum(p => String.IsNullOrEmpty(p.Par) || p.Par == "-" ? 0 : Convert.ToInt32(p.Par)),
                    FieldRoundCount = _context.Reviews.Count(r => r.GolfClubId == s.GolfClub.Id),
                    FieldMinScore = _context.Reviews.Where(r => r.GolfClubId == s.GolfClubId).Count() == 0 ? 0 : _context.Reviews.Where(r => r.GolfClubId == s.GolfClubId).Select(r => r.Score).Min(),
                    ScreenRoundCount = _context.LifeBestMonths.Where(l => l.UserId == tokenUserId).Count(l => l.GolfClubId == s.GolfClubId),
                    ScreenMinScore = _context.LifeBestMonths.Where(l => l.GolfClubId == s.GolfClubId).Count() == 0 ? 0 : _context.LifeBestMonths.Where(l => l.GolfClubId == s.GolfClubId && l.UserId == tokenUserId).Select(l => l.BestScore).Min(),
                    revewUserCount = _context.Reviews.Count(re => re.GolfClubId == s.GolfClubId),
                    reviewScoreAverage =
                    s.GolfClub.Reviews.Count() != 0 ?
                    ((s.GolfClub.Reviews.Average(re => re.SCourse)
                    + s.GolfClub.Reviews.Average(re => re.SFacility)
                    + s.GolfClub.Reviews.Average(re => re.SCaddy)
                    + s.GolfClub.Reviews.Average(re => re.SFoodAndDrink)) / 4) : 0,
                    favoriteUserAllCount = _context.GolfFavorites.Where(gf => gf.GolfClubId == s.GolfClubId).Count(),
                    createdAt = s.CreatedAt
                }
                ));
            }
        }

        [HttpGet("api/[controller]/favorites/convenience")]
        public async Task<ActionResult<List<ConvenienceFavorites>>> GetConvenienceFavorites(int? id, double poX, double poY)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            string error = "no result";

            IQueryable<ConvenienceFavorites> data = null;

            var po = new Point(poX, poY);

            data =
                    from convenienceFavorites in _context.ConvenienceFavorites
                                                        .OrderByDescending(cf => cf.Id)
                                                        .Include(cf => cf.Convenience)
                                                        .ThenInclude(con => con.GeoList)
                                                        .AsNoTracking()
                                                        .AsSplitQuery()
                    where convenienceFavorites.UserId == tokenUserId
                    where id != null ? convenienceFavorites.Id < id : true
                    where convenienceFavorites.Convenience != null ? convenienceFavorites.Convenience.Status == 1 : false
                    select convenienceFavorites;

            data.Where(cf => id != null ? cf.Id < id : true);

            if (data == null)
            {
                return Json(PaginatedList<int>.nothing(error));
            }

            PaginatedList<ConvenienceFavorites> _conveniences = await PaginatedList<ConvenienceFavorites>.CreateAsync(data, 1, 10);

            return Json(_conveniences.Select(s => new
            {
                id = s.Id,
                s.Convenience,
                favoriteUserAllCount = _context.ConvenienceFavorites.Where(gf => gf.ConvenienceId == s.ConvenienceId).Count(),
                distance = s.Convenience.GeoList.Position.Distance(po),
                createdAt = s.CreatedAt
            }));
        }

        //shaking
        [HttpGet("api/[controller]/find/golfclub")]
        public async Task<ActionResult> FindGolfClubByLocationSP(string poX, string poY)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            int tokenUserNo = JwtManager.getJwtNo(_httpContext);

            //tokenUserId = 80;
            //tokenUserNo = 1794859;

            //poX = "127.40969119241988";
            //poY = "36.41575497687466";

            if (tokenUserId == 0) return Unauthorized();

            var retObject = new List<dynamic>();
            bool eventState = false;

            //MileageManager.SetEvent(_context, tokenUserNo, tokenUserId, eventType, MileageManager.EVENT_GROUP_REVIEW)
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Find_GolfClub_By_Location";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_X", SqlDbType.VarChar) { Value = poX });
                cmd.Parameters.Add(new SqlParameter("@P_Y", SqlDbType.VarChar) { Value = poY });

                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();
                
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

                            if (dataReader.GetName(iFiled) == "holeId")
                            {
                                if (!dataReader.IsDBNull(iFiled))
                                {
                                    eventState = true;
                                }
                            }
                        }

                        retObject.Add((ExpandoObject)dataRow);
                    }
                }
            }

            try
            {
                if (retObject.Count > 0)
                {
                    if (retObject[0].golfClubId != null && retObject[0].golfClubId > 0)
                    {
                        int gId = (int)(retObject[0].golfClubId);
                        var golfClub = _context.GolfClubs.Where(g => g.Id == gId).Where(g => g.GcNum != null && g.GcNum.Length > 0 && g.GcNum != "0").FirstOrDefault();
                        if (golfClub != null)
                        {
                            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
                            {
                                cmd.CommandText = "Roulette_Add_Stamp";
                                cmd.CommandType = CommandType.StoredProcedure;
                                // set some parameters of the stored procedure
                                cmd.Parameters.Add(new SqlParameter("@UserId",
                                    SqlDbType.Int)
                                { Value = tokenUserId });
                                cmd.Parameters.Add(new SqlParameter("@State",
                                    SqlDbType.VarChar)
                                { Value = "쉐이킹" });

                                if (cmd.Connection.State != ConnectionState.Open)
                                    cmd.Connection.Open();

                                var retObject1 = new List<dynamic>();
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

                                        retObject1.Add((ExpandoObject)dataRow);
                                    }
                                }

                                var retStamp = retObject1[0].type;
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
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("||||||||FindGolfClubByLocationSP|||||");
                Console.WriteLine(e);
            }

            if (eventState)
            {
                
                var d = await MileageManager.SetEvent(_context, tokenUserNo, tokenUserId,
                                        MileageManager.EVENT_TYPE_SHAKING, MileageManager.EVENT_GROUP_LOGIN);
                Console.WriteLine("||||||||eventState|||||");
                Console.WriteLine(d);
            }
            

            return Json(retObject);
        }

        [HttpGet("api/[controller]/favorites/convenience_sp")]
        public async Task<ActionResult> GetConvenienceFavoritesSP(string poX, string poY, int page = 1)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();


            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Favorite_Around_Convenience";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_USER_ID",SqlDbType.Int){ Value = tokenUserId });
                cmd.Parameters.Add(new SqlParameter("@P_PAGE",SqlDbType.Int){ Value = page });
                cmd.Parameters.Add(new SqlParameter("@P_X",SqlDbType.VarChar){ Value = poX });
                cmd.Parameters.Add(new SqlParameter("@P_Y",SqlDbType.VarChar){ Value = poY });

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

                return Json(retObject);
            }
        }

        [HttpGet("api/[controller]/favorite/{type}/{typeId}")]
        public async Task<ActionResult> Favorite(string type, int typeId)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            if (type.Equals("golfclub"))
            {
                var notFoundGolfClub = _context.GolfClubs.Count(g => g.Id == typeId) == 0;
                if (notFoundGolfClub)
                {
                    return Json(false);
                }

                var favorite = _context.GolfFavorites.Where(gf => gf.GolfClubId == typeId && tokenUserId == gf.UserId).FirstOrDefault();
                if (favorite != null)
                {
                    _context.GolfFavorites.Remove(favorite);
                }

                var newGolfFavorite = new GolfFavorites
                {
                    GolfClubId = typeId,
                    UserId = tokenUserId
                };

                _context.GolfFavorites.Add(newGolfFavorite);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    id = newGolfFavorite.Id
                });
            }
            else if (type.Equals("convenience"))
            {
                var notFoundGolfClub = _context.Conveniences.Count(c => c.Id == typeId) == 0;
                if (notFoundGolfClub)
                {
                    return Json(false);
                }

                var favorite = _context.ConvenienceFavorites.Where(gf => gf.ConvenienceId == typeId && tokenUserId == gf.UserId).FirstOrDefault();
                if (favorite != null)
                {
                    _context.ConvenienceFavorites.Remove(favorite);
                }

                var newConvenienceFavorite = new ConvenienceFavorites
                {
                    ConvenienceId = typeId,
                    UserId = tokenUserId
                };

                _context.ConvenienceFavorites.Add(newConvenienceFavorite);
                await _context.SaveChangesAsync();

                return Json(new
                {
                    id = newConvenienceFavorite.Id
                });
            }

            return Json(false);
        }

        [HttpGet("api/[controller]/unfavorite/{type}/{typeId}")]
        public async Task<ActionResult> UnFavorite(string type, int typeId)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            if (type.Equals("golfclub"))
            {
                var favorite = _context.GolfFavorites.Find(typeId);
                if (favorite != null)
                {
                    if (favorite.UserId == tokenUserId)
                    {
                        _context.GolfFavorites.Remove(favorite);
                        await _context.SaveChangesAsync();
                        return Json(true);
                    }
                }
            }
            else if (type.Equals("convenience"))
            {
                var favorite = _context.ConvenienceFavorites.Find(typeId);
                if (favorite != null)
                {
                    if (favorite.UserId == tokenUserId)
                    {
                        _context.ConvenienceFavorites.Remove(favorite);
                        await _context.SaveChangesAsync();
                        return Json(true);
                    }
                }
            }

            return Json(false);
        }

        private class ReviewImageViewModel
        {
            public int Id { get; set; }
            public int ReviewId { get; set; }
            public string OriginalName { get; set; }
            public string Uri { get; set; }
        }

        [HttpGet("api/[controller]/check/report_review")]
        public async Task<ActionResult> CheckWrongReview(int reviewId)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            var data = from r in _context.ReviewReports
                       where r.UserId == tokenUserId && r.ReviewId == reviewId
                       select r.Id;

            return Json(data.Count() == 1);
        }

        [HttpGet("api/[controller]/detail/picture/golfclub/images")]
        public async Task<ActionResult> GetGolfClubImages(int golfClubId)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            //if (tokenUserId == 0) return Unauthorized();

            var data = from golfclubs in _context.GolfClubs
                   where golfclubs.Id == golfClubId && golfclubs.Status == 1
                   join golfclubImages in _context.GolfClubImages on golfclubs.Id equals golfclubImages.GolfClubId
                   select new
                   {
                       Id = golfclubImages.Id,
                       OriginalName = golfclubImages.OriginalName,
                       Uri = golfclubImages.Uri,
                   };

            return Json(data);
        }

        [HttpGet("api/[controller]/detail/picture/{type}/{typeId}")]
        public async Task<ActionResult<List<ReviewImage>>> GetReviewPicture(string type, int typeId, int? reviewImageId)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            //if (tokenUserId == 0) return Unauthorized();


            string error = "no result";
            IQueryable<ReviewImageViewModel> data = null;

            data =
                   from review in _context.Reviews
                                                       //.OrderByDescending(r => r.Id)
                                                       //.AsNoTracking()
                                                       //.AsSplitQuery()
                    //orderby review.Id descending
                   where type.Equals("golfclub") ? review.GolfClubId == typeId : review.ConvenienceId == typeId
                   join reviewImages in _context.ReviewImages on review.Id equals reviewImages.ReviewId
                   join user in _context.Users on review.UserId equals user.Id
                   where review.OpenState == 1 && (reviewImageId != null ? reviewImages.Id < reviewImageId : true) &&  user.Blinded == null
                   orderby reviewImages.Id descending
                   select new ReviewImageViewModel
                   {
                       Id = reviewImages.Id,
                       ReviewId = reviewImages.ReviewId,
                       OriginalName = reviewImages.OriginalName,
                       Uri = reviewImages.Uri,
                   };




            //if (data == null)
            //{
            //    return Json(PaginatedList<int>.nothing(error));
            //}

            //PaginatedList<ReviewImage> _reviewImages = await PaginatedList<ReviewImage>.CreateAsync(data, 1, 20);


            return Json(data.Take(20));
        }

        private class ReviewType
        {
            public Review review { get; set; }
            public User user { get; set; }
            public Setting setting { get; set; }
            public string name { get; set; } = "";
            public int? code { get; set; }
        }

        [HttpGet("api/[controller]/review/all")]
        public async Task<ActionResult<List<Review>>> GetReviewAll(string type, int? targetId, int? sort, int? mypage, int page = 1)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            //if (tokenUserId == 0) return Unauthorized();

            // type 
            // 그 외 = 모두
            // g = 골프장 리뷰만
            // c = 편의시설 리뷰만
            // u = 유저 리뷰만
            // t = 태그 리뷰만
            if (targetId == null)
            {
                targetId = 0;
            }

            var _r = from review in _context.Reviews
                     select review;

            var _reviewLinq = from review in _r
                              where review.State == 0
                              join user in _context.Users on review.UserId equals user.Id
                              where (review.OpenState == 1 || (mypage == 1 && user.Id == tokenUserId)) && (user.Id == tokenUserId || user.Blinded == null)
                              join setting in _context.Settings on user.Id equals setting.UserId
                              join blackUser in _context.ReviewBlackUsers
                              on new { UserId = tokenUserId, BlackUserId = review.UserId } equals new { UserId = blackUser.UserId, BlackUserId = blackUser.BlackUserId } into blackUserGroup
                              from blackUser in blackUserGroup.DefaultIfEmpty()
                              where blackUser == null
                              select new ReviewType { review = review, user = user, setting = setting, name = "" };


            if (targetId != null)
            {
                if (type == "g")
                {
                    _reviewLinq = from review in _reviewLinq
                                   where review.review.GolfClubId == targetId
                                   //join golfClub in _context.GolfClubs on review.review.GolfClubId equals golfClub.Id
                                   select new ReviewType {
                                     review = review.review,
                                     user = review.user,
                                     setting = review.setting,
                                     //name = golfClub.Name,
                                   };
                }
                else if (type == "c")
                {
                    _reviewLinq = from review in _reviewLinq

                                  where review.review.ConvenienceId == targetId
                                  //join convenience in _context.Conveniences on review.review.ConvenienceId equals convenience.Id
                                  select new ReviewType
                                  {
                                      review = review.review,
                                      user = review.user,
                                      setting = review.setting,
                                      //name = convenience.Name,
                                  };
                }
                else if (type == "u")
                {
                    _reviewLinq = from review in _reviewLinq
                                   where review.user.Id == targetId
                                   select review;
                }
                else if (type == "t")
                {
                    _reviewLinq = from review in _reviewLinq
                                   join reviewTag in _context.ReviewTags on review.review.Id equals reviewTag.ReviewId
                                   where reviewTag.TagId == targetId
                                   select review;
                }
            }

            _reviewLinq = from review in _reviewLinq
                          join golfClub in _context.GolfClubs on review.review.GolfClubId equals golfClub.Id into golfClubGroup
                          from golfClub in golfClubGroup.DefaultIfEmpty()
                          join clubHouse in _context.ClubHouses on golfClub.Id equals clubHouse.GolfClubId into chg
                          from clubHouse in chg.DefaultIfEmpty()
                          join geo in _context.GeoLists on clubHouse.GeoListId equals geo.Id into geoGroup
                          from geo in geoGroup.DefaultIfEmpty()
                          join convenience in _context.Conveniences on review.review.ConvenienceId equals convenience.Id into convenienceGroup
                          from convenience in convenienceGroup.DefaultIfEmpty()
                          where (convenience == null || convenience.Status == 1) && (golfClub == null || golfClub.Status == 1) && ((geo == null || geo.Code > 0))
                          select new ReviewType
                          {
                              review = review.review,
                              user = review.user,
                              setting = review.setting,
                              code = geo != null ? geo.Code : 0,
                              name = golfClub != null ? golfClub.Name : (convenience != null ? convenience.Name : ""),
                          };


            _reviewLinq = from review in _reviewLinq
                          orderby review.review.Id descending
                          select review;

            if (sort == 2)
            {
                _reviewLinq = from review in _reviewLinq
                              where review.review.GolfClubId != null
                              orderby review.review.PlayDate descending
                              select review;
            }
            else if (sort == 3)
            {
                _reviewLinq = from review in _reviewLinq
                              where review.review.GolfClub != null && review.review.Score > 0
                              orderby review.review.Score ascending
                              select review;
            }

            var _reviewLinq2 = from review in _reviewLinq
                                             select new {
                                                 Id = review.review.Id,
                                                 Review = new {
                                                     review.review.CaddyName,
                                                     review.review.UserId,
                                                     review.review.CreatedAt,
                                                     review.review.PlayDate,
                                                     review.review.ReviewContent,
                                                     review.review.SCaddy,
                                                     review.review.SCourse,
                                                     review.review.SFacility,
                                                     review.review.SFoodAndDrink,
                                                     review.review.DCaddy,
                                                     review.review.DCourse,
                                                     review.review.DFacility,
                                                     review.review.DFoodAndDrink,
                                                     review.review.Score,
                                                     review.review.SkinType,
                                                     review.review.GolfClubId,
                                                     review.review.OpenState,
                                                     review.review.ConvenienceId,
                                                     review.review.Representation,
                                                     review.review.Views,
                                                     review.review.VisitCount
                                                 },
                                                 User = new { 
                                                    review.user.Id,
                                                    review.user.Nickname,
                                                    review.user.UserNum
                                                 },
                                                 Setting = new
                                                 {
                                                     review.setting.PrivateAgree
                                                 },
                                                 Name = review.name,
                                                 Code = review.code,
                                             };

            if (page < 1) page = 1;
            var data = _reviewLinq2.Skip((page - 1) * 10).Take(10);

            var data2 = from d in data
                        join reviewGood in _context.ReviewGoods on d.Id equals reviewGood.ReviewId into reviewGoodGroup
                        from reviewGood in reviewGoodGroup.DefaultIfEmpty()
                        group reviewGood by d into r
                        select new
                        {
                            Id = r.Key.Id,
                            Review = r.Key.Review,
                            User = r.Key.User,
                            Setting = r.Key.Setting,
                            Name = r.Key.Name,
                            Code = r.Key.Code,
                            GoodCount = r.Where(r => r != null).Where(r => r.State == 1).Count(),
                            DislikeCount = r.Where(r => r != null).Where(r => r.State == 2).Count(),
                            RepresentativeImageId = _context.ReviewImages.Where(_r => _r.ReviewId == r.Key.Id && _r.Representative).Select(_r => _r.Id).FirstOrDefault(),
                            MyGoodState = _context.ReviewGoods.Where(rg => rg.ReviewId == r.Key.Id).Where(rg => rg.UserId == tokenUserId).Select(rg => new { rg.Id, rg.State }).FirstOrDefault(),
                        };

            var data3 = from d in data2.ToList()
                        join reviewImage in _context.ReviewImages on d.Id equals reviewImage.ReviewId into reviewImageGroup
                        from reviewImage in reviewImageGroup.DefaultIfEmpty()
                        group reviewImage by d into r
                        select new
                        {
                            Id = r.Key.Id,
                            Review = r.Key.Review,
                            User = r.Key.User,
                            Setting = r.Key.Setting,
                            Name = r.Key.Name,
                            Code = r.Key.Code,
                            GoodCount = r.Key.GoodCount,
                            DislikeCount = r.Key.DislikeCount,
                            MyGoodState = r.Key.MyGoodState,
                            RepresentativeImageId = r.Key.RepresentativeImageId,
                            GradeAvgScore = _context.ReviewGrades.Where(rg => rg.ReviewId == r.Key.Id).Count() > 0 ? _context.ReviewGrades.Where(rg => rg.ReviewId == r.Key.Id).Average(rg => rg.Score) : 0,
                            //Images = d.Images
                            Images = _context.ReviewImages.Where(_r => _r.ReviewId == r.Key.Id).Select(_r => new
                            {
                                _r.Uri,
                                _r.Representative
                            }).ToList(),
                            VisitCount = r.Key.Review.VisitCount//r.Key.Review.GolfClubId == null ? 0 : _context.Reviews.Where(_r => _r.GolfClubId == r.Key.Review.GolfClubId).Where(_r => _r.UserId == r.Key.User.Id).Count(),
                        };

            var data4 = from d in data3
                          orderby d.Id descending
                          select d;

            if (sort == 2)
            {
                data4 = from d in data4
                              orderby d.Review.PlayDate descending
                              select d;
            }
            else if (sort == 3)
            {
                data4 = from d in data4
                        orderby d.Review.Score ascending
                              select d;
            }

            return Json(data4);
        }
        
        [HttpGet("api/[controller]/search_together/{golfClubId}")]
        public async Task<ActionResult> GetTogetherSearchGolfClubs(int golfClubId)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            //if (tokenUserId == 0) return Unauthorized();

            var userIds = from sh in _context.GolfClubSearchHistories
                          where sh.GolfClubId == golfClubId
                          group sh by sh.UserId into shg
                          select new
                          {
                              UserId = shg.Key
                          };

            var golfClubCntBefore = from user in userIds where user.UserId != tokenUserId
                              join sh in _context.GolfClubSearchHistories on user.UserId equals sh.UserId
                              group sh by new { sh.GolfClubId, sh.UserId } into shGroup
                              select new
                              {
                                  GolfClubId = shGroup.Key.GolfClubId,
                                  UserId = shGroup.Key.UserId,
                              };

            var golfClubCnt = from e in golfClubCntBefore
                              group e by e.GolfClubId into geGroup
                              orderby geGroup.Count() descending
                              select new
                              {
                                  GolfClubId = geGroup.Key,
                                  Count = geGroup.Count()
                              };

            var golfClubTop5 = golfClubCnt.Where(g => g.Count > 0 && g.GolfClubId != golfClubId).OrderByDescending(e => e.Count).Take(5).Select(e => new { e.GolfClubId });

            var golfClubs = from golfClubTop in golfClubTop5
                            join golfClub in _context.GolfClubs on golfClubTop.GolfClubId equals golfClub.Id
                            select new
                            {
                                golfClub.Id,
                                golfClub.Name,
                                golfClub.Address,
                                golfClub.TFileUri,
                            };

            var golfClubsWithHoleCnt = from golfClub in golfClubs
                                       join course in _context.Courses on golfClub.Id equals course.GolfClubId
                                       group golfClub by new
                                       {
                                           golfClub.Id,
                                           golfClub.Name,
                                           golfClub.Address,
                                           golfClub.TFileUri,
                                       } into gcGroup
                                       select new
                                       {
                                           gcGroup.Key.Id,
                                           gcGroup.Key.Name,
                                           gcGroup.Key.Address,
                                           gcGroup.Key.TFileUri,
                                           HoleCnt = gcGroup.Count() * 9,
                                       };

            var golfClubsWithFavorite = from golfClub in golfClubsWithHoleCnt
                                        join favorite in _context.GolfFavorites on new { GolfClubId = golfClub.Id, UserId = tokenUserId } equals new { GolfClubId = favorite.GolfClubId, UserId = favorite.UserId } into favoriteGroup
                                        from favorite in favoriteGroup.DefaultIfEmpty()
                                        select new
                                        {
                                            golfClub.Id,
                                            golfClub.Name,
                                            golfClub.Address,
                                            golfClub.TFileUri,
                                            golfClub.HoleCnt,
                                            Favorite = favorite == null ? null : new
                                            {
                                                favorite.Id
                                            }
                                        };

            
            return Json(golfClubsWithFavorite);
        }

        [HttpGet("api/[controller]/review/avg/{id}")]
        public async Task<ActionResult> GetReviewAvg(int id)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            //if (tokenUserId == 0) return Unauthorized();

            var _reviews = _context.Reviews
                .Where(r => r.GolfClubId == id)
                .Where(r => r.OpenState == 1)
                .Where(r => r.State == 0).Select(r => new {
                    r.SCaddy,
                    r.SCourse,
                    r.SFacility,
                    r.SFoodAndDrink,
                }).ToList();

            var reviews = from r in _reviews
                          select new
                          {
                              Id = 1,
                              SCaddy = r.SCaddy,
                              SCourse = r.SCourse,
                              SFacility = r.SFacility,
                              SFoodAndDrink = r.SFoodAndDrink,
                          };

            var reviews2 = from review in reviews
                           group review by review.Id into r
                           let AllCount = r.Count()
                           select
                            new
                            {
                                AllCount,
                                AllAvg = AllCount > 0 ? r.Average(r => (r.SCaddy + r.SCourse + r.SFacility + r.SFoodAndDrink) / 4) : 0,
                                CaddyAvg = AllCount > 0 ? r.Average(r => r.SCaddy) : 0,
                                CourseAvg = AllCount > 0 ? r.Average(r => r.SCourse) : 0,
                                FacilityAvg = AllCount > 0 ? r.Average(r => r.SFacility) : 0,
                                FoodAndDrinkAvg = AllCount > 0 ? r.Average(r => r.SFoodAndDrink) : 0,
                            };

            return Json(reviews2.FirstOrDefault());

            //var _reviews = _context.Reviews.Where(r => r.GolfClubId == id)
            //    //.Where(r => r.State == 0).Where(r => r.DCaddy != null || r.DCourse != null || r.DFacility != null || r.DFoodAndDrink != null).Select(r => new {
            //    .Where(r => r.State == 0).Select(r => new {
            //         r.DCaddy,
            //        r.SCaddy,
            //        r.DCourse,
            //        r.SCourse,
            //        r.DFacility,
            //        r.SFacility,
            //        r.DFoodAndDrink,
            //        r.SFoodAndDrink,
            //    }).ToList();

            //var reviews = from r in _reviews
            //              //where (r.DCaddy !=null && r.DCaddy.Length > 0) || (r.DCourse != null && r.DCourse.Length > 0) || (r.DFacility != null && r.DFacility.Length > 0) || (r.DFoodAndDrink != null && r.DFoodAndDrink.Length > 0)
            //              select new
            //              {
            //                  Id = 1,
            //                  IsCaddy = r.DCaddy != null && r.DCaddy.Length > 0,
            //                  IsCourse = r.DCourse != null && r.DCourse.Length > 0,
            //                  IsFacility = r.DFacility != null && r.DFacility.Length > 0,
            //                  IsFoodAndDrink = r.DFoodAndDrink != null && r.DFoodAndDrink.Length > 0,
            //                  SCaddy = r.SCaddy,
            //                  SCourse = r.SCourse,
            //                  SFacility = r.SFacility,
            //                  SFoodAndDrink = r.SFoodAndDrink,
            //              };

            //var reviews2 = from review in reviews
            //               group review by review.Id into r
            //               let AllCount = r.Count()
            //               let CaddyCount = r.Count(r => r.IsCaddy)
            //               let CourseCount = r.Count(r => r.IsCourse)
            //               let FacilityCount = r.Count(r => r.IsFacility)
            //               let FoodAndDrinkCount = r.Count(r => r.IsFoodAndDrink)
            //               select 
            //                new
            //                {
            //                    AllCount,
            //                    AllAvg = AllCount > 0 ? r.Average(r => (r.SCaddy + r.SCourse + r.SFacility + r.SFoodAndDrink) / 4) : 0,
            //                    CaddyCount,
            //                    CaddyAvg = CaddyCount > 0 ? r.Where(r => r.IsCaddy).Average(r => r.SCaddy) : 0,
            //                    CourseCount,
            //                    CourseAvg = CourseCount > 0 ? r.Where(r => r.IsCourse).Average(r => r.SCourse) : 0,
            //                    FacilityCount,
            //                    FacilityAvg = FacilityCount > 0 ? r.Where(r => r.IsFacility).Average(r => r.SFacility) : 0,
            //                    FoodAndDrinkCount,
            //                    FoodAndDrinkAvg = FoodAndDrinkCount > 0 ? r.Where(r => r.IsFoodAndDrink).Average(r => r.SFoodAndDrink) : 0,
            //                };

            //return Json(reviews2.FirstOrDefault());
        }

        [HttpGet("api/[controller]/review/all2")]
        public async Task<ActionResult<List<Review>>> GetReviewAll2(int? id, int type, int? golfId, int? conId)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            string error = "no result";

            if (id != null)
            {
                IQueryable<Review> data = null;

                data =
                       from review in _context.Reviews
                                                           .OrderByDescending(re => re.Id)
                                                           .Include(re => re.ReviewGoods)
                                                           .ThenInclude(reg => reg.User)
                                                           .Include(re => re.ReviewImages)
                                                           .Include(re => re.User)
                                                           .ThenInclude(u => u.Setting)
                                                           .AsNoTracking()
                                                           .AsSplitQuery()
                       where type == 0 ? review.GolfClubId == golfId : review.ConvenienceId == conId
                       where review.Id < id
                       where review.UserId == tokenUserId || review.OpenState == 1
                       where review.User.Blinded == null
                       select review;

                if (data == null)
                {
                    return Json(PaginatedList<int>.nothing(error));
                }

                PaginatedList<Review> _reviews = await PaginatedList<Review>.CreateAsync(data, 1, 10);


                return Json(_reviews.Select(s => new
                {
                    id = s.Id,
                    golfClubId = s.GolfClubId,
                    convenienceId = s.ConvenienceId,
                    userId = s.User.Id,
                    userName = s.User.Username,
                    nickname = s.User.Nickname,
                    privateAgree = s.User.Setting.PrivateAgree,
                    reviewContent = s.ReviewContent,
                    playDate = s.PlayDate,
                    score = s.Score,
                    sCourse = s.SCourse,
                    sFacility = s.SFacility,
                    sCaddy = s.SCaddy,
                    sFoodAndDrink = s.SFoodAndDrink,
                    dCourse = s.DCourse,
                    dFacility = s.DFacility,
                    dCaddy = s.DCaddy,
                    dFoodAndDrink = s.DFoodAndDrink,
                    caddyName = s.CaddyName,
                    representation = s.Representation,
                    openState = s.OpenState,
                    state = s.State,
                    views = s.Views,
                    skinType = s.SkinType,
                    //reCourseId = s.ReCourseId,
                    reviewGoods = s.ReviewGoods,
                    reviewImages = s.ReviewImages,
                    revewUserCount =
                    s.GolfClubId != null
                    ? _context.Reviews.Count(re => re.GolfClubId == s.GolfClubId && re.UserId == tokenUserId)
                    : _context.Reviews.Count(re => re.ConvenienceId == s.ConvenienceId && re.UserId == tokenUserId),
                    createdAt = s.CreatedAt
                }
                ));
            }
            else
            {
                var reviews = _context.Reviews
                    .OrderByDescending(re => re.Id)
                    .Where(re => type == 0 ? re.GolfClubId == golfId : re.ConvenienceId == conId)
                    .Where(re => re.UserId == tokenUserId || re.OpenState == 1)
                    .Where(re => re.User.Blinded == null)
                    .Include(re => re.ReviewGoods)
                    .ThenInclude(reg => reg.User)
                    .Include(re => re.ReviewImages)
                    .Include(re => re.User)
                    .ThenInclude(u => u.Setting)
                    .AsNoTracking()
                    .AsSplitQuery()
                    .Take(10)
                    .ToList();

                return Json(reviews.Select(s => new
                {
                    id = s.Id,
                    golfClubId = s.GolfClubId,
                    convenienceId = s.ConvenienceId,
                    userId = s.User.Id,
                    userName = s.User.Username,
                    nickname = s.User.Nickname,
                    privateAgree = s.User.Setting.PrivateAgree,
                    reviewContent = s.ReviewContent,
                    playDate = s.PlayDate,
                    score = s.Score,
                    sCourse = s.SCourse,
                    sFacility = s.SFacility,
                    sCaddy = s.SCaddy,
                    sFoodAndDrink = s.SFoodAndDrink,
                    dCourse = s.DCourse,
                    dFacility = s.DFacility,
                    dCaddy = s.DCaddy,
                    dFoodAndDrink = s.DFoodAndDrink,
                    caddyName = s.CaddyName,
                    representation = s.Representation,
                    openState = s.OpenState,
                    state = s.State,
                    views = s.Views,
                    skinType = s.SkinType,
                    //reCourseId = s.ReCourseId,
                    reviewGoods = s.ReviewGoods,
                    reviewImages = s.ReviewImages,
                    revewUserCount =
                    s.GolfClubId != null
                    ? _context.Reviews.Count(re => re.GolfClubId == s.GolfClubId && re.UserId == tokenUserId)
                    : _context.Reviews.Count(re => re.ConvenienceId == s.ConvenienceId && re.UserId == tokenUserId),
                    createdAt = s.CreatedAt
                }
                ));
            }
        }


        [HttpPost("api/[controller]/review/good")]
        public async Task<ActionResult<List<Review>>> GetReviewGoodSet([FromBody] ReviewGood param, bool del = false)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            param.UserId = tokenUserId;

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (param != null)
            {
                if (del)
                {
                    var prevGood = _context.ReviewGoods.Find(param.Id);
                    if (prevGood != null)
                    {
                        if (prevGood.UserId == tokenUserId) _context.ReviewGoods.Remove(prevGood);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    var prevGood = _context.ReviewGoods.Where(e => e.ReviewId == param.ReviewId && e.UserId == param.UserId).FirstOrDefault();

                    if (prevGood != null)
                    {
                        _context.ReviewGoods.Remove(prevGood);
                    }

                    ReviewGood newReviewGood = new ReviewGood
                    {
                        State = param.State,
                        ReviewId = param.ReviewId,
                        UserId = tokenUserId
                    };

                    _context.ReviewGoods.Add(newReviewGood);

                    if (newReviewGood.State == 1)
                    {
                        var review = await _context.Reviews.FindAsync(newReviewGood.ReviewId);
                        var user = _context.Users.Include(u => u.Setting).FirstOrDefault(u => u.Id == review.UserId);
                        
                        if (user.Setting.ReviewLike && tokenUserId != user.Id)
                        {
                            string contents = review.CreatedAt.ToString("yyyy.MM.dd") + " 에 작성한 리뷰에 좋아요가 추가되었습니다.";

                            await UpdateNotification.Update(_context, 20, tokenUserId, review.UserId, review.GolfClubId, review.ConvenienceId, null, contents, review.Id);
                            await _context.SaveChangesAsync();
                        }
                    }

                    await _context.SaveChangesAsync();
                    return Json(new
                    {
                        Id = newReviewGood.Id
                    });
                }
                return Ok();
            }
            else
            {
                return Json("ERROR");
            }

        }

        [HttpGet("api/[controller]/review/report/get/{id}")]
        public async Task<ActionResult<List<ReviewReport>>> ReportReview(int id)
        {
            var reviewReports = await _context.ReviewReports
                .Include(r => r.User)
                .Where(re => re.ReviewId == id)
                .AsNoTracking()
                .AsSplitQuery()
                .ToListAsync();

            return Json(reviewReports);
        }

        [HttpPost("api/[controller]/review/report")]
        public async Task<ActionResult> ReportReview(ReviewReportDto param)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var review = await _context.Reviews.FindAsync(param.ReviewId);

            bool doesReviewExist = review != null;
            if (!doesReviewExist)
            {
                return BadRequest("Review does not exist");
            }

            if (review.UserId == tokenUserId)
            {
                return BadRequest("You can't make a report on your review.");
            }

            ReviewReport newReviewReport = new ReviewReport
            {
                ReviewId = param.ReviewId,
                UserId = tokenUserId,
                Content = param.Content
            };

            _context.ReviewReports.Add(newReviewReport);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("api/[controller]/report")]
        public async Task<ActionResult<List<GolfClub>>> CreateWrongInfoItem([FromForm] WrongInfoDto param)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            int tokenUserNo = JwtManager.getJwtNo(_httpContext);
            if (tokenUserId == 0 || tokenUserNo == 0) return Unauthorized();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            WrongInfo newWrongInfo = new WrongInfo
            {
                Content = param.Content,
                UserId = tokenUserId
            };

            if (param.Type == "g")
            {
                var golfclub = await _context.GolfClubs.FindAsync(param.Id);

                bool doesExist = golfclub != null;
                if (!doesExist)
                {
                    return BadRequest("GolfClub does not exist");
                }

                newWrongInfo.GolfClubId = param.Id;
            }
            else
            {
                var convenience = await _context.Conveniences.FindAsync(param.Id);

                bool doesExist = convenience != null;
                if (!doesExist)
                {
                    return BadRequest("Convenience does not exist");
                }

                newWrongInfo.ConvenienceId = param.Id;
            }

            List<WrongInfoImage> imgs = null;
            List<Upload.UploadedFile> newFileInfoList = null;

            if (param.WrongInfoImages != null)
            {
                imgs = JsonConvert.DeserializeObject<List<WrongInfoImage>>(param.WrongInfoImages);

                if (!TryValidateModel(imgs, nameof(imgs)))
                {
                    return BadRequest(ModelState);
                }
            }

            //파일 확장자 검사
            if ((param.FileImages != null && !AllowedImageExtensions.Validate(param.FileImages)))
            {
                return BadRequest("You can upload only image files");
            }

            try
            {
                //파일 업로드
                if (param.FileImages != null)
                {
                    newFileInfoList = await Upload.UploadImageAndReturnInfo(param.FileImages, Upload.IMAGE_TYPE_REVIEW);
                }

                _context.WrongInfos.Add(newWrongInfo);
                await _context.SaveChangesAsync();

                for (int i = 0; i < imgs.Count; i++)
                {
                    imgs[i].WrongInfoId = newWrongInfo.Id;

                    var idx = imgs[i].ImageIndex.Value;
                    imgs[i].Name = newFileInfoList[idx].Name;
                    imgs[i].OriginalName = newFileInfoList[idx].OriginalName;
                    imgs[i].Uri = newFileInfoList[idx].Uri;

                    imgs[i].Next = i;

                    _context.WrongInfoImages.Update(imgs[i]);
                    await _context.SaveChangesAsync();
                }

                //고객사 요청으로 자동지급 잠시 주석처리함. 210926
                //MileageManager.SetReport(_context, tokenUserNo, tokenUserId);

                return Json(newWrongInfo.Id);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Json(false);
            }
            return Json(false);
        }

        [HttpGet("api/[controller]/review/grade/{reviewId}")]
        public async Task<ActionResult> GetReviewGrade(int reviewId)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            return Json(_context.ReviewGrades.Where(r => r.UserId == tokenUserId && r.ReviewId == reviewId).Select(r => new { Score = r.Score }).FirstOrDefault());
        }

        [HttpPost("api/[controller]/review/grade")]
        public async Task<ActionResult> EvaluateReviewGrade(ReviewGradeDto param)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var review = await _context.Reviews.FindAsync(param.ReviewId);

            bool doesReviewExist = review != null;
            if (!doesReviewExist)
            {
                return BadRequest("Review does not exist");
            }

            if (review.UserId == tokenUserId)
            {
                return BadRequest("You can't evaluate on your review.");
            }

            var alreadyReviewEvaludated = _context.ReviewGrades.Where(rg => rg.ReviewId == param.ReviewId && tokenUserId == rg.UserId).FirstOrDefault();
            if (alreadyReviewEvaludated != null)
            {
                alreadyReviewEvaludated.Score = param.Score;
                await _context.SaveChangesAsync();
                return Ok();
            }

            ReviewGrade newReviewGrade = new ReviewGrade
            {
                ReviewId = param.ReviewId,
                UserId = tokenUserId,
                Score = param.Score
            };

            _context.ReviewGrades.Add(newReviewGrade);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("api/[controller]/review/item/create")]
        public async Task<ActionResult<List<GolfClub>>> CreateReviewItem([FromForm] ReviewDto param)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            int tokenUserNo = JwtManager.getJwtNo(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Review re = null;
            List<ReviewImage> reImg = null;
            List<RefCourseList> refCourseLists = null;
            List<Upload.UploadedFile> newFileInfoList = null;
            bool reviewCreated = false;

            //골프클럽 유호성 검사
            if (param.Review != null)
            {
                re = JsonConvert.DeserializeObject<Review>(param.Review);

                if (!TryValidateModel(re, nameof(re)))
                {
                    return BadRequest(ModelState);
                }

                re.UserId = tokenUserId;

                if (re.Id != 0)
                {
                    reviewCreated = true;
                }
            }

            if (param.ReviewImages != null)
            {
                reImg = JsonConvert.DeserializeObject<List<ReviewImage>>(param.ReviewImages);

                if (!TryValidateModel(reImg, nameof(reImg)))
                {
                    return BadRequest(ModelState);
                }
            }

            if (param.RefCourseLists != null)
            {
                refCourseLists = JsonConvert.DeserializeObject<List<RefCourseList>>(param.RefCourseLists);

                if (!TryValidateModel(refCourseLists, nameof(refCourseLists)))
                {
                    return BadRequest(ModelState);
                }
            }

            //파일 확장자 검사
            if ((param.FileImages != null && !AllowedImageExtensions.Validate(param.FileImages)))
            {
                return BadRequest("You can upload only image files");
            }


            try
            {
                //파일 업로드
                if (param.FileImages != null)
                {
                    newFileInfoList = await Upload.UploadImageAndReturnInfo(param.FileImages, Upload.IMAGE_TYPE_REVIEW);
                }


                if (re != null)
                {
                    if (re.Id != 0)
                    {
                        _context.ReviewTags.RemoveRange(_context.ReviewTags.Where(e => e.ReviewId == re.Id).ToList());

                    }
                    if (re.GolfClubId != null && re.VisitCount == 0)
                    {
                        re.VisitCount = _context.Reviews.Where(r => r.GolfClubId == re.GolfClubId && r.UserId == tokenUserId).Count() + 1;
                    }

                    _context.Reviews.Update(re);
                    await _context.SaveChangesAsync();

                    List<string> tags = HashTag.ParseTag(re.ReviewContent);
                    Dictionary<string, bool> alreadyIn = new Dictionary<string, bool>();

                    foreach (string tag in tags)
                    {
                        string tag2 = tag.ToUpper();
                        if (alreadyIn.ContainsKey(tag2)) continue;

                        Tag targetTag = _context.Tags.Where(e => e.Name == tag2).FirstOrDefault();

                        if (targetTag == null)
                        {
                            int? max = _context.Tags.Max(e => e.Sequence);
                            if (max == null)
                            {
                                max = 0;
                            }

                            targetTag = new Tag
                            {
                                Name = tag2,
                                IsActive = false
                            };

                            _context.Tags.Add(targetTag);
                            await _context.SaveChangesAsync();
                        }

                        ReviewTag newReviewTag = new ReviewTag
                        {
                            ReviewId = re.Id,
                            TagId = targetTag.Id
                        };

                        alreadyIn.Add(tag2, true);

                        _context.ReviewTags.Add(newReviewTag);
                        await _context.SaveChangesAsync();
                    }

                    var dbRefCourseLists = await _context.RefCourseLists.Where(e => e.ReviewId == re.Id).AsNoTracking().ToListAsync();

                    for (int i = 0; i < dbRefCourseLists.Count(); i++)
                    {
                        if (refCourseLists.Where(e => e.Id == dbRefCourseLists[i].Id).FirstOrDefault() == null)
                        {
                            _context.Remove(_context.RefCourseLists.Find(dbRefCourseLists[i].Id));
                        }
                        await _context.SaveChangesAsync();
                    }

                    for (int i = 0; i < refCourseLists.Count; i++)
                    {
                        refCourseLists[i].ReviewId = re.Id;
                        _context.RefCourseLists.Update(refCourseLists[i]);
                        await _context.SaveChangesAsync();
                    }

                    var dbReImgs = await _context.ReviewImages.Where(reImg_ => reImg_.ReviewId == re.Id).AsNoTracking().ToListAsync();

                    for (int i = 0; i < dbReImgs.Count(); i++)
                    {
                        if (reImg.Where(reImg_ => reImg_.Id == dbReImgs[i].Id).FirstOrDefault() == null)
                        {
                            if (dbReImgs[i].Uri != null)
                            {
                                Upload.DeleteImage(dbReImgs[i].Uri, Upload.IMAGE_TYPE_ADMIN);
                            }

                            _context.Remove(_context.ReviewImages.Find(dbReImgs[i].Id));
                        }
                        await _context.SaveChangesAsync();
                    }

                    for (int i = 0; i < reImg.Count; i++)
                    {
                        var dbReImg = await _context.ReviewImages.Where(reImg_ => reImg_.Id == reImg[i].Id).AsNoTracking().SingleOrDefaultAsync();
                        if (reImg[i].ImageIndex != null)
                        {
                            reImg[i].ReviewId = re.Id;

                            if (dbReImg != null && dbReImg.Uri != null)
                            {
                                Upload.DeleteImage(dbReImg.Uri, Upload.IMAGE_TYPE_ADMIN);
                            }

                            var idx = reImg[i].ImageIndex.Value;
                            reImg[i].Name = newFileInfoList[idx].Name;
                            reImg[i].OriginalName = newFileInfoList[idx].OriginalName;
                            reImg[i].Uri = newFileInfoList[idx].Uri;

                            reImg[i].Next = i;

                            _context.ReviewImages.Update(reImg[i]);
                            await _context.SaveChangesAsync();
                        }

                        _context.ReviewImages.Update(reImg[i]);
                        await _context.SaveChangesAsync();
                    }

                    if (param.SkinImage != null && reImg != null)
                    {
                        var k = reImg.Where(r => r.Representative).FirstOrDefault();
                        if (k != null)
                        {
                            await Upload.UploadImage(param.SkinImage, Upload.IMAGE_TYPE_REVIEW, k.Uri.Replace("reviewimages\\", "") + "_skin");
                        }
                    }

                    if (re.GolfClubId != null)
                    {
                        if (re.DCaddy.Any() && re.DCourse.Any() && re.DFacility.Any() && re.DFoodAndDrink.Any())
                        {
                            if (re.DCaddy.Count() >= 10 && re.DCourse.Count() >= 10 && re.DFacility.Count() >= 10 && re.DFoodAndDrink.Count() >= 10)
                            {
                                try
                                {
                                    using (var cmd = _context.Database.GetDbConnection().CreateCommand())
                                    {
                                        cmd.CommandText = "Roulette_Add_Stamp";
                                        cmd.CommandType = CommandType.StoredProcedure;
                                        // set some parameters of the stored procedure
                                        cmd.Parameters.Add(new SqlParameter("@UserId",
                                            SqlDbType.Int)
                                        { Value = tokenUserId });
                                        cmd.Parameters.Add(new SqlParameter("@State",
                                            SqlDbType.VarChar)
                                        { Value = "리뷰" });

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
                            }
                        }
                    }

                    /////////////////////////////////////////////////////////
                    //////////          리뷰 이벤트 코드
                    /////////////////////////////////////////////////////////

                    if (reviewCreated)
                    {
                        return Json(true);
                    }


                    //리뷰 이벤트

                    int eventType = 0;

                    bool reconSt = false;
                    if (re.ReviewContent.Any())
                    {
                        if (re.ReviewContent.Count() >= 10)
                        {
                            reconSt = true;
                        }
                    }

                    if (re.GolfClubId != null)
                    {
                        bool itemSt = false;
                        if (re.DCaddy.Any() && re.DCourse.Any() && re.DFacility.Any() && re.DFoodAndDrink.Any())
                        {
                            if (re.DCaddy.Count() >= 10 && re.DCourse.Count() >= 10 && re.DFacility.Count() >= 10 && re.DFoodAndDrink.Count() >= 10)
                            {
                                itemSt = true;
                            }
                        }
                        eventType = MileageManager.GetEventType(MileageManager.EVENT_TYPE_GOLF, reconSt, itemSt, reImg.Count);
                    }
                    else if (re.ConvenienceId != null)
                    {
                        eventType = MileageManager.GetEventType(MileageManager.EVENT_TYPE_CON, reconSt, false, reImg.Count);
                    }

                    //////////////////
                    if (eventType == 0)
                    {
                        return Json(true);
                    }



                    await MileageManager.SetOneEvent(_context, tokenUserNo, tokenUserId, MileageManager.EVENT_TYPE_FIRST, MileageManager.EVENT_GROUP_REVIEW);

                    Console.WriteLine("리뷰 사용자 아이디 ::: {0}", tokenUserNo);
                    if (await MileageManager.SetEvent(_context, tokenUserNo, tokenUserId, eventType, MileageManager.EVENT_GROUP_REVIEW))
                    {
                        //리뷰이벤트 마일리지 적립 성공
                        Console.WriteLine("리뷰적립!!!");
                    }
                    else
                    {
                        Console.WriteLine("리뷰 적립 싈패");
                    }
                    /////////////////////////////////////////////////////////

                    return Json(true);
                }
                else
                {

                    return Json(false);
                }
            }
            catch (Exception e)
            {
                var pathToSave = @"D:\wwwroot\zxzx\Upload\Temp\test.txt";
                string textValue = e.ToString();
                System.IO.File.WriteAllText(pathToSave, textValue, Encoding.Default);






                Console.WriteLine(e);
                return Json(false);
            }
        }




        [HttpGet("api/[controller]/review/item/delete")]
        public async Task<ActionResult<List<GolfClub>>> CreateReviewDelete(int id)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var review = await _context.Reviews
                .Where(re => re.Id == id && re.UserId == tokenUserId)
                .Include(re => re.ReviewImages)
                .FirstOrDefaultAsync();


            if (review != null)
            {
                for (int i = 0; i < review.ReviewImages.Count(); i++)
                {
                    Upload.DeleteImage(review.ReviewImages.ElementAt(i).Uri, Upload.IMAGE_TYPE_ADMIN);
                }

                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();

                return Ok();
            }
            else
            {
                return NotFound();
            }
        }

        [HttpGet("api/[controller]/review/item")]
        public async Task<ActionResult<List<GolfClub>>> GetReviewItem(int type, int? golfId, int? conId)
        {

            if (type == 0)
            {
                var golfClub = await _context.GolfClubs
                    .Where(gc => gc.Id == golfId)
                    .Include(gc => gc.Courses)
                    .Include(gc => gc.ClubHouse)
                    .ThenInclude(ch => ch.GeoList)
                    .AsNoTracking()
                    .AsSplitQuery()
                    .FirstOrDefaultAsync();

                return Json(golfClub);
            }
            else if (type == 1)
            {
                var convenience = await _context.Conveniences
                    .Where(con => con.Id == conId)
                    .AsNoTracking()
                    .AsSplitQuery()
                    .FirstOrDefaultAsync();

                return Json(convenience);
            }
            else
            {
                return Json(false);
            }
        }


        [HttpGet("api/[controller]/review/get/{id}")]
        public async Task<ActionResult<Review>> GetReview(int id)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            //if (tokenUserId == 0) return Unauthorized();

            var r = _context.Reviews.Where(a => a.Id == id).FirstOrDefault();

            if (r == null)
            {
                return NotFound();
            }

            r.Views += 1;
            await _context.SaveChangesAsync();

            var review = await _context.Reviews
                .Include(re => re.ReviewImages)
                .Include(re => re.GolfClub)
                .ThenInclude(g => g.ClubHouse)
                .ThenInclude(ch => ch.GeoList)
                .Include(re => re.Convenience)
                .Include(re => re.RefCourseLists)
                .Include(re => re.ReviewGoods)
                .Include(re => re.ReviewGrades)
                .Include(re => re.User)
                .ThenInclude(u => u.Setting)
                .Where(re => re.User.Blinded == null)
                .Select((s) => new
                {
                    id = s.Id,
                    golfClub = s.GolfClub,
                    convenience = s.Convenience,
                    user = s.User,
                    reviewContent = s.ReviewContent,
                    playDate = s.PlayDate,
                    score = s.Score,
                    sCourse = s.SCourse,
                    sFacility = s.SFacility,
                    sCaddy = s.SCaddy,
                    sFoodAndDrink = s.SFoodAndDrink,
                    dCourse = s.DCourse,
                    dFacility = s.DFacility,
                    dCaddy = s.DCaddy,
                    dFoodAndDrink = s.DFoodAndDrink,
                    caddyName = s.CaddyName,
                    representation = s.Representation,
                    openState = s.OpenState,
                    state = s.State,
                    views = s.Views,
                    skinType = s.SkinType,
                    //reCourseId = s.ReCourseId,
                    reviewGoods = s.ReviewGoods,
                    reviewGrades = s.ReviewGrades,
                    reviewImages = s.ReviewImages,
                    refCourseLists = s.RefCourseLists,
                    revewUserCount = s.VisitCount,
                    createdAt = s.CreatedAt,
                    code = (s.GolfClub != null && s.GolfClub.ClubHouse != null && s.GolfClub.GeoList != null) ? s.GolfClub.ClubHouse.GeoList.Code : 0,
                })
                .AsNoTracking()
                .AsSingleQuery()
                .SingleOrDefaultAsync(re => re.id  == id);

            return Json(review);
        }



        [HttpGet("api/[controller]/review/get")]
        public async Task<ActionResult<Review>> GetReviewOther(int id, int? golfId, int? conId)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            //if (tokenUserId == 0) return Unauthorized();

            var r = _context.Reviews.Find(id);
            if (r == null) return NotFound();

            if (golfId == null && conId == null)
            {
                var review = await _context.Reviews
                    .Where(re => re.Id != id)
                    .Where(re => re.GolfClubId == null && re.ConvenienceId == null)
                    .Where(re => re.UserId != tokenUserId)
                    .Include(re => re.GolfClub)
                    .Include(re => re.Convenience)
                    .Include(re => re.ReviewImages)
                    .Include(re => re.ReviewGoods)
                    .Include(re => re.User)
                    .ThenInclude(u => u.Setting)
                    .Where(re => re.User.Blinded == null && re.State == 0 && re.OpenState == 1)
                    .Select((s) => new
                    {
                        id = s.Id,
                        golfClub = s.GolfClub,
                        convenience = s.Convenience,
                        user = s.User,
                        reviewContent = s.ReviewContent,
                        playDate = s.PlayDate,
                        score = s.Score,
                        sCourse = s.SCourse,
                        sFacility = s.SFacility,
                        sCaddy = s.SCaddy,
                        sFoodAndDrink = s.SFoodAndDrink,
                        dCourse = s.DCourse,
                        dFacility = s.DFacility,
                        dCaddy = s.DCaddy,
                        dFoodAndDrink = s.DFoodAndDrink,
                        caddyName = s.CaddyName,
                        representation = s.Representation,
                        openState = s.OpenState,
                        state = s.State,
                        views = s.Views,
                        skinType = s.SkinType,
                        //reCourseId = s.ReCourseId,
                        reviewGoods = s.ReviewGoods,
                        reviewImages = s.ReviewImages,
                        GradeAvgScore = _context.ReviewGrades.Where(rg => rg.ReviewId == s.Id).Count() > 0 ? _context.ReviewGrades.Where(rg => rg.ReviewId == s.Id).Average(rg => rg.Score) : 0,
                        revewUserCount = s.VisitCount,
                        createdAt = s.CreatedAt
                    })
                    .OrderByDescending(re => re.reviewGoods.Count(r => r.State == 1))
                    .ThenByDescending(re => re.createdAt)
                    .Take(5)
                    .AsNoTracking()
                    .AsSingleQuery()
                    .ToListAsync();

                return Json(review);
            }
            else
            {
                var review = await _context.Reviews
                    .Where(re => re.Id != id)
                    .Where(re => re.GolfClubId == r.GolfClubId && re.ConvenienceId == r.ConvenienceId)
                    .Where(re => re.UserId != tokenUserId)
                    .Include(re => re.ReviewImages)
                    .Include(re => re.GolfClub)
                    .ThenInclude(g => g.ClubHouse)
                    .ThenInclude(ch => ch.GeoList)
                    .Include(re => re.Convenience)
                    .Include(re => re.ReviewGoods)
                    .Include(re => re.User)
                    .ThenInclude(u => u.Setting)
                    .Where(re => re.User.Blinded == null && re.State == 0 && re.OpenState == 1 && (re.GolfClub != null ? re.GolfClub.Status == 1 : (re.Convenience != null ? re.Convenience.Status == 1 : true)))
                    .Select((s) => new
                    {
                        id = s.Id,
                        golfClub = s.GolfClub,
                        convenience = s.Convenience,
                        user = s.User,
                        reviewContent = s.ReviewContent,
                        playDate = s.PlayDate,
                        score = s.Score,
                        sCourse = s.SCourse,
                        sFacility = s.SFacility,
                        sCaddy = s.SCaddy,
                        sFoodAndDrink = s.SFoodAndDrink,
                        dCourse = s.DCourse,
                        dFacility = s.DFacility,
                        dCaddy = s.DCaddy,
                        dFoodAndDrink = s.DFoodAndDrink,
                        caddyName = s.CaddyName,
                        representation = s.Representation,
                        openState = s.OpenState,
                        state = s.State,
                        views = s.Views,
                        skinType = s.SkinType,
                        //reCourseId = s.ReCourseId,
                        reviewGoods = s.ReviewGoods,
                        reviewImages = s.ReviewImages,
                        GradeAvgScore = _context.ReviewGrades.Where(rg => rg.ReviewId == s.Id).Count() > 0 ? _context.ReviewGrades.Where(rg => rg.ReviewId == s.Id).Average(rg => rg.Score) : 0,
                        revewUserCount = s.VisitCount,
                        createdAt = s.CreatedAt,
                        code = (s.GolfClub != null && s.GolfClub.ClubHouse != null && s.GolfClub.ClubHouse.GeoList != null) ? s.GolfClub.ClubHouse.GeoList.Code : 0,
                    })
                    .OrderByDescending(re => re.sCaddy + re.sCourse + re.sFacility + re.sFoodAndDrink)
                    .ThenByDescending(re => re.revewUserCount)
                    .ThenByDescending(re => re.createdAt)
                    .Take(5)
                    .AsNoTracking()
                    .AsSingleQuery()
                    .ToListAsync();

                return Json(review);
            }
        }

        [HttpGet("api/[controller]/get_filters")]
        public async Task<ActionResult<List<FilterObject>>> GetAll()
        {

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Mappage_Get_Filter";
                cmd.CommandType = CommandType.StoredProcedure;

                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();

                var retObject = new List<Dictionary<string, object>>();
                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var dataRow = new Dictionary<string, object>();

                        for (var iFiled = 0; iFiled < dataReader.FieldCount; iFiled++)
                        {
                            dataRow.Add(
                                dataReader.GetName(iFiled),
                                dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
                            );
                        }

                        retObject.Add(dataRow);
                    }
                }

                return Json(retObject.Select(e => new
                {
                    id = e["Id"],
                    name = e["Name"]
                }));
            }




            return Json(await _context.FilterObjects.ToArrayAsync());
        }

        [HttpGet("api/[controller]/notification")]
        public async Task<ActionResult> GetNotifications(int? id)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            string error = "no result";

            try
            {
                if (id != null)
                {
                    IQueryable<Notification> data = null;

                    data = from notification in _context.Notifications
                                .Include(noti => noti.GolfClub)
                                .Include(noti => noti.Convenience)
                                .Include(noti => noti.EventTemplate)
                                .Include(noti => noti.AuthorUser)
                                .Include(noti => noti.TargetUser)
                                .OrderByDescending(noti => noti.CreatedAt)
                                .AsNoTracking()
                                .AsSplitQuery()
                           where notification.Id < id
                           where notification.TargetUserId == tokenUserId
                           select notification;

                    if (data == null)
                    {
                        return Json(PaginatedList<int>.nothing(error));
                    }

                    PaginatedList<Notification> _notifications = await PaginatedList<Notification>.CreateAsync(data, 1, 10);


                    return Json(_notifications.Select(no => new
                    {

                        no.Id,
                        Author = new
                        {
                            Id = no.AuthorUserId,
                            Name = no.AuthorUserId != null ? no.AuthorUser.Fullname : null,
                        },
                        Target = new
                        {
                            Id = no.TargetUserId,
                            Name = no.TargetUserId != null ? no.TargetUser.Fullname : null,
                        },
                        no.GolfClub,
                        no.Convenience,
                        no.EventTemplate,
                        no.Contents,
                        no.Type,
                        no.IsRead,
                        no.CreatedAt,
                        no.ReviewId
                    }));
                }
                else
                {
                    var notifications = _context.Notifications
                        .Where(noti => noti.TargetUserId == tokenUserId)
                        .Include(noti => noti.GolfClub)
                        .Include(noti => noti.Convenience)
                        .Include(noti => noti.EventTemplate)
                        .Include(noti => noti.AuthorUser)
                        .Include(noti => noti.TargetUser)
                        .OrderByDescending(noti => noti.CreatedAt)
                        .AsNoTracking()
                        .AsSplitQuery()
                        .Take(10)
                        .ToList();

                    return Json(notifications.Select(no => new
                    {
                        no.Id,
                        Author = new
                        {
                            Id = no.AuthorUserId,
                            Name = no.AuthorUserId != null ? no.AuthorUser.Fullname : null,
                        },
                        Target = new
                        {
                            Id = no.TargetUserId,
                            Name = no.TargetUserId != null ? no.TargetUser.Fullname : null,
                        },
                        no.GolfClub,
                        no.Convenience,
                        no.EventTemplate,
                        no.Contents,
                        no.Type,
                        no.IsRead,
                        no.CreatedAt,
                        no.ReviewId
                    }));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Json(false);
            }


            //var notifications = from notification in _context.Notifications
            //        .Include(noti => noti.GolfClub)
            //        .Include(noti => noti.Convenience)
            //        .Include(noti => noti.EventTemplate)
            //        .Include(noti => noti.AuthorUser)
            //        .Include(noti => noti.TargetUser)
            //        .OrderByDescending(noti => noti.CreatedAt)
            //        .AsNoTracking()
            //    where notification.TargetUserId == tokenUserId
            //    select new
            //    {
            //        notification.Id,
            //        Author = new
            //        {
            //            Id = notification.AuthorUserId,
            //            Name = notification.AuthorUserId != null ? notification.AuthorUser.Fullname : null,
            //        },
            //        Target = new
            //        {
            //            Id = notification.TargetUserId,
            //            Name = notification.TargetUserId != null ? notification.TargetUser.Fullname : null,
            //        },
            //        notification.GolfClub,
            //        notification.Convenience,
            //        notification.EventTemplate,
            //        notification.Contents,
            //        notification.Type,
            //        notification.IsRead,
            //        notification.CreatedAt
            //    };
            //try
            //{
            //    return Json(await notifications.ToListAsync());
            //}
            //catch (Exception e)
            //{
            //    return Json(false);
            //}
        }

        [HttpGet("api/[controller]/notification/count")]
        public async Task<ActionResult> GetNotificationCount()
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            var _notificationCount = await _context.Notifications.Where(n => n.TargetUserId == tokenUserId).Where(n => !n.IsRead).CountAsync();

            //var _notificationCount = (from notification in _context.Notifications
            //                         where notification.TargetUserId == tokenUserId
            //                         where notification.IsRead == false
            //                         select notification).Count();

            return Json(_notificationCount);
        }

        [HttpGet("api/[controller]/notification/read/{notificationId}")]
        public async Task<ActionResult> NotificationRead(int notificationId)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            try
            {
                Notification notification = _context.Notifications
                    .Where(no => no.TargetUserId == tokenUserId)
                    .Where(no => no.Id == notificationId)
                    .FirstOrDefault();
                //Find(notificationId);


                if (notification != null)
                {
                    notification.IsRead = true;
                }

                _context.Notifications.Update(notification);

                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
            }

            return Ok();
        }

        [HttpGet("api/[controller]/mypage/visit")]
        public async Task<ActionResult> ViewMypageVisit()
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            ////int testUserId = 1793569;

            ////var s = from golfClub in _context.GolfClubs.Include(g => g.GeoList)
            ////        //join geoList in _context.GeoLists on golfClub.GeoListId equals geoList.Id
            ////        join review in _context.Reviews on golfClub.Id equals review.GolfClubId
            ////        group golfClub by golfClub.GeoList.Code into keyGroup
            ////        select keyGroup;
            //Console.WriteLine(tokenUserId);
            //tokenUserId = id;
            //Console.WriteLine(tokenUserId);

            List<dynamic> v = new List<dynamic>();
            List<dynamic> b = new List<dynamic>();

            v = GzApi.GetVisit(_context, "Mypage_Field_Visit", tokenUserId);
            b = GzApi.GetVisit(_context, "Mypage_Screen_Visit", tokenUserId);

            return Json(new { field = v, screen = b });

            //using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            //{
            //    cmd.CommandText = "Mypage_Visit";
            //    cmd.CommandType = CommandType.StoredProcedure;
            //    // set some parameters of the stored procedure
            //    cmd.Parameters.Add(new SqlParameter("@P_USER_ID",
            //        SqlDbType.TinyInt)
            //    { Value = tokenUserId });

            //    if (cmd.Connection.State != ConnectionState.Open)
            //        cmd.Connection.Open();

            //    var retObject = new List<dynamic>();
            //    using (var dataReader = await cmd.ExecuteReaderAsync())
            //    {
            //        while (await dataReader.ReadAsync())
            //        {
            //            var dataRow = new ExpandoObject() as IDictionary<string, object>;
            //            for (var iFiled = 0; iFiled < dataReader.FieldCount; iFiled++)
            //            {
            //                // one can modify the next line to
            //                //   if (dataReader.IsDBNull(iFiled))
            //                //       dataRow.Add(dataReader.GetName(iFiled), dataReader[iFiled]);
            //                // if one want don't fill the property for NULL
            //                // returned from the database
            //                dataRow.Add(
            //                    dataReader.GetName(iFiled),
            //                    dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
            //                );
            //            }

            //            retObject.Add((ExpandoObject)dataRow);
            //        }
            //    }

            //    v = retObject;
            //    //return Json(retObject);
            //}


            //var v = _context.GolfClubs
            //    .Include(g => g.GeoList)
            //    .Include(g => g.Reviews.Where(r => r.UserId == tokenUserId))
            //    .Select(g => new
            //    {
            //        code = g.GeoListId != null ? g.GeoList.Code : 0,
            //        g.Id,
            //        isVisit = g.Reviews.Count > 0 ? true : false,
            //    })
            //    .Where(g => g.code > 0)
            //    .AsEnumerable()
            //    .GroupBy(g => g.code)
            //    .Select(_g => new
            //    {
            //        code = _g.First()?.code,
            //        count = _g.Count(),
            //        visitCount = _g.Count(c => c.isVisit),
            //    })
            //    .ToList();

            //List<GzApi.LifeBest> ccPlay = GzApi.GetLifeBest(tokenUserId);

            //var golfClubs = await _context.GolfClubs
            //    .Include(gc => gc.GeoList)
            //    .Where(gc => gc.IsScreen)
            //    .AsNoTracking()
            //    .AsSplitQuery()
            //    .ToListAsync();

            //var reScreen = golfClubs
            //    .GroupBy(gc => gc.GeoListId != null ? gc.GeoList.Code : 0)
            //    .Select(gc => new
            //    {
            //        Code = gc.Key,
            //        Count = gc.Count()
            //    });

            //var screenCnt = from _ccPlay in ccPlay
            //                join golfClub in golfClubs on _ccPlay.CiCode.ToString() equals golfClub.GcNum
            //                group _ccPlay by golfClub.GeoListId != null ? golfClub.GeoList.Code : 0 into newCCPlay
            //                select new
            //                {
            //                    Code = newCCPlay.Key,
            //                    Count = newCCPlay.Count(),
            //                };


            ////var visitCount = s.ToList();

            //return Json(new { field = v, screen = new { play = screenCnt, all = reScreen } });
        }

        [HttpGet("api/[controller]/mypage")]
        public async Task<ActionResult> ViewMypage()
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            var user = await _context.Users
                .Where(u => u.Id == tokenUserId)
                .Include(u => u.Setting)
                .ThenInclude(st => st.SettingFilters)
                .Select(u=> new {
                    nickname = u.Nickname,
                    profileImage = u.ProfileImage,
                    point = u.Point,
                    Setting = new 
                    {
                        Id = u.Setting.Id,
                        UserId = tokenUserId,
                        PrivateAgree = u.Setting.PrivateAgree,
                        FriendReview = u.Setting.FriendReview,
                        ReviewLike = u.Setting.ReviewLike,
                        NewEvent = u.Setting.NewEvent,
                        GolfclubName = u.Setting.GolfclubName,
                        Weather = u.Setting.Weather,
                        ScreenIcon = u.Setting.ScreenIcon,
                        FieldIcon = u.Setting.FieldIcon,
                        PremiumIcon = u.Setting.PremiumIcon,
                        settingFilters = u.Setting.SettingFilters.Select(s => new { s.Id, s.SettingId, s.FilterObjectId })
                    },
                })
                .FirstOrDefaultAsync();

            //using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            //{
            //    cmd.CommandText = "Mappage_Get_Filter";
            //    cmd.CommandType = CommandType.StoredProcedure;

            //    if (cmd.Connection.State != ConnectionState.Open)
            //        cmd.Connection.Open();

            //    var retObject = new List<Dictionary<string, object>>();
            //    using (var dataReader = cmd.ExecuteReader())
            //    {
            //        while (dataReader.Read())
            //        {
            //            var dataRow = new Dictionary<string, object>();

            //            for (var iFiled = 0; iFiled < dataReader.FieldCount; iFiled++)
            //            {
            //                dataRow.Add(
            //                    dataReader.GetName(iFiled),
            //                    dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
            //                );
            //            }

            //            retObject.Add(dataRow);
            //        }
            //    }

            //    return Json(retObject.Select(e => new
            //    {
            //        id = e["id"],
            //        fieldIcon = e["holeCount"],
            //        friendReview = e["courseCount"],
            //        golfclubName = e["filterObjectIdList"].ToString() == "" ? new List<int>() : e["filterObjectIdList"].ToString().Split(",").Select(e => int.Parse(e)).ToList(),
            //        newEvent = e["filterName"],
            //        premiumIcon = e["filterName"],
            //        privateAgree = e["filterName"],
            //        reviewLike = e["filterName"],
            //        screenIcon = e["filterName"],
            //        userId = e["filterName"],
            //        weather = e["filterName"],
            //        settingFilters = new
            //        {
            //            id = e["id"],
            //            settingId = e["golfClub.address"],
            //            filterObjectId = e["golfClub.grade"]
            //        }
            //    }));
            //}

            return Json(user);
        }

        [HttpPost("api/[controller]/mypage/update")]
        public async Task<ActionResult> UpdateSetting(Setting st)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                st.UserId = tokenUserId;

                _context.Settings.Update(st);
                await _context.SaveChangesAsync();
              
                return Json(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Json(false);
            }
        }

        [HttpPost("api/[controller]/mypage/updatefilter")]
        public async Task<ActionResult> UpdateSettingFilter(SettingFilterDto st)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                if (st.Id == null)
                {
                    var dbSt = await _context.SettingFilters
                        .Include(s => s.Setting)
                        .Where(s => s.Setting.UserId == tokenUserId)
                        .Where(s => s.SettingId == st.SettingId)
                        .Where(s => s.FilterObjectId == st.FilterObjectId)
                        .FirstOrDefaultAsync();

                    if (dbSt != null)
                    {
                        return Json(false);
                    }

                    var paramSt = new SettingFilter
                    {
                        SettingId = st.SettingId,
                        FilterObjectId = st.FilterObjectId
                    };

                    _context.SettingFilters.Add(paramSt);
                    await _context.SaveChangesAsync();


                    return Json(paramSt.Id);
                }
                else
                {
                    var dbSt = await _context.SettingFilters
                        .Include(s => s.Setting)
                        .Where(s => s.Setting.UserId == tokenUserId)
                        .Where(s => s.Id == st.Id)
                        .FirstOrDefaultAsync();

                    if (dbSt != null)
                    {
                        _context.SettingFilters.Remove(dbSt);
                        await _context.SaveChangesAsync();

                        return Json(true);
                    }
                    else
                    {
                        return Json(false);
                    }
                }


                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Json(false);
            }
        }

        [HttpGet("api/[controller]/other_mypage/user/{userId}")]
        public async Task<ActionResult> GetOtherReviews(int userId)
        {
            var user = _context.Users.SingleOrDefault(u => u.Id == userId);

            return Json(user);
        }

        [HttpGet("api/[controller]/other_mypage/reviews/{userId}")]
        public async Task<ActionResult> GetOtherReviews(int userId, int? id, int type = 1)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            string error = "no result";
            List<int> blackUsers = _context.ReviewBlackUsers.Where(e => e.UserId == tokenUserId).ToList().Select(b => b.BlackUserId).ToList();

            if (id != null)
            {
                IQueryable<Review> data = null;

                data =
                    from review in _context.Reviews
                                                        .Include(re => re.GolfClub)
                                                        .Include(re => re.Convenience)
                                                        .Include(re => re.ReviewGoods)
                                                        .ThenInclude(reg => reg.User)
                                                        .Include(re => re.ReviewGrades)
                                                        .Include(re => re.ReviewImages)
                                                        .Include(re => re.User)
                                                        .ThenInclude(u => u.Setting)
                                                        .OrderByDescending(re => re.CreatedAt)
                                                        .AsNoTracking()
                                                        .AsSplitQuery()
                        //join a in _context.Reviews
                    where review.Id < id && review.OpenState == 1
                    where review.UserId == userId && review.User.Blinded == null
                    select review;

                if (type == 1)
                {
                    //작성일순
                    data = data.OrderByDescending(re => re.CreatedAt);
                }
                else if (type == 2)
                {
                    //플레이날짜순
                    data = data.OrderByDescending(re => re.PlayDate);
                }
                else if (type == 3)
                {
                    //최저타순
                    data = data.OrderBy(re => re.Score);
                }


                if (data == null)
                {
                    return Json(PaginatedList<int>.nothing(error));
                }

                PaginatedList<Review> _reviews = await PaginatedList<Review>.CreateAsync(data, 1, 10);

                return Json(_reviews.Select(s => new
                {
                    id = s.Id,
                    golfClubId = s.GolfClubId,
                    convenienceId = s.ConvenienceId,
                    name = s.GolfClubId != null ? s.GolfClub.Name : (s.ConvenienceId != null ? s.Convenience.Name : ""),
                    type = s.GolfClubId != null ? 'g' : (s.ConvenienceId != null ? 'c' : 'n'),
                    userId = s.User.Id,
                    userName = s.User.Username,
                    nickname = s.User.Nickname,
                    reviewContent = s.ReviewContent,
                    playDate = s.PlayDate,
                    score = s.Score,
                    sCourse = s.SCourse,
                    sFacility = s.SFacility,
                    sCaddy = s.SCaddy,
                    sFoodAndDrink = s.SFoodAndDrink,
                    dCourse = s.DCourse,
                    dFacility = s.DFacility,
                    dCaddy = s.DCaddy,
                    dFoodAndDrink = s.DFoodAndDrink,
                    caddyName = s.CaddyName,
                    representation = s.Representation,
                    openState = s.OpenState,
                    state = s.State,
                    views = s.Views,
                    skinType = s.SkinType,
                    //reCourseId = s.ReCourseId,
                    reviewGrades = s.ReviewGrades.Select(reg => new {
                        reg.Id,
                        reg.ReviewId,
                        reg.UserId,
                        reg.Score
                    }),
                    reviewGoods = s.ReviewGoods.Select(rg => new
                    {
                        rg.Id,
                        rg.ReviewId,
                        rg.User,
                        rg.UserId,
                        rg.State
                    }),
                    reviewImages = s.ReviewImages.Select(rei => new {
                        rei.Id,
                        rei.Uri
                    }),
                    revewUserCount =
                    s.GolfClubId != null
                    ? _context.Reviews.Count(re => re.GolfClubId == s.GolfClubId && re.UserId == userId)
                    : _context.Reviews.Count(re => re.ConvenienceId == s.ConvenienceId && re.UserId == userId),
                    createdAt = s.CreatedAt
                }
                ));
            }
            else
            {
                IQueryable<Review> reviews;


                reviews = _context.Reviews
                .OrderByDescending(re => re.Id)
                .Include(re => re.GolfClub)
                .Include(re => re.Convenience)
                .Include(re => re.ReviewGrades)
                .Include(re => re.ReviewGoods)
                .ThenInclude(reg => reg.User)
                .Include(re => re.ReviewImages)
                .Include(re => re.User)
                .Where(re => re.UserId == userId && re.User.Blinded == null && re.OpenState == 1)
                .Take(10)
                .AsNoTracking()
                .AsSingleQuery();

                if (type == 1)
                {
                    //작성일순
                    reviews = reviews.OrderByDescending(re => re.CreatedAt);
                }
                else if (type == 2)
                {
                    //플레이날짜순
                    reviews = reviews.OrderByDescending(re => re.PlayDate);
                }
                else if (type == 3)
                {
                    //최저타순
                    reviews = reviews.OrderBy(re => re.Score);
                }

                return Json(reviews.Select(s => new
                {
                    id = s.Id,
                    golfClubId = s.GolfClubId,
                    convenienceId = s.ConvenienceId,
                    name = s.GolfClubId != null ? s.GolfClub.Name : (s.ConvenienceId != null ? s.Convenience.Name : ""),
                    type = s.GolfClubId != null ? 'g' : (s.ConvenienceId != null ? 'c' : 'n'),
                    userId = s.User.Id,
                    userName = s.User.Username,
                    nickname = s.User.Nickname,
                    reviewContent = s.ReviewContent,
                    playDate = s.PlayDate,
                    score = s.Score,
                    sCourse = s.SCourse,
                    sFacility = s.SFacility,
                    sCaddy = s.SCaddy,
                    sFoodAndDrink = s.SFoodAndDrink,
                    dCourse = s.DCourse,
                    dFacility = s.DFacility,
                    dCaddy = s.DCaddy,
                    dFoodAndDrink = s.DFoodAndDrink,
                    caddyName = s.CaddyName,
                    representation = s.Representation,
                    openState = s.OpenState,
                    state = s.State,
                    views = s.Views,
                    skinType = s.SkinType,
                    //reCourseId = s.ReCourseId,
                    reviewGrades = s.ReviewGrades.Select(reg => new {
                        reg.Id,
                        reg.ReviewId,
                        reg.UserId,
                        reg.Score
                    }),
                    reviewGoods = s.ReviewGoods.Select(rg => new
                    {
                        rg.Id,
                        rg.ReviewId,
                        rg.User,
                        rg.UserId,
                        rg.State
                    }),
                    reviewImages = s.ReviewImages.Select(rei => new {
                        rei.Id,
                        rei.Uri
                    }),
                    revewUserCount = s.GolfClubId != null ? _context.Reviews.Count(re => re.GolfClubId == s.GolfClubId && re.UserId == userId)
                    : (s.ConvenienceId != null ? _context.Reviews.Count(re => re.ConvenienceId == s.ConvenienceId && re.UserId == userId) : 0),
                    createdAt = s.CreatedAt
                }
                )); ;
            }
        }

        [HttpGet("api/[controller]/mypage/reviews")]
        public async Task<ActionResult> GetMyReviews(int? id, int type = 1)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            string error = "no result";
            List<int> blackUsers = _context.ReviewBlackUsers.Where(e => e.UserId == tokenUserId).ToList().Select(b => b.BlackUserId).ToList();

            if (id != null)
            {
                IQueryable<Review> data = null;

                data =
                    from review in _context.Reviews
                                                        .Include(re => re.GolfClub)
                                                        .Include(re => re.Convenience)
                                                        .Include(re => re.ReviewGoods)
                                                        .ThenInclude(reg => reg.User)
                                                        .Include(re => re.ReviewGrades)
                                                        .Include(re => re.ReviewImages)
                                                        .Include(re => re.User)
                                                        .OrderByDescending(re => re.CreatedAt)
                                                        .AsNoTracking()
                                                        .AsSplitQuery()
                        //join a in _context.Reviews
                    where review.Id < id
                    where review.UserId == tokenUserId && review.User.Blinded == null
                    select review;

                if (type == 1)
                {
                    //작성일순
                    data = data.OrderByDescending(re => re.CreatedAt);
                }
                else if (type == 2)
                {
                    //플레이날짜순
                    data = data.OrderByDescending(re => re.PlayDate);
                }
                else if (type == 3)
                {
                    //최저타순
                    data = data.OrderBy(re => re.Score);
                }
                

                if (data == null)
                {
                    return Json(PaginatedList<int>.nothing(error));
                }

                PaginatedList<Review> _reviews = await PaginatedList<Review>.CreateAsync(data, 1, 10);

                return Json(_reviews.Select(s => new
                {
                    id = s.Id,
                    golfClubId = s.GolfClubId,
                    convenienceId = s.ConvenienceId,
                    name = s.GolfClubId != null ? s.GolfClub.Name : (s.ConvenienceId != null ? s.Convenience.Name : ""),
                    type = s.GolfClubId != null ? 'g' : (s.ConvenienceId != null ? 'c' : 'n'),
                    userId = s.User.Id,
                    userName = s.User.Username,
                    nickname = s.User.Nickname,
                    reviewContent = s.ReviewContent,
                    playDate = s.PlayDate,
                    score = s.Score,
                    sCourse = s.SCourse,
                    sFacility = s.SFacility,
                    sCaddy = s.SCaddy,
                    sFoodAndDrink = s.SFoodAndDrink,
                    dCourse = s.DCourse,
                    dFacility = s.DFacility,
                    dCaddy = s.DCaddy,
                    dFoodAndDrink = s.DFoodAndDrink,
                    caddyName = s.CaddyName,
                    representation = s.Representation,
                    openState = s.OpenState,
                    state = s.State,
                    views = s.Views,
                    skinType = s.SkinType,
                    //reCourseId = s.ReCourseId,
                    reviewGrades = s.ReviewGrades.Select(reg => new {
                        reg.Id,
                        reg.ReviewId,
                        reg.UserId,
                        reg.Score
                    }),
                    reviewGoods = s.ReviewGoods.Select(rg => new
                    {
                        rg.Id,
                        rg.ReviewId,
                        rg.User,
                        rg.UserId,
                        rg.State
                    }),
                    reviewImages = s.ReviewImages.Select(rei => new {
                        rei.Id,
                        rei.Uri
                    }),
                    revewUserCount =
                    s.GolfClubId != null
                    ? _context.Reviews.Count(re => re.GolfClubId == s.GolfClubId && re.UserId == tokenUserId)
                    : _context.Reviews.Count(re => re.ConvenienceId == s.ConvenienceId && re.UserId == tokenUserId),
                    createdAt = s.CreatedAt
                }
                ));
            }
            else
            {
                IQueryable<Review> reviews;

            
                reviews = _context.Reviews
                .OrderByDescending(re => re.Id)
                .Include(re => re.GolfClub)
                .Include(re => re.Convenience)
                .Include(re => re.ReviewGrades)
                .Include(re => re.ReviewGoods)
                .ThenInclude(reg => reg.User)
                .Include(re => re.ReviewImages)
                .Include(re => re.User)
                .Where(re => re.UserId == tokenUserId && re.User.Blinded == null)
                .Take(10)
                .AsNoTracking()
                .AsSingleQuery();

                if (type == 1)
                {
                    //작성일순
                    reviews = reviews.OrderByDescending(re => re.CreatedAt);
                }
                else if (type == 2)
                {
                    //플레이날짜순
                    reviews = reviews.OrderByDescending(re => re.PlayDate);
                }
                else if (type == 3)
                {
                    //최저타순
                    reviews = reviews.OrderBy(re => re.Score);
                }

                return Json(reviews.Select(s => new
                {
                    id = s.Id,
                    golfClubId = s.GolfClubId,
                    convenienceId = s.ConvenienceId,
                    name = s.GolfClubId != null ? s.GolfClub.Name : (s.ConvenienceId != null ? s.Convenience.Name : ""),
                    type = s.GolfClubId != null ? 'g' : (s.ConvenienceId != null ? 'c' : 'n'),
                    userId = s.User.Id,
                    userName = s.User.Username,
                    nickname = s.User.Nickname,
                    reviewContent = s.ReviewContent,
                    playDate = s.PlayDate,
                    score = s.Score,
                    sCourse = s.SCourse,
                    sFacility = s.SFacility,
                    sCaddy = s.SCaddy,
                    sFoodAndDrink = s.SFoodAndDrink,
                    dCourse = s.DCourse,
                    dFacility = s.DFacility,
                    dCaddy = s.DCaddy,
                    dFoodAndDrink = s.DFoodAndDrink,
                    caddyName = s.CaddyName,
                    representation = s.Representation,
                    openState = s.OpenState,
                    state = s.State,
                    views = s.Views,
                    skinType = s.SkinType,
                    //reCourseId = s.ReCourseId,
                    reviewGrades = s.ReviewGrades.Select(reg => new {
                        reg.Id,
                        reg.ReviewId,
                        reg.UserId,
                        reg.Score
                    }),
                    reviewGoods = s.ReviewGoods.Select(rg => new
                    {
                        rg.Id,
                        rg.ReviewId,
                        rg.User,
                        rg.UserId,
                        rg.State
                    }),
                    reviewImages = s.ReviewImages.Select(rei => new {
                        rei.Id,
                        rei.Uri
                    }),
                    revewUserCount = s.GolfClubId != null ? _context.Reviews.Count(re => re.GolfClubId == s.GolfClubId && re.UserId == tokenUserId)
                    : (s.ConvenienceId != null ? _context.Reviews.Count(re => re.ConvenienceId == s.ConvenienceId && re.UserId == tokenUserId) : 0),
                    createdAt = s.CreatedAt
                }
                )); ;
            }
        }

        [HttpGet("api/[controller]/mypage/history")]
        public async Task<ActionResult> GetUserGolfSearchHistory(int? id)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            string error = "no result";

            if (id != null)
            {
                IQueryable<GolfClubSearchHistory> data = null;

                data =
                       from history in _context.GolfClubSearchHistories
                                                           .Include(hi => hi.GolfClub)
                                                           .OrderByDescending(hi => hi.CreatedAt)
                       where history.Id < id
                       where history.UserId == tokenUserId
                       select history;


                if (data == null)
                {
                    return Json(PaginatedList<int>.nothing(error));
                }

                PaginatedList<GolfClubSearchHistory> _history = await PaginatedList<GolfClubSearchHistory>.CreateAsync(data, 1, 10);


                return Json(_history.Select(hi => new
                {
                    hi.Id,
                    hi.GolfClubId,
                    hi.GolfClub.Address,
                    hi.GolfClub.TFileUri,
                    hi.GolfClub.Name,
                    Favorite = _context.GolfFavorites.Where(gf => gf.GolfClubId == hi.GolfClubId).Where(gf => gf.UserId == tokenUserId).FirstOrDefault(),
                    hi.CreatedAt
                }));
            }
            else
            {
                var history = await _context.GolfClubSearchHistories
                                                    .Where(hi => hi.UserId == tokenUserId)
                                                    .Include(hi => hi.GolfClub)
                                                    .OrderByDescending(hi => hi.CreatedAt)
                                                    .Take(10)
                                                    .ToListAsync();

                return Json(history.Select(hi => new
                {
                    hi.Id,
                    hi.GolfClubId,
                    hi.GolfClub.Address,
                    hi.GolfClub.TFileUri,
                    hi.GolfClub.Name,
                    Favorite = _context.GolfFavorites.Where(gf => gf.GolfClubId == hi.GolfClubId).Where(gf => gf.UserId == tokenUserId).FirstOrDefault(),
                    hi.CreatedAt
                }));
            }
        }

        [HttpGet("api/[controller]/mypage/history_sp")]
        public async Task<ActionResult> GetUserGolfSearchHistorySP(int page = 1)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            string error = "no result";

            if (page < 1)
            {
                page = 1;
            }

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Mypage_Search_Histories";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_USER_ID",
                    SqlDbType.Int)
                { Value = tokenUserId });
                cmd.Parameters.Add(new SqlParameter("@P_PAGE",
                    SqlDbType.TinyInt)
                { Value = page });

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

                return Json(retObject);
            }

            //return Json(GzApi.GetSingleParamSP(_context, "Mypage_Search_Histories", "@P_USER_ID", tokenUserId));
        }

        [HttpGet("api/[controller]/mypage/history/delete")]
        public async Task<ActionResult> GetUserGolfSearchHistoryDelete(int? id)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            try
            {
                if (id == null) 
                {
                    var history = await _context.GolfClubSearchHistories
                        .Where(hi => hi.UserId == tokenUserId)
                        .ToListAsync();

                    if (history.Count <= 0) 
                    {
                        return Json(false);
                    }

                    _context.GolfClubSearchHistories.RemoveRange(history);
                    await _context.SaveChangesAsync();

                    return Json(true);
                }
                else
                {
                    var histories = from h in _context.GolfClubSearchHistories
                                    where h.GolfClubId == id && h.UserId == tokenUserId
                                    select h;

                    _context.GolfClubSearchHistories.RemoveRange(histories);
                    await _context.SaveChangesAsync();

                    //var history = await _context.GolfClubSearchHistories
                    //    .Where(hi => hi.UserId == tokenUserId)
                    //    .Where(hi => hi.Id == id)
                    //    .FirstOrDefaultAsync();

                    //if (history == null)
                    //{
                    //    return Json(false);
                    //}

                    //_context.GolfClubSearchHistories.Remove(history);
                    //await _context.SaveChangesAsync();

                    return Json(true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Json(false);
            }
        }

        [HttpGet("api/[controller]/filter")]
        public async Task<ActionResult> GetFilter()
        {
            var filterObjects = await _context.FilterObjects
                .Select(f => new
                {
                    f.Id,
                    f.Name,
                }).ToListAsync();

            return Json(filterObjects);
        }

        [HttpGet("api/[controller]/local/info")]
        public async Task<ActionResult> GetLocalInfo(int code = 0)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Mypage_Local_Info";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_USER_ID",
                    SqlDbType.Int)
                { Value = tokenUserId });
                cmd.Parameters.Add(new SqlParameter("@P_CODE",
                    SqlDbType.TinyInt)
                { Value = code });

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
                            // one can modify the next line to
                            //   if (dataReader.IsDBNull(iFiled))
                            //       dataRow.Add(dataReader.GetName(iFiled), dataReader[iFiled]);
                            // if one want don't fill the property for NULL
                            // returned from the database
                            dataRow.Add(
                                dataReader.GetName(iFiled),
                                dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
                            );
                        }

                        retObject.Add((ExpandoObject)dataRow);
                    }
                }

                return Json(retObject.FirstOrDefault());
            }

            return Unauthorized();
        }

        [HttpGet("api/[controller]/local/golfclub/info")]
        public async Task<ActionResult> GetLocalGolfClubInfo(int golfClubId = 0)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            int tokenUserNo = JwtManager.getJwtNo(_httpContext);
            if (tokenUserId == 0 || tokenUserNo == 0) return Unauthorized();

            if (golfClubId < 1)
            {
                return BadRequest();
            }

            //int golfClub = (from g in _context.GolfClubs
            //                where g.Id == golfClubId && g.Status == 1
            //                select g.Id).FirstOrDefault();

            var golfClub = _context.GolfClubs
                .Where(gc => gc.Id == golfClubId)
                .Where(gc => gc.Status == 1)
                .FirstOrDefault();

            if (golfClub == null)
            {
                return BadRequest();
            }

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Mypage_GolfClub_Info";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_USER_ID",
                    SqlDbType.Int)
                { Value = tokenUserId });
                cmd.Parameters.Add(new SqlParameter("@P_GOLFCLUB_ID",
                    SqlDbType.Int)
                { Value = golfClubId });

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
                            // one can modify the next line to
                            //   if (dataReader.IsDBNull(iFiled))
                            //       dataRow.Add(dataReader.GetName(iFiled), dataReader[iFiled]);
                            // if one want don't fill the property for NULL
                            // returned from the database
                            dataRow.Add(
                                dataReader.GetName(iFiled),
                                dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
                            );
                        }

                        retObject.Add((ExpandoObject)dataRow);
                    }
                }

                return Json(retObject.FirstOrDefault());
            }

            return BadRequest();
        }

        [HttpGet("api/[controller]/local/field")]
        public async Task<ActionResult> GetLocalField(int code = 0)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            //int tokenUserId2 = 1793569;

            var reviews = (from review in _context.Reviews
                          where review.UserId == tokenUserId
                          join golfClub in _context.GolfClubs on review.GolfClubId equals golfClub.Id
                          where golfClub.IsField
                          join clubHouse in _context.ClubHouses on golfClub.Id equals clubHouse.GolfClubId
                          join geo in _context.GeoLists on clubHouse.GeoListId equals geo.Id into geoList
                          from geo in geoList.DefaultIfEmpty()
                          where code != 0 ? (geo == null ? false : geo.Code == code) : true
                           select new
                          {
                              review.Score,
                              GolfClubId = golfClub.Id
                          }).ToList();

            //Console.WriteLine(reviews.ToQueryString());
            //return Json(reviews);

            var fieldCount = (from golfClub in _context.GolfClubs
                            where golfClub.IsField
                            join clubHouse in _context.ClubHouses on golfClub.Id equals clubHouse.GolfClubId
                            join geo in _context.GeoLists on clubHouse.GeoListId equals geo.Id into geoList
                            from geo in geoList
                            where code != 0 ? (geo == null ? false : geo.Code == code) : true
                            select golfClub.Id).Count();

            //var golfClubs




            //var reviews = await _context.Reviews
            //     .Where(re => re.UserId == tokenUserId)
            //     .Include(re => re.GolfClub)
            //     .ThenInclude(gc => gc.GeoList)
            //     .Where(re => re.GolfClub.IsField == true)
            //     .Where(re => code != 0 ? re.GolfClub.GeoList.Code == code : true)
            //     .AsNoTracking()
            //     .AsSplitQuery()
            //     .ToListAsync();

            //var reReviews = await _context.GolfClubs
            //    .Include(gc => gc.GeoList)
            //    .Where(gc => code != 0 ? gc.GeoList.Code == code : true)
            //    .AsNoTracking()
            //    .AsSplitQuery()
            //    .ToListAsync();

            //var fieldCnt = reviews.GroupBy(re => re.GolfClubId)
            //    .Select(gg => new
            //    {
            //        Count = gg.Count(),
            //    });


            ////List<GzApi.LifeBest> ccPlay = GzApi.GetLifeBest(1796019);
            //List<GzApi.LifeBest> ccPlay = GzApi.GetLifeBest(tokenUserId);

            //var reScreen = await _context.GolfClubs
            //    .Include(gc => gc.GeoList)
            //    .Where(gc => gc.IsScreen)
            //    .Where(gc => code != 0 ? gc.GeoList.Code == code : true)
            //    .AsNoTracking()
            //    .AsSplitQuery()
            //    .ToListAsync();


            //var screenCnt = ccPlay.GroupBy(cc => cc.CiCode)
            //    .Select(gg => new
            //    {
            //        Key = gg.Key,
            //        Count = gg.Count(),
            //    });

            //var codeCount = 0;
            //var golfClubs = _context.GolfClubs.Include(gc => gc.GeoList).ToArray();
            //for (int i = 0;i < screenCnt.Count();i++)
            //{
            //    codeCount += golfClubs.Where(gc => gc.GcNum != null ? gc.GcNum.Equals(screenCnt.ElementAt(i).Key.ToString()) : false).Where(gc => code != 0 ? (gc.GeoListId != null ? gc.GeoList.Code == code : false) : true).Count();
            //}

            var notvisitCount = from golfClub in _context.GolfClubs
                               join review in _context.Reviews on golfClub.Id equals review.GolfClubId into rg
                               from review in rg.DefaultIfEmpty()
                               where review == null
                               select golfClub.Id;

            var data = new
            {
                FieldNotVisitCount = notvisitCount.GroupBy(n => n).Count(),
                FieldBestScore = reviews.Any() ? reviews.Min(re => re.Score) : 0,
                FieldAvgScore = reviews.Any() ? reviews.Average(re => re.Score) : 0,
                FieldVisitCnt = reviews.GroupBy(r => r.GolfClubId).Count(),
                FieldCnt = reviews.Count,
                FieldAllCnt = fieldCount,

                ScreenBestScore = 0,
                ScreenAvgScore = 0,
                ScreenVisitCnt = 0,
                ScreenCnt = 0,
                ScreenAllCnt = 0

                //ScreenBestScore = ccPlay.Any() ? ccPlay.Min(cc => cc.BestScore) : 0,
                //ScreenAvgScore = ccPlay.Any() ? ccPlay.Sum(cc => cc.TotalScore) / ccPlay.Sum(cc => cc.VisitCnt) : 0,
                //ScreenVisitCnt = ccPlay.Any() ? ccPlay.Sum(cc => cc.VisitCnt) : 0,
                //ScreenCnt = codeCount,//screenCnt.Any() ? screenCnt.Count() : 0,
                //ScreenAllCnt = reScreen.Count()
            };

            return Json(data);
        }

        [HttpGet("api/[controller]/local/golfclub/field/contents_sp")]
        public async Task<ActionResult> GetLocalGolfClubFieldContents(int golfClubId = 0, int page = 1)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            //var _reviews = from review in _context.Reviews
            if (page < 1) page = 1;

            int golfClub = (from g in _context.GolfClubs
                            where g.Id == golfClubId && g.Status == 1
                            select g.Id).FirstOrDefault();

            if (golfClub <= 0)
            {
                return BadRequest();
            }

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Mypage_GolfClub_Field_Contents";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_USER_ID",
                    SqlDbType.Int)
                { Value = tokenUserId });
                cmd.Parameters.Add(new SqlParameter("@P_GOLFCLUB_ID",
                    SqlDbType.Int)
                { Value = golfClubId });
                cmd.Parameters.Add(new SqlParameter("@P_PAGE",
                    SqlDbType.TinyInt)
                { Value = page });

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

                return Json(retObject);
            }
        }

        [HttpGet("api/[controller]/local/golfclub/screen/contents_sp")]
        public async Task<ActionResult> GetLocalGolfClubScreenContents(int golfClubId = 0, int page = 1)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            int tokenUserNo = JwtManager.getJwtNo(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            //var _reviews = from review in _context.Reviews
            if (page < 1) page = 1;

            var golfClub = (from g in _context.GolfClubs
                            where g.Id == golfClubId && g.Status == 1
                            select g).FirstOrDefault();

            if (golfClub == null)
            {
                return BadRequest();
            }


            IEnumerable<GzApi.CCPlay> ccPlays = new List<GzApi.CCPlay>();

            if (golfClub.GcNums != null)
            {
                var gcNums = golfClub.GcNums.Split(',');

                foreach (var item in gcNums)
                {
                    Console.WriteLine("::: {0}, {1}",tokenUserNo, int.Parse(item));
                    ccPlays = ccPlays.Union(GzApi.GetCCPlay(tokenUserNo, int.Parse(item)));
                }
            }
            
            return Json(ccPlays);
        }


        [HttpGet("api/[controller]/local/field/contents_sp")]
        public async Task<ActionResult> GetLocalFieldContents(int code = 0, int type = 1, int page = 1)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            //var _reviews = from review in _context.Reviews

            if (type != 1 && type != 2)
            {
                return NotFound();
            }
            if (page < 1) page = 1;

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Mypage_Local_Field_Contents";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_USER_ID",
                    SqlDbType.Int)
                { Value = tokenUserId });
                cmd.Parameters.Add(new SqlParameter("@P_CODE",
                    SqlDbType.TinyInt)
                { Value = code });
                cmd.Parameters.Add(new SqlParameter("@P_TYPE",
                    SqlDbType.TinyInt)
                { Value = type });
                cmd.Parameters.Add(new SqlParameter("@P_PAGE",
                    SqlDbType.TinyInt)
                { Value = page });

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
                            // one can modify the next line to
                            //   if (dataReader.IsDBNull(iFiled))
                            //       dataRow.Add(dataReader.GetName(iFiled), dataReader[iFiled]);
                            // if one want don't fill the property for NULL
                            // returned from the database
                            dataRow.Add(
                                dataReader.GetName(iFiled),
                                dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
                            );
                        }

                        retObject.Add((ExpandoObject)dataRow);
                    }
                }

                if (type == 1)
                {
                    var t = from r in retObject
                            group new
                            {
                                bestScore = r.score,
                                id = r.golfClubId,
                                name = r.golfClubName,
                                r.courseNames,
                                r.tFileUri,
                                r.playDate
                            } by r.playDate.Substring(0, 7) into _r
                            select new
                            {
                                date = _r.Key,
                                golfClub = _r,
                            };

                    return Json(t);
                }

                return Json(retObject);
            }


            //var reviews = (from review in _context.Reviews
            //              where review.UserId == tokenUserId
            //               where review.PlayDate != null
            //               //join refCourse in _context.RefCourseLists on review.Id equals refCourse.ReviewId
            //               //join course2 in _context.Courses on refCouse.CourseId equals course2.Id
            //               join golfClub in _context.GolfClubs on review.GolfClubId equals golfClub.Id
            //              where golfClub.IsField
            //              join clubHouse in _context.ClubHouses on golfClub.Id equals clubHouse.GolfClubId
            //              join geo in _context.GeoLists on golfClub.GeoListId equals geo.Id into geoList
            //              from geo in geoList.DefaultIfEmpty()
            //              join course in _context.Courses on golfClub.Id equals course.GolfClubId
            //              join hole in _context.Holes on course.Id equals hole.CourseId
            //              where code != 0 ? (geo == null ? false : geo.Code == code) : true
            //              select new
            //              {
            //                  Id = review.Id,
            //                  GolfClubId = golfClub.Id,
            //                  GolfClubName = golfClub.Name,
            //                  GolfClubTFileUri = golfClub.TFileUri,
            //                  PlayDate = review.PlayDate,
            //                  CourseName = course.CourseName,
            //                  HoleName = hole.Name,
            //                  Score = review.Score,
            //                  ReviewCreatedAt = review.CreatedAt,
            //              }).ToList();

            ////var _reviews = reviews.GroupBy(r => r.Id).Select(r => new
            ////{
            ////    r.Id,
            ////    r.GolfClubId,
            ////    r.GolfClubName,
            ////    r.GolfClubTFileUri,
            ////    r.PlayDate,
            ////    r.CourseName,
            ////    r.HoleName
            ////})

            //if (type == 1)
            //{
            //    //작성일순
            //    var reviews2 = (from review in reviews
            //                   orderby review.PlayDate descending
            //                   group review by review.PlayDate.Value.ToString("yyyy-MM") into r
            //                   select new
            //                   {
            //                       Id = r.Key,
            //                       Count = r.Count(),
            //                       Date = r.Key,
            //                       GolfClub = from _r in r
            //                                  group _r by _r.GolfClubId into rr
            //                                  select new
            //                                  {
            //                                      Id = rr.FirstOrDefault().GolfClubId,
            //                                      Name = rr.FirstOrDefault().GolfClubName,
            //                                      TFileUri = rr.FirstOrDefault().GolfClubTFileUri,
            //                                      HoleName = rr.FirstOrDefault().HoleName,
            //                                      CourseName = rr.Select(_rr => _rr.CourseName),
            //                                      BestScore = rr.Max(_rr => _rr.Score),
            //                                      PlayDate = rr.FirstOrDefault().PlayDate,
            //                                      CreatedAt = rr.FirstOrDefault().ReviewCreatedAt,
            //                                  },
            //                   }).Skip((page - 1) * 10).Take(10);

            //    return Json(reviews2);
            //}
            //else
            //{
            //    var reviews2 = (from review in reviews
            //                   orderby review.Score ascending
            //                   //group review by review.PlayDate == null ? null : review.PlayDate.Value.ToString("yyyy-MM") into r
            //                   select new
            //                   {
            //                        Id = review.Id,
            //                        GolfClubId = review.GolfClubId,
            //                        Name = review.GolfClubName,
            //                        TFileUri = review.GolfClubTFileUri,
            //                        HoleName = review.HoleName,
            //                        CourseName = review.CourseName,
            //                        BestScore = review.Score,
            //                        PlayDate = review.PlayDate,
            //                        CreatedAt = review.ReviewCreatedAt,
            //                   }).Skip((page - 1) * 10).Take(10);

            //    return Json(reviews2);
            //}





            ////var reviews = await _context.Reviews
            ////     .Where(re => re.UserId == tokenUserId)
            ////     .Include(re => re.GolfClub)
            ////     .ThenInclude(gc => gc.GeoList)
            ////     .Include(re => re.GolfClub)
            ////     .ThenInclude(gc => gc.Courses)
            ////     .ThenInclude(co => co.Holes)
            ////     .Where(re => re.GolfClub.IsField == true)
            ////     .Where(re => code != 0 ? re.GolfClub.GeoList.Code == code : true)
            ////     .AsNoTracking()
            ////     .AsSplitQuery()
            ////     .ToListAsync();



            ////var gReviews = reviews.GroupBy(re =>  re.PlayDate != null ? re.PlayDate.Value.ToString("yyyy-MM") : null)
            ////     .Select(rg => new
            ////     {
            ////         Count = rg.Count(),
            ////         Date = rg.Key,

            ////         GolfClub = rg.GroupBy(r => r.GolfClubId).Select(a => new 
            ////         {
            ////             Id = a.FirstOrDefault().GolfClubId,
            ////             a.FirstOrDefault().GolfClub.Name,
            ////             a.FirstOrDefault().GolfClub.TFileUri,
            ////             //수정해야함 성국이.
            ////             //HoleName = a.FirstOrDefault().GolfClub.Courses.Where(c => c.Id == a.FirstOrDefault().ReCourseId).FirstOrDefault().CourseName,
            ////             HoleName = "수정중",
            ////             BestScore = a.Max(r => r.Score),
            ////             PlayDate = a.FirstOrDefault().PlayDate,
            ////             CreatedAt = a.FirstOrDefault().CreatedAt,
            ////         })

            ////     });

            ////if (type == 1)
            ////{
            ////    //작성일순
            ////    var gData = gReviews.Select( gr => new 
            ////    {
            ////        Count = gr.Count,
            ////        Date = gr.Date,
            ////        GolfClub = gr.GolfClub.OrderByDescending(g => g.CreatedAt)
            ////    });

            ////    return Json(gData);
            ////}
            ////else if (type == 2)
            ////{
            ////    //최저타순
            ////    var gData = gReviews.Select(gr => new
            ////    {
            ////        Count = gr.Count,
            ////        Date = gr.Date,
            ////        GolfClub = gr.GolfClub.OrderBy(g => g.PlayDate)
            ////    });

            ////    return Json(gData);
            ////}


            ////return Json(gReviews);
            //return Json(1);
        }


        [HttpGet("api/[controller]/local/screen/contents_sp")]
        public async Task<ActionResult> GetLocalScreenContents(int code = 0, int type = 1, int page = 1)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            //var _reviews = from review in _context.Reviews

            if (type != 1 && type != 2)
            {
                return NotFound();
            }
            if (page < 1) page = 1;

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Mypage_Local_Screen_Contents";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_USER_ID",
                    SqlDbType.Int)
                { Value = tokenUserId });
                cmd.Parameters.Add(new SqlParameter("@P_CODE",
                    SqlDbType.TinyInt)
                { Value = code });
                cmd.Parameters.Add(new SqlParameter("@P_TYPE",
                    SqlDbType.TinyInt)
                { Value = type });
                cmd.Parameters.Add(new SqlParameter("@P_PAGE",
                    SqlDbType.TinyInt)
                { Value = page });

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
                            // one can modify the next line to
                            //   if (dataReader.IsDBNull(iFiled))
                            //       dataRow.Add(dataReader.GetName(iFiled), dataReader[iFiled]);
                            // if one want don't fill the property for NULL
                            // returned from the database
                            dataRow.Add(
                                dataReader.GetName(iFiled),
                                dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
                            );
                        }

                        retObject.Add((ExpandoObject)dataRow);
                    }
                }

                if (type == 1)
                {
                    var t = from r in retObject
                            group new
                            {
                                bestScore = r.score,
                                id = r.golfClubId,
                                name = r.golfClubName,
                                r.tFileUri,
                                r.playDate
                            } by r.month into _r
                            select new
                            {
                                date = _r.Key,
                                golfClub = _r,
                            };

                    return Json(t);
                }

                return Json(retObject);
            }

        }


        [HttpGet("api/[controller]/local/screen/contents")]
        public async Task<ActionResult> GetLocalScreenContents(int code = 0, int type = 1)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            int testUserId = 1793569;
            var golfClubs = _context.GolfClubs.Include(gc => gc.GeoList).ToList();

            if (type != 1)
            {
                var data = GzApi.GetLifeBest(testUserId);

                var _data = from d in data
                            join golfClub in golfClubs on d.CiCode.ToString() equals golfClub.GcNum
                            where code != 0 ? (golfClub.GeoListId != null ? golfClub.GeoList.Code == code : false) : true
                            orderby d.BestScore ascending
                            select new
                            {
                                BestVisitDate = d.BestVisitDate,
                                BestScore = d.BestScore,
                                GCName = golfClub.Name,
                                GCTFileUri = golfClub.TFileUri,
                                GCNum = golfClub.GcNum,
                                GCId = golfClub.Id,
                            };

                return Json(_data);

            }
            else
            {
                var bestMonth = GzApi.GetLifeBestMonth(testUserId);


                bestMonth.OrderByDescending(b => b.VisitDate).GroupBy(b => b.VisitDate);

                var gbm = from bm in bestMonth
                          join golfClub in golfClubs on bm.CiCode.ToString() equals golfClub.GcNum
                          where code != 0 ? (golfClub.GeoListId != null ? golfClub.GeoList.Code == code : false) : true
                          select new
                          {
                              VisitDate = bm.VisitDate,
                              BestVisitDate = bm.BestVisitDate,
                              BestScore = bm.BestScore,
                              GCName = golfClub.Name,
                              GCTFileUri = golfClub.TFileUri,
                              GCNum = golfClub.GcNum,
                              GCId = golfClub.Id,
                          };

                var _gbm = from __gbm in gbm
                           orderby __gbm.VisitDate descending
                           group __gbm by __gbm.VisitDate into newBm
                           select new
                           {
                               VisitDate = newBm.Key,
                               Items = newBm,
                           };
                return Json(_gbm);
            }


            //List<GolfClub> golfClubs = new List<GolfClub>();

            //var ccPlayList = GzApi.GetCCPlay(tokenUserId, 102555905);

            //foreach (var item in ccPlayList)
            //{
            //    var tg = await _context.GolfClubs
            //        .Include(gc => gc.GeoList)
            //        .FirstOrDefault();
            //    golfClubs.Add()
            //}




            return Ok();
        }

        [HttpGet("api/[controller]/local/field/notvisit")]
        public async Task<ActionResult> GetLocalFieldNotVisit (int code = 0, int page = 1)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            if (page < 1) page = 1;

            var golfClubs = (from golfClub in _context.GolfClubs
                            where golfClub.Status == 1
                            join clubHouse in _context.ClubHouses on golfClub.Id equals clubHouse.GolfClubId
                            join geo in _context.GeoLists on clubHouse.GeoListId equals geo.Id into geoList
                            from geo in geoList.DefaultIfEmpty()
                            where code != 0 ? (geo == null ? false : geo.Code == code) : true && geo.Code > 0
                            join review in _context.Reviews
                            on new { GolfClubId = golfClub.Id, UserId = tokenUserId } 
                            equals new { GolfClubId = (int)review.GolfClubId, UserId = review.UserId }
                            into reviews
                            //where reviews.DefaultIfEmpty().Count() == 0
                            from review in reviews.DefaultIfEmpty()
                            where review == null
                            join course in _context.Courses on golfClub.Id equals course.GolfClubId
                            join hole in _context.Holes on course.Id equals hole.CourseId
                            select new
                            {
                                Id = golfClub.Id,
                                Name = golfClub.Name,
                                TFileUri = golfClub.TFileUri,
                                SignatureHole = golfClub.SignatureHole,
                                FairwayGrass = golfClub.FairwayGrass,
                                CourseId = course.Id,
                                CourseName = course.CourseName,
                                HoleCount = 1,
                                Par = hole.Par,
                                Length = course.Length
                            }).ToList();

            if (!golfClubs.Any()) return Json(new List<byte>());

            var golfClubs2 = (golfClubs.GroupBy(g => g.CourseId).Select(c => new
            {
                c.First().Id,
                c.First().Name,
                c.First().TFileUri,
                c.First().SignatureHole,
                c.First().FairwayGrass,
                c.First().CourseId,
                c.First().CourseName,
                CourseCount = 1,
                HoleCount = c.Sum(_c => _c.HoleCount),
                Par = c.Sum(_c => int.TryParse(_c.Par, out int result) ? result : 0),
                Length = c.First().Length
            }).GroupBy(c => c.Id).Select(g => new
            {
                g.First().Id,
                g.First().Name,
                g.First().TFileUri,
                g.First().SignatureHole,
                g.First().FairwayGrass,
                CourseCount = g.Sum(_g => _g.CourseCount),
                HoleCount = g.Sum(_g => _g.HoleCount),
                Par = g.Sum(_g => _g.Par),
                Length = g.Sum(_g => _g.Length),
                AvgLength = g.Sum(_g => _g.Length) / g.Count() * 2
            })).Skip((page - 1) * 10).Take(10).ToList();

            var golfClubs3 = golfClubs2.GroupJoin(_context.Reviews, g => g.Id, r => r.GolfClubId, (g, r) => new
            {
                GolfClub = g,
                Review = r,
            })
                .SelectMany(
                        tuple => tuple.Review.Where(r => r.OpenState == 1).DefaultIfEmpty(),
                        (g, r) => new
                        {
                            g.GolfClub.Id,
                            g.GolfClub.Name,
                            g.GolfClub.TFileUri,
                            g.GolfClub.SignatureHole,
                            g.GolfClub.CourseCount,
                            g.GolfClub.HoleCount,
                            g.GolfClub.Par,
                            g.GolfClub.Length,
                            g.GolfClub.AvgLength,
                            g.GolfClub.FairwayGrass,
                            SCount = r == null ? 0 : 1,
                            SCaddy = r == null ? 0 : r.SCaddy,
                            SCourse = r == null ? 0 : r.SCourse,
                            SFacility = r == null ? 0 : r.SFacility,
                            SFoodAndDrink = r == null ? 0 : r.SFoodAndDrink,
                            //SCount = r == null ? 0 : (r.DCaddy == null ? 1 : 0 + r.DCourse == null ? 1 : 0 + r.DFacility == null ? 1 : 0 + r.DFoodAndDrink == null ? 1 : 0),
                            //SCaddy = r == null ? -1 : (r.DCaddy == null ? -1 : r.SCaddy),
                            //SCourse = r == null ? -1 : (r.DCourse == null ? -1 : r.SCourse),
                            //SFacility = r == null ? -1 : (r.DFacility == null ? -1 : r.SFacility),
                            //SFoodAndDrink = r == null ? -1 : (r.DFoodAndDrink == null ? -1 : r.SFoodAndDrink),
                            //golfclub = golfclub.GolfClub,
                            //visit = review.UserId == tokenUserId ? 1 : 0,
                            //RC = review == null ? 0 : 1,
                            //AS = review == null ? 0 : (review.SCourse + review.SFacility + review.SFoodAndDrink + review.SCaddy) / 4,
                            //Score = review == null ? 0 : review.Score,
                        }
                    )
                .GroupBy(g => g.Id).Select(g => new
            {
                g.First().Id,
                g.First().Name,
                g.First().TFileUri,
                g.First().SignatureHole,
                g.First().FairwayGrass,
                g.First().CourseCount,
                FilterName = _context.FilterObjects.Join(_context.FilterLists, fo => fo.Id, fl => fl.FilterObjectId, (fo, fl) => new { fo, fl }).Where(s => s.fl.GolfClubId == g.First().Id && s.fo.Id >= 6 && s.fo.Id <= 9).Select(s => s.fo.Name).FirstOrDefault(),
                g.First().HoleCount,
                g.First().Par,
                g.First().Length,
                g.First().AvgLength,
                AvgReviewScore = g.Average(r => r.SCount == 0 ? 0 : (r.SCaddy + r.SCourse + r.SFacility + r.SFoodAndDrink) / 4),
                ReviewCount = g.Count(r => r.SCount > 0),
            }).ToList();

            return Json(golfClubs3);

            //var golfClub3 = from golfClub in golfClubs2
            //                join review in _context.Reviews on golfClub.Id equals review.GolfClubId into reviews
            //                from review in reviews.DefaultIfEmpty()
            //                group golfClub by golfClub.Id into golfClub
            //                select new
            //                {

            //                };


            return Json(golfClubs2);
        }

        [HttpGet("api/[controller]/local/screen/notvisit")]
        public async Task<ActionResult> GetLocalScreenNotVisit(int code = 0, int page = 1)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            if (page < 1) page = 1;

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Mypage_Screen_Notvisit";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_USER_ID",
                    SqlDbType.Int)
                { Value = tokenUserId });
                cmd.Parameters.Add(new SqlParameter("@P_CODE",
                    SqlDbType.TinyInt)
                { Value = code });
                cmd.Parameters.Add(new SqlParameter("@P_PAGE",
                    SqlDbType.TinyInt)
                { Value = page });

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
                            // one can modify the next line to
                            //   if (dataReader.IsDBNull(iFiled))
                            //       dataRow.Add(dataReader.GetName(iFiled), dataReader[iFiled]);
                            // if one want don't fill the property for NULL
                            // returned from the database
                            dataRow.Add(
                                dataReader.GetName(iFiled),
                                dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
                            );
                        }

                        retObject.Add((ExpandoObject)dataRow);
                    }
                }

                return Json(retObject);
            }
        }




        //return Json(GzApi.GetCCPlay(1793569, 102555905));
        //return Json(GzApi.GetCCPlayBest(1793569, 102555905));
        //return Json(GzApi.GetRecommend(1796019));
        //return Json(GzApi.GetLifeBestMonth(1796019));
        //return Json(GzApi.GetLifeBest(1796019));
        //return Json(GzApi.GetFriends(1796019));


        [HttpGet("api/[controller]/local/field/lifebest")]
        public async Task<ActionResult> FieldLifeBest(int id)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            var reviews = (from review in _context.Reviews
                        where review.UserId == tokenUserId
                        where review.PlayDate != null
                        join golfClub in _context.GolfClubs on id equals golfClub.Id
                        orderby review.PlayDate descending
                        select new
                        {
                            Name = golfClub.Name,
                            Score = review.Score,
                            PlayDate = review.PlayDate.Value.ToString("yyyy-MM-dd"),
                        }).ToList();

            var data = new
            {
                Name = reviews.First().Name,
                Best = reviews.Min(r => r.Score),
                Avg = reviews.Average(r => r.Score),
                Count = reviews.Count(),
                PlayData = reviews,
            };

            return Json(data);
        }



        //ppt page 31-7
        [HttpGet("api/[controller]/gz/cntscore")]
        public async Task<ActionResult> GetCntOrBestScore(int gcNum)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            //tokenUserId = 1793569;
            //gcNum = 102555905;


            List<GzApi.CCPlayBest> ccPlayBest = GzApi.GetCCPlayBest(tokenUserId, gcNum);

            int bestScore = 0;

            if (ccPlayBest != null)
            {
                bestScore = ccPlayBest.Max(c => c.BestScore);
            }

            int visitCnt = 0;
            List<LifeBest> ccPlay = GzApi.GetLifeBest(tokenUserId);
            if (ccPlay != null)
            {
                visitCnt = ccPlay.Where(c => c.CiCode == gcNum).FirstOrDefault().VisitCnt;
            }

            return Json(new { VisitCnt = visitCnt, BestScore = bestScore });
        }


        //스크린 라운딩수
        [HttpGet("api/[controller]/gz/cnt")]
        public async Task<ActionResult> GetCnt(int gcNum)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            tokenUserId = 1793569;
            gcNum = 102555905;

            int visitCnt = 0;
            List<LifeBest> ccPlay = GzApi.GetLifeBest(tokenUserId);
            if (ccPlay != null)
            {
                visitCnt = ccPlay.Where(c => c.CiCode == gcNum).FirstOrDefault().VisitCnt;
            }

            return Json(new { VisitCnt = visitCnt });
        }



        [HttpGet("api/[controller]/mileage/get")]
        public async Task<ActionResult<int>> GetMileage()
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            int tokenUserNo = JwtManager.getJwtNo(_httpContext);

            Console.WriteLine(tokenUserNo);

            if (tokenUserId == 0 && tokenUserNo == 0) return Unauthorized();

            var userMileage = GzApi.GetMileageBalance(tokenUserNo);

            return userMileage.USEPOSSBMILEAGE;
        }

        [HttpGet("api/[controller]/mappage/city/weather/all_sp")]
        public async Task<ActionResult> GetCityWeatherAll()
        {
            //128.07706343281654,
            //        37.50915927292147

            return Json(GzApi.GetDefaultSP(_context, "Mappage_City_Weather_All"));
        }

        [HttpGet("api/[controller]/mappage/city/weather/item_sp")]
        public async Task<ActionResult> GetCityWeatherItem(double x, double y)
        {   
            var point = new Point(x, y) { SRID = 4326};

            foreach (var item in _geoMore.Value.features)
            {
                if (item.geometry.Contains(point))
                {
                    return Json(GzApi.GetSingleParamSP(_context, "Mappage_City_Weather_Item", "@CITY_CODE", item.properties.SIG_CD));
                }
            }

            return NotFound();
        }

        [HttpGet("api/[controller]/flagnew")]
        public async Task<ActionResult> GetFlagNew()
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            return Json(_context.Users.Find(tokenUserId).FlagNew);
        }

        [HttpPost("api/[controller]/flagnew")]
        public async Task<ActionResult> SetFlagNew()
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            var dbUser = _context.Users.Find(tokenUserId);

            dbUser.FlagNew = false;
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("api/[controller]/roulette")]
        public async Task<ActionResult> GetRoulette()
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            return Json(GzApi.GetDefaultSP(_context, "Roulette_Get"));
        }

        [HttpGet("api/[controller]/roulette/prev")]
        public async Task<ActionResult> GetRoulettePrev()
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            return Json(GzApi.GetSingleParamSP(_context, "Roulette_Get_Prev", "@P_USER_ID", tokenUserId));
        }

        [HttpGet("api/[controller]/stamps")]
        public async Task<ActionResult> GetStamps()
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            return Json(GzApi.GetSingleParamSP(_context, "Roulette_Get_Stamp", "@UserId", tokenUserId));
        }

        [HttpGet("api/[controller]/items")]
        public async Task<ActionResult> GetItems()
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            return Json(GzApi.GetDefaultSP(_context, "Roulette_Get_Items"));
        }

        [HttpPost("api/[controller]/roulette")]
        public async Task<ActionResult> RouletteResult([FromBody] RouletteResultDto param)
        {
            var retObject = new List<dynamic>();
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            int tokenUserNo = JwtManager.getJwtNo(_httpContext);
            if (tokenUserId == 0 || tokenUserNo == 0) return Unauthorized();

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Roulette_Result";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_USER_ID",
                    SqlDbType.Int)
                { Value = tokenUserId });
                cmd.Parameters.Add(new SqlParameter("@P_ROULETTE_ID",
                    SqlDbType.Int)
                { Value = param.RouletteId });
                cmd.Parameters.Add(new SqlParameter("@P_ROULETTE_VERSION",
                    SqlDbType.Int)
                { Value = param.RouletteVersion });

                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();

                
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
            }
            try
            {

                if (retObject.Any())
                {
                    if (retObject[0].rouletteItemType == "m")
                    {
                        string po = retObject[0].rouletteItemWin;

                        var p = int.Parse(po.Replace("P", "").Trim().Replace("마일리지/", ""));

                        if (MileageManager.SetRouletteEvent(_context, tokenUserNo, tokenUserId, p))
                        {
                            //성공

                            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
                            {
                                cmd.CommandText = "Roulette_Result_Update";
                                cmd.CommandType = CommandType.StoredProcedure;
                                // set some parameters of the stored procedure
                                cmd.Parameters.Add(new SqlParameter("@RouletteResultId",
                                    SqlDbType.Int)
                                { Value = retObject[0].resultId });
                                cmd.Parameters.Add(new SqlParameter("@Result",
                                    SqlDbType.Bit)
                                { Value = true });

                                if (cmd.Connection.State != ConnectionState.Open)
                                    cmd.Connection.Open();

                                using (var dataReader = cmd.ExecuteReader()) { };
                            }
                        }
                        else
                        {
                            //실패
                        }

                    }
                }
            }
            catch (Exception e)
            {
                return Json(e);
            }

            return Json(retObject);
        }

        [HttpPost("api/[controller]/roulette/wininfo")]
        public async Task<ActionResult> RouletteWin([FromBody] RouletteWinDto param)
        {
            int tokenUserId = JwtManager.getJwtId(_httpContext);
            if (tokenUserId == 0) return Unauthorized();

            var result = _context.RouletteResults.Where(r => r.Id == param.ResultId && r.UserId == tokenUserId).FirstOrDefault();

            if (result == null) return NotFound();

            result.Contact = param.Contact;
            result.Address = param.Address;
            result.AddressDetail = param.AddressDetail;
            result.Name = param.Name;

            await _context.SaveChangesAsync();

            return Ok();
        }


        ////웹소켓 붙이는중 
        //[HttpGet("api/[controller]/mypage/history/ws")]
        //    public async Task Get()
        //    {
        //        if (HttpContext.WebSockets.IsWebSocketRequest)
        //        {
        //            using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
        //            await Echo(webSocket);
        //        }
        //        else
        //        {
        //            HttpContext.Response.StatusCode = 400;
        //        }
        //    }

        //    private async Task Echo(WebSocket webSocket)
        //    {
        //        var buffer = new byte[1024 * 4];
        //        var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        //        //_logger.Log(LogLevel.Information, "Message received from Client");
        //        //Console.WriteLine("Message received from Client ::{0} ",result.ToString());

        //        while (!result.CloseStatus.HasValue)
        //        {
        //            Console.WriteLine(result.CloseStatus.HasValue);

        //            var serverMsg = Encoding.UTF8.GetBytes($"Server: Hello. You said: {Encoding.UTF8.GetString(buffer)}");
        //            await webSocket.SendAsync(new ArraySegment<byte>(serverMsg, 0, serverMsg.Length), result.MessageType, result.EndOfMessage, CancellationToken.None);
        //            //_logger.Log(LogLevel.Information, "Message sent to Client");
        //            Console.WriteLine("Message sent to Client");

        //            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
        //            //_logger.Log(LogLevel.Information, "Message received from Client");
        //            Console.WriteLine("Message received from Client");
        //        }
        //        await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        //        //_logger.Log(LogLevel.Information, "WebSocket connection closed");
        //        Console.WriteLine("WebSocket connection closed");
        //    }



















        public class SearchHistoryResult
        {
            public List<GolfClub> HistoryList { get; set; }
            public bool MoreState { get; set; } 
        }

        public class RouletteWinDto
        {
            public int ResultId { get; set; }
            public string Name { get; set; }
            public string Contact { get; set; }
            public string Address { get; set; }
            public string AddressDetail { get; set; }
        }

        public class RouletteResultDto
        {
            public int RouletteId { get; set; }
            public int RouletteVersion { get; set; }
        }

        public class ReviewDto
        {
            public string Review { get; set; }
            public string ReviewImages { get; set; }
            public string RefCourseLists { get; set; }
            public IFormFile[] FileImages { get; set; }
            public IFormFile SkinImage { get; set; } = null;
        }

        public class WrongInfoDto
        {
            public int Id { get;  set; }
            public string Type { get; set; }
            public string Content {  get; set; }
            public string WrongInfoImages { get; set; }
            public IFormFile[] FileImages { get; set; }
        }

        public class ReviewGradeDto
        {
            public int ReviewId { get; set; }
            public double Score { get; set; }
        }

        public class ReviewReportDto
        {
            public int ReviewId { get; set; }
            public string Content { get; set; }
        }

        public class ReviewResult
        {
            public List<GolfClub> GolfClubs { get; set; }
            public List<Convenience> Conveniences { get; set; }
        }

        public class ReviewAllResult
        {
            public List<GolfClub> GolfClubs { get; set; }
            public List<Convenience> Conveniences { get; set; }
            public List<User> Users { get; set; }
            public List<Tag> Tags { get; set; }
        }

        public class SettingFilterDto
        {
            public int? Id { get; set; }
            public int SettingId { get; set; }
            public int FilterObjectId { get; set; }
        }

        public class ConvenienceDto
        {
            public double Po1x { get; set; }
            public double Po1y { get; set; }
        }

        public class FilterAllDto
        {
            public double Po1x { get; set; }
            public double Po1y { get; set; }
            public double Po2x { get; set; }
            public double Po2y { get; set; }
            public double Level { get; set; }

            public double myX { get; set; } = 0;
            public double myY { get; set; } = 0;

            public Polygon GetPosition()
            {
                Polygon poly = new Polygon(new LinearRing(new Coordinate[]
                    {
                        new Coordinate(Po1x, Po2y), // 왼쪽 위
                        new Coordinate(Po1x, Po1y), // 왼쪽 아래
                        new Coordinate(Po2x, Po1y), // 오른쪽 아래
                        new Coordinate(Po2x, Po2y),// 오른쪽 위
                        new Coordinate(Po1x, Po2y) // 다시 원점
                    }));

                poly.SRID = 4326;

                return poly;
            }
            public DateTime Date { get; set; } = DateTime.UtcNow.AddHours(9);
        }
       

        public class ReturnData
        {
            public string Feature { get; set; }
            public string GolfClub { get; set; }
        }

        public class TestData
        {
            public string Test { get; set; }
        }

        
    }
}
