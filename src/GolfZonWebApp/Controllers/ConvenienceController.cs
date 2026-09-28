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
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Microsoft.Extensions.Options;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Dynamic;

namespace GolfZonWebApp.Controllers
{
    [ApiController]
    public class ConvenienceController : Controller
    {
        private readonly GolfzonContext _context;
        private readonly IOptions<Geo> _geoData;

        public ConvenienceController(GolfzonContext context, IOptions<Geo> geo)
        {
            _context = context;
            _geoData = geo;
        }

        [HttpPost("api/[controller]/test")]
        public async Task<ActionResult> Test([FromBody] string sql)
        {
            return Json(_context.Conveniences.FromSqlRaw(sql).ToList());
        }

        [HttpPost("api/[controller]/save/refgolf")]
        public async Task<ActionResult> SaveRefGolf([FromBody] SaveRefGolfParam param)
        {
            if (param.Ids == null) return BadRequest();

            var oldRefGolfList = _context.RefGolfLists.Where(g => g.ConvenienceId == param.convId).ToList();
            var recvRefGolfList = param.Ids.ToList().Select(i => new RefGolfList { GolfClubId = i, ConvenienceId = param.convId });

            //추가
            //recv - old
            var add = recvRefGolfList.Where(r => !oldRefGolfList.Select(o => o.GolfClubId).Contains(r.GolfClubId));

            //삭제
            //old - recv
            var delete = oldRefGolfList.Where(o => !recvRefGolfList.Select(r => r.GolfClubId).Contains(o.GolfClubId));

            _context.RefGolfLists.AddRange(add);
            _context.RefGolfLists.RemoveRange(delete);
            _context.SaveChanges();

            return Ok();
        }

        [HttpGet("api/[controller]/get/refgolf")]
        public async Task<ActionResult> GetRefGolf(int convId = 0)
        {
            var res = GzApi.GetSingleParamSP(_context, "Admin_Get_RefGolf", "@P_CONV_ID", convId);

            return Json(res);
        }

        [HttpGet("api/[controller]/search/refgolf")]
        public async Task<ActionResult> SearchRefGolf(string search = "", int convId = 0)
        {
            var retObject = new List<IDictionary<string, object>>();

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Admin_RefGolfClub_Search";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_SEARCH",
                    SqlDbType.NVarChar)
                { Value = search });
                cmd.Parameters.Add(new SqlParameter("@P_CONV_ID",
                    SqlDbType.Int)
                { Value = convId });

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

