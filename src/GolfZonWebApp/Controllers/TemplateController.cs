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
//using Microsoft.AspNetCore.Cors;


namespace GolfZonWebApp.Controllers
{
    //[EnableCors("Development")]
    [ApiController]
    public class TemplateController : Controller
    {
        private readonly GolfzonContext _context;
        public TemplateController(GolfzonContext context)
        {
            _context = context;
        }
        public class CreateTemplateDto
        {
            public string Template { get; set; }
        }
        public class UpdateTemplateDto
        {
            public bool OnlyTemplate { get; set; }
            public string Template { get; set; }
            public IFormFile[] FileImages { get; set; }
        }
        public class CreateGolfClubTemplateDto
        {
            public string GolfClubTemplates { get; set; }
        }
        public class UpdateGolfClubTemplateDto
        {
            public string Template { get; set; }
        }
        public class CreateConvenienceTemplateDto
        {
            public string ConvenienceTemplates { get; set; }
        }
        public class UpdateConvenienceTemplateDto
        {
            public string ConvenienceTemplates { get; set; }
        }
        public class CreateEventTemplateDto
        {
            public string EventTemplates { get; set; }
        }
        public class UpdateEventTemplateDto
        {
            public string EventTemplates { get; set; }
            public IFormFile[] FileImages { get; set; }
        }

        public class GetTemplate
        {
            public Template template { get; set; }
            public int contents { get; set; }

            public GetTemplate(Template t, int c)
            {
                this.template = t;
                this.contents = c;
            }
        }

