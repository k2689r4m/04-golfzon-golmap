using GolfZonWebApp.Data;
using GolfZonWebApp.Models;
using GolfZonWebApp.Lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using GolfZonWebApp.Types;


namespace GolfZonWebApp.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : Controller
    {
        private readonly GolfzonContext _context;
        public UserController(GolfzonContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAll(string option, string content, int page)
        {
            //var users = await _context.Users
            //    .Include(u => u.UserLoginHistorys.OrderByDescending(x => x.CreatedAt).Take(1))
            //    .OrderByDescending(u => u.Id)
            //    .ToArrayAsync();

            //return Json(users);

            string error = "no result";

            if (page <= 0)
            {
                page = 1;
            }

            IQueryable<User> data = null;

            if (content == null && option == null)
            {
                data =
                    from users in _context.Users
                                                        .Include(u => u.UserLoginHistorys.OrderByDescending(x => x.CreatedAt).Take(1))
                    orderby users.Id descending
                    select users;
            }
            else if (option != null && content != null)
            {
                string[] options = { "회원번호", "아이디", "이름", "닉네임", "휴대전화", "이메일" };
                if (Array.Exists(options, element => element == option))
                {
                    if (content != "")
                    {
                        data =
                                from users in _context.Users
                                                                    .Include(u => u.UserLoginHistorys.OrderByDescending(x => x.CreatedAt).Take(1))
                                orderby users.Id descending
                                select users;

                        switch (option)
                        {
                            case "회원번호":
                                data = data.Where(d => EF.Functions.Like(d.Id.ToString(), "%" + content + "%")).OrderByDescending(u => u.Id);
                                break;
                            case "아이디":
                                data = data.Where(d => EF.Functions.Like(d.Username, "%" + content + "%")).OrderByDescending(u => u.Id);
                                break;
                            case "이름":
                                data = data.Where(d => EF.Functions.Like(d.Fullname, "%" + content + "%")).OrderByDescending(u => u.Id);
                                break;
                            case "닉네임":
                                data = data.Where(d => EF.Functions.Like(d.Nickname, "%" + content + "%")).OrderByDescending(u => u.Id);
                                break;
                            case "휴대전화":
                                data = data.Where(d => EF.Functions.Like(d.Phone, "%" + content + "%")).OrderByDescending(u => u.Id);
                                break;
                            case "이메일":
                                data = data.Where(d => EF.Functions.Like(d.Email, "%" + content + "%")).OrderByDescending(u => u.Id);
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        error = "content is too short";
                    }
                }
                else
                {
                    error = "invalid option";
                }
            }
            else
            {
                if (content == null)
                {
                    error = "content is too short";
                }
                else if (option == null)
                {
                    error = "invalid option";
                }
            }

            if (data == null)
            {
                return Json(PaginatedList<int>.nothing(error));
            }

            PaginatedList<User> _users = await PaginatedList<User>.CreateAsync(data, page);

            return Json(_users.paginate());
        }

        [HttpPost("review/delete/{reviewId}")]
        public async Task<ActionResult<List<Review>>> DeleteReviewItem(int reviewId)
        {
            var review = _context.Reviews.Find(reviewId);

            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }

            return Ok();
        }

        [HttpGet("review/{id}")]
        public async Task<ActionResult<List<Review>>> GetReviewItems(int id, int page = 1)
        {
            //var reviews = _context.Reviews
            //    .Where(r => r.UserId == id)
            //    .OrderByDescending(r => r.CreatedAt)
            //    .Take(5)
            //    .Include(r => r => ReviewGoods)
            //    .Select(r => new
            //    {
            //        r,
            //        likeCnt = r.ReviewGoods.Count(x2 => x2.State == 1),
            //        hateCnt = r.ReviewGoods.Count(x2 => x2.State == 2)
            //    });

            var reviews1 = (from review in _context.Reviews
                                           where review.UserId == id
                                           orderby review.CreatedAt descending
                                           select review).Skip((page - 1) * 5).Take(5);

            var reviews2 = from review in reviews1
                              join rg in _context.ReviewGoods on review.Id equals rg.ReviewId into rgGroup
                              from rg in rgGroup.DefaultIfEmpty()
                              group rg by new
                              {
                                  review.Id,
                                  review.GolfClubId,
                                  review.ConvenienceId,
                                  review.UserId,
                                  review.ReviewContent,
                                  review.PlayDate,
                                  review.Score,
                                  review.SCourse,
                                  review.SFacility,
                                  review.SCaddy,
                                  review.SFoodAndDrink,
                                  review.DCourse,
                                  review.DFacility,
                                  review.DCaddy,
                                  review.DFoodAndDrink,
                                  review.CaddyName,
                                  review.Representation,
                                  review.OpenState,
                                  review.CreatedAt,
                              } into reviewGroup
                              select new
                              {
                                  reviewGroup.Key.Id,
                                  reviewGroup.Key.GolfClubId,
                                  reviewGroup.Key.ConvenienceId,
                                  reviewGroup.Key.UserId,
                                  reviewGroup.Key.ReviewContent,
                                  reviewGroup.Key.PlayDate,
                                  reviewGroup.Key.Score,
                                  reviewGroup.Key.SCourse,
                                  reviewGroup.Key.SFacility,
                                  reviewGroup.Key.SCaddy,
                                  reviewGroup.Key.SFoodAndDrink,
                                  reviewGroup.Key.DCourse,
                                  reviewGroup.Key.DFacility,
                                  reviewGroup.Key.DCaddy,
                                  reviewGroup.Key.DFoodAndDrink,
                                  reviewGroup.Key.CaddyName,
                                  reviewGroup.Key.Representation,
                                  reviewGroup.Key.OpenState,
                                  reviewGroup.Key.CreatedAt,
                                  LikeCnt = reviewGroup.Count(rg => rg != null && rg.State == 1),
                                  HateCnt = reviewGroup.Count(rg => rg != null && rg.State == 2),
                              };

            var reviews3 = from review in reviews2.ToList()
                           join ri in _context.ReviewImages on review.Id equals ri.ReviewId into riGroup
                           from ri in riGroup.DefaultIfEmpty()
                           group ri by new
                           {
                               review.Id,
                               review.GolfClubId,
                               review.ConvenienceId,
                               review.UserId,
                               review.ReviewContent,
                               review.PlayDate,
                               review.Score,
                               review.SCourse,
                               review.SFacility,
                               review.SCaddy,
                               review.SFoodAndDrink,
                               review.DCourse,
                               review.DFacility,
                               review.DCaddy,
                               review.DFoodAndDrink,
                               review.CaddyName,
                               review.Representation,
                               review.OpenState,
                               review.LikeCnt,
                               review.HateCnt,
                               review.CreatedAt,
                           } into reviewImageGroup
                           select new
                           {
                               reviewImageGroup.Key.Id,
                               reviewImageGroup.Key.GolfClubId,
                               reviewImageGroup.Key.ConvenienceId,
                               reviewImageGroup.Key.UserId,
                               reviewImageGroup.Key.ReviewContent,
                               reviewImageGroup.Key.PlayDate,
                               reviewImageGroup.Key.Score,
                               reviewImageGroup.Key.SCourse,
                               reviewImageGroup.Key.SFacility,
                               reviewImageGroup.Key.SCaddy,
                               reviewImageGroup.Key.SFoodAndDrink,
                               reviewImageGroup.Key.DCourse,
                               reviewImageGroup.Key.DFacility,
                               reviewImageGroup.Key.DCaddy,
                               reviewImageGroup.Key.DFoodAndDrink,
                               reviewImageGroup.Key.CaddyName,
                               reviewImageGroup.Key.Representation,
                               reviewImageGroup.Key.OpenState,
                               reviewImageGroup.Key.LikeCnt,
                               reviewImageGroup.Key.HateCnt,
                               reviewImageGroup.Key.CreatedAt,
                               Name = reviewImageGroup.Key.GolfClubId != null ? _context.GolfClubs.Where(g => g.Id == reviewImageGroup.Key.GolfClubId).Select(g => g.Name).FirstOrDefault()
                               : reviewImageGroup.Key.ConvenienceId != null ? _context.Conveniences.Where(c => c.Id == reviewImageGroup.Key.ConvenienceId).Select(c => c.Name).FirstOrDefault() : "",
                               Images = reviewImageGroup.Count(ri => ri != null) > 0 ? reviewImageGroup.Select(ri => ri.Uri) : new List<string>(),
                               AvgGrade = _context.ReviewGrades.Where(rg => rg.ReviewId == reviewImageGroup.Key.Id).Count() > 0 ? _context.ReviewGrades.Where(rg => rg.ReviewId == reviewImageGroup.Key.Id).Average(rg => rg.Score) : 0,
                               User = _context.Users.Where(u => u.Id == reviewImageGroup.Key.UserId).Select((u) => new
                               {
                                   u.Nickname
                               }).FirstOrDefault(),
                               VisitCount = _context.Reviews.Where(r => r.GolfClubId != null ? r.GolfClubId == reviewImageGroup.Key.GolfClubId : false).Count()
                           };


            var TotalPage = ((_context.Reviews.Where(r => r.UserId == id).Count() - 1) / 5) + 1;

            return Json(new
            {
                Items = reviews3,
                TotalPage,
            });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetItem(int id)
        {
            //var po = GzApi.GetMileageBalance(id).USEPOSSBMILEAGE;

            var users =  _context.Users
                .Include(u => u.PointHistorys.OrderByDescending(p => p.Id))
                .Include(u => u.Reviews)
                .ThenInclude(r => r.GolfClub)
                .OrderByDescending(u => u.Id)
                .Select(u => new
                {
                    u.Id,
                    u.Blinded,
                    u.CreatedAt,
                    u.Email,
                    u.Nickname,
                    u.Phone,
                    //u.Point,
                    Point = GzApi.GetMileageBalance(u.UserNum).USEPOSSBMILEAGE,
                    u.PointHistorys,
                    u.ProfileImage,
                    u.Username,
                    u.UserNum,
                    Reviews = u.Reviews.Select(r => new {
                        r.GolfClubId,
                        r.State,
                        GolfClub = r.GolfClub != null ? new
                        {
                            GcNum = r.GolfClub.GcNum,
                            Name = r.GolfClub.Name,
                            Status = r.GolfClub.Status
                        } : null,
                        r.CreatedAt
                    }),
                    Screen = _context.LifeBestMonths
                    .Join(_context.GolfClubs, (a) => a.CiCode.ToString(), (b) => b.GcNum, (a, b) => new
                    {
                        lb = a,
                        g = b
                    })
                    .Join(_context.ClubHouses, (a) => a.g.Id, (b) => b.GolfClubId, (a, b) => new
                    {
                        lb = a.lb,
                        g = a.g,
                        geoId = b.GeoListId
                    })
                    .Join(_context.GeoLists, (a) => a.geoId, (b) => b.Id, (a, b) => new
                    {
                        lb = a.lb,
                        g = a.g,
                        code = b.Code
                    })
                    .Where(a => a.lb.UserId == u.Id && a.g.GcNum != null && a.g.GcNum != "0" && a.g.Status == 1 && a.code > 0)
                    .Sum(a => a.lb.VisitCnt)
                })
                .FirstOrDefault(u => u.Id == id);

            return Json(users);
        }


        [HttpPost("{id}")]
        public async Task<IActionResult> BlindUpdate(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await _context.Users
                .Include(u => u.PointHistorys.OrderByDescending(p => p.Id))
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user.Blinded != null)
            {
                user.Blinded = null;
            }
            else
            {
                user.Blinded = DateTime.UtcNow;
            }

            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(user);
        }
        
    }
}
