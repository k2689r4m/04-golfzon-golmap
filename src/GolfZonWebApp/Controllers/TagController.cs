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
    public class TagController : Controller
    {
        private readonly GolfzonContext _context;

        public TagController(GolfzonContext context)
        {
            this._context = context;
        }

        //[HttpGet("[controller]/test")]
        //public async Task<IActionResult> Test()
        //{
        //    var r = context.RefCourseLists.ToList();

        //    foreach (var _r in r)
        //    {
        //        context.RefCourseLists.Remove(_r);
        //    }

        //    await context.SaveChangesAsync();

        //    return Json(1);
        //}

        //[HttpGet("[controller]/test/add")]
        //public async Task<ActionResult<Tag>> TestAdd(string name)
        //{
        //    if (name == null || name.Length <= 0)
        //    {
        //        return Json(-1);
        //    }

        //    Tag tag = context.Tags.Where(t => t.Name == name).FirstOrDefault();
        //    if (tag != null)
        //    {
        //        return Json(-2);
        //    }

        //    tag = new Tag() { Name = name };

        //    context.Tags.Add(tag);
        //    await context.SaveChangesAsync();

        //    return Json(tag);
        //}

        //private class TagItem
        //{
        //    public int Id { get; set; }
        //    public string Name { get; set; }
        //    public int ReviewCount { get; set; }
        //}

        //private class GetTagAllItem
        //{
        //    public int Id { get; set; }
        //    public string Name { get; set; }
        //    public int? Sequence { get; set; }
        //    public bool IsActive { get; set; }
        //    public string TFileUri { get; set; }
        //    public int ReviewCount { get; set; }
        //}

        [HttpGet("api/[controller]/all")]
        public async Task<ActionResult<List<Tag>>> GetTagAll(int page = 1, string content = "")
        {
            string error = "no result";

            if (page <= 0)
            {
                page = 1;
            }

            
            if (content == "")
            {
                var data = _context.Tags
                    .OrderByDescending(e => e.IsActive)
                    .ThenBy(e => e.Sequence == null ? -e.Id : e.Sequence)
                    .Select(tag => new
                    {
                        Id = tag.Id,
                        Name = tag.Name,
                        IsActive = tag.IsActive,
                        Sequence = tag.Sequence,
                        TFileUri = tag.TFileUri,
                        ReviewCount = _context.Reviews.Include(r => r.User).Where(r => r.User.Blinded == null && r.OpenState == 1 && r.State == 0).Join(_context.ReviewTags, r => r.Id, rt => rt.ReviewId, (r, rt) => new
                        {
                            ReviewId = r.Id,
                            TagId = rt.TagId
                        }).Count(e => e.TagId == tag.Id)
                    });

                var _tags = await PaginatedList<object>.CreateAsync(data, page);
                return Json(_tags.paginate());
                //data =
                //    from tag in _context.Tags
                //    join reviewTag in _context.ReviewTags on tag.Id equals reviewTag.Id into reviews
                //    orderby tag.Id descending
                //    select tag;
                //select new GetTagAllItem()
                //{
                //    Id = tag.Id,
                //    Name = tag.Name,
                //    IsActive = tag.IsActive,
                //    Sequence = tag.Sequence,
                //    TFileUri = tag.TFileUri,
                //    ReviewCount = reviews.Count(),
                //};

                //return Json(new
                //{
                //    Count = data.ToList().Count()
                //});

                //var _tags = await PaginatedList<GetTagAllItem>.CreateAsync(data, page);
                //return Json(_tags.paginate());
            }
            else
            {
                var data = _context.Tags
                    .Where(e => content.Length > 0 ? EF.Functions.Like(e.Name, "%" + content + "%") : true)
                    .OrderByDescending(e => e.IsActive)
                    .ThenBy(e => e.Sequence == null ? -e.Id : e.Sequence)
                    .Select(tag => new
                    {
                        Id = tag.Id,
                        Name = tag.Name,
                        IsActive = tag.IsActive,
                        Sequence = tag.Sequence,
                        TFileUri = tag.TFileUri,
                        ReviewCount = _context.Reviews.Join(_context.ReviewTags, r => r.Id, rt => rt.ReviewId, (r, rt) => new
                        {
                            ReviewId = r.Id,
                            TagId = rt.TagId
                        }).Count(e => e.TagId == tag.Id)
                        //ReviewCount = _context.Reviews()
                        //_context.ReviewTags.Count(e => e.TagId == tag.Id)
                    });

                var _tags = await PaginatedList<object>.CreateAsync(data, page);
                return Json(_tags.paginate());
                //var data =
                //    from tag in _context.Tags
                //    where content.Length > 0 ? tag.Name.Equals(content) : true
                //    join reviewTag in _context.ReviewTags on tag.Id equals reviewTag.Id into reviews
                //    orderby tag.Id descending
                //    select new
                //    {
                //        Id = tag.Id,
                //        Name = tag.Name,
                //        IsActive = tag.IsActive,
                //        Sequence = tag.Sequence,
                //        TFileUri = tag.TFileUri,
                //        ReviewCount = reviews.Count(),
                //    }.ToList();

                //var _tags = await PaginatedList<object>.CreateAsync(data, page);
                //return Json(_tags.paginate());
            }

            //IQueryable<int> countQuery = from tag in context.Tags
            //                            where search.Length > 0 ? tag.Name.Equals(search) : true
            //                            select tag.Id;

            

            //IQueryable<TagItem> query = from tag in context.Tags
            //                            where search.Length > 0 ? tag.Name.Equals(search) : true
            //                             join reviewTag in context.ReviewTags on tag.Id equals reviewTag.TagId into reviews
            //                             // orderby tag.Sequence ascending, tag.Id descending
            //                             orderby tag.Id descending
            //                             select new TagItem()
            //                             {
            //                                 Id = tag.Id,
            //                                 Name = tag.Name,
            //                                 IsActive = tag.IsActive,
            //                                 Sequence = tag.Sequence,
            //                                 TFileUri = tag.TFileUri,
            //                                 ReviewCount = reviews.Count(),
            //                             };

            //int count = await PaginatedList<int>.CountAsync(countQuery);
            //PaginatedList<TagItem> _tags = await PaginatedList<TagItem>.CreateAsync(query, count, page, 10);

            //return Json(_tags);
        }

        [HttpGet("api/[controller]/{id}")]
        public async Task<ActionResult<Tag>> GetTag(int id)
        {
            var tag = _context.Tags.Find(id);

            return tag;
            //var _tag = from tag in context.Tags
            //           where tag.Id == id
            //           select tag;

            //var t = _tag.ToList();

            //return Json(t);
        }

        [HttpPost("api/[controller]/update")]
        public async Task<ActionResult<Tag>> UpdateTag([FromForm] UpdateTagDto param)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            //파일 확장자 검사
            if ((param.Thumbnail!= null && !AllowedImageExtensions.Validate(param.Thumbnail)))
            {
                return BadRequest("You can upload only image files");
            }


            Tag tag = _context.Tags.Find(param.Id);

            tag.IsActive = param.IsActive;

            if (tag.IsActive)
            {
                var max = _context.Tags.Max(t => t.Sequence);
                if (max == null)
                {
                    tag.Sequence = 1;
                }
                else {
                    tag.Sequence = max + 1;
                }
                
            }
            else
            {
                var tmpTags = _context.Tags.Where(t => t.Sequence > tag.Sequence).AsNoTracking().ToList();

                if (tmpTags.Count > 0)
                {
                    foreach (var tmpTag in tmpTags)
                        {
                            tmpTag.Sequence -= 1;
                        }
                        _context.Tags.UpdateRange(tmpTags);
                }

                tag.Sequence = null;
                
            }

            if (param.ImageIndex == -1)
            {
                try
                {
                    Upload.DeleteImage(tag.TFileUri, Upload.IMAGE_TYPE_ADMIN);
                }
                catch { }
                tag.TFileName = null;
                tag.TFileOriginalName = null;
                tag.TFileUri = null;
            }

            if (param.Thumbnail != null)
            {
                var file = await Upload.UploadImageAndReturnInfo(param.Thumbnail, Upload.IMAGE_TYPE_ADMIN);

                tag.TFileName = file.Name;
                tag.TFileOriginalName = file.OriginalName;
                tag.TFileUri = file.Uri;
            }

            _context.Tags.Update(tag);
            await _context.SaveChangesAsync();

            return Ok();
        }


        public class SequenceDto
        {
            public int id { get; set; }
            public int direction { get; set; }
        }
        [HttpPost("api/[controller]/changesequence")]
        public async Task<ActionResult> ChangeSequence([FromBody] SequenceDto data)
        {
            try
            {
                int id = data.id;
                int direction = data.direction;

                var dbTemps = await _context.Tags.Where(e => e.IsActive).ToListAsync();

                Tag dbTemp = null;
                if (0 < dbTemps.Count)
                {
                    dbTemp = dbTemps.Where(e => e.Id == id).FirstOrDefault();
                }

                if (dbTemp != null)
                {
                    if (direction == 0)  //up
                    {
                        if (dbTemp.Sequence == 1)
                        {
                            return Json(false);
                        }

                        dbTemps.Where(e => e.Sequence == dbTemp.Sequence - 1).FirstOrDefault()
                            .Sequence = dbTemp.Sequence;

                        dbTemp.Sequence -= 1;
                    }
                    else
                    {
                        var tg = dbTemps.Where(e => e.Sequence == dbTemp.Sequence + 1).FirstOrDefault();

                        if (tg != null)
                        {
                            tg.Sequence = dbTemp.Sequence;
                            dbTemp.Sequence += 1;
                        }
                        else
                        {
                            return Json(false);
                        }
                    }

                    _context.Tags.UpdateRange(dbTemps);
                    await _context.SaveChangesAsync();
                }

                return Json(true);
            }
            catch (Exception e)
            {
                return Json(e);
            }
        }

        public class UpdateTagDto
        {
            public int Id { get; set; }
            public bool IsActive { get; set; } = false;
            public int? ImageIndex { get; set; }
            public IFormFile Thumbnail { get; set; }
        }
    }
}
