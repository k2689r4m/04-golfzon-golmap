using GolfZonWebApp.Data;
using GolfZonWebApp.Models;
using GolfZonWebApp.Scheduler.Managers;
using GolfZonWebApp.Scheduler.Repositories;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using GolfZonWebApp.Lib;
using Microsoft.AspNetCore.Http;

namespace GolfZonWebApp.Controllers
{
    [ApiController]
    public class ApiTestController : Controller
    {
        private readonly IManagerRepository repository;
        private readonly GolfzonContext context;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly HttpContext httpContext;

        public ApiTestController(IManagerRepository repository, GolfzonContext context, IHttpContextAccessor httpContextAccessor)
        {
            this.repository = repository;
            this.context = context;
            this.httpContextAccessor = httpContextAccessor;
            this.httpContext = httpContextAccessor.HttpContext;
        }

        [HttpGet("[controller]/index")]
        public IActionResult Index()
        {
            //return Json(1);
            return Json(repository.GetAllMileages());
        }

        [HttpGet("[controller]/test")]
        public IActionResult Test()
        {
            User user = context.Users.FirstOrDefault();

            if (user != null)
            {
                Mileage m = repository.AddMileage(new Mileage() { UserPossbMileage = 1, UserId = 1 });
                return Json(m);
            }

            return NotFound();
        }

        [HttpGet("[controller]/addtag/{reviewId}")]
        public async Task<IActionResult> AddTag (int reviewId)
        {
            Review _review = context.Reviews.FirstOrDefault();

            if (_review == null)
            {
                return Json("리뷰가 존재하지 않습니다.");
            }

            try
            {
                ReviewTag reviewTag = repository.RelateTag("편안한 골프장", _review.Id);
                return Json(reviewTag);
            }
            catch (Exception e)
            {
                return Json(e.Message);
            }
        }

        [HttpGet("api/usergolf/mileage")]
        public IActionResult GetMileage ()
        {
            int tokenUserId = JwtManager.getJwtId(httpContext);
            if (tokenUserId == 0) return Unauthorized();

            try
            {
                return Json(repository.GetMileage(tokenUserId));
            }
            catch
            {
                Mileage m = repository.AddMileage(new Mileage() { UserPossbMileage = tokenUserId * 100, UserId = tokenUserId });

                return Json(m);
            }
        }

        //[HttpGet("api/usergolf/suggestion")]
    }
}
