using GolfZonWebApp.Data;
using GolfZonWebApp.Models;
using GolfZonWebApp.Lib;
using GolfZonWebApp.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using NetTopologySuite.Geometries;

namespace GolfZonWebApp.Controllers
{
    [ApiController]
    public class ReviewController : Controller
    {
        private readonly int STATE_NORMAL = 0;
        private readonly int STATE_DELETE = 1;
        private readonly int STATE_FORCED_DELETE = 2;
        private readonly GolfzonContext _context;
        public ReviewController(GolfzonContext context)
        {
            _context = context;
        }

        [HttpGet("api/[controller]/get/{id}")]
        public async Task<ActionResult<List<ReviewReport>>> GetItem(int id)
        {
            var reviewReports = await _context.ReviewReports
                .Include(r => r.User)
                .Where(re => re.ReviewId == id)
                .AsNoTracking()
                .AsSplitQuery()
                .ToListAsync();

            return Json(reviewReports);
        }

        [HttpGet("api/[controller]/all")]
        public async Task<ActionResult<List<Review>>> GetAll(string option, string content, int page)
        {
            string error = "no result";

            if (page <= 0)
            {
                page = 1;
            }

            IQueryable<Review> data = null;

            //var cons = await _context.Reviews
            //    .Include(re => re.ReviewGoods)
            //    .Include(re => re.User)
            //    .Include(re => re.GolfClub)
            //    .Include(re => re.Convenience)
            //    .OrderByDescending(re => re.Id)
            //    .AsNoTracking()
            //    .ToListAsync();

            //return Json(cons.Take(100));

            if (content == null && option == null)
            {
                data =
                    from review in _context.Reviews
                            .Include(re => re.ReviewGoods)
                            .Include(re => re.ReviewImages)
                            .Include(re => re.ReviewReports)
                            .Include(re => re.User)
                            .Include(re => re.GolfClub)
                            .Include(re => re.Convenience)
                            .OrderByDescending(re => re.Id)
                            .AsNoTracking()
                    select review;


            }
            else if (option != null && content != null)
            {
                string[] options = { "회원번호", "아이디", "이름", "닉네임", "장소" };
                if (Array.Exists(options, element => element == option))
                {
                    if (content != "")
                    {
                        data =
                        from review in _context.Reviews
                            .Include(re => re.ReviewGoods)
                            .Include(re => re.ReviewReports)
                            .Include(re => re.User)
                            .Include(re => re.GolfClub)
                            .Include(re => re.Convenience)
                            .OrderByDescending(re => re.Id)
                            .AsNoTracking()
                        where EF.Functions.Like(
                            option == "회원번호" ? review.User.UserNum.ToString() : (
                                option == "아이디" ? review.User.Username : (
                                    option == "이름" ? review.User.Fullname : (
                                        option == "닉네임" ? review.User.Nickname : (
                                            review.GolfClub != null ? review.GolfClub.Name : review.Convenience.Name
                                        )
                                    )
                                    )
                                )
                            , "%" + content + "%")
                        select review;
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

            PaginatedList<Review> reviews = await PaginatedList<Review>.CreateAsync(data, page);

            return Json(reviews.paginate());
        }


        [HttpPost("api/[controller]/representation")]
        public async Task<IActionResult> ReviewAdminRe([FromBody] IDDto param)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            int id = param.id;

            try
            {
                var dbReview = await _context.Reviews.SingleOrDefaultAsync(re => re.Id == id);

                if (dbReview != null)
                {
                    if (!dbReview.Representation)
                    {
                        List<Review> dbReviews = null;
                        if (dbReview.GolfClubId != null)
                        {
                            dbReviews = await _context.Reviews.Where(re_ => re_.GolfClubId == dbReview.GolfClubId).Where(re_ => re_.Representation).AsNoTracking().ToListAsync();
                        }
                        else
                        {
                            dbReviews = await _context.Reviews.Where(re_ => re_.ConvenienceId == dbReview.ConvenienceId).Where(re_ => re_.Representation).AsNoTracking().ToListAsync();
                        }

                        if (0 < dbReviews.Count)
                        {
                            return Json("-1");
                        }
                    }

                    dbReview.Representation = !dbReview.Representation;

                    _context.Reviews.Update(dbReview);
                    await _context.SaveChangesAsync();
                }

                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Json(true);
            }
        }



        [HttpPost("api/[controller]/representation/re")]
        public async Task<IActionResult> ReviewAdminReRe([FromBody] IDDto param)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            int id = param.id;

            try
            {
                var dbReview = await _context.Reviews.SingleOrDefaultAsync(re => re.Id == id); ;

                if (dbReview != null)
                {
                    if (!dbReview.Representation)
                    {
                        List<Review> dbReviews = null;
                        if (dbReview.GolfClubId != null)
                        {
                            dbReviews = await _context.Reviews.Where(re_ => re_.GolfClubId == dbReview.GolfClubId).Where(re_ => re_.Representation).ToListAsync();
                        }
                        else
                        {
                            dbReviews = await _context.Reviews.Where(re_ => re_.ConvenienceId == dbReview.ConvenienceId).Where(re_ => re_.Representation).ToListAsync();
                        }

                        if (0 < dbReviews.Count)
                        {
                            for (int i = 0; i < dbReviews.Count; i++)
                            {
                                dbReviews[i].Representation = false;
                            }
                            _context.Reviews.UpdateRange(dbReviews);
                        }
                    
                        dbReview.Representation = true;

                        _context.Reviews.Update(dbReview);
                        await _context.SaveChangesAsync();
                    }
                }

                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Json(true);
            }
        }



        [HttpPost("api/[controller]/state")]
        public async Task<IActionResult> ReviewAdminSt([FromBody] IDDto param)
        {
            
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            int id = param.id;

            try
            {
                var dbReview = await _context.Reviews.SingleOrDefaultAsync(re => re.Id == id);

                if (dbReview != null)
                {
                    if (dbReview.State != STATE_FORCED_DELETE)
                    {
                        dbReview.State = STATE_FORCED_DELETE;
                        dbReview.Representation = false;

                        _context.Reviews.Update(dbReview);
                        await _context.SaveChangesAsync();

                        UpdateNotification.DeleteReview(_context, dbReview.UserId, dbReview.GolfClubId, dbReview.ConvenienceId, dbReview.CreatedAt);
                    }
                }
                    

                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }


            return Ok();
        }

        public class IDDto
        {
            public int id { get; set; }
        }
        public class ReviewResult
        {
            public List<GolfClub> GolfClubs { get; set; }
        }

    }
}
