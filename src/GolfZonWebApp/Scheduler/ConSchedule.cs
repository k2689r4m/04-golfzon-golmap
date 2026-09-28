using System;


using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Net;
using System.IO;
using Quartz;
using Newtonsoft.Json;
using GolfZonWebApp.Types;
using GolfZonWebApp.Models;
using System.Data.SqlClient;
using GolfZonWebApp.Data;
using NetTopologySuite.Geometries;
using Microsoft.Extensions.Options;
using GolfZonWebApp.Lib;

namespace GolfZonWebApp.Scheduler
{
    [DisallowConcurrentExecution]
    public class ConSchedule : IJob
    {
        private readonly IOptions<Geo> _geo;
        public static int qCount = 0;
        //private readonly ILogger<HelloWorldJob> _logger;

        //private readonly GolfzonContext _context;
        //public HelloWorldJob(GolfzonContext context)
        //{
        //    _context = context;
        //}

        public ConSchedule(IOptions<Geo> geo)
        {
            _geo = geo;
        }

        public Task Execute(IJobExecutionContext context)
        {
            //_logger.LogInformation("API LOAD START");

            int dis = 10000;

            try
            {
                Console.WriteLine("카페 START ++++++++++++++++++++++++++++++++");
                GetConList("카페", "CE7", dis);
                Console.WriteLine("카페 END ++++++++++++++++++++++++++++++++");

                Console.WriteLine("편의점 START ++++++++++++++++++++++++++++++++");
                GetConList("편의점", "CS2", dis);
                Console.WriteLine("편의점 END ++++++++++++++++++++++++++++++++");

                Console.WriteLine("주유소 START ++++++++++++++++++++++++++++++++");
                GetConList("주유소", "OL7", dis);
                Console.WriteLine("주유소 END ++++++++++++++++++++++++++++++++");

                Console.WriteLine("관광 START ++++++++++++++++++++++++++++++++");
                GetConList("관광", "AT4", dis);
                Console.WriteLine("관광 END ++++++++++++++++++++++++++++++++");

                Console.WriteLine("숙박 START ++++++++++++++++++++++++++++++++");
                GetConList("숙박", "AD5", dis);
                Console.WriteLine("숙박 END ++++++++++++++++++++++++++++++++");

                Console.WriteLine("음식점 START ++++++++++++++++++++++++++++++++");
                GetConList("음식점", "FD6", dis);
                Console.WriteLine("음식점 END ++++++++++++++++++++++++++++++++");

                Console.WriteLine("GDR START ++++++++++++++++++++++++++++++++");
                GetConList("GDR", "GDR", dis);
                Console.WriteLine("GDR END ++++++++++++++++++++++++++++++++");

                Console.WriteLine("골프존마켓 START ++++++++++++++++++++++++++++++++");
                GetConList("골프존마켓", "골프존마켓", dis);
                Console.WriteLine("골프존마켓 END ++++++++++++++++++++++++++++++++");

                Console.WriteLine("골프존 START ++++++++++++++++++++++++++++++++");
                GetConList("골프존", "골프존", dis);
                Console.WriteLine("골프존 END ++++++++++++++++++++++++++++++++");

                Console.WriteLine("연습장 START ++++++++++++++++++++++++++++++++");
                GetConList("골프연습장", "연습장", dis);
                Console.WriteLine("연습장 END ++++++++++++++++++++++++++++++++");
            }
            catch (Exception e)
            {
                Console.WriteLine("API LOAD ERROR");
                Console.WriteLine(e.Message);
            }


            return Task.CompletedTask;
        }

