using GolfZonWebApp.Data;
using GolfZonWebApp.Models;
using GolfZonWebApp.Lib;
using GolfZonWebApp.Types;
using System;
using System.IO;
using System.Linq;
using System.Dynamic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using System.Globalization;
using GolfZonWebApp.Repositories;

namespace GolfZonWebApp.Controllers
{
    [ApiController]
    public class PointController : Controller
    {
        private readonly GolfzonContext _context;

        public PointController(GolfzonContext context)
        {
            _context = context;
        }

        [HttpGet("api/[controller]/object/all")]
        public async Task<ActionResult> GetObjectAll()
        {
            //var pointObjects = await _context.PointObjects
            //    .OrderByDescending(po => po.Id)
            //    .ToListAsync();


            var pointObjects = GzApi.GetDefaultSP(_context, "Admin_PointObject_All");

            return Json(pointObjects);
        }

        [HttpGet("api/[controller]/object/itemlist")]
        public async Task<ActionResult<List<PointObject>>> GetObjectItemList(int type)
        {
            //if (state == 0) 
            //{
            //    var date = DateTime.UtcNow.AddHours(9);
            //    var nowDate = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);

            //    var pointObjects = await _context.PointObjects
            //        .Where(po => po.Type == type)
            //        .Where(po => po.EndDate.CompareTo(nowDate) >= 0)
            //        .ToListAsync();

            //    var pointObjects2 = await _context.PointObjects
            //        .Where(po => po.Type == type)
            //        .ToListAsync();

            //    var d = new { type1 = pointObjects, type2 = pointObjects2 };

            //    return Json(d);
            //}
            //else
            //{
                var pointObjects = await _context.PointObjects
                    .Where(po => po.Type == type)
                    .ToListAsync();

                return pointObjects;
            //}
        }

        [HttpGet("api/[controller]/object/pastitem")]
        public async Task<ActionResult<List<PointObject>>> GetObjectPastItem(int type)
        {
            var pointObjects = await _context.PointObjects
                .Where(po => po.Type == type)
                .ToListAsync();

            return pointObjects;
        }

        [HttpPost("api/[controller]/object/set")]
        public async Task<IActionResult> ObjectSet([FromBody] List<PointObject> param)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var date = DateTime.UtcNow.AddHours(9);
            var nowDate = new DateTime(date.Year,date.Month,date.Day,0,0,0);
            //var culture = new CultureInfo("ko-KR");

            //foreach (var item in param)
            //{
            //    if (item.EndDate.CompareTo(nowDate) == -1)
            //    {
            //        var r = new { state = 404, id = item.Id, errorCode = 101, errorMsg = "날짜 오류"};

            //        return Json(r);
            //    }
                
            //    if (item.StartDate.CompareTo(nowDate) == 0 && item.EndDate.CompareTo(nowDate) == 0)
            //    {
            //        var r = new { state = 404, id = item.Id, errorCode = 102, errorMsg = "같은 날짜" };

            //        return Json(r);
            //    }
            //}

            try
            {
                foreach (var item in param)
                {
                    if (item.State == -1)
                    {
                        _context.PointObjects.Remove(item);
                    }
                    else
                    {
                        _context.PointObjects.Update(item);
                    }
                    
                    await _context.SaveChangesAsync();
                }

                return Ok();
            }
            catch(Exception)
            {
                return NotFound();
            }
        }


        [HttpGet("api/[controller]/history/all")]
        public async Task<ActionResult> GetHistoryAll(string option, string content, int page)
        {
            string error = "no result";

            if (page <= 0)
            {
                page = 1;
            }


            if (content == null && option == null)
            {
                var data = _context.PointHistorys
                                    .Include(po => po.User)
                                    .OrderByDescending(po => po.Id)
                                    .Select(po => new
                                    {
                                        po.Id,
                                        po.User.UserNum,
                                        po.User.Username,
                                        po.User.Fullname,
                                        po.User.Nickname,
                                        po.CreatedAt,
                                        po.Accumulate,
                                        po.Point
                                    });

                int pageIndex = page;
                var count = await data.CountAsync();
                var totalPages = (int)Math.Ceiling(count / (double)10);
                var items = await data.Skip((pageIndex - 1) * 10).Take(10).ToListAsync();

                return Json(new
                {
                    items = items,
                    page = pageIndex,
                    totalPages = totalPages,
                    error = ""
                });
            }
            else if (option != null && content != null)
            {
                string[] options = { "회원번호", "아이디", "이름", "닉네임" };
                if (Array.Exists(options, element => element == option))
                {
                    if (content != "")
                    {
                        var data =
                        (from pointHistory in _context.PointHistorys
                                                            .Include(po => po.User)
                                                            .OrderByDescending(po => po.Id)
                         where EF.Functions.Like(
                             option == "회원번호" ? pointHistory.User.UserNum.ToString() : (
                                 option == "아이디" ? pointHistory.User.Username : (
                                     option == "이름" ? pointHistory.User.Fullname : pointHistory.User.Nickname
                                     )
                                 )
                             , "%" + content + "%")
                         select pointHistory)
                        .Select(po => new
                        {
                            po.Id,
                            po.User.UserNum,
                            po.User.Username,
                            po.User.Fullname,
                            po.User.Nickname,
                            po.CreatedAt,
                            po.Accumulate,
                            po.Point
                        });


                        int pageIndex = page;
                        var count = await data.CountAsync();
                        var totalPages = (int)Math.Ceiling(count / (double)10);
                        var items = await data.Skip((pageIndex - 1) * 10).Take(10).ToListAsync();

                        return Json(new
                        {
                            items = items,
                            page = pageIndex,
                            totalPages = totalPages,
                            error = ""
                        });
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

            return Json(PaginatedList<int>.nothing(error));

            //var pointHistorys = await _context.PointHistorys
            //    .Include(po => po.User)
            //    .OrderByDescending(po => po.Id)
            //    .AsNoTracking()
            //    .ToListAsync();

            //return pointHistorys;
        }


        


        [HttpGet("api/[controller]/test2")]
        public async Task<ActionResult> test2()
        {
            return Json(GzApi.GetMileageBalance(1799767));
        }



    }
}
