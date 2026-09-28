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
using Microsoft.Extensions.Options;
//using System.Text.Json;


namespace GolfZonWebApp.Controllers
{
    [ApiController]
    public class GolfClubController : Controller
    {
        private readonly GolfzonContext _context;
        private readonly IOptions<Geo> _geoDate;


        public GolfClubController(GolfzonContext context, IOptions<Geo> geo)
        {
            _context = context;
            _geoDate = geo;
        }

        [HttpGet("api/[controller]/get/refcon")]
        public async Task<ActionResult<List<GolfClub>>> GetRefConv(int golfId = 0)
        {
            if (golfId == 0) return NotFound();

            var conv = GzApi.GetSingleParamSP(_context, "ADMIN_GET_REFCON", "@P_GOLF_ID", golfId);
            return Json(conv);
        }

        [HttpGet("api/[controller]/test/match_format")]
        public async Task<ActionResult<List<GolfClub>>> MatchFormat()
        {
            var golfclubsDoesnotHaveClubhouse = _context.GolfClubs.Where(g => g.ClubHouse == null);
            var golfclubsDoesnotHaveShadeHouse = _context.GolfClubs.Where(g => g.ShadeHouses.Count == 0);

            foreach (var g in golfclubsDoesnotHaveClubhouse)
            {
                var emptyClubhouse = new ClubHouse
                {
                    GolfClubId = g.Id
                };

                _context.ClubHouses.Add(emptyClubhouse);
            }

            foreach (var g in golfclubsDoesnotHaveShadeHouse)
            {
                var emptyShadeHouse = new ShadeHouse
                {
                    GolfClubId = g.Id
                };

                _context.ShadeHouses.Add(emptyShadeHouse);
            }

            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpGet("api/[controller]/all")]
        public async Task<ActionResult<List<GolfClub>>> GetAll(string option, string content, int page)
        {
            //var hole = _context.GeoLists
            //    .Where(i => i.Id == 233)

            //    .FirstOrDefault();

            //return Json(hole);


            //for (int i=0;i<_context.Holes.Count();i++)
            //{
            //    Console.WriteLine(i);
            //    hole = _context.Holes
            //    .Where(ho => ho.Id == i)
            //    .Include(ho => ho.GeoList)
            //    .FirstOrDefault();
            //}




            string error = "no result";

            if (page <= 0)
            {
                page = 1;
            }


            if (content == null && option == null)
            {
                var data = _context.GolfClubs
                                    .Include(gc => gc.Courses)
                                    .Include(gc => gc.WrongInfos)
                                    .OrderByDescending(gc => gc.Id)
                                    .Select(gc => new
                                    {
                                        gc.Id,
                                        gc.GcNum,
                                        gc.GcNums,
                                        gc.Name,
                                        gc.Address,
                                        gc.Contact,
                                        gc.Status,
                                        Courses = gc.Courses.Select(c => new { c.Id }),
                                        gc.WrongInfos,
                                        ReviewCount = _context.Reviews.Where(r => r.GolfClubId == gc.Id && r.State == 0).Count(),
                                        Grade = _context.Reviews.Where(r => r.GolfClubId == gc.Id && r.State == 0).Count() > 0 ? _context.Reviews.Where(r => r.GolfClubId == gc.Id && r.State == 0).Average(r => (r.SCourse + r.SFacility + r.SFoodAndDrink + r.SCaddy) / 4) : 0
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
                string[] options = { "골프장번호", "골프장명", "주소", "전화번호" };
                if (Array.Exists(options, element => element == option))
                {
                    if (content != "")
                    {
                        var data =
                        (from golfclub in _context.GolfClubs
                                                            .Include(gc => gc.GeoList)
                                                            .Include(gc => gc.Courses)
                                                            .Include(gc => gc.WrongInfos)
                                                            .OrderByDescending(gc => gc.Id)
                        where EF.Functions.Like(
                            option == "골프장번호" ? golfclub.GcNum : ( 
                                option == "골프장명" ? golfclub.Name : (
                                    option == "주소" ? golfclub.Address : golfclub.Contact
                                    )
                                )
                            , "%" + content + "%") select golfclub)
                        .Select(gc => new
                        {
                            gc.Id,
                            gc.GcNum,
                            gc.GcNums,
                            gc.Name,
                            gc.Address,
                            gc.Contact,
                            gc.Status,
                            Courses = gc.Courses.Select(c => new { c.Id }),
                            gc.WrongInfos,
                            ReviewCount = _context.Reviews.Where(r => r.GolfClubId == gc.Id).Count(),
                            Grade = _context.Reviews.Where(r => r.GolfClubId == gc.Id).Count() > 0 ? _context.Reviews.Where(r => r.GolfClubId == gc.Id).Average(r => (r.SCourse + r.SFacility + r.SFoodAndDrink + r.SCaddy) / 4) : 0
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

           

            //var golfClubs = await _context.GolfClubs
            //.Include(gc => gc.GeoList)
            //.Include(gc => gc.GolfClubImages)
            //.Include(gc => gc.Courses)
            //.ThenInclude(gc => gc.Holes)
            //.ThenInclude(ho => ho.GeoList)
            //.Include(gc => gc.ClubHouse)
            //.ThenInclude(gh => gh.GeoList)
            //.Include(gc => gc.ClubHouse.ClubHouseMenus)
            //.Include(gc => gc.ShadeHouses)
            //.ThenInclude(sh => sh.GeoList)
            //.Include(gc => gc.ShadeHouseMenus)
            //.Include(gc => gc.FilterLists)
            //.Include(gc => gc.WrongInfos)
            //.OrderByDescending(gc => gc.Id)
            //.AsSplitQuery()
            //.ToListAsync();
            //Request.

            //return Request.CreateResponse(HttpStatusCode.OK, result);
        }

        [HttpGet("api/[controller]/get/{id}")]
        public async Task<ActionResult<List<GolfClub>>> GetIteml(int id)
        {
            var golfClubs = await _context.GolfClubs
                .AsNoTracking()
                .Include(gc => gc.GeoList)
                .Include(gc => gc.GolfClubImages.OrderBy(gc => gc.Id))
                .Include(gc => gc.Courses)
                .ThenInclude(gc => gc.Holes)
                .ThenInclude(ho => ho.GeoList)
                .Include(gc => gc.ClubHouse)
                .ThenInclude(gh => gh.GeoList)
                .Include(gc => gc.ClubHouse.ClubHouseMenus)
                .Include(gc => gc.ShadeHouses)
                .ThenInclude(sh => sh.GeoList)
                .Include(gc => gc.ShadeHouseMenus)
                .Include(gc => gc.FilterLists)
                //.Skip(1)
                //.OrderBy(x => x.Id)
                .AsSplitQuery()
                .SingleOrDefaultAsync(gc => gc.Id == id);

            return Json(golfClubs);
        }

        [HttpPost("api/[controller]/create/golfclub")]
        public async Task<IActionResult> CreateGolfClub([FromForm] GolfClubDto param)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            GolfClub gc = null;
            List<GolfClubImage> gcImg = null;
            List<Upload.UploadedFile> newFileInfoList = null;

            //골프클럽 유호성 검사
            if (param.GolfClub != null)
            {
                gc = JsonConvert.DeserializeObject<GolfClub>(param.GolfClub);

                if (!TryValidateModel(gc, nameof(gc)))
                {
                    return BadRequest(ModelState);
                }
            }

            if (param.GolfClubImages != null)
            {
                gcImg = JsonConvert.DeserializeObject<List<GolfClubImage>>(param.GolfClubImages);

                if (!TryValidateModel(gcImg, nameof(gcImg)))
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
                    newFileInfoList = await Upload.UploadImageAndReturnInfo(param.FileImages, Upload.IMAGE_TYPE_ADMIN);
                }

                //골프클럽
                if (gc != null)
                {
                    if (0 < gc.Id)
                    {
                        var dbGolf = await _context.GolfClubs.Where(gc_ => gc_.Id == gc.Id).Include(_gc => _gc.FilterLists).AsNoTracking().SingleAsync();

                        if (gc.ImageIndex == -1)
                        {
                            Upload.DeleteImage(dbGolf.TFileUri, Upload.IMAGE_TYPE_ADMIN);

                            gc.TFileName = null;
                            gc.TFileOriginalName = null;
                            gc.TFileUri = null;
                        }
                        else if (gc.ImageIndex != null)
                        {
                            if (dbGolf.MFileUri != null)
                            {
                                Upload.DeleteImage(dbGolf.TFileUri, Upload.IMAGE_TYPE_ADMIN);
                            }

                            var idx = gc.ImageIndex.Value;
                            gc.TFileName = newFileInfoList[idx].Name;
                            gc.TFileOriginalName = newFileInfoList[idx].OriginalName;
                            gc.TFileUri = newFileInfoList[idx].Uri;
                        }


                        if (gc.FilterLists != null)
                        {
                            var existFilterList = dbGolf.FilterLists;

                            if (existFilterList != null)
                            {
                                for (int i = 0; i < existFilterList.Count; i++)
                                {
                                    if (!gc.FilterLists.Any(f => f.Id == existFilterList.ElementAt(i).Id))
                                    {
                                        _context.FilterLists.Remove(_context.FilterLists.Find(existFilterList.ElementAt(i).Id));
                                    }
                                }
                            }
                        }

                        _context.GolfClubs.Update(gc);
                        await _context.SaveChangesAsync();


                        //if (gc.Address.StartsWith("서울") || gc.Address.StartsWith("경기") || gc.Address.StartsWith("경기도"))
                        //{
                        //    if (gc.GeoListId != null)
                        //    {
                        //        var dbGeo = await _context.GeoLists.Where(geo => geo.Id == gc.GeoListId).SingleOrDefaultAsync();

                        //        if (dbGeo != null)
                        //        {
                        //            dbGeo.Code = gc.Direction.Value;

                        //            _context.GeoLists.Update(dbGeo);
                        //            await _context.SaveChangesAsync();
                        //        }
                        //    }
                        //}
                    }
                    else
                    {
                        //골프클럽
                        if (gc.ImageIndex != null)
                        {
                            var idx = gc.ImageIndex.Value;
                            gc.TFileName = newFileInfoList[idx].Name;
                            gc.TFileOriginalName = newFileInfoList[idx].OriginalName;
                            gc.TFileUri = newFileInfoList[idx].Uri;
                        }

                        _context.GolfClubs.Update(gc);
                        await _context.SaveChangesAsync();
                    }

                    


                    //골프클럽 이미지
                    if (gcImg != null)
                    {
                        var dbImg = await _context.GolfClubImages.Where(gci => gci.GolfClubId == gc.Id).AsNoTracking().ToArrayAsync();

                        for (int i = 0; i < dbImg.Count(); i++)
                        {
                            if (gcImg.Where(gi => gi.Id == dbImg[i].Id).FirstOrDefault() == null)
                            {
                                if (dbImg[i].Uri != null)
                                {
                                    Upload.DeleteImage(dbImg[i].Uri, Upload.IMAGE_TYPE_ADMIN);
                                }

                                _context.Remove(_context.GolfClubImages.Find(dbImg[i].Id));
                            }
                            await _context.SaveChangesAsync();
                        }

                        for (int i = 0; i < gcImg.Count; i++)
                        {
                            if (0 < gcImg[i].Id)
                            {
                                var tg = await _context.GolfClubImages.Where(gc => gc.Id == gcImg[i].Id).AsNoTracking().SingleAsync();

                                if (gcImg[i].ImageIndex != null)
                                {
                                    gcImg[i].GolfClubId = gc.Id;

                                    if (tg != null && tg.Uri != null)
                                    {
                                        Upload.DeleteImage(tg.Uri, Upload.IMAGE_TYPE_ADMIN);
                                    }

                                    var idx = gcImg[i].ImageIndex.Value;
                                    gcImg[i].Name = newFileInfoList[idx].Name;
                                    gcImg[i].OriginalName = newFileInfoList[idx].OriginalName;
                                    gcImg[i].Uri = newFileInfoList[idx].Uri;
                                }
                            }
                            else
                            {
                                gcImg[i].GolfClubId = gc.Id;

                                if (gcImg[i].ImageIndex != null)
                                {
                                    var idx = gcImg[i].ImageIndex.Value;
                                    gcImg[i].Name = newFileInfoList[idx].Name;
                                    gcImg[i].OriginalName = newFileInfoList[idx].OriginalName;
                                    gcImg[i].Uri = newFileInfoList[idx].Uri;
                                }
                            }

                            gcImg[i].Next = i;


                            _context.GolfClubImages.Update(gcImg[i]);
                            await _context.SaveChangesAsync();
                        }
                    }

                    return Json(gc.Id);
                }

                return Json(-1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Json(-1);
            }
        }

        [HttpPost("api/[controller]/create/course")]
        public async Task<IActionResult> CreateCourse([FromForm] CourseDto param)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            List<Course> gcCo = null;

            //골프클럽 유호성 검사 asdsadasdgffgfdd
            if (param.Courses != null)
            {
                gcCo = JsonConvert.DeserializeObject<List<Course>>(param.Courses);

                if (!TryValidateModel(gcCo, nameof(gcCo)))
                {
                    return BadRequest(ModelState);
                }
            }

            try
            {
                var dbGolf = await _context.GolfClubs.Where(gc => gc.Id == param.GolfClubId).SingleOrDefaultAsync();

                if (dbGolf != null)
                {
                    dbGolf.FairwayGrass = param.FairwayGrass;
                    dbGolf.GreenGrass = param.GreenGrass;
                    dbGolf.DifficultyLevel = param.DifficultyLevel;
                    dbGolf.GreenDifficultyLevel = param.GreenDifficultyLevel;
                    dbGolf.SignatureHole = param.SignatureHole;
                    dbGolf.HoleLength = param.HoleLength;
                    dbGolf.CoursesType = param.CoursesType;

                    _context.GolfClubs.Update(dbGolf);
                    
                    dbGolf = null;
                }

                //골프클럽 코스
                if (gcCo != null)
                {
                    var tgs = await _context.Courses.Where(gc_ => gc_.GolfClubId == param.GolfClubId).AsNoTracking().ToListAsync();

                    //삭제
                    for (int i = 0; i < tgs.Count; i++)
                    {
                        if (gcCo.Where(co => co.Id == tgs[i].Id).FirstOrDefault() == null)
                        {
                            _context.Courses.Remove(_context.Courses.Find(tgs[i].Id));
                            await _context.SaveChangesAsync();
                        }
                    }


                    for (int i = 0; i < gcCo.Count; i++)
                    {
                        List<Hole> hole = null;
                        if (gcCo[i].Holes != null)
                        {
                            hole = new List<Hole>(gcCo[i].Holes);
                            gcCo[i].Holes.Clear();
                        }
                        gcCo[i].GolfClubId = param.GolfClubId;

                        _context.Courses.Update(gcCo[i]);
                        await _context.SaveChangesAsync();

                        //코스 홀
                        if (hole != null)
                        {
                            for (int ii = 0; ii < hole.Count; ii++)
                            {
                                hole[ii].CourseId = gcCo[i].Id;

                                _context.Holes.Update(hole[ii]);
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                }

                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Json(-1);
            }
        }

        [HttpPost("api/[controller]/create/house")]
        public async Task<IActionResult> CreateClubHouse([FromForm] HouseDto param)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            

            ClubHouse gcCh = null;
            List<ShadeHouse> gcSh = null;
            List<ShadeHouseMenu> gcShMenu = null;
            List<Upload.UploadedFile> newFileInfoList = null;

            //골프클럽 유호성 검사
            if (param.ClubHouse != null)
            {
                gcCh = JsonConvert.DeserializeObject<ClubHouse>(param.ClubHouse);

                if (!TryValidateModel(gcCh, nameof(gcCh)))
                {
                    return BadRequest(ModelState);
                }
            }

            if (param.ShadeHouses != null)
            {
                gcSh = JsonConvert.DeserializeObject<List<ShadeHouse>>(param.ShadeHouses);

                if (!TryValidateModel(gcSh, nameof(gcSh)))
                {
                    return BadRequest(ModelState);
                }
            }

            if (param.ShadeHouseMenus != null)
            {
                gcShMenu = JsonConvert.DeserializeObject<List<ShadeHouseMenu>>(param.ShadeHouseMenus);

                if (!TryValidateModel(gcShMenu, nameof(gcShMenu)))
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
                    newFileInfoList = await Upload.UploadImageAndReturnInfo(param.FileImages, Upload.IMAGE_TYPE_ADMIN);
                }

                //이것은 그늘집 대표 이미지
                var dbGolf = await _context.GolfClubs.Where(gc => gc.Id == param.GolfClubId).SingleOrDefaultAsync();

                if (param.ImageIndex == -1)
                {
                    Upload.DeleteImage(dbGolf.MFileUri, Upload.IMAGE_TYPE_ADMIN);

                    dbGolf.MFileName = null;
                    dbGolf.MFileOriginalName = null;
                    dbGolf.MFileUri = null;

                    _context.GolfClubs.Update(dbGolf);
                    await _context.SaveChangesAsync();
                }
                else if (param.ImageIndex != null)
                {
                    if (dbGolf.MFileUri != null)
                    {
                        Upload.DeleteImage(dbGolf.MFileUri, Upload.IMAGE_TYPE_ADMIN);
                    }

                    var idx = param.ImageIndex.Value;
                    dbGolf.MFileName = newFileInfoList[idx].Name;
                    dbGolf.MFileOriginalName = newFileInfoList[idx].OriginalName;
                    dbGolf.MFileUri = newFileInfoList[idx].Uri;

                    _context.GolfClubs.Update(dbGolf);
                    await _context.SaveChangesAsync();
                }
                dbGolf = null;


                //클럽 하우스
                if (gcCh != null)
                {
                    var dbCh = await _context.ClubHouses.Where(gc => gc.GolfClubId == param.GolfClubId).AsNoTracking().SingleOrDefaultAsync();

                    List<ClubHouseMenu> menu = null;
                    if (gcCh.ClubHouseMenus != null)
                    {
                        menu = new List<ClubHouseMenu>(gcCh.ClubHouseMenus);
                        gcCh.ClubHouseMenus.Clear();
                    }

                    gcCh.GolfClubId = param.GolfClubId;

                    if (gcCh.ImageIndex == -1)
                    {
                        Upload.DeleteImage(dbCh.MFileUri, Upload.IMAGE_TYPE_ADMIN);

                        gcCh.MFileName = null;
                        gcCh.MFileOriginalName = null;
                        gcCh.MFileUri = null;
                    }
                    else if (gcCh.ImageIndex != null)
                    {
                        if (dbCh != null && dbCh.MFileUri != null)
                        {
                            Upload.DeleteImage(dbCh.MFileUri, Upload.IMAGE_TYPE_ADMIN);
                        }

                        var idx = gcCh.ImageIndex.Value;
                        gcCh.MFileName = newFileInfoList[idx].Name;
                        gcCh.MFileOriginalName = newFileInfoList[idx].OriginalName;
                        gcCh.MFileUri = newFileInfoList[idx].Uri;
                    }

                    _context.ClubHouses.Update(gcCh);
                    await _context.SaveChangesAsync();


                    //클럽하우스 메뉴
                    if (menu != null)
                    {
                        var dbChMenus = await _context.ClubHouseMenus.Where(chm => chm.ClubHouseId == gcCh.Id).AsNoTracking().ToListAsync();

                        for (int ii = 0; ii < dbChMenus.Count; ii++)
                        {
                            if (menu.Where(mn => mn.Id == dbChMenus[ii].Id).FirstOrDefault() == null)
                            {
                                if (dbChMenus[ii] != null &&  dbChMenus[ii].MFileUri != null)
                                {
                                    Upload.DeleteImage(dbChMenus[ii].MFileUri, Upload.IMAGE_TYPE_ADMIN);
                                }
                                _context.ClubHouseMenus.Remove(_context.ClubHouseMenus.Find(dbChMenus[ii].Id));
                                await _context.SaveChangesAsync();
                            }
                        }

                        for (int ii = 0; ii < menu.Count; ii++)
                        {
                            var tg_ = dbChMenus.Where(chm => chm.Id == menu[ii].Id).FirstOrDefault();

                            menu[ii].ClubHouseId = gcCh.Id;
                            menu[ii].Next = ii;

                            if (menu[ii].ImageIndex == -1)
                            {
                                Upload.DeleteImage(tg_.MFileUri, Upload.IMAGE_TYPE_ADMIN);

                                menu[ii].MFileName = null;
                                menu[ii].MFileOriginalName = null;
                                menu[ii].MFileUri = null;
                            }
                            else if (menu[ii].ImageIndex != null)
                            {
                                if (tg_ != null && tg_.MFileUri != null)
                                {
                                    Upload.DeleteImage(tg_.MFileUri, Upload.IMAGE_TYPE_ADMIN);
                                }

                                var idx = menu[ii].ImageIndex.Value;
                                menu[ii].MFileName = newFileInfoList[idx].Name;
                                menu[ii].MFileOriginalName = newFileInfoList[idx].OriginalName;
                                menu[ii].MFileUri = newFileInfoList[idx].Uri;
                            }

                            _context.ClubHouseMenus.Update(menu[ii]);
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                //그늘집
                if (gcSh != null)
                {
                    var dbSh = await _context.ShadeHouses.Where(sh => sh.GolfClubId == param.GolfClubId).AsNoTracking().ToListAsync();

                    for (int ii = 0; ii < dbSh.Count; ii++)
                    {
                        if (gcSh.Where(gs => gs.Id == dbSh[ii].Id).FirstOrDefault() == null)
                        {
                            _context.ShadeHouses.Remove(_context.ShadeHouses.Find(dbSh[ii].Id));
                            await _context.SaveChangesAsync();
                        }
                    }

                    for (int i = 0; i < gcSh.Count; i++)
                    {
                        gcSh[i].GolfClubId = param.GolfClubId;

                        _context.ShadeHouses.Update(gcSh[i]);
                        await _context.SaveChangesAsync();
                    }
                }




                //그늘집 메뉴
                if (gcShMenu != null)
                {
                    //for (int i = 0; i < gcShMenu.Count; i++)
                    //{
                    //    if (gcShMenu[i].ImageIndex != null)
                    //    {
                    //        var idx = gcShMenu[i].ImageIndex.Value;
                    //        gcShMenu[i].MFileName = newFileInfoList[idx].Name;
                    //        gcShMenu[i].MFileOriginalName = newFileInfoList[idx].OriginalName;
                    //        gcShMenu[i].MFileUri = newFileInfoList[idx].Uri;
                    //    }
                    //    _context.ShadeHouseMenus.Add(gcShMenu[i]);
                    //    await _context.SaveChangesAsync();
                    //}



                    var dbShMenus = await _context.ShadeHouseMenus.Where(shm => shm.GolfClubId == param.GolfClubId).AsNoTracking().ToListAsync();
                    for (int ii = 0; ii < dbShMenus.Count; ii++)
                    {
                        if (gcShMenu.Where(gsm => gsm.Id == dbShMenus[ii].Id).FirstOrDefault() == null)
                        {
                            _context.ShadeHouseMenus.Remove(_context.ShadeHouseMenus.Find(dbShMenus[ii].Id));
                            await _context.SaveChangesAsync();
                        }
                    }

                    for (int i = 0; i < gcShMenu.Count; i++)
                    {
                        var tg = dbShMenus.Where(shm => shm.Id == gcShMenu[i].Id).FirstOrDefault();
                        gcShMenu[i].GolfClubId = param.GolfClubId;

                        if (gcShMenu[i].ImageIndex == -1)
                        {
                            Upload.DeleteImage(tg.MFileUri, Upload.IMAGE_TYPE_ADMIN);

                            gcShMenu[i].MFileName = null;
                            gcShMenu[i].MFileOriginalName = null;
                            gcShMenu[i].MFileUri = null;
                        }
                        else if (gcShMenu[i].ImageIndex != null)
                        {
                            if (tg != null && tg.MFileUri != null)
                            {
                                Upload.DeleteImage(tg.MFileUri, Upload.IMAGE_TYPE_ADMIN);
                            }

                            var idx = gcShMenu[i].ImageIndex.Value;
                            gcShMenu[i].MFileName = newFileInfoList[idx].Name;
                            gcShMenu[i].MFileOriginalName = newFileInfoList[idx].OriginalName;
                            gcShMenu[i].MFileUri = newFileInfoList[idx].Uri;
                        }

                        _context.ShadeHouseMenus.Update(gcShMenu[i]);
                        await _context.SaveChangesAsync();
                    }
                }

                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Json(-1);
            }
        }

        [HttpPost("api/[controller]/create/geo")]
        public async Task<IActionResult> CreateGeo([FromForm] GeoDto param)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var settings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
            foreach (var conv in GeoJsonSerializer.Create(settings, new GeometryFactory(new PrecisionModel(), 4326)).Converters)
            {
                settings.Converters.Add(conv);
            }

            GolfClubGeo gc = null;
            List<HoleGeo> gcHo = null;
            ClubHouseGeo gcCh = null;
            List<ShadeHouse> gcSh = null;


            //골프클럽 유호성 검사
            if (param.GolfClubGeo != null)
            {
                gc = JsonConvert.DeserializeObject<GolfClubGeo>(param.GolfClubGeo, settings);

                if (!TryValidateModel(gc, nameof(gc)))
                {
                    return BadRequest(ModelState);
                }

                if (gc.GeoList != null)
                {
                    if (gc.GeoList.Position.IsValid)
                    {
                        if (typeof(Polygon) == gc.GeoList.Position.GetType())
                        {
                            var _geo = (Polygon)gc.GeoList.Position;

                            if (!_geo.Shell.IsCCW)
                            {
                                gc.GeoList.Position = _geo.Reverse();
                            }
                        }

                        gc.GeoList.Code = GeoTool.GetCode(gc.GeoList.Position.Centroid, _geoDate.Value.features);
                    }
                    else
                    {
                        return Json("도형에러");
                    }
                }
            }

            if (param.HoleGeo != null)
            {
                gcHo = JsonConvert.DeserializeObject<List<HoleGeo>>(param.HoleGeo, settings);

                if (!TryValidateModel(gcHo, nameof(gcHo)))
                {
                    return BadRequest(ModelState);
                }

                for (int i = 0; i < gcHo.Count; i++)
                {
                    if (gcHo[i].GeoList != null)
                    {
                        if (gcHo[i].GeoList.Position.IsValid)
                        {
                            if (typeof(Polygon) == gcHo[i].GeoList.Position.GetType())
                            {
                                var _geo = (Polygon)gcHo[i].GeoList.Position;
                                if (!_geo.Shell.IsCCW)
                                {
                                    gcHo[i].GeoList.Position = _geo.Reverse();
                                }
                            }

                            gcHo[i].GeoList.Code = GeoTool.GetCode(gcHo[i].GeoList.Position.Centroid, _geoDate.Value.features);
                        }
                        else
                        {
                            return Json("도형에러");
                        }
                    }
                }
            }

            if (param.ClubHouseGeo != null)
            {
                gcCh = JsonConvert.DeserializeObject<ClubHouseGeo>(param.ClubHouseGeo, settings);

                if (!TryValidateModel(gcCh, nameof(gcCh)))
                {
                    return BadRequest(ModelState);
                }

                if (gcCh.GeoList != null)
                {
                    if (gcCh.GeoList.Position.IsValid)
                    {
                        if (typeof(Polygon) == gcCh.GeoList.Position.GetType())
                        {
                            var _geo = (Polygon)gcCh.GeoList.Position;

                            if (!_geo.Shell.IsCCW)
                            {
                                gcCh.GeoList.Position = _geo.Reverse();
                            }
                        }

                        gcCh.GeoList.Code = GeoTool.GetCode(gcCh.GeoList.Position.Centroid, _geoDate.Value.features);
                    }
                    else
                    {
                        return Json("도형에러");
                    }
                }
            }

            if (param.ShadeHouses != null)
            {
                gcSh = JsonConvert.DeserializeObject<List<ShadeHouse>>(param.ShadeHouses, settings);

                if (!TryValidateModel(gcSh, nameof(gcSh)))
                {
                    return BadRequest(ModelState);
                }

                for (int i = 0; i < gcSh.Count; i++)
                {
                    if (gcSh[i].GeoList != null)
                    {
                        if (gcSh[i].GeoList.Position.IsValid)
                        {
                            if (typeof(Polygon) == gcSh[i].GeoList.Position.GetType())
                            {
                                var _geo = (Polygon)gcSh[i].GeoList.Position;

                                if (!_geo.Shell.IsCCW)
                                {
                                    gcSh[i].GeoList.Position = _geo.Reverse();
                                }
                            }

                            gcSh[i].GeoList.Code = GeoTool.GetCode(gcSh[i].GeoList.Position.Centroid, _geoDate.Value.features);
                        }
                        else
                        {
                            return Json("도형에러");
                        }
                    }
                }
            }

            try
            {
                if (gc != null && gc.GeoList != null)
                {
                    var dbGolf = await _context.GolfClubs.Where(gc_ => gc_.Id == gc.Id).SingleOrDefaultAsync();

                    if (dbGolf != null)
                    {
                        _context.GeoLists.Update(gc.GeoList);
                        await _context.SaveChangesAsync();

                        dbGolf.GeoListId = gc.GeoList.Id;
                        _context.GolfClubs.Update(dbGolf);
                        await _context.SaveChangesAsync();
                    }
                }

                if (gcHo != null)
                {
                    for (int i = 0; i < gcHo.Count; i++)
                    {
                        if (gcHo[i].GeoList != null)
                        {
                            var dbHole = await _context.Holes.Where(ho_ => ho_.Id == gcHo[i].Id).SingleOrDefaultAsync();

                            if (dbHole != null)
                            {
                                _context.GeoLists.Update(gcHo[i].GeoList);
                                await _context.SaveChangesAsync();

                                dbHole.GeoListId = gcHo[i].GeoList.Id;
                                _context.Holes.Update(dbHole);
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                }

                //그늘집
                if (gcSh != null)
                {
                    var tgs = await _context.ShadeHouses
                        .Where(sh_ => sh_.GolfClubId == param.GolfClubId)
                        .AsNoTracking()
                        .ToListAsync();

                    for (int ii = 0; ii < tgs.Count; ii++)
                    {
                        if (gcSh.Where(gs => gs.Id == tgs[ii].Id).FirstOrDefault() == null)
                        {
                            _context.ShadeHouses.Remove(_context.ShadeHouses.Find(tgs[ii].Id));
                            await _context.SaveChangesAsync();
                        }
                    }

                    for (int i = 0; i < gcSh.Count; i++)
                    {
                        if (gcSh[i].GeoList != null)
                        {
                            gcSh[i].GolfClubId = param.GolfClubId;

                            _context.GeoLists.Update(gcSh[i].GeoList);
                            await _context.SaveChangesAsync();

                            _context.ShadeHouses.Update(gcSh[i]);
                            await _context.SaveChangesAsync();
                        }
                    }
                }


                if (gcCh != null && gcCh.GeoList != null)
                {
                    var dbClub = await _context.ClubHouses.Where(ch_ => ch_.Id == gcCh.Id).SingleOrDefaultAsync();

                    if (dbClub != null)
                    {
                        _context.GeoLists.Update(gcCh.GeoList);
                        await _context.SaveChangesAsync();

                        dbClub.GeoListId = gcCh.GeoList.Id;
                        _context.ClubHouses.Update(dbClub);
                        await _context.SaveChangesAsync();

                        GzApi.UpdateWeather(_context, gcCh.GeoList.Id, param.GolfClubId);
                    }
                }


                

                //if (gc != null)
                //{
                //    GzApi.UpdateWeather(_context, gcCh.GeoList.Id, dbClub.GolfClubId);
                //}

                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                return Json(-1);
            }
        }









        [HttpPost("api/[controller]/geo")]
        public async Task<ActionResult> setGeo(List<TestGeo> tGeo)
        {
            List<TestGeoRe> reData = new List<TestGeoRe>();


            foreach (var item in tGeo)
            {
                
                GeoList newGeo = null;
                    
                if (item.Geo != null)
                {
                    try
                    {
                        if (item.Geo.IsValid)
                        {
                            if (typeof(Polygon) == item.Geo.GetType())
                            {
                                var _geo = (Polygon)item.Geo;

                                if (!_geo.Shell.IsCCW)
                                {
                                    item.Geo = _geo.Reverse();
                                }
                            }

                            newGeo.Position = item.Geo;
                            newGeo.Code = GeoTool.GetCode(item.Geo.Centroid, _geoDate.Value.features);
                            newGeo.Group = 3;

                            _context.GeoLists.Add(newGeo);
                            _context.SaveChanges();


                            var dbHole = _context.Holes.Find(item.Id);

                            if (dbHole != null)
                            {
                                dbHole.GeoListId = newGeo.Id;
                                _context.Holes.Update(dbHole);
                                _context.SaveChanges();
                            }


                            reData.Add(new TestGeoRe
                            {
                                Id = item.Id,
                                Re = "true"
                            });
                        }
                        else
                        {
                            reData.Add(new TestGeoRe
                            {
                                Id = item.Id,
                                Re = "도형 에러"
                            });
                        }

                    }
                    catch (Exception e)
                    {
                        reData.Add(new TestGeoRe
                        {
                            Id = item.Id,
                            Re = e.ToString()
                        });
                    }
                }
                else
                {
                    reData.Add(new TestGeoRe
                    {
                        Id = item.Id,
                        Re = "Geo Null"
                    });
                }
            }


            return Json(reData);
        }













        [HttpPut("api/[controller]/update/{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] GolfClubDto_ param) 
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            var settings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
            foreach (var conv in GeoJsonSerializer.Create(settings, new GeometryFactory(new PrecisionModel(), 4326)).Converters)
            {
                settings.Converters.Add(conv);
            }

            GolfClub gc = null;
            List<GolfClubImage> gcImg = null;
            List<Course> gcCo = null;
            ClubHouse gcCh = null;
            List<ShadeHouse> gcSh = null;
            List<ShadeHouseMenu> gcShMenu = null;
            List<Upload.UploadedFile> newFileInfoList = null;

            //골프클럽 유호성 검사
            if (param.GolfClub != null)
            {
                gc = JsonConvert.DeserializeObject<GolfClub>(param.GolfClub, settings);

                if (!TryValidateModel(gc, nameof(gc)))
                {
                    return BadRequest(ModelState);
                }

                if (gc.GeoList != null)
                {
                    if (gc.GeoList.Position.IsValid)
                    {
                        if (typeof(Polygon) == gc.GeoList.Position.GetType())
                        {
                            var _geo = (Polygon)gc.GeoList.Position;

                            if (!_geo.Shell.IsCCW)
                            {
                                gc.GeoList.Position = _geo.Reverse();
                            }
                        }
                    }
                    else
                    {
                        return Json("도형에러");
                    }

                    if (gc.FilterLists != null)
                    {
                        var dbGolf = await _context.GolfClubs.Include(_gc => _gc.FilterLists).AsNoTracking().SingleOrDefaultAsync(_gc => _gc.Id == gc.Id);
                        var existFilterList = dbGolf.FilterLists;

                        if (existFilterList != null)
                        {

                            for (int i = 0; i < existFilterList.Count; i++)
                            {
                                if (!gc.FilterLists.Any(f => f.Id == existFilterList.ElementAt(i).Id))
                                {
                                    _context.FilterLists.Remove(_context.FilterLists.Find(existFilterList.ElementAt(i).Id));
                                }
                            }
                        }
                    }
                }
            }

            if (param.GolfClubImages != null)
            {
                gcImg = JsonConvert.DeserializeObject<List<GolfClubImage>>(param.GolfClubImages, settings);

                if (!TryValidateModel(gcImg, nameof(gcImg)))
                {
                    return BadRequest(ModelState);
                }
            }

            if (param.Courses != null)
            {
                gcCo = JsonConvert.DeserializeObject<List<Course>>(param.Courses, settings);

                if (!TryValidateModel(gcCo, nameof(gcCo)))
                {
                    return BadRequest(ModelState);
                }

                for (int i = 0; i < gcCo.Count; i++)
                {
                    if (gcCo[i].Holes != null)
                    {
                        for (int ii = 0; ii < gcCo[i].Holes.Count; ii++)
                        {
                            if (gcCo[i].Holes.ElementAt(ii).GeoList != null)
                            {
                                if (gcCo[i].Holes.ElementAt(ii).GeoList.Position.IsValid)
                                {
                                    if (typeof(Polygon) == gcCo[i].Holes.ElementAt(ii).GeoList.Position.GetType())
                                    {
                                        var _geo = (Polygon)gcCo[i].Holes.ElementAt(ii).GeoList.Position;
                                        if (!_geo.Shell.IsCCW)
                                        {
                                            gcCo[i].Holes.ElementAt(ii).GeoList.Position = _geo.Reverse();
                                        }
                                    }
                                }
                                else
                                {
                                    return Json("도형에러");
                                }
                            }
                        }
                    }
                }
            }

            if (param.ClubHouse != null)
            {
                gcCh = JsonConvert.DeserializeObject<ClubHouse>(param.ClubHouse, settings);

                if (!TryValidateModel(gcCh, nameof(gcCh)))
                {
                    return BadRequest(ModelState);
                }

                if (gcCh.GeoList != null)
                {
                    if (gcCh.GeoList.Position.IsValid)
                    {
                        if (typeof(Polygon) == gcCh.GeoList.Position.GetType())
                        {
                            var _geo = (Polygon)gcCh.GeoList.Position;

                            if (!_geo.Shell.IsCCW)
                            {
                                gcCh.GeoList.Position = _geo.Reverse();
                            }
                        }
                    }
                    else
                    {
                        return Json("도형에러");
                    }
                }

            }

            if (param.ShadeHouses != null)
            {
                gcSh = JsonConvert.DeserializeObject<List<ShadeHouse>>(param.ShadeHouses, settings);

                if (!TryValidateModel(gcSh, nameof(gcSh)))
                {
                    return BadRequest(ModelState);
                }

                for (int i=0;i<gcSh.Count;i++)
                {
                    if (gcSh[i].GeoList != null)
                    {
                        if (gcSh[i].GeoList.Position.IsValid)
                        {
                            if (typeof(Polygon) == gcSh[i].GeoList.Position.GetType())
                            {
                                var _geo = (Polygon)gcSh[i].GeoList.Position;

                                if (!_geo.Shell.IsCCW)
                                {
                                    gcSh[i].GeoList.Position = _geo.Reverse();
                                }
                            }
                        }
                        else
                        {
                            return Json("도형에러");
                        }
                    }
                }

                

            }

            if (param.ShadeHouseMenus != null)
            {
                gcShMenu = JsonConvert.DeserializeObject<List<ShadeHouseMenu>>(param.ShadeHouseMenus, settings);

                if (!TryValidateModel(gcShMenu, nameof(gcShMenu)))
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
                    newFileInfoList = await Upload.UploadImageAndReturnInfo(param.FileImages, Upload.IMAGE_TYPE_ADMIN);
                }

                var dbGolf = await _context.GolfClubs
                    .Include(gc => gc.GeoList)
                    .Include(gc => gc.GolfClubImages)
                    .Include(gc => gc.Courses)
                    .ThenInclude(gc => gc.Holes)
                    .ThenInclude(ho => ho.GeoList)
                    .Include(gc => gc.ClubHouse)
                    .ThenInclude(gh => gh.GeoList)
                    .Include(gc => gc.ClubHouse.ClubHouseMenus)
                    .Include(gc => gc.ShadeHouses)
                    .ThenInclude(sh => sh.GeoList)
                    .Include(gc => gc.ShadeHouseMenus)
                    .Include(gc => gc.FilterLists)
                    .AsSplitQuery()
                    .AsNoTracking()
                    .SingleOrDefaultAsync(gc => gc.Id == id);

                //골프클럽
                if (gc != null)
                {

                    if (gc.ImageIndex == -1)
                    {
                        Upload.DeleteImage(dbGolf.TFileUri, Upload.IMAGE_TYPE_ADMIN);

                        gc.TFileName = null;
                        gc.TFileOriginalName = null;
                        gc.TFileUri = null;
                    }
                    else if (gc.ImageIndex != null)
                    {
                        if (dbGolf.TFileUri != null)
                        {
                            Upload.DeleteImage(dbGolf.TFileUri, Upload.IMAGE_TYPE_ADMIN);
                        }

                        var idx = gc.ImageIndex.Value;
                        gc.TFileName = newFileInfoList[idx].Name;
                        gc.TFileOriginalName = newFileInfoList[idx].OriginalName;
                        gc.TFileUri = newFileInfoList[idx].Uri;
                    }

                    _context.GolfClubs.Update(gc);
                    await _context.SaveChangesAsync();
                }


                //골프클럽 이미지
                if (gcImg != null)
                {
                    for (int i = 0; i < dbGolf.GolfClubImages.Count; i++)
                    {
                        var delTarget = dbGolf.GolfClubImages.ElementAt(i);
                        if (gcImg.Where(gi => gi.Id == delTarget.Id).FirstOrDefault() == null)
                        {
                            if (delTarget.Uri != null)
                            {
                                Upload.DeleteImage(delTarget.Uri, Upload.IMAGE_TYPE_ADMIN);
                            }

                            _context.Remove(_context.GolfClubImages.Find(delTarget.Id));
                        }
                        await _context.SaveChangesAsync();
                    }

                    var cnt = 0;
                    for (int i = 0; i < gcImg.Count; i++)
                    {
                        var tg = dbGolf.GolfClubImages.Where(gc => gc.Id == gcImg[i].Id).FirstOrDefault();

                        //if (gcImg[i].ImageIndex == -1)
                        //{
                        //    Upload.DeleteImage(tg.Uri);

                        //    _context.GolfClubImages.Remove(gcImg[i]);
                        //    await _context.SaveChangesAsync();
                        //}
                        //else 
                        if (gcImg[i].ImageIndex != null)
                        {
                            gcImg[i].GolfClubId = id;

                            if (tg != null && tg.Uri != null)
                            {
                                Upload.DeleteImage(tg.Uri, Upload.IMAGE_TYPE_ADMIN);
                            }

                            var idx = gcImg[i].ImageIndex.Value;
                            gcImg[i].Name = newFileInfoList[idx].Name;
                            gcImg[i].OriginalName = newFileInfoList[idx].OriginalName;
                            gcImg[i].Uri = newFileInfoList[idx].Uri;

                            gcImg[i].Next = cnt++;

                            _context.GolfClubImages.Update(gcImg[i]);
                            await _context.SaveChangesAsync();
                        }
                    }
                }

                //골프클럽 코스
                if (gcCo != null)
                {
                    var tgs = dbGolf.Courses.Where(co => co.GolfClubId == id).ToList();

                    for (int i=0;i< tgs.Count;i++)
                    {
                        if (gcCo.Where(co => co.Id == tgs[i].Id).FirstOrDefault() == null)
                        {
                            _context.Courses.Remove(_context.Courses.Find(tgs[i].Id) );
                            await _context.SaveChangesAsync();
                        }
                    }

                    for (int i = 0; i < gcCo.Count; i++)
                    {
                        List<Hole> hole = null;
                        if (gcCo[i].Holes != null)
                        {
                            hole = new List<Hole>(gcCo[i].Holes);
                            gcCo[i].Holes.Clear();
                        }
                        gcCo[i].GolfClubId = id;

                        _context.Courses.Update(gcCo[i]);
                        await _context.SaveChangesAsync();

                        //코스 홀
                        if (hole != null)
                        {
                            for (int ii = 0; ii < hole.Count; ii++)
                            {
                                hole[ii].CourseId = gcCo[i].Id;

                                _context.Holes.Update(hole[ii]);
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                }

                //클럽 하우스
                if (gcCh != null)
                {
                    var tg = dbGolf.ClubHouse;

                    List<ClubHouseMenu> menu = null;
                    if (gcCh.ClubHouseMenus != null)
                    {
                        menu = new List<ClubHouseMenu>(gcCh.ClubHouseMenus);
                        gcCh.ClubHouseMenus.Clear();
                    }

                    gcCh.GolfClubId = id;

                    if (gcCh.ImageIndex == -1)
                    {
                        Upload.DeleteImage(tg.MFileUri, Upload.IMAGE_TYPE_ADMIN);

                        gcCh.MFileName = null;
                        gcCh.MFileOriginalName = null;
                        gcCh.MFileUri = null;
                    }
                    else if (gcCh.ImageIndex != null)
                    {
                        if (tg.MFileUri != null)
                        {
                            Upload.DeleteImage(tg.MFileUri, Upload.IMAGE_TYPE_ADMIN);
                        }

                        var idx = gcCh.ImageIndex.Value;
                        gcCh.MFileName = newFileInfoList[idx].Name;
                        gcCh.MFileOriginalName = newFileInfoList[idx].OriginalName;
                        gcCh.MFileUri = newFileInfoList[idx].Uri;
                    }
                    //_context.GeoLists.Update(gcCh.GeoList);
                    //await _context.SaveChangesAsync();

                    _context.ClubHouses.Update(gcCh);
                    await _context.SaveChangesAsync();

                    //클럽하우스 메뉴
                    if (menu != null)
                    {
                        var tgs = dbGolf.ClubHouse.ClubHouseMenus.Where(chm => chm.ClubHouseId == gcCh.Id).ToList();
                        for (int ii = 0; ii < tgs.Count; ii++)
                        {
                            if (menu.Where(mn => mn.Id == tgs[ii].Id).FirstOrDefault() == null)
                            {
                                if (tgs[ii].MFileUri != null)
                                {
                                    Upload.DeleteImage(tgs[ii].MFileUri, Upload.IMAGE_TYPE_ADMIN);
                                }
                                _context.ClubHouseMenus.Remove(_context.ClubHouseMenus.Find(tgs[ii].Id));
                                await _context.SaveChangesAsync();
                            }
                        }

                        for (int ii = 0; ii < menu.Count; ii++)
                        {
                            var tg_ = dbGolf.ClubHouse.ClubHouseMenus.Where(chm => chm.Id == menu[ii].Id).FirstOrDefault();

                            menu[ii].ClubHouseId = gcCh.Id;
                            menu[ii].Next = ii;

                            if (menu[ii].ImageIndex == -1)
                            {
                                Upload.DeleteImage(tg_.MFileUri, Upload.IMAGE_TYPE_ADMIN);

                                menu[ii].MFileName = null;
                                menu[ii].MFileOriginalName = null;
                                menu[ii].MFileUri = null;
                            }
                            else if (menu[ii].ImageIndex != null)
                            {
                                if (tg_ != null && tg_.MFileUri != null)
                                {
                                    Upload.DeleteImage(tg_.MFileUri, Upload.IMAGE_TYPE_ADMIN);
                                }

                                var idx = menu[ii].ImageIndex.Value;
                                menu[ii].MFileName = newFileInfoList[idx].Name;
                                menu[ii].MFileOriginalName = newFileInfoList[idx].OriginalName;
                                menu[ii].MFileUri = newFileInfoList[idx].Uri;
                            }

                            _context.ClubHouseMenus.Update(menu[ii]);
                            await _context.SaveChangesAsync();
                        }
                    }
                }


                //그늘집
                if (gcSh != null)
                {
                    var tgs = dbGolf.ShadeHouses.Where(sh => sh.GolfClubId == id).ToList();
                    for (int ii = 0; ii < tgs.Count; ii++)
                    {
                        if (gcSh.Where(gs => gs.Id == tgs[ii].Id).FirstOrDefault() == null)
                        {
                            _context.ShadeHouses.Remove(_context.ShadeHouses.Find(tgs[ii].Id));
                            await _context.SaveChangesAsync();
                        }
                    }

                    for (int i = 0; i < gcSh.Count; i++)
                    {
                        gcSh[i].GolfClubId = gc.Id;

                        //_context.GeoLists.Update(gcSh[i].GeoList);
                        //await _context.SaveChangesAsync();

                        _context.ShadeHouses.Update(gcSh[i]);
                        await _context.SaveChangesAsync();
                    }
                }

                //그늘집 메뉴
                if (gcShMenu != null)
                {
                    var tgs = dbGolf.ShadeHouseMenus.Where(shm => shm.GolfClubId == id).ToList();
                    for (int ii = 0; ii < tgs.Count; ii++)
                    {
                        if (gcShMenu.Where(gsm => gsm.Id == tgs[ii].Id).FirstOrDefault() == null)
                        {
                            _context.ShadeHouseMenus.Remove(_context.ShadeHouseMenus.Find(tgs[ii].Id));
                            await _context.SaveChangesAsync();
                        }
                    }

                    for (int i = 0; i < gcShMenu.Count; i++)
                    {
                        var tg = dbGolf.ShadeHouseMenus.Where(shm => shm.Id == gcShMenu[i].Id).FirstOrDefault();
                        gcShMenu[i].GolfClubId = id;

                        if (gcShMenu[i].ImageIndex == -1)
                        {
                            Upload.DeleteImage(tg.MFileUri, Upload.IMAGE_TYPE_ADMIN);

                            gcShMenu[i].MFileName = null;
                            gcShMenu[i].MFileOriginalName = null;
                            gcShMenu[i].MFileUri = null;
                        }
                        else if (gcShMenu[i].ImageIndex != null)
                        {
                            if (tg != null && tg.MFileUri != null)
                            {
                                Upload.DeleteImage(tg.MFileUri, Upload.IMAGE_TYPE_ADMIN);
                            }

                            var idx = gcShMenu[i].ImageIndex.Value;
                            gcShMenu[i].MFileName = newFileInfoList[idx].Name;
                            gcShMenu[i].MFileOriginalName = newFileInfoList[idx].OriginalName;
                            gcShMenu[i].MFileUri = newFileInfoList[idx].Uri;
                        }

                        _context.ShadeHouseMenus.Update(gcShMenu[i]);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Ok(-1);
            }

            return Ok();

        }


        public class TestGeo
        {
            public int Id { get; set; }
            public Geometry Geo { get; set; }
        }

        public class TestGeoRe
        {
            public int Type { get; set; }   
            public int Id { get; set; }
            public string Re { get; set; }
        }



        public class GolfClubDto
        {
            public string GolfClub { get; set; }
            public string GolfClubImages { get; set; }

            public IFormFile[] FileImages { get; set; }
        }

        public class CourseDto
        {
            public int GolfClubId { get; set; }

            [StringLength(255)]
            public string FairwayGrass { get; set; }
            [StringLength(255)]
            public string GreenGrass { get; set; }
            [StringLength(255)]
            public string CoursesType { get; set; }
            [Range(0.5, 5.0)]
            public double? DifficultyLevel { get; set; }
            [Range(0.5, 5.0)]
            public double? GreenDifficultyLevel { get; set; }
            [StringLength(255)]
            public string SignatureHole { get; set; }
            [Range(1, 2)]
            public int HoleLength { get; set; } // 1 긴 2 짧은

            public string Courses { get; set; }
        }

        public class HouseDto
        {
            public int GolfClubId { get; set; }
            public int? ImageIndex { get; set; }
            public string ClubHouse { get; set; }
            public string ShadeHouses { get; set; }
            public string ShadeHouseMenus { get; set; }

            public IFormFile[] FileImages { get; set; }
        }

        public class GeoDto
        {
            public int GolfClubId { get; set; }
            public string GolfClubGeo { get; set; }
            public string HoleGeo { get; set; }
            public string ClubHouseGeo { get; set; }
            public string ShadeHouses { get; set; }
        }



        public class GolfClubGeo
        {
            public int Id { get; set; }
            public GeoList GeoList { get; set; }
        }


        public class HoleGeo
        {
            public int Id { get; set; }
            public GeoList GeoList { get; set; }
        }

        public class ClubHouseGeo
        {
            public int Id { get; set; }
            public GeoList GeoList { get; set; }
        }

        public class GolfClubDto_
        {
            public string GolfClub { get; set; }
            public string GolfClubImages { get; set; }

            public string Courses { get; set; }
            //public string Holes { get; set; }

            public string ClubHouse { get; set; }
            //public string ClubHouseMenus { get; set; }

            public string ShadeHouses { get; set; }
            public string ShadeHouseMenus { get; set; }

            //public string FilterLists { get; set; }
            
            public IFormFile[] FileImages { get; set; }
        }

        


        //static Geometry GeoValidate(Geometry geo)
        //{
        //    if (hole[ii].Position != null)
        //    {
        //        if (hole[ii].Position.IsValid)
        //        {
        //            if (!hole[ii].Position.Shell.IsCCW)
        //            {
        //                hole[ii].Position = (Polygon)hole[ii].Position.Reverse();
        //            }
        //        }
        //        else
        //        {
        //            Console.WriteLine("공간데이터 오류 ");
        //            return Json("공간데이터 오류");
        //        }
        //    }


        //    return null;
        //}


    }
}