        public bool GetConList(string keyWord, string _code, int radius) 
        {
            using (var con = new HelloWorldJobContext())
            {
                //string app_key = "KakaoAK 318970795ee8250e9d7c502ae6603c89";
                string app_key = "KakaoAK 1b3ef1220b2dcac15f25ce247367b0ea";

                //var geoList = con.GeoLists.Where(ge => ge.Group == 1).ToList();
                //Console.WriteLine(geoList.Count);

                var geoList = con.GeoLists
                    .Where(geo => geo.Group == 4)
                    .ToList();
                
                var type = GetCategory(_code);
                for (int i = 0; i < geoList.Count; i++)
                {
                    if (typeof(Point) != geoList[i].Position.GetType())
                    {
                        //Console.WriteLine("ERROR :: GeoList ID : {0} 타입은 Point 이어야 합니다.", geoList[i].Id);
                        continue;
                    }
                    
                    var ch = con.ClubHouses.Where(con => con.GeoListId == geoList[i].Id).FirstOrDefault();

                    if (ch == null)
                    {
                        //Console.WriteLine("ERROR :: GeoList ID : {0} 찾을수 없는 FK");
                        continue;
                    }
                    var golfId = ch.GolfClubId;
                    var par = new KeyParams(keyWord, 1, radius, _code, (Point)geoList[i].Position);

                    var gc = con.GolfClubs.Find(golfId);

                    while (true)
                    {
                        HttpWebRequest myRequest = null;
                        if (keyWord == "GDR" || keyWord == "골프존마켓" || keyWord == "골프존" || keyWord == "골프연습장")
                        {
                            if (gc == null)
                            {
                                break;
                            }
                            //Console.WriteLine(par.GetParam(gc.Name + " " + keyWord));
                            myRequest = (HttpWebRequest)WebRequest.Create(par.GetParam(keyWord));
                        }
                        else
                        {
                            myRequest = (HttpWebRequest)WebRequest.Create(par.GetParam());
                        }
                            
                        myRequest.Method = "GET";
                        myRequest.Headers.Add("Authorization", app_key);

                        SearchItem reData = null;


                        using (HttpWebResponse resp = (HttpWebResponse)myRequest.GetResponse())
                        {
                            HttpStatusCode status = resp.StatusCode;
                            Stream respStream = resp.GetResponseStream();
                            qCount += 1;
                            //Console.WriteLine("{0} ::: {1}", par.getParam(), qCount);

                            using (StreamReader sr = new StreamReader(respStream))
                            {
                                reData = JsonConvert.DeserializeObject<SearchItem>(sr.ReadToEnd());
                                //Console.WriteLine(JsonConvert.SerializeObject(reData));
                            }

                            

                            if (reData.meta.is_end)
                            {
                                SetConData(reData, golfId, type);
                                break;
                            }
                            else
                            {
                                par.Page += 1;
                                SetConData(reData, golfId, type);
                            }
                        }

                    }

                    //Console.WriteLine("편의 시설 {0} 업데이트 중 ....  {1}/{2}", keyWord, i, geoList.Count);
                }
                return true;
            }
        }