                        retObject.Add(dataRow);
                    }
                }

            }

            return Json(retObject);
        }

        [HttpGet("api/[controller]/all")]
        public async Task<ActionResult<List<Convenience>>> GetAll(string option="", string content="", int page = 1)
        {
            var retObject = new List<IDictionary<string, object>>();

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Admin_Convenience_Get_All";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_OPTION",
                    SqlDbType.NVarChar)
                { Value = option });
                cmd.Parameters.Add(new SqlParameter("@P_CONTENT",
                    SqlDbType.NVarChar)
                { Value = content });
                cmd.Parameters.Add(new SqlParameter("@P_PAGE",
                    SqlDbType.TinyInt)
                { Value = page });

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

                        retObject.Add(dataRow);
                    }
                }

            }

            return Json(new
            {
                items = retObject,
                page = page,
                totalPages = retObject.Count == 0 ? 0 : retObject[0]["totalPage"]
            });

            //string error = "no result";

            //if (page <= 0)
            //{
            //    page = 1;
            //}

            //IQueryable<Convenience> data = null;

            //if (content == null && option == null)
            //{
            //    data =
            //        from convenience in _context.Conveniences
            //                                    .Include(con => con.WrongInfos)
            //                                    .Include(con => con.ConvenienceMenus.OrderByDescending(menu => menu.Id))
            //                                    .Include(con => con.ConvenienceImages.OrderByDescending(img => img.Id))
            //                                    .Include(con => con.ConUpdateLists)
            //                                    .OrderByDescending(con => con.Id)
            //                                    .AsSplitQuery()
            //        select convenience;
            //}
            //else if (option != null && content != null)
            //{
            //    string[] options = { "카테고리", "업체명", "주소", "전화번호"};
            //    if (Array.Exists(options, element => element == option))
            //    {
            //        if (content != "")
            //        {
            //            data =
            //            from convenience in _context.Conveniences
            //                                        .Include(con => con.WrongInfos)
            //                                        .Include(con => con.ConvenienceMenus.OrderByDescending(menu => menu.Id))
            //                                        .Include(con => con.ConvenienceImages.OrderByDescending(img => img.Id))
            //                                        .Include(con => con.ConUpdateLists)
            //                                        .OrderByDescending(con => con.Id)
            //                                        .AsSplitQuery()
            //            where EF.Functions.Like(
            //                    option == "카테고리" ? convenience.Type: (
            //                        option == "업체명" ? convenience.Name : (
            //                            option == "주소" ? convenience.Address : convenience.Contact
            //                            )
            //                        )
            //                    , "%" + content + "%")
            //            select convenience;
            //        }
            //        else
            //        {
            //            error = "content is too short";
            //        }
            //    }
            //    else
            //    {
            //        error = "invalid option";
            //    }
            //}
            //else
            //{
            //    if (content == null)
            //    {
            //        error = "content is too short";
            //    }
            //    else if (option == null)
            //    {
            //        error = "invalid option";
            //    }
            //}

            //if (data == null)
            //{
            //    return Json(PaginatedList<int>.nothing(error));
            //}

            //PaginatedList<Convenience> _conveniences = await PaginatedList<Convenience>.CreateAsync(data, page);

            //return Json(_conveniences.paginate());
            //    var cons = await _context.Conveniences
            //    //.Include(con => con.ConvenienceMenus.OrderByDescending(menu=> menu.Id))
            //    //.Include(con => con.ConvenienceImages.OrderByDescending(img => img.Id))
            //    .OrderByDescending(con => con.Id)
            //    .AsSplitQuery()
            //    .AsNoTracking()
            //    .ToListAsync();

            //return Json(cons.Take(100));
        }

        [HttpGet("api/[controller]/item/{id}")]
        public async Task<ActionResult<Convenience>> GetItem(int id)
        {
            var con = await _context.Conveniences
                .Include(c => c.GeoList)
                .Include(c => c.ConvenienceMenus.OrderBy(menu => menu.Next))
                .Include(c => c.ConvenienceImages.OrderByDescending(img => img.Id))
                .OrderByDescending(c => c.Id)
                .AsSplitQuery()
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            return con;
        }

        [HttpPost("api/[controller]/create")]
        public async Task<IActionResult> Create([FromForm] ConvenienceDto param)
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

            //var options = new JsonSerializerOptions
            //{
            //    PropertyNameCaseInsensitive = true
            //};


            Convenience con = null;
            List<ConvenienceImage> conImages = null;
            List<ConvenienceMenu> conMenus = null;
            List<Upload.UploadedFile> newFileInfoList = null;

            //편의시설 유호성 검사
            if (param.Convenience != null)
            {
                con = JsonConvert.DeserializeObject<Convenience>(param.Convenience, settings);
                //con = JsonSerializer.Deserialize<Convenience>(param.Convenience, options);

                if (!TryValidateModel(con, nameof(con)))
                {
                    return BadRequest(ModelState);
                }

                if (con.GeoList != null)
                {
                    if (con.GeoList.Position.IsValid)
                    {
                        if (typeof(Point) == con.GeoList.Position.GetType())
                        {
                            var _geo = (Point)con.GeoList.Position;

                            con.GeoList.Code = GeoTool.GetCode(con.GeoList.Position.Centroid, _geoData.Value.features);
                        }
                    }
                    else
                    {
                        return Json("도형에러");
                    }
                }
            }

            if (param.ConvenienceImages != null)
            {
                //conImages = JsonSerializer.Deserialize<List<ConvenienceImage>>(param.ConvenienceImages, options);
                conImages = JsonConvert.DeserializeObject<List<ConvenienceImage>>(param.ConvenienceImages);

                if (!TryValidateModel(conImages, nameof(conImages)))
                {
                    return BadRequest(ModelState);
                }
            }

            if (param.ConvenienceMenus != null)
            {
                //conMenus = JsonSerializer.Deserialize<List<ConvenienceMenu>>(param.ConvenienceMenus, options);
                conMenus = JsonConvert.DeserializeObject<List<ConvenienceMenu>>(param.ConvenienceMenus);

                if (!TryValidateModel(conMenus, nameof(conMenus)))
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

                //편의시설
                if (con != null)
                {
                    if (con.MImageIndex != null)
                    {
                        var idx = con.MImageIndex.Value;
                        con.MFileName = newFileInfoList[idx].Name;
                        con.MFileOriginalName = newFileInfoList[idx].OriginalName;
                        con.MFileUri = newFileInfoList[idx].Uri;
                    }

                    if (con.TImageIndex != null)
                    {
                        var idx = con.TImageIndex.Value;
                        con.TFileName = newFileInfoList[idx].Name;
                        con.TFileOriginalName = newFileInfoList[idx].OriginalName;
                        con.TFileUri = newFileInfoList[idx].Uri;
                    }

                    if (con.GeoList != null)
                    {
                        _context.GeoLists.Add(con.GeoList);
                        await _context.SaveChangesAsync();
                    }

                    _context.Conveniences.Add(con);
                    await _context.SaveChangesAsync();
                }

                //편의시설 메뉴
                if (conMenus != null)
                {
                    for (int i = 0; i < conMenus.Count; i++)
                    {
                        conMenus[i].ConvenienceId = con.Id;

                        if (conMenus[i].ImageIndex != null)
                        {
                            var idx = conMenus[i].ImageIndex.Value;
                            conMenus[i].MFileName = newFileInfoList[idx].Name;
                            conMenus[i].MFileOriginalName = newFileInfoList[idx].OriginalName;
                            conMenus[i].MFileUri = newFileInfoList[idx].Uri;
                        }

                        conMenus[i].Next = i;

                        _context.ConvenienceMenus.Add(conMenus[i]);
                        await _context.SaveChangesAsync();
                    }

                    //for (int i = 0; i < conMenus.Count - 1; i++)
                    //{
                    //    conMenus[i].Next = conMenus[i + 1].Id;
                    //}
                }

                //편의시설 이미지
                if (conImages != null)
                {
                    for (int i = 0; i < conImages.Count; i++)
                    {
                        conImages[i].ConvenienceId = con.Id;

                        if (conImages[i].ImageIndex != null)
                        {
                            var idx = conImages[i].ImageIndex.Value;
                            conImages[i].Name = newFileInfoList[idx].Name;
                            conImages[i].OriginalName = newFileInfoList[idx].OriginalName;
                            conImages[i].Uri = newFileInfoList[idx].Uri;
                        }

                        conImages[i].Next = i;

                        _context.ConvenienceImages.Add(conImages[i]);
                        await _context.SaveChangesAsync();
                    }

                    //for (int i = 0; i < conImages.Count - 1; i++)
                    //{
                    //    conImages[i].Next = conImages[i + 1].Id;
                    //}
                }

                return Ok(con.Id);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Ok(-1);
            }
        }


        [HttpGet("api/[controller]/updatemorelist")]
        public async Task<IActionResult> GetUpdateMoreList()
        {
            var conMoreLists = await _context.ConUpdateLists.ToListAsync();

            return Json(conMoreLists);
        }

        [HttpPost("api/[controller]/updatemorelist/delete")]
        public async Task<IActionResult> DeleteUpdateMoreList([FromBody] UpdateMoreListDto param)
        {
            var conMoreList = await _context.ConUpdateLists.Where(con => con.Id == param.Id).SingleOrDefaultAsync();

            if (conMoreList != null)
            {
                _context.Remove(conMoreList);
                await _context.SaveChangesAsync();

                return Ok();
            }
            else 
            {
                return NotFound();
            }
            
        }

        [HttpPost("api/[controller]/updatemorelist/update")]
        public async Task<IActionResult> DeleteUpdateMoreList([FromBody] List<UpdateMoreListDto> param)
        {
            foreach (var item in param)
            {
                var conMoreList = await _context.ConUpdateLists.Where(con => con.Id == item.Id).AsNoTracking().SingleOrDefaultAsync();

                if (conMoreList == null)
                {
                    return NotFound();
                }

                var dbCon = await _context.Conveniences.Include(con => con.GeoList).AsNoTracking().SingleOrDefaultAsync(con => con.Id == conMoreList.ConvenienceId);

                if (dbCon == null)
                {
                    return NotFound();
                }

                //GeoList _geo = null;

                if (dbCon.GeoList == null)
                {
                    var _geo = new GeoList
                    {
                        Code = conMoreList.Code,
                        Group = 2,
                        Position = conMoreList.APosition
                    };

                    //ADD
                    _context.GeoLists.Add(_geo);
                    await _context.SaveChangesAsync();

                    dbCon.GeoListId = _geo.Id;
                }
                else
                {
                    //UPDATE
                    //_geo = new GeoList
                    //{
                    //    Id = dbCon.GeoListId,
                    //    Code = conMoreList.Code,
                    //    Group = 2,
                    //    Position = conMoreList.APosition
                    //};
                    dbCon.GeoList.Code = conMoreList.Code;
                    dbCon.GeoList.Group = 2;
                    dbCon.GeoList.Position = conMoreList.APosition;


                    //_context.GeoLists.Update(_geo);
                    //await _context.SaveChangesAsync();
                }


                dbCon.Name = conMoreList.AName;
                dbCon.Type = conMoreList.AType;
                dbCon.Address = conMoreList.AAddress;
                dbCon.Contact = conMoreList.AContact;


                _context.Conveniences.Update(dbCon);
                await _context.SaveChangesAsync();

                _context.ConUpdateLists.Remove(conMoreList);
                await _context.SaveChangesAsync();
            }


            return Ok();
        }


        [HttpPost("api/[controller]/update")]
        public async Task<IActionResult> Update([FromForm] ConvenienceDto param)
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

            //var options = new JsonSerializerOptions
            //{
            //    PropertyNameCaseInsensitive = true
            //};


            Convenience con = null;
            List<ConvenienceImage> conImages = null;
            List<ConvenienceMenu> conMenus = null;
            List<Upload.UploadedFile> newFileInfoList = null;

            //편의시설 유호성 검사
            if (param.Convenience != null)
            {
                con = JsonConvert.DeserializeObject<Convenience>(param.Convenience, settings);

                if (!TryValidateModel(con, nameof(con)))
                {
                    return BadRequest(ModelState);
                }

                if (con.GeoList != null)
                {
                    if (con.GeoList.Position.IsValid)
                    {
                        if (typeof(Point) == con.GeoList.Position.GetType())
                        {
                            var _geo = (Point)con.GeoList.Position;
                            con.GeoList.Code = GeoTool.GetCode(con.GeoList.Position.Centroid, _geoData.Value.features);
                        }
                    }
                    else
                    {
                        return Json("도형에러");
                    }
                }
            }

            if (param.ConvenienceImages != null)
            {
                //conImages = JsonSerializer.Deserialize<List<ConvenienceImage>>(param.ConvenienceImages, options);
                conImages = JsonConvert.DeserializeObject<List<ConvenienceImage>>(param.ConvenienceImages);
                if (!TryValidateModel(conImages, nameof(conImages)))
                {
                    return BadRequest(ModelState);
                }
            }

            if (param.ConvenienceMenus != null)
            {
                //conMenus = JsonSerializer.Deserialize<List<ConvenienceMenu>>(param.ConvenienceMenus, options);
                conMenus = JsonConvert.DeserializeObject<List<ConvenienceMenu>>(param.ConvenienceMenus);

                if (!TryValidateModel(conMenus, nameof(conMenus)))
                {
                    return BadRequest(ModelState);
                }
            }

            try
            {
                //파일 확장자 검사
                if ((param.FileImages != null && !AllowedImageExtensions.Validate(param.FileImages)))
                {
                    return BadRequest("You can upload only image files");
                }

                var dbCon = await _context.Conveniences
                    .Include(con => con.GeoList)
                    .Include(con => con.ConvenienceMenus)
                    .Include(con => con.ConvenienceImages)
                    .AsSplitQuery()
                    .AsNoTracking()
                    .SingleOrDefaultAsync(con => con.Id == param.Id);

                //파일 업로드
                if (param.FileImages != null)
                {
                    newFileInfoList = await Upload.UploadImageAndReturnInfo(param.FileImages, Upload.IMAGE_TYPE_ADMIN);
                }

                //편의시설
                if (con != null)
                {
                    if (con.MImageIndex == -1)
                    {
                        Upload.DeleteImage(dbCon.MFileUri, Upload.IMAGE_TYPE_ADMIN);

                        con.MFileName = null;
                        con.MFileOriginalName = null;
                        con.MFileUri = null;
                    }
                    else if (con.MImageIndex != null)
                    {
                        if (dbCon.MImageIndex == -1)
                        {
                            Upload.DeleteImage(dbCon.MFileUri, Upload.IMAGE_TYPE_ADMIN);
                        }

                        var idx = con.MImageIndex.Value;
                        con.MFileName = newFileInfoList[idx].Name;
                        con.MFileOriginalName = newFileInfoList[idx].OriginalName;
                        con.MFileUri = newFileInfoList[idx].Uri;
                    }

                    if (con.TImageIndex == -1)
                    {
                        Upload.DeleteImage(dbCon.TFileUri, Upload.IMAGE_TYPE_ADMIN);

                        con.TFileName = null;
                        con.TFileOriginalName = null;
                        con.TFileUri = null;

                    }
                    else if (con.TImageIndex != null)
                    {
                        if (dbCon.TImageIndex == -1)
                        {
                            Upload.DeleteImage(dbCon.TFileUri, Upload.IMAGE_TYPE_ADMIN);
                        }

                        var idx = con.TImageIndex.Value;
                        con.TFileName = newFileInfoList[idx].Name;
                        con.TFileOriginalName = newFileInfoList[idx].OriginalName;
                        con.TFileUri = newFileInfoList[idx].Uri;
                    }

                    _context.Conveniences.Update(con);
                    await _context.SaveChangesAsync();
                }

                

                //편의시설 메뉴
                if (conMenus != null)
                {
                    for (int i = 0; i < dbCon.ConvenienceMenus.Count; i++)
                    {
                        var delTarget = dbCon.ConvenienceMenus.ElementAt(i);
                        if (conMenus.Where(con => con.Id == delTarget.Id).FirstOrDefault() == null)
                        {
                            if (delTarget.MFileUri != null)
                            {
                                Upload.DeleteImage(delTarget.MFileUri, Upload.IMAGE_TYPE_ADMIN);
                            }

                            _context.Remove(_context.ConvenienceMenus.Find(delTarget.Id));
                            }
                        await _context.SaveChangesAsync();
                    }

                    for (int i=0;i<conMenus.Count;i++)
                    {
                        var dbConM = dbCon.ConvenienceMenus.Where(c => c.Id == conMenus[i].Id).FirstOrDefault();

                        if (0 < conMenus[i].Id)
                        {
                            if (conMenus[i].ImageIndex == -1)
                            {
                                Upload.DeleteImage(dbConM.MFileUri, Upload.IMAGE_TYPE_ADMIN);

                                conMenus[i].MFileName = null;
                                conMenus[i].MFileOriginalName = null;
                                conMenus[i].MFileUri = null;
                            }
                            else if (conMenus[i].ImageIndex != null)
                            {
                                if (conMenus[i].ImageIndex == -1)
                                {
                                    Upload.DeleteImage(dbConM.MFileUri, Upload.IMAGE_TYPE_ADMIN);
                                }
                                var idx = conMenus[i].ImageIndex.Value;
                                conMenus[i].MFileName = newFileInfoList[idx].Name;
                                conMenus[i].MFileOriginalName = newFileInfoList[idx].OriginalName;
                                conMenus[i].MFileUri = newFileInfoList[idx].Uri;
                            }

                            _context.ConvenienceMenus.Update(conMenus[i]);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            conMenus[i].ConvenienceId = con.Id;

                            if (conMenus[i].ImageIndex != null)
                            {
                                var idx = conMenus[i].ImageIndex.Value;
                                conMenus[i].MFileName = newFileInfoList[idx].Name;
                                conMenus[i].MFileOriginalName = newFileInfoList[idx].OriginalName;
                                conMenus[i].MFileUri = newFileInfoList[idx].Uri;
                            }

                            _context.ConvenienceMenus.Add(conMenus[i]);
                            await _context.SaveChangesAsync();
                        }

                        conMenus[i].Next = i;
                        await _context.SaveChangesAsync();
                    }
                }

                //편의시설 이미지
                if (conImages != null)
                {
                    for (int i = 0; i < dbCon.ConvenienceImages.Count; i++)
                    {
                        var delTarget = dbCon.ConvenienceImages.ElementAt(i);
                        if (conImages.Where(con => con.Id == delTarget.Id).FirstOrDefault() == null)
                        {
                            if (delTarget.Uri != null)
                            {
                                Upload.DeleteImage(delTarget.Uri, Upload.IMAGE_TYPE_ADMIN);
                            }

                            _context.Remove(_context.ConvenienceImages.Find(delTarget.Id));
                        }
                        await _context.SaveChangesAsync();
                    }

                    for (int i = 0; i < conImages.Count; i++)
                    {
                        var dbConM = dbCon.ConvenienceImages.Where(c => c.Id == conImages[i].Id).FirstOrDefault();

                        if (0 < conImages[i].Id)
                        {
                            if (conImages[i].ImageIndex == -1)
                            {
                                Upload.DeleteImage(dbConM.Uri, Upload.IMAGE_TYPE_ADMIN);

                                conImages[i].Name = null;
                                conImages[i].OriginalName = null;
                                conImages[i].Uri = null;
                            }
                            else if (conImages[i].ImageIndex != null)
                            {
                                if (conImages[i].ImageIndex == -1)
                                {
                                    Upload.DeleteImage(dbConM.Uri, Upload.IMAGE_TYPE_ADMIN);
                                }
                                var idx = conImages[i].ImageIndex.Value;
                                conImages[i].Name = newFileInfoList[idx].Name;
                                conImages[i].OriginalName = newFileInfoList[idx].OriginalName;
                                conImages[i].Uri = newFileInfoList[idx].Uri;
                            }

                            _context.ConvenienceImages.Update(conImages[i]);
                            await _context.SaveChangesAsync();
                        }
                        else
                        {
                            conImages[i].ConvenienceId = con.Id;

                            if (conImages[i].ImageIndex != null)
                            {
                                var idx = conImages[i].ImageIndex.Value;
                                conImages[i].Name = newFileInfoList[idx].Name;
                                conImages[i].OriginalName = newFileInfoList[idx].OriginalName;
                                conImages[i].Uri = newFileInfoList[idx].Uri;
                            }

                            _context.ConvenienceImages.Add(conImages[i]);
                            await _context.SaveChangesAsync();
                        }

                        conImages[i].Next = i;
                        await _context.SaveChangesAsync();
                    }
                }

                await _context.SaveChangesAsync();

                return Ok();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }


            return Ok();
        }

        public class SaveRefGolfParam
        {
            public int convId { get; set; }
            public int[] Ids { get; set; }
        }

        public class GetItemParam
        {
            public int Id { get; set; }
        }

        public class ConvenienceDto
        {
            public int Id { get; set; }
            public string Convenience { get; set; }
            public string ConvenienceMenus { get; set; }
            public string ConvenienceImages { get; set; }
            public IFormFile[] FileImages { get; set; }
        }

        public class UpdateMoreListDto
        {
            public int Id { get; set; }
        }

    }
}
