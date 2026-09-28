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
using Newtonsoft.Json;

namespace GolfZonWebApp.Controllers
{
    [ApiController]
    public class WrongInfoController : Controller
    {
        private readonly GolfzonContext _context;
        public WrongInfoController(GolfzonContext context)
        {
            _context = context;
        }

        [HttpGet("api/[controller]/all")]
        public async Task<ActionResult<List<WrongInfo>>> GetAll(string option, string content, int page)
        {
            //var wrongInfos = await _context.WrongInfos
            //    .Include(wi => wi.WrongInfoImages)
            //    .Include(wi => wi.User)
            //    .Include(wi => wi.GolfClub)
            //    .Include(wi => wi.Convenience)
            //    .OrderByDescending(wi => wi.Id)
            //    .AsNoTracking()
            //    .ToListAsync();

            //return Json(wrongInfos.Take(100));


            string error = "no result";

            if (page <= 0)
            {
                page = 1;
            }

            IQueryable<WrongInfo> data = null;

            if (content == null && option == null)
            {
                //var wrongInfos = await _context.WrongInfos
                //.Include(wi => wi.WrongInfoImages)
                //.Include(wi => wi.User)
                //.Include(wi => wi.GolfClub)
                //.Include(wi => wi.Convenience)
                //.OrderByDescending(wi => wi.Id)
                //.AsNoTracking()
                //.ToListAsync();

                data =
                    from wrongInfo in _context.WrongInfos
                                                            .Include(wi => wi.WrongInfoImages)
                                                            .Include(wi => wi.User)
                                                            .Include(wi => wi.GolfClub)
                                                            .Include(wi => wi.Convenience)
                                                            .AsNoTracking()
                    orderby wrongInfo.Id descending
                    select wrongInfo;
            }
            else if (option != null && content != null)
            {
                string[] options = { "카테고리", "골프장/업체명", "주소", "신고자 회원번호", "신고자 아이디" };
                if (Array.Exists(options, element => element == option))
                {
                    if (content != "")
                    {
                        data =
                            from wrongInfo in _context.WrongInfos
                                .Include(wi => wi.WrongInfoImages)
                                .Include(wi => wi.User)
                                .Include(wi => wi.GolfClub)
                                .Include(wi => wi.Convenience)
                                .AsNoTracking()
                            orderby wrongInfo.Id descending
                            select wrongInfo;

                        switch (option)
                        {
                            case "카테고리":
                                string[] categiries = { "맛집", "카페", "숙박", "명소", "편의점", "주유소", "골프연습장", "골프존" };
                                //if (Array.Exists(categiries, element => element == content))
                                //{
                                if (content == "골프장")
                                {
                                    data = data.Where(d => d.GolfClubId != null);
                                }
                                else
                                {
                                    data = data.Where(d => d.Convenience.Type == content);
                                }
                                //}
                                //else 
                                //{
                                //    data = null;
                                //    error = "invalid category";
                                //}
                                break;
                            case "골프장/업체명":
                                data = data.Where(d => EF.Functions.Like(d.GolfClub.Name, "%" + content + "%") || EF.Functions.Like(d.Convenience.Name, "%" + content + "%"));
                                break;
                            case "주소":
                                data = data.Where(d => EF.Functions.Like(d.GolfClub.Address, "%" + content + "%") || EF.Functions.Like(d.Convenience.Address, "%" + content + "%"));
                                break;
                            case "신고자 회원번호":
                                data = data.Where(d => EF.Functions.Like(d.UserId.ToString(), "%" + content + "%"));
                                break;
                            case "신고자 아이디":
                                data = data.Where(d => EF.Functions.Like(d.User.Username, "%" + content + "%"));
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

            PaginatedList<WrongInfo> _wrongInfo = await PaginatedList<WrongInfo>.CreateAsync(data, page);

            return Json(_wrongInfo.paginate());
        }

        [HttpGet("api/[controller]/get/golfclub/{golfClubId}")]
        public async Task<ActionResult<List<WrongInfo>>> GetItemsByGolfClubId(int golfClubId)
        {
            var wrongInfo = await _context.WrongInfos
                .Include(wi => wi.WrongInfoImages)
                .Include(wi => wi.User)
                .AsNoTracking()
                .Where(wi => wi.GolfClubId == golfClubId)
                .ToListAsync();

            return Json(wrongInfo);
        }

        [HttpGet("api/[controller]/get/convenience/{convenienceId}")]
        public async Task<ActionResult<List<WrongInfo>>> GetItemsByConvenienceId(int convenienceId)
        {
            var wrongInfo = await _context.WrongInfos
                .Include(wi => wi.WrongInfoImages)
                .Include(wi => wi.User)
                .AsNoTracking()
                .Where(wi => wi.ConvenienceId == convenienceId)
                .ToListAsync();

            return Json(wrongInfo);
        }

        [HttpPost("api/[controller]/state/{id}")]
        public async Task<ActionResult<WrongInfo>> StateSave(int id, [FromBody] StateSaveDto param)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var dbWrongInfo = await _context.WrongInfos.SingleOrDefaultAsync(wi => wi.Id == id);

                if (dbWrongInfo != null)
                {
                    dbWrongInfo.State = param.State;

                    _context.WrongInfos.Update(dbWrongInfo);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }


            return Ok();
        }

        [HttpPost("api/[controller]/create")]
        public async Task<IActionResult> CreateGolfClub([FromForm] WrongInfoDto param)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            //var settings = new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore };
            //foreach (var conv in GeoJsonSerializer.Create(settings, new GeometryFactory(new PrecisionModel(), 4326)).Converters)
            //{
            //    settings.Converters.Add(conv);
            //}

            WrongInfo wi = null;
            List<WrongInfoImage> wiImg = null;
            List<Upload.UploadedFile> newFileInfoList = null;

            //골프클럽 유호성 검사
            if (param.WrongInfo != null)
            {
                wi = JsonConvert.DeserializeObject<WrongInfo>(param.WrongInfo);

                if (!TryValidateModel(wi, nameof(wi)))
                {
                    return BadRequest(ModelState);
                }
            }

            if (param.WrongInfoImages != null)
            {
                wiImg = JsonConvert.DeserializeObject<List<WrongInfoImage>>(param.WrongInfoImages);

                if (!TryValidateModel(wiImg, nameof(wiImg)))
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

                //잘못된정보
                if (wi != null)
                {
                    wi.State = 0;
                    _context.WrongInfos.Add(wi);
                    await _context.SaveChangesAsync();

                    //잘못된정보 이미지
                    if (wiImg != null)
                    {
                        for (int i = 0; i < wiImg.Count; i++)
                        {
                                wiImg[i].WrongInfoId = wi.Id;

                                if (wiImg[i].ImageIndex != null)
                                {
                                    var idx = wiImg[i].ImageIndex.Value;
                                    wiImg[i].Name = newFileInfoList[idx].Name;
                                    wiImg[i].OriginalName = newFileInfoList[idx].OriginalName;
                                    wiImg[i].Uri = newFileInfoList[idx].Uri;
                                }

                            wiImg[i].Next = i;


                            _context.WrongInfoImages.Update(wiImg[i]);
                            await _context.SaveChangesAsync();
                        }
                    }

                    return Json(wi.Id);
                }

                return Json(-1);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Json(-1);
            }
        }

        public class StateChangeObject
        {
            public int id { get; set; }
            public int state { get; set; }
        }

        [HttpPost("api/[controller]/state")]
        public async Task<ActionResult> StateChange([FromBody] StateChangeObject data) 
        {
            try
            {
                WrongInfo w = _context.WrongInfos.Find(data.id);
                if (w == null)
                {
                    return NotFound();
                }

                w.State = data.state;
                _context.SaveChanges();

                return Ok();
            }
            catch { }

            return NotFound();
        }

        //[HttpGet("api/[controller]/test")]
        //public async Task<ActionResult<WrongInfo>> Test()
        //{
        //    var newWrongInfo = new List<WrongInfo>
        //    {
        //        new WrongInfo
        //        {
        //            GolfClubId = 1,
        //            UserId = 1,
        //            Content = "이것은 골프장 데이터가 잘못된 정보 1"
        //        },
        //        new WrongInfo
        //        {
        //            GolfClubId = 2,
        //            UserId = 1,
        //            Content = "이것은 골프장 데이터가 잘못된 정보 2"
        //        },
        //        new WrongInfo
        //        {
        //            GolfClubId = 3,
        //            UserId = 2,
        //            Content = "이것은 골프장 데이터가 잘못된 정보 3"
        //        },
        //        new WrongInfo
        //        {
        //            ConvenienceId = 1,
        //            UserId = 3,
        //            Content = "이것은 편의시설 데이터가 잘못된 정보 1"
        //        },
        //        new WrongInfo
        //        {
        //            ConvenienceId = 2,
        //            UserId = 4,
        //            Content = "이것은 편의시설 데이터가 잘못된 정보 2"
        //        },
        //    };

        //    _context.WrongInfos.AddRange(newWrongInfo);
        //    await _context.SaveChangesAsync();


        //    return Ok();
        //}

        public class StateSaveDto
        {
            public int State { get; set; }
        }

        public class WrongInfoDto
        {
            public string WrongInfo { get; set; }
            public string WrongInfoImages { get; set; }

            public IFormFile[] FileImages { get; set; }
        }
    }
}