        public async void SetConData(SearchItem reData, int golfId, string type)
        {
            try
            {
                using (var con = new HelloWorldJobContext())
                {
                    foreach (var item in reData.documents)
                    {
                        var dbCon = con.Conveniences.Where(con => con.KaKaoId == item.id).FirstOrDefault();
                        var cCode = type;
                        var position = new Point(item.x, item.y) { SRID = 4326 };

                        if (dbCon == null)
                        {
                            if (cCode == "none")
                            {
                                continue;
                            }

                            var geo = new GeoList
                            {
                                Position = position,
                                Code = GeoTool.GetCode(new Point(item.x, item.y), _geo.Value.features),
                                Group = 2
                            };

                            con.GeoLists.Add(geo);
                            await con.SaveChangesAsync();

                            var addCon = new Convenience
                            {
                                KaKaoId = item.id,
                                GeoListId = geo.Id,
                                Name = item.place_name,
                                Address = item.address_name,
                                Contact = item.phone,
                                Type = cCode,
                                Status = 1
                            };
                            con.Conveniences.Add(addCon);
                            await con.SaveChangesAsync();

                            
                        }
                        else
                        {
                            var tPo = await con.GeoLists.FindAsync(dbCon.GeoListId);

                            //if (tPo == null)
                            //{
                            //    continue;
                            //}

                            if (dbCon.Name != item.place_name || dbCon.Type != cCode || dbCon.Address != item.address_name || dbCon.Contact != item.phone || !tPo.Position.Equals(position))
                            {
                                var dbCu = con.ConUpdateLists.Where(cu => cu.ConvenienceId == dbCon.Id).FirstOrDefault();

                                if (dbCu != null)
                                {
                                    con.ConUpdateLists.Remove(dbCu);
                                }

                                con.ConUpdateLists.Add(new ConUpdateList
                                {
                                    ConvenienceId = dbCon.Id,
                                    BName = dbCon.Name,
                                    AName = item.place_name,
                                    BType = dbCon.Type,
                                    AType = cCode,
                                    BAddress = dbCon.Address,
                                    AAddress = item.address_name,
                                    BContact = dbCon.Contact,
                                    AContact = item.phone,
                                    BPosition = tPo.Position,
                                    APosition = position,
                                    Code = GeoTool.GetCode(position, _geo.Value.features)
                                });
                                await con.SaveChangesAsync();
                            }
                        }


                        var _dbCon = con.Conveniences.Where(con => con.KaKaoId == item.id).FirstOrDefault();

                        if (_dbCon != null)
                        {
                            var _refGolf = con.RefGolfLists
                                .Where(re => re.GolfClubId == golfId)
                                .Where(re => re.ConvenienceId == _dbCon.Id)
                                .FirstOrDefault();

                            if (_refGolf == null)
                            {
                                con.RefGolfLists.Add(new RefGolfList
                                {
                                    GolfClubId = golfId,
                                    ConvenienceId = _dbCon.Id
                                });
                                await con.SaveChangesAsync();
                            }
                            //else
                            //{
                            //    _refGolf.

                            //    con.RefGolfLists.Update(new RefGolfList
                            //    {

                            //        GolfClubId = golfId,
                            //        ConvenienceId = _dbCon.Id
                            //    });
                            //    await con.SaveChangesAsync();
                            //}


                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        static string GetCategory(string code)
        {


            if (code == "CE7")          //카페 => 카페
            {
                return "카페";
            }
            else if (code == "CS2")     //편의점 => 편의점
            {
                return "편의점";
            }
            else if (code == "OL7")     //주유소 => 주유소
            {
                return "주유소";
            }
            else if (code == "AT4")     //관광명소 => 명소
            {
                return "명소";
            }
            else if (code == "AD5")     //숙박 => 숙박
            {
                return "숙박";
            }
            else if (code == "FD6")     //음식점 => 맛집
            {
                return "음식점";
            }
            else if (code == "GDR")     //음식점 => 맛집
            {
                return "GDR";
            }
            else if (code == "골프존마켓")    
            {
                return "골프존마켓";
            }
            else if (code == "골프존")     
            {
                return "골프존";
            }
            else if (code == "연습장")
            {
                return "연습장";
            }
            else
            {
                return "none";
            }
            
        }


        public class KeyParams
        {
            public KeyParams(string query, int page, int radius, string code, Point location)
            {
                this.Query = query;
                this.Page = page;
                this.Radius = radius;
                this.Location = location;
                this.Code = code;
            }

            public string Query { get; set; }
            public int Page { get; set; }
            public int Radius { get; set; }
            public Point Location { get; set; }
            public string Code { get; set; }

            public string GetParam()
            {
                return "https://dapi.kakao.com/v2/local/search/keyword.json?sort=distance&query=" + this.Query
                + "&category_group_code=" + this.Code
                + "&size=15&page=" + this.Page
                + "&y=" + Location.Y
                + "&x=" + Location.X
                + "&radius=" + Radius;
            }

            public string GetParam(string q)
            {
                return "https://dapi.kakao.com/v2/local/search/keyword.json?sort=distance&query=" + q
                    + "&size=15&page=" + this.Page
                    + "&y=" + Location.Y
                    + "&x=" + Location.X
                    + "&radius=" + Radius;
            }



            //Console.WriteLine("편의점 START ++++++++++++++++++++++++++++++++");
            //    GetConList("편의점", "CS2", 1500);
            //Console.WriteLine("편의점 END ++++++++++++++++++++++++++++++++");

            //    Console.WriteLine("주요소 START ++++++++++++++++++++++++++++++++");
            //    GetConList("주요소", "OL7", 1500);
            //Console.WriteLine("주요소 END ++++++++++++++++++++++++++++++++");

        }


    }
}
