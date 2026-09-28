using GolfZonWebApp.Data;
using GolfZonWebApp.Models;
using GolfZonWebApp.Scheduler.Managers;
using GolfZonWebApp.Scheduler.Repositories;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using GolfZonWebApp.Lib;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Dynamic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace GolfZonWebApp.Controllers
{
    [ApiController]
    public class RouletteController : Controller
    {
        private readonly GolfzonContext context;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly HttpContext httpContext;

        public RouletteController(GolfzonContext context, IHttpContextAccessor httpContextAccessor)
        {
            this.context = context;
            this.httpContextAccessor = httpContextAccessor;
            this.httpContext = httpContextAccessor.HttpContext;
        }

        //[HttpGet("api/roulette_setting/get/action_options")]
        //public IActionResult RouletteActionOptions ()
        //{

        //}

        public class SearchOptions 
        {
            public int page { get; set; } = 1;
            public List<int> actionOptionIds { get; set; }
            public DateTime? startDate { get; set; }
            public DateTime? endDate { get; set; }
            public List<string> status { get; set; }
        }

        [HttpGet("api/roulette_setting/get/options/all")]
        public IActionResult RouletteGetOptionAll()
        {
            return Json(GzApi.GetDefaultSP(context, "Roulette_Get_Action_Option_Objects"));
        }

        [HttpGet("api/roulette_winner/get/options/all")]
        public IActionResult RouletteGetOptionAll2()
        {
            return Json(GzApi.GetDefaultSP(context, "Roulette_Get_Action_Option_Objects"));
        }

        [HttpGet("api/roulette_setting/get/all")]
        public IActionResult RouletteGetAll(int page, string sActionOptionIds, DateTime? startDate, DateTime? endDate, string status)
        {
            if (page < 1)
            {
                page = 1;
            }

            List<string> statusList = status == null || status == "" ? new List<string>() :
                status.Split(',')
                .Select(e => 
                    int.Parse(e) == 1 ? "대기" : 
                    (int.Parse(e) == 2 ? "진행" : (int.Parse(e) == 3 ? "완료" : ""))).ToList();

            List<int> actionOptionIds = sActionOptionIds == null || sActionOptionIds == "" ? new List<int>() : sActionOptionIds.Split(',').Select(e => int.Parse(e)).ToList();

            int lastPage = (context.Roulettes.Include(r => r.RouletteActionOptions)
                .Where(r => actionOptionIds.Count == 0 ? true : actionOptionIds.Contains(r.Id))
                .Where(r => startDate == null ? true : ((DateTime)startDate).Date <= r.StartDate && ((DateTime)startDate).Date <= r.EndDate)
                .Where(r => endDate == null ? true : ((DateTime)endDate).Date >= r.StartDate && ((DateTime)endDate).Date >= r.EndDate)
                .Where(r => statusList.Count == 0 ? true : statusList.Contains(r.Status)).Count() - 1) / 10;

            lastPage = lastPage < 1 ? 1 : lastPage;

            List<Roulette> roulettes = context.Roulettes.Include(r => r.RouletteActionOptions)
                .Where(r => actionOptionIds.Count == 0 ? true : actionOptionIds.Contains(r.Id))
                .Where(r => startDate == null ? true : ((DateTime)startDate).Date <= r.StartDate && ((DateTime)startDate).Date <= r.EndDate)
                .Where(r => endDate == null ? true : ((DateTime)endDate).Date >= r.StartDate && ((DateTime)endDate).Date >= r.EndDate)
                .Where(r => statusList.Count == 0 ? true : statusList.Contains(r.Status))
                .OrderByDescending(r => r.Id).Skip((page - 1) * 10).Take(10).ToList();

            return Json(new { 
                lastPage = lastPage,
                items = roulettes
            });
        }

        public class SearchWinnerOptions
        {
            public int page { get; set; } = 1;
            public List<int> actionOptionIds { get; set; }
            public DateTime? startDate { get; set; }
            public DateTime? endDate { get; set; }
            public string type { get; set; }
            public string option { get; set; }
            public string content { get; set; }
        }
        [HttpGet("api/roulette_winner/get/all")]
        public IActionResult RouletteWinnerGetAll(int page, string sActionOptionIds, DateTime? startDate, DateTime? endDate, string type, string option, string content)
        {
            List<int> actionOptionIds = sActionOptionIds == null ? new List<int>() : sActionOptionIds.Split(',').Select(e => int.Parse(e)).ToList();

            using (var cmd = context.Database.GetDbConnection().CreateCommand())
            {
                DataTable table = new DataTable();
                DataColumn column = new DataColumn();
                column.DataType = System.Type.GetType("System.Int32");
                column.ColumnName = "id";
                table.Columns.Add(column);
                DataRow row;
                if (actionOptionIds != null)
                {
                    foreach (var o in actionOptionIds)
                    {
                        row = table.NewRow();
                        row["id"] = o;
                        table.Rows.Add(row);
                    }
                }
                

                cmd.CommandText = "Roulette_Get_Winner";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@Page",
                    SqlDbType.Int)
                { Value = page });
                cmd.Parameters.Add(new SqlParameter("@Option",
                    SqlDbType.VarChar)
                { Value = option == null ? DBNull.Value : option });
                cmd.Parameters.Add(new SqlParameter("@Content",
                    SqlDbType.VarChar)
                { Value = content == null ? DBNull.Value : content });
                cmd.Parameters.Add(new SqlParameter("@ActionOptionIds",
                    SqlDbType.Structured)
                { Value = table });
                cmd.Parameters.Add(new SqlParameter("@Type",
                    SqlDbType.VarChar)
                { Value = type == null ? DBNull.Value : type });
                cmd.Parameters.Add(new SqlParameter("@StartDate",
                    SqlDbType.Date)
                { Value = startDate == null ? DBNull.Value : startDate });
                cmd.Parameters.Add(new SqlParameter("@EndDate",
                    SqlDbType.Date)
                { Value = endDate == null ? DBNull.Value : endDate });

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


        [HttpGet("api/roulette_setting/get/{Id}")]
        public IActionResult RouletteSettingGet(int Id)
        {
            var result = context.Roulettes.Include(r => r.RouletteItems).Include(r => r.RouletteActionOptions).Where(r => r.Id == Id).FirstOrDefault();

            var rr = context.RouletteResults.Where(r => r.RouletteId == Id).GroupBy(r => r.RouletteItemId).Select(r => new
            {
                rouletteItemId = r.Key,
                count = r.Count()
            });

            if (result == null) return NotFound();

            return Json(new { 
                roulette = result,
                rouletteResult = rr
            });
        }

        public class RouletteDto
        {
            public string Roulette { get; set; }
            public string RouletteItems { get; set; }
            public string ActionOptionIds { get; set; }

            public IFormFile[] FileImages { get; set; }
        }
        [HttpPost("api/roulette_setting/set")]
        public async Task<ActionResult> RouletteSettingSet([FromForm] RouletteDto param)
        {
            Roulette roulette = null;
            List<RouletteItem> rouletteItems = null;
            List<Upload.UploadedFile> newFileInfoList = null;

            if (param.Roulette != null)
            {
                roulette = JsonConvert.DeserializeObject<Roulette>(param.Roulette);

                if (!TryValidateModel(roulette, nameof(roulette)))
                {
                    return BadRequest(ModelState);
                }

                roulette.RouletteItems = null;
                roulette.RouletteActionOptions = null;
            }

            if (param.RouletteItems != null)
            {
                rouletteItems = JsonConvert.DeserializeObject<List<RouletteItem>>(param.RouletteItems);

                if (!TryValidateModel(rouletteItems, nameof(rouletteItems)))
                {
                    return BadRequest(ModelState);
                }
            }

            try
            {
                if (roulette == null) return BadRequest(); 
                if (rouletteItems == null) return BadRequest();
                if (roulette.Size < 2 || roulette.Size > 8) return BadRequest();
                if (roulette.Size != rouletteItems.Count) return BadRequest();

                int _r = 0;
                var rrrrr = context.Roulettes.Where(
                            r =>
                            !(
                                (
                                    r.StartDate.Date > roulette.EndDate.Date
                                ) ||
                                (
                                    r.EndDate.Date < roulette.StartDate.Date
                                )
                            )
                        );
                if (roulette.Id > 0)
                {
                    _r = rrrrr.Where(r => r.Id != roulette.Id).Count();
                }
                else
                {
                    _r = rrrrr.Count();
                }
                if (_r > 0) return BadRequest("해당 기간에 이벤트가 존재합니다.");

                foreach (var item in rouletteItems)
                {
                    if (item.Win == null || item.Win == "") return BadRequest();
                    if (item.Percentage < 0) return BadRequest();
                    if (item.Quantity < 0) return BadRequest();
                    if (item.FileUri == null || item.FileUri == "")
                    {
                        if (item.ImageIndex < 0 || item.ImageIndex == null) return BadRequest();
                    }
                }
            }
            catch {
                return BadRequest();
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

                if (roulette.Id > 0)
                {
                    roulette.Version = roulette.Version + 1;
                    context.Roulettes.Update(roulette);
                }
                else
                {
                    context.Roulettes.Add(roulette);
                }

                await context.SaveChangesAsync();

                var options = context.RouletteActionOptions.Where(r => r.RouletteId == roulette.Id).ToList();
                context.RouletteActionOptions.RemoveRange(options);

                try
                {

                    var ids = param.ActionOptionIds.Split(',');
                    foreach (var id in ids)
                    {
                        int _id = int.Parse(id);

                        context.RouletteActionOptions.Add(new RouletteActionOption
                        {
                            RouletteId = roulette.Id,
                            ActionOptionObjectId = _id
                        });
                    }
                }
                catch { }

                await context.SaveChangesAsync();


                for (int i = 0; i < rouletteItems.Count(); i++)
                {
                    if (rouletteItems[i].Id > 0)
                    {
                        if (rouletteItems[i].ImageIndex != null && newFileInfoList != null && newFileInfoList.Count > rouletteItems[i].ImageIndex)
                        {
                            try
                            {
                                Upload.DeleteImage(rouletteItems[i].FileUri, Upload.IMAGE_TYPE_ADMIN);
                            }
                            catch { }

                            var idx = rouletteItems[i].ImageIndex.Value;
                            rouletteItems[i].FileName = newFileInfoList[idx].Name;
                            rouletteItems[i].FileOriginalName = newFileInfoList[idx].OriginalName;
                            rouletteItems[i].FileUri = newFileInfoList[idx].Uri;
                        }

                        context.RouletteItems.Update(rouletteItems[i]);
                    }
                    else
                    {
                        rouletteItems[i].RouletteId = roulette.Id;
                        if (rouletteItems[i].ImageIndex != null && newFileInfoList != null && newFileInfoList.Count > rouletteItems[i].ImageIndex)
                        {
                            var idx = rouletteItems[i].ImageIndex.Value;
                            rouletteItems[i].FileName = newFileInfoList[idx].Name;
                            rouletteItems[i].FileOriginalName = newFileInfoList[idx].OriginalName;
                            rouletteItems[i].FileUri = newFileInfoList[idx].Uri;
                        }

                        context.RouletteItems.Add(rouletteItems[i]);
                    }
                }

                await context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return BadRequest();
            }
        }
    }
}