        [HttpGet("api/[controller]")]
        public async Task<ActionResult<List<Template>>> GetAll(string option, string content, int page)
        {
            string error = "no result";

            if (page <= 0)
            {
                page = 1;
            }

            IQueryable<Template> data = null;

            if (content == null && option == null)
            {
                data =
                    from template in _context.Templates
                                                        .Include(t => t.EventTemplates)
                                                        .Include(t => t.GolfClubTemplates)
                                                        .Include(t => t.ConvenienceTemplates)
                    orderby template.IsActive descending, template.Sequence, template.Id descending
                    select new Template()
                    {
                        Id = template.Id,
                        TemplateType = template.TemplateType,
                        Type = template.Type,
                        MenuName = template.MenuName,
                        IsActive = template.IsActive,
                        StartDate = template.StartDate,
                        EndDate = template.EndDate,
                        Contents = template.EventTemplates.Count + template.GolfClubTemplates.Count + template.ConvenienceTemplates.Count,
                        Sequence = template.Sequence,
                        CreatedAt = template.CreatedAt
                    };
            }
            else if (option != null && content != null)
            {
                string[] options = { "형태", "메뉴명" };
                if (Array.Exists(options, element => element == option))
                {
                    if (content != "")
                    {
                        data =
                        from template in _context.Templates
                                                            .Include(t => t.EventTemplates)
                                                            .Include(t => t.GolfClubTemplates)
                                                            .Include(t => t.ConvenienceTemplates)
                        where EF.Functions.Like(option == "메뉴명" ? template.MenuName : template.Type, "%" + content + "%")
                        orderby template.IsActive descending, template.Sequence, template.Id descending
                        select new Template()
                        {
                            Id = template.Id,
                            TemplateType = template.TemplateType,
                            Type = template.Type,
                            MenuName = template.MenuName,
                            IsActive = template.IsActive,
                            StartDate = template.StartDate,
                            EndDate = template.EndDate,
                            Contents = template.EventTemplates.Count + template.GolfClubTemplates.Count + template.ConvenienceTemplates.Count,
                            Sequence = template.Sequence,
                            CreatedAt = template.CreatedAt
                        };
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

            PaginatedList<Template> _templates = await PaginatedList<Template>.CreateAsync(data, page);

            return Json(_templates.paginate());
        }

        [HttpGet("api/[controller]/{id}")]
        public async Task<ActionResult<Template>> GetItem(int id)
        {
            var template = await _context.Templates
                .Include(t => t.EventTemplates)
                    .ThenInclude(t => t.EventTemplateGolfClubs)
                        .ThenInclude(t => t.GolfClub)
                .Include(t => t.GolfClubTemplates)
                    .ThenInclude(t => t.GolfClub)
                .Include(t => t.ConvenienceTemplates)
                    .ThenInclude(t => t.Convenience)
                .AsSplitQuery()
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new Template()
                {
                    Id = t.Id,
                    TemplateType = t.TemplateType,
                    Type = t.Type,
                    MenuName = t.MenuName,
                    IsActive = t.IsActive,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    EventTemplates = t.EventTemplates,
                    GolfClubTemplates = t.GolfClubTemplates,
                    ConvenienceTemplates = t.ConvenienceTemplates,
                    Contents = t.EventTemplates.Count + t.GolfClubTemplates.Count + t.ConvenienceTemplates.Count,
                    Sequence = t.Sequence
                })
                .FirstOrDefaultAsync();

            if (template == null)
            {
                return NotFound();
            }

            return template;
            //return Ok("GetItem");
        }

        [HttpPost("api/[controller]")]
        public async Task<ActionResult<Template>> Create([FromForm] CreateTemplateDto param)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var template = JsonSerializer.Deserialize<Template>(param.Template, options);

            if (!TryValidateModel(template, nameof(template)))
            {
                return BadRequest(ModelState);
            }

            template.Sequence = null;

            if (template.StartDate < DateTime.Now && template.EndDate > DateTime.Now)
            {
                template.IsActive = true;

                var max = _context.Templates.Max(t => t.Sequence);

                if (max == null)
                {
                    max = 0;
                }

                template.Sequence = max + 1;

                GzApi.GetDefaultSPAsync(_context, "Template_Update");
            }

            _context.Templates.Add(template);
            await _context.SaveChangesAsync();

            return await _context.Templates
                .Include(t => t.EventTemplates)
                    .ThenInclude(t => t.EventTemplateGolfClubs)
                        .ThenInclude(t => t.GolfClub)
                .Include(t => t.GolfClubTemplates)
                    .ThenInclude(t => t.GolfClub)
                .Include(t => t.ConvenienceTemplates)
                    .ThenInclude(t => t.Convenience)
                .AsSplitQuery()
                .AsNoTracking()
                .Where(t => t.Id == template.Id)
                .Select(t => new Template()
                {
                    Id = t.Id,
                    TemplateType = t.TemplateType,
                    Type = t.Type,
                    MenuName = t.MenuName,
                    IsActive = t.IsActive,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    EventTemplates = t.EventTemplates,
                    GolfClubTemplates = t.GolfClubTemplates,
                    ConvenienceTemplates = t.ConvenienceTemplates,
                    Contents = t.EventTemplates.Count + t.GolfClubTemplates.Count + t.ConvenienceTemplates.Count,
                    Sequence = t.Sequence
                })
                .FirstOrDefaultAsync();
        }

        [HttpPost("api/[controller]/update/{id}")]
        public async Task<ActionResult<Template>> Update(int id, [FromForm] UpdateTemplateDto param)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var template = JsonSerializer.Deserialize<Template>(param.Template, options);

            if (!TryValidateModel(template, nameof(template)))
            {
                return BadRequest(ModelState);
            }

            //OnlyUpdate:Start
            //UpdateOrCreate말고 Update만 동작하고 싶으면 주석 해제
            if (template.Id != id)
            {
                return NotFound();
            }

            var _template = await _context.Templates.AsNoTracking().Where(t => t.Id == id).FirstOrDefaultAsync();

            if (_template == null)
            {
                return NotFound();
            }
            //OnlyUpdate:End

            //if (template.Type != _template.Type)
            //{
            //    template.Sequence = null;
            //    template.IsActive = false;

            //    if (_template.Sequence != null)
            //    {
            //        var dbTemp = await _context.Templates.Where(te => _template.Sequence < te.Sequence).ToListAsync();

            //        for (int i = 0; i < dbTemp.Count; i++)
            //        {
            //            dbTemp[i].Sequence -= 1;
            //        }

            //        _context.Templates.UpdateRange(dbTemp);
            //    }

            //    if (_template.Type == "이벤트공지형")
            //    {
            //        var _subTemplate = await _context.EventTemplates.Where(et => et.TemplateId == _template.Id).ToListAsync();
            //        foreach (var s in _subTemplate)
            //        {
            //            _context.EventTemplates.Remove(s);
            //        }

            //        template.EventTemplates = new List<EventTemplate>();
            //    }
            //    else if (_template.Type == "골프장소개형")
            //    {
            //        var _subTemplate = await _context.GolfClubTemplates.Where(gct => gct.TemplateId == _template.Id).ToListAsync();
            //        foreach (var s in _subTemplate)
            //        {
            //            _context.GolfClubTemplates.Remove(s);
            //        }

            //        template.GolfClubTemplates = new List<GolfClubTemplate>();
            //    }
            //    else if (_template.Type == "업체소개형")
            //    {
            //        var _subTemplate = await _context.ConvenienceTemplates.Where(ct => ct.TemplateId == _template.Id).ToListAsync();
            //        foreach (var s in _subTemplate)
            //        {
            //            _context.ConvenienceTemplates.Remove(s);
            //        }

            //        template.ConvenienceTemplates = new List<ConvenienceTemplate>();
            //    }
            //}

            bool isAddEvent = false;
            bool newFlag = false;
            DateTime today = DateTime.UtcNow.AddHours(9).Date;

            template.Type = _template.Type;

            if (param.OnlyTemplate) {
                _template.TemplateType = template.TemplateType;
                _template.MenuName = template.MenuName;
                _template.StartDate = template.StartDate;
                _template.EndDate = template.EndDate;

                template = _template;
            }
            else
            {

                if (template.IsActive != _template.IsActive)
                {
                    if (template.IsActive)
                    {
                        var max = _context.Templates.Max(t => t.Sequence);

                        if (max == null)
                        {
                            max = 0;
                        }
                        
                        template.Sequence = max + 1;
                    }
                    else
                    {
                        if (_template.Sequence != null)
                        {
                            var dbTemp = await _context.Templates.Where(te => _template.Sequence < te.Sequence).ToListAsync();

                            for (int i = 0; i < dbTemp.Count; i++)
                            {
                                dbTemp[i].Sequence -= 1;
                            }

                            _context.Templates.UpdateRange(dbTemp);

                            template.Sequence = null;
                        }
                        else
                        {
                            template.Sequence = null;
                        }
                    }
                }
                else if (_template.IsActive)
                {
                    template.Sequence = _template.Sequence;
                }
                else
                {
                    template.Sequence = null;
                }

                bool dateFlag = _template.StartDate.Date <= today 
                    && _template.EndDate.Date >= today 
                    && template.IsActive;

                if (template.IsActive && !_template.IsActive)
                {
                    if (dateFlag)
                    {
                        newFlag = true;
                    }
                }

                if (_template.Type == "이벤트공지형")
                {
                    // 이미지 확장자 체크
                    if ((param.FileImages != null && !AllowedImageExtensions.Validate(param.FileImages)))
                    {
                        return BadRequest("You can upload only image files");
                    }
                    
                    try
                    {
                        var newFileInfoList = new List<Upload.UploadedFile>();

                        if (param.FileImages != null && param.FileImages.Length != 0)
                        {
                            newFileInfoList = await Upload.UploadImageAndReturnInfo(param.FileImages, Upload.IMAGE_TYPE_ADMIN);
                        }

                        var subItemIds = await _context.EventTemplates.Where(et => et.TemplateId == _template.Id).Select(et => et.Id).ToListAsync();

                        if (template.IsActive && dateFlag)
                        {
                            newFlag = true;
                        }

                        foreach (var et in template.EventTemplates)
                        {
                            if (et.Id == 0 && et.Status == 1 && et.IsMainExposure && !et.IsTemp && template.IsActive)
                            {
                                if (et.IsPush)
                                {
                                    isAddEvent = true;
                                }
                            }

                            subItemIds.Remove(et.Id);

                            var _subItemIds = await _context.EventTemplatesGolfClubs.Where(etgc => etgc.EventTemplateId == et.Id).Select(etgc => etgc.Id).ToListAsync();

                            if (et.ImageIndex != null)
                            {
                                if (et.ImageIndex == -1)
                                {
                                    et.TFileName = null;
                                    et.TFileOriginalName = null;
                                    et.TFileUri = null;
                                }
                                else
                                {
                                    var idx = et.ImageIndex.Value;
                                    et.TFileName = newFileInfoList[idx].Name;
                                    et.TFileOriginalName = newFileInfoList[idx].OriginalName;
                                    et.TFileUri = newFileInfoList[idx].Uri;
                                }
                            }

                            foreach (var etgc in et.EventTemplateGolfClubs)
                            {
                                _subItemIds.Remove(etgc.Id);

                                _context.EventTemplatesGolfClubs.Update(etgc);
                            }

                            foreach (var _sii in _subItemIds)
                            {
                                _context.EventTemplatesGolfClubs.Remove(_context.EventTemplatesGolfClubs.Find(_sii));
                            }

                            _context.EventTemplates.Update(et);
                        }

                        foreach (var sii in subItemIds)
                        {
                            _context.EventTemplates.Remove(_context.EventTemplates.Find(sii));
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
                else if (_template.Type == "골프장소개형")
                {
                    var subItemIds = await _context.GolfClubTemplates.Where(gct => gct.TemplateId == _template.Id).Select(gct => gct.Id).ToListAsync();

                    if (dateFlag && template.IsActive)
                    {
                        newFlag = true;
                    }

                    foreach (var gct in template.GolfClubTemplates)
                    {
                        subItemIds.Remove(gct.Id);

                        _context.GolfClubTemplates.Update(gct);
                    }

                    foreach (var sii in subItemIds)
                    {
                        _context.GolfClubTemplates.Remove(_context.GolfClubTemplates.Find(sii));
                    }
                }
                else if (_template.Type == "업체소개형")
                {
                    var subItemIds = await _context.ConvenienceTemplates.Where(ct => ct.TemplateId == _template.Id).Select(ct => ct.Id).ToListAsync();

                    if (dateFlag && template.IsActive)
                    {
                        newFlag = true;
                    }

                    foreach (var ct in template.ConvenienceTemplates)
                    {

                        subItemIds.Remove(ct.Id);

                        _context.ConvenienceTemplates.Update(ct);
                    }

                    foreach (var sii in subItemIds)
                    {
                        _context.ConvenienceTemplates.Remove(_context.ConvenienceTemplates.Find(sii));
                    }
                }
            }

            _context.Templates.Update(template);
            await _context.SaveChangesAsync();

            if (isAddEvent)
            {
                GzApi.GetDefaultSPAsync(_context, "Noti_Event");
            }
            if (newFlag)
            {
                GzApi.GetDefaultSPAsync(_context, "Template_Update");
            }

            return await _context.Templates
                .Include(t => t.EventTemplates)
                    .ThenInclude(t => t.EventTemplateGolfClubs)
                        .ThenInclude(t => t.GolfClub)
                .Include(t => t.GolfClubTemplates)
                    .ThenInclude(t => t.GolfClub)
                .Include(t => t.ConvenienceTemplates)
                    .ThenInclude(t => t.Convenience)
                .AsSplitQuery()
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new Template()
                {
                    Id = t.Id,
                    TemplateType = t.TemplateType,
                    Type = t.Type,
                    MenuName = t.MenuName,
                    IsActive = t.IsActive,
                    StartDate = t.StartDate,
                    EndDate = t.EndDate,
                    EventTemplates = t.EventTemplates,
                    GolfClubTemplates = t.GolfClubTemplates,
                    ConvenienceTemplates = t.ConvenienceTemplates,
                    Contents = t.EventTemplates.Count + t.GolfClubTemplates.Count + t.ConvenienceTemplates.Count,
                    Sequence = t.Sequence,
                    CreatedAt = t.CreatedAt
                }) 
                .FirstOrDefaultAsync();
        }


        //sub
        [HttpGet("api/[controller]/golfClubList/{search}")]
        public async Task<ActionResult<List<GolfClub>>> GetGolfClubList (string search)
        {
            string _search = search;
            string regex = RegexConvert.Regex(search);

            var golfClubs = await _context.GolfClubs
            //.Select(gc => new 
            //{
            //    gc.Id,
            //    gc.Name,
            //})
            .Where(gc => EF.Functions.Like(gc.Name, regex + "%"))
            .Include(gc => gc.Courses)
            .AsSplitQuery()
            .AsNoTracking()
            .OrderBy(gc => gc.Name)
            .Take(10)
            .ToListAsync();

            return golfClubs;
        }

        //[HttpPost("api/[controller]/golfClubTemplate/{id}")]
        //public async Task<ActionResult> CreateGolfClubTemplate(int id, [FromForm] CreateGolfClubTemplateDto param)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    var options = new JsonSerializerOptions
        //    {
        //        PropertyNameCaseInsensitive = true
        //    };

        //    var _template = _context.Templates.Find(id);
        //    if (_template == null)
        //    {
        //        return NotFound("Invalid Parameter");
        //    }

        //    var golfClubTemplates = JsonSerializer.Deserialize<List<GolfClubTemplate>>(param.GolfClubTemplates, options);

        //    foreach (var golfClubTemplate in golfClubTemplates)
        //    {
        //        if (!TryValidateModel(golfClubTemplate, nameof(golfClubTemplate)))
        //        {
        //            return BadRequest(ModelState);
        //        }

        //        if (golfClubTemplate.TemplateId != id)
        //        {
        //            return BadRequest("Invalid Parameter");
        //        }
        //    }

        //    var _golfClubTemplates = await _context.GolfClubTemplates
        //        .Where(gct => gct.TemplateId == _template.Id)
        //        .AsNoTracking()
        //        .AsSplitQuery()
        //        .ToListAsync();

        //    if (_golfClubTemplates.Count > 0)
        //    {
        //        return BadRequest();
        //    }

        //    foreach (var golfClubTemplate in golfClubTemplates)
        //    {
        //        _context.GolfClubTemplates.Add(golfClubTemplate);
        //    }

        //    await _context.SaveChangesAsync();

        //    return Ok("GolfClubTemplate Created");
        //}

        //[HttpPut("api/[controller]/golfClubTemplate/{id}")]
        //public async Task<ActionResult<Template>> UpdateGolfClubTemplate(int id, [FromForm] UpdateGolfClubTemplateDto param)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }
            
        //    var options = new JsonSerializerOptions
        //    {
        //        PropertyNameCaseInsensitive = true
        //    };

        //    var _template = _context.Templates.Find(id);
        //    if (_template == null)
        //    {
        //        return NotFound("Invalid Parameter");
        //    }

        //    var template = JsonSerializer.Deserialize<Template>(param.Template, options);
        //    var golfClubTemplates = template.GolfClubTemplates;
        //    _template.IsActive = template.IsActive;
        //    //_context.Templates.Update(_template);

        //    foreach (var golfClubTemplate in golfClubTemplates)
        //    {
        //        if (!TryValidateModel(golfClubTemplate, nameof(golfClubTemplate)))
        //        {
        //            return BadRequest(ModelState);
        //        }

        //        if (golfClubTemplate.TemplateId != id)
        //        {
        //            return BadRequest("Invalid Parameter");
        //        }
        //    }

        //    var _golfClubTemplateIds = await _context.GolfClubTemplates.Where(gct => gct.Id == id).Select(gct => gct.Id).ToListAsync();
        //    foreach (var golfClubTemplate in golfClubTemplates)
        //    {
        //        _context.GolfClubTemplates.Update(golfClubTemplate);
        //        _golfClubTemplateIds.Remove(golfClubTemplate.Id);
        //    }

        //    foreach (var _golfClubTemplateId in _golfClubTemplateIds)
        //    {
        //        _context.GolfClubTemplates.Remove(_context.GolfClubTemplates.Find(_golfClubTemplateId));
        //    }

        //    await _context.SaveChangesAsync();

        //    //수정 해야 함
        //    return await _context.Templates
        //        .Include(t => t.EventTemplates)
        //            .ThenInclude(t => t.EventTemplateGolfClubs)
        //                .ThenInclude(t => t.GolfClub)
        //        .Include(t => t.GolfClubTemplates)
        //            .ThenInclude(t => t.GolfClub)
        //        .Include(t => t.ConvenienceTemplates)
        //            .ThenInclude(t => t.Convenience)
        //        .AsSplitQuery()
        //        .AsNoTracking()
        //        .Where(t => t.Id == id)
        //        .Select(t => new Template()
        //        {
        //            Id = t.Id,
        //            TemplateType = t.TemplateType,
        //            Type = t.Type,
        //            MenuName = t.MenuName,
        //            IsActive = t.IsActive,
        //            StartDate = t.StartDate,
        //            EndDate = t.EndDate,
        //            EventTemplates = t.EventTemplates,
        //            GolfClubTemplates = t.GolfClubTemplates,
        //            ConvenienceTemplates = t.ConvenienceTemplates,
        //            Contents = t.EventTemplates.Count + t.GolfClubTemplates.Count + t.ConvenienceTemplates.Count
        //        })
        //        .FirstOrDefaultAsync();

        //    //return await _context.GolfClubTemplates.Where(gct => gct.TemplateId == id)
        //    //    .Include(gct => gct.GolfClub)
        //    //    .AsNoTracking()
        //    //    .AsSplitQuery()
        //    //    .ToListAsync();

        //    //return __golfClubTemplates;

        //    //return Ok("GolfClubTemplate Updated");
        //}

        [HttpGet("api/[controller]/eventList/{id}")]
        public async Task<ActionResult<EventTemplate>> GetEvent (int id)
        {
            var eventTemplate = await _context.EventTemplates
                .Where(et => et.Id == id)
                .Include(et => et.EventTemplateGolfClubs)
                .OrderBy(et => et.CreatedAt)
                    .ThenByDescending(et => et.Id)
                .AsNoTracking()
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            return eventTemplate;
        }

        //[HttpPut("api/[controller]/eventTemplate/{id}")]
        //public async Task<ActionResult> UpdateEventTemplate(int id, [FromForm] UpdateEventTemplateDto param)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    var options = new JsonSerializerOptions
        //    {
        //        PropertyNameCaseInsensitive = true
        //    };

        //    var _template = _context.Templates.Find(id);
        //    if (_template == null)
        //    {
        //        return NotFound("Invalid Parameter");
        //    }

        //    var eventTemplates = JsonSerializer.Deserialize<List<EventTemplate>>(param.EventTemplates, options);

        //    foreach (var eventTemplate in eventTemplates)
        //    {
        //        if (!TryValidateModel(eventTemplate, nameof(eventTemplate)))
        //        {
        //            return BadRequest(ModelState);
        //        }

        //        if (eventTemplate.TemplateId != id)
        //        {
        //            return BadRequest("Invalid Parameter");
        //        }
        //    }
        //    // 이미지 확장자 체크
        //    if ((param.FileImages != null && !AllowedImageExtensions.Validate(param.FileImages)))
        //    {
        //        return BadRequest("You can upload only image files");
        //    }

        //    try
        //    {
        //        var newFileInfoList = new List<Upload.UploadedFile>();
        //        if (param.FileImages != null && param.FileImages.Length != 0)
        //        {
        //            newFileInfoList = await Upload.UploadImageAndReturnInfo(param.FileImages);
        //        }

        //        var _eventTemplateIds = await _context.EventTemplates.Where(et => et.Id == id).Select(et => et.Id).ToListAsync();
        //        foreach (var eventTemplate in eventTemplates)
        //        {
        //            if (eventTemplate.ImageIndex != null)
        //            {
        //                eventTemplate.TFileName = null;
        //                eventTemplate.TFileOriginalName = null;
        //                eventTemplate.TFileUri = null;

        //                if (eventTemplate.ImageIndex != -1)
        //                {
        //                    eventTemplate.TFileName = newFileInfoList[eventTemplate.ImageIndex.Value].Name;
        //                    eventTemplate.TFileOriginalName = newFileInfoList[eventTemplate.ImageIndex.Value].OriginalName;
        //                    eventTemplate.TFileUri = newFileInfoList[eventTemplate.ImageIndex.Value].Uri;
        //                }
        //            }

        //            var _eventTemplateGolfClubIds = await _context.EventTemplatesGolfClubs
        //                .Where(etgc => etgc.EventTemplateId == eventTemplate.Id).Select(etgc => etgc.Id).ToListAsync();

        //            foreach (var eventTemplateGolfClub in eventTemplate.EventTemplateGolfClubs)
        //            {
        //                _context.EventTemplatesGolfClubs.Update(eventTemplateGolfClub);
        //                _eventTemplateGolfClubIds.Remove(eventTemplateGolfClub.Id);
        //            }

        //            _context.EventTemplates.Update(eventTemplate);
        //            _eventTemplateIds.Remove(eventTemplate.Id);

        //            foreach (var _eventTemplateGolfClubId in _eventTemplateGolfClubIds)
        //            {
        //                _context.EventTemplatesGolfClubs.Remove(_context.EventTemplatesGolfClubs.Find(_eventTemplateGolfClubId));
        //            }
        //        }

        //        foreach (var _eventTemplateId in _eventTemplateIds)
        //        {
        //            _context.EventTemplates.Remove(_context.EventTemplates.Find(_eventTemplateId));
        //        }

        //        await _context.SaveChangesAsync();

        //        return Ok("EventTemplate Updated");
        //    }
        //    catch (Exception e)
        //    {
        //        Console.WriteLine(e);
        //        return BadRequest();
        //    }
        //}


        [HttpGet("api/[controller]/convenienceList/{search}")]
        public async Task<ActionResult<List<Convenience>>> GetConvenienceList(string search)
        {
            string _search = search;
            string regex = RegexConvert.Regex(search);

            var conveniences = await _context.Conveniences
            //.Select(c => new 
            //{
            //    c.Id,
            //    c.Name,
            //})
            .Where(c => EF.Functions.Like(c.Name, regex + "%"))
            .AsSplitQuery()
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Take(10)
            .ToListAsync();

            return conveniences;
        }

        //[HttpPost("api/[controller]/golfClubTemplate/{id}")]
        //public async Task<ActionResult> CreateConvenienceTemplate(int id, [FromForm] CreateConvenienceTemplateDto param)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    var options = new JsonSerializerOptions
        //    {
        //        PropertyNameCaseInsensitive = true
        //    };

        //    var _template = _context.Templates.Find(id);
        //    if (_template == null)
        //    {
        //        return NotFound("Invalid Parameter");
        //    }

        //    var convenienceTemplates = JsonSerializer.Deserialize<List<ConvenienceTemplate>>(param.ConvenienceTemplates, options);

        //    foreach (var convenienceTemplate in convenienceTemplates)
        //    {
        //        if (!TryValidateModel(convenienceTemplate, nameof(convenienceTemplate)))
        //        {
        //            return BadRequest(ModelState);
        //        }

        //        if (convenienceTemplate.TemplateId != id)
        //        {
        //            return BadRequest("Invalid Parameter");
        //        }
        //    }

        //    var _convenienceTemplates = await _context.ConvenienceTemplates
        //        .Where(gct => gct.TemplateId == _template.Id)
        //        .AsNoTracking()
        //        .AsSplitQuery()
        //        .ToListAsync();

        //    if (_convenienceTemplates.Count > 0)
        //    {
        //        return BadRequest();
        //    }

        //    foreach (var convenienceTemplate in convenienceTemplates)
        //    {
        //        _context.ConvenienceTemplates.Add(convenienceTemplate);
        //    }

        //    await _context.SaveChangesAsync();

        //    return Ok("ConvenienceTemplate Created");
        //}

        //[HttpPut("api/[controller]/convenienceTemplate/{id}")]
        //public async Task<ActionResult> UpdateConvenienceTemplate(int id, [FromForm] UpdateConvenienceTemplateDto param)
        //{
        //    if (!ModelState.IsValid)
        //    {
        //        return BadRequest(ModelState);
        //    }

        //    var options = new JsonSerializerOptions
        //    {
        //        PropertyNameCaseInsensitive = true
        //    };

        //    var _template = _context.Templates.Find(id);
        //    if (_template == null)
        //    {
        //        return NotFound("Invalid Parameter");
        //    }

        //    var convenienceTemplates = JsonSerializer.Deserialize<List<ConvenienceTemplate>>(param.ConvenienceTemplates, options);

        //    foreach (var convenienceTemplate in convenienceTemplates)
        //    {
        //        if (!TryValidateModel(convenienceTemplate, nameof(convenienceTemplate)))
        //        {
        //            return BadRequest(ModelState);
        //        }

        //        if (convenienceTemplate.TemplateId != id)
        //        {
        //            return BadRequest("Invalid Parameter");
        //        }
        //    }

        //    var _convenienceTemplateIds = await _context.ConvenienceTemplates.Where(ct => ct.Id == id).Select(ct => ct.Id).ToListAsync();
        //    foreach (var convenienceTemplate in convenienceTemplates)
        //    {
        //        _context.ConvenienceTemplates.Update(convenienceTemplate);
        //        _convenienceTemplateIds.Remove(convenienceTemplate.Id);
        //    }

        //    foreach (var _convenienceTemplateId in _convenienceTemplateIds)
        //    {
        //        _context.ConvenienceTemplates.Remove(_context.ConvenienceTemplates.Find(_convenienceTemplateId));
        //    }

        //    await _context.SaveChangesAsync();

        //    return Ok("ConvenienceTemplate Updated");
        //}

        [HttpPost("api/[controller]/upload")]
        public async Task<ActionResult<string>> ImageUpload([FromForm] IFormFile img) 
        {
            try
            {
                var newFileInfo = await Upload.UploadImageAndReturnInfo(img, Upload.IMAGE_TYPE_ADMIN);

                var info = newFileInfo.Uri.Split("\\");

                

                var template = JsonSerializer.Serialize(new Img("https://golmap.golfzon.com/images/" + info[1]));
                
                return template;
            }
            catch (Exception e)
            {
                return Json(e);
            }
        }

        public class Img 
        {
            public Img(string ln)
            {
                link = ln;
            }
            public string link { get; set; }
        }



        //[HttpGet("api/[controller]/delete")]
        //public async Task<ActionResult> ImageDelete()
        //{
        //    try
        //    {
        //        var uri_ = HttpContext.Request.Query["uri"];
        //        Upload.DeleteImage(uri_);

        //        return Json(true);
        //    }
        //    catch (Exception e)
        //    {
        //        return Json(e);
        //    }
        //}


        [HttpPost("api/[controller]/changesequence/{id}/{direction}")]
        public async Task<ActionResult> ImageDelete(int id, int direction)
        {
            try
            {
                var dbTemps = await _context.Templates.Where(te => te.IsActive).ToListAsync();

                Template dbTemp = null;
                if (0 < dbTemps.Count) 
                {
                     dbTemp = dbTemps.Where(te => te.Id == id).FirstOrDefault();
                }


                if (dbTemp != null)
                {
                    if(direction == 0)  //up
                    {
                        if (dbTemp.Sequence == 1)
                        {
                            return Json(false);
                        }

                        dbTemps.Where(te => te.Sequence == dbTemp.Sequence - 1).FirstOrDefault()
                            .Sequence = dbTemp.Sequence;

                        dbTemp.Sequence -=1;
                    }
                    else
                    {
                        var tg = dbTemps.Where(te => te.Sequence == dbTemp.Sequence + 1).FirstOrDefault();

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

                    _context.Templates.UpdateRange(dbTemps);
                    await _context.SaveChangesAsync();
                }

                return Json(true);
            }
            catch (Exception e)
            {
                return Json(e);
            }
        }

    }
}
