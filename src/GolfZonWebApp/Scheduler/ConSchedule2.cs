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
    public class ConSchedule2 : IJob
    {
        private readonly IOptions<GeoMore> _geoMore;
        public static int qCount = 0;

        public ConSchedule2(IOptions<GeoMore> geoMore)
        {
            _geoMore = geoMore;
        }


        public Task Execute(IJobExecutionContext context)
        {
            var geoCityList = _geoMore.Value.features;

            //for (int i=0;i<_geoMore.Value.features.Count;i++)
            //{
            //    Console.WriteLine(_geoMore.Value.features[i].properties.SIG_KOR_NM);
            //}


            //_logger.LogInformation("API LOAD START");
            try
            {
                GetWeather(geoCityList);
            }
            catch (Exception e)
            {
                Console.WriteLine("API LOAD ERROR");
                Console.WriteLine(e.Message);
            }


            return Task.CompletedTask;
        }

        public class KeyParams
        {
            public KeyParams(Point location)
            {
                Location = location;
            }

            public KeyParams(string city)
            {
                City = city;
            }

            public KeyParams(Point location, int code)
            {
                Location = location;
                Code = code;
            }

            public Point Location { get; set; }
            public string City { get; set; }
            public int Code { get; set; }

            public string GetParam()
            {
                if (Code == 1 || Code == 2)
                {
                    return "https://api.openweathermap.org/data/2.5/forecast/daily?"
                        + "lat=" + Location.Y
                        + "&lon=" + Location.X
                        + "&cnt=" + 14
                        + "&lang=kr"
                        + "&units=metric"
                        + "&appid=177fc80bf95e216225d3d65dcbb7714c";
                }
                else if (City == null)
                {
                    return "https://api.openweathermap.org/data/2.5/forecast/daily?"
                        + "lat=" + Location.Y
                        + "&lon=" + Location.X
                        + "&cnt=" + 14
                        + "&lang=kr"
                        + "&units=metric"
                        + "&appid=177fc80bf95e216225d3d65dcbb7714c";
                }
                else
                {
                    return "https://api.openweathermap.org/data/2.5/forecast/daily?"
                        + "q=" + City
                        + "&cnt=" + 14
                        + "&lang=kr"
                        + "&units=metric"
                        + "&appid=177fc80bf95e216225d3d65dcbb7714c";
                }
            }


            public string GetNowParam()
            {
                if (Code == 1 || Code == 2)
                {
                    return "https://api.openweathermap.org/data/2.5/weather?"
                        + "lat=" + Location.Y
                        + "&lon=" + Location.X
                        + "&lang=kr"
                        + "&units=metric"
                        + "&appid=177fc80bf95e216225d3d65dcbb7714c";
                }
                else if (City == null)
                {
                    return "https://api.openweathermap.org/data/2.5/weather?"
                        + "lat=" + Location.Y
                        + "&lon=" + Location.X
                        + "&lang=kr"
                        + "&units=metric"
                        + "&appid=177fc80bf95e216225d3d65dcbb7714c";
                }
                else
                {
                    return "https://api.openweathermap.org/data/2.5/weather?"
                        + "q=" + City
                        + "&lang=kr"
                        + "&units=metric"
                        + "&appid=177fc80bf95e216225d3d65dcbb7714c";
                }
            }
        }

        public class GeoCode
        {
            public string KorName { get; set; }
            public string EngName { get; set; }
            public int Code { get; set; }
            public Point Position { get; set; } = null;
        }



        static async void SetWeather(WeatherMap reData, int id, int type)
        {
            try
            {
                if (type == 0)
                {
                    using (var con = new HelloWorldJobContext())
                    {
                        var dWeather = con.Weathers.Where(wt => wt.GolfClubId == id).ToList();
                        con.Weathers.RemoveRange(dWeather);


                        foreach (var item in reData.List)
                        {
                            //Console.WriteLine("id: {0}", id);
                            //Console.WriteLine(TimeStampToDateTime(item.Dt));
                            //Console.WriteLine(item.Temp.Min);
                            //Console.WriteLine(item.Temp.Max);
                            //Console.WriteLine(item.Speed);
                            //Console.WriteLine(item.Deg);
                            //Console.WriteLine(item.Weather[0].Id);
                            //Console.WriteLine(item.Weather[0].Main);

                            con.Weathers.Add(new Weather
                            {
                                GolfClubId = id,
                                Dt = TimeStampToDateTime(item.Dt),
                                Now = (item.Temp.Min+item.Temp.Max)/2,
                                Min = item.Temp.Min,
                                Max = item.Temp.Max,
                                Speed = item.Speed,
                                Deg = item.Deg,
                                WeatherId = item.Weather[0].Id,
                                Main = item.Weather[0].Main,
                                Description = item.Weather[0].Description,
                                Icon = item.Weather[0].Icon
                            });
                            await con.SaveChangesAsync();
                        }
                    }
                }
                else if (type == 1)
                {
                    using (var con = new HelloWorldJobContext())
                    {
                        var dWeather = con.WeatherAreas.Where(wt => wt.Code == id).ToList();
                        con.WeatherAreas.RemoveRange(dWeather);

                        foreach (var item in reData.List)
                        {
                            con.WeatherAreas.Add(new WeatherArea
                            {
                                Code = id,
                                Dt = TimeStampToDateTime(item.Dt),
                                Now = (item.Temp.Min + item.Temp.Max) / 2,
                                Min = item.Temp.Min,
                                Max = item.Temp.Max,
                                Speed = item.Speed,
                                Deg = item.Deg,
                                WeatherId = item.Weather[0].Id,
                                Main = item.Weather[0].Main,
                                Description = item.Weather[0].Description,
                                Icon = item.Weather[0].Icon
                            });
                            await con.SaveChangesAsync();
                        }
                    }
                }
                else if (type == 2) 
                {
                    using (var con = new HelloWorldJobContext())
                    {
                        var dWeather = con.WeatherCities.Where(wt => wt.CityCode == id).ToList();
                        con.WeatherCities.RemoveRange(dWeather);

                        foreach (var item in reData.List)
                        {
                            con.WeatherCities.Add(new WeatherCity
                            {
                                CityCode = id,
                                Dt = TimeStampToDateTime(item.Dt),
                                Now = (item.Temp.Min + item.Temp.Max) / 2,
                                Min = item.Temp.Min,
                                Max = item.Temp.Max,
                                Speed = item.Speed,
                                Deg = item.Deg,
                                WeatherId = item.Weather[0].Id,
                                Main = item.Weather[0].Main,
                                Description = item.Weather[0].Description,
                                Icon = item.Weather[0].Icon
                            });
                            await con.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(":::::::::::: SetWeather Exception ::::::::::::");
                Console.WriteLine(e.Message);
            }
        }

        static bool GetWeather(List<FeaturesMore> geoCityList) 
        {
            using (var con = new HelloWorldJobContext())
            {
                foreach (var item in con.GeoLists.Where(ge => ge.Group == 1).ToList().Select((val, idx) => (val, idx)))
                {
                    try
                    {
                        var par = new KeyParams(item.val.Position.Centroid);

                        HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(par.GetParam());
                        myRequest.Method = "GET";

                        using (HttpWebResponse resp = (HttpWebResponse)myRequest.GetResponse())
                        {
                            HttpStatusCode status = resp.StatusCode;
                            Stream respStream = resp.GetResponseStream();
                            qCount += 1;

                            using (StreamReader sr = new StreamReader(respStream))
                            {
                                // if (item.val.Id == 5) { Console.WriteLine(sr.ReadToEnd()); }

                                var reData = JsonConvert.DeserializeObject<WeatherMap>(sr.ReadToEnd());
                                var tgGolf = con.GolfClubs.Where(g => g.GeoListId == item.val.Id).SingleOrDefault();
                                
                                if (tgGolf != null)
                                {
                                    SetWeather(reData, tgGolf.Id, 0);

                                    var nowData = GetNowWeather(par.GetNowParam());

                                    if (nowData != null)
                                    {
                                        var tw_ = con.Weathers.Where(w => w.GolfClubId == tgGolf.Id).OrderBy(w => w.Dt).FirstOrDefault();

                                        if (tw_ != null)
                                        {
                                            tw_.Now = nowData.Main.Temp;
                                            tw_.Speed = nowData.Wind.Speed;
                                            tw_.Deg = nowData.Wind.Deg;
                                            tw_.WeatherId = nowData.Weather[0].Id;
                                            tw_.Main = nowData.Weather[0].Main;
                                            tw_.Icon = nowData.Weather[0].Icon;
                                            tw_.Description = nowData.Weather[0].Description;

                                            con.Weathers.Update(tw_);
                                            con.SaveChanges();
                                        }
                                    }
                                }
                            }
                        }

                        Console.WriteLine("날씨 업데이트 중 ....  {0}/{1}", item.idx, con.GeoLists.Count(ge => ge.Group == 1));
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }


                var cityList = SetGeoList();
                foreach (var item in cityList.Select((val, idx) => (val, idx)))
                {
                    try
                    { 
                        KeyParams par = null;   

                        if (item.val.Code == 1 || item.val.Code == 1)
                        {
                            par = new KeyParams(item.val.Position, item.val.Code);
                        }
                        else
                        {
                            par = new KeyParams(item.val.EngName);
                        }

                        //Console.WriteLine(par.GetParam());
                        HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(par.GetParam());
                        myRequest.Method = "GET";
                        //myRequest.Headers.Add("Authorization", app_key);

                        using (HttpWebResponse resp = (HttpWebResponse)myRequest.GetResponse())
                        {
                            HttpStatusCode status = resp.StatusCode;
                            Stream respStream = resp.GetResponseStream();
                            qCount += 1;
                            //Console.WriteLine("{0} ::: {1}", par.getParam(), qCount);

                            using (StreamReader sr = new StreamReader(respStream))
                            {
                                //Console.WriteLine(sr.ReadToEnd());
                                var reData = JsonConvert.DeserializeObject<WeatherMap>(sr.ReadToEnd());
                                SetWeather(reData, item.val.Code, 1);

                                var nowData = GetNowWeather(par.GetNowParam());

                                if (nowData != null)
                                {
                                    var tw_ = con.WeatherAreas.Where(w => w.Code == item.val.Code).OrderBy(w => w.Dt).FirstOrDefault();

                                    if (tw_ != null)
                                    {
                                        tw_.Now = nowData.Main.Temp;
                                        tw_.Speed = nowData.Wind.Speed;
                                        tw_.Deg = nowData.Wind.Deg;
                                        tw_.WeatherId = nowData.Weather[0].Id;
                                        tw_.Main = nowData.Weather[0].Main;
                                        tw_.Icon = nowData.Weather[0].Icon;
                                        tw_.Description = nowData.Weather[0].Description;

                                        con.WeatherAreas.Update(tw_);
                                        con.SaveChanges();
                                    }
                                }
                            }
                        }

                        Console.WriteLine("행정구역 날씨 업데이트 중 ....  {0}/{1}", item.idx, cityList.Count());
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }


                for (int i = 0; i < geoCityList.Count; i++)
                {
                    try
                    {

                        var par = new KeyParams(geoCityList[i].geometry.Centroid);

                        HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(par.GetParam());
                        myRequest.Method = "GET";

                        using (HttpWebResponse resp = (HttpWebResponse)myRequest.GetResponse())
                        {
                            HttpStatusCode status = resp.StatusCode;
                            Stream respStream = resp.GetResponseStream();
                            qCount += 1;

                            using (StreamReader sr = new StreamReader(respStream))
                            {
                                var reData = JsonConvert.DeserializeObject<WeatherMap>(sr.ReadToEnd());

                                SetWeather(reData, geoCityList[i].properties.SIG_CD, 2);

                                var nowData = GetNowWeather(par.GetNowParam());

                                if (nowData != null)
                                {
                                    var tw_ = con.WeatherCities.Where(w => w.CityCode == geoCityList[i].properties.SIG_CD).OrderBy(w => w.Dt).FirstOrDefault();

                                    if (tw_ != null)
                                    {
                                        tw_.Now = nowData.Main.Temp;
                                        tw_.Speed = nowData.Wind.Speed;
                                        tw_.Deg = nowData.Wind.Deg;
                                        tw_.WeatherId = nowData.Weather[0].Id;
                                        tw_.Main = nowData.Weather[0].Main;
                                        tw_.Icon = nowData.Weather[0].Icon;
                                        tw_.Description = nowData.Weather[0].Description;

                                        con.WeatherCities.Update(tw_);
                                        con.SaveChanges();
                                    }
                                }
                            }
                        }

                        Console.WriteLine("도시 날씨 업데이트 중 ....  {0}/{1}", i, geoCityList.Count);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(":::::::::::: GetWeather Exception ::::::::::::");
                        Console.WriteLine(e.Message);
                    }
                }










                return true;
            }
        }


        static WeatherNow GetNowWeather(string q)
        {
            WeatherNow reData = null;
            try
            {
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(q);
                myRequest.Method = "GET";

                using (HttpWebResponse resp = (HttpWebResponse)myRequest.GetResponse())
                {
                    HttpStatusCode status = resp.StatusCode;
                    Stream respStream = resp.GetResponseStream();

                    using (StreamReader sr = new StreamReader(respStream))
                    {
                        reData = JsonConvert.DeserializeObject<WeatherNow>(sr.ReadToEnd());

                        //Console.WriteLine(reData.Main.Temp);

                        return reData;
                    }
                }
            }
            catch (Exception e) 
            {
                Console.WriteLine(e);
                reData = null;
                return reData;
            }
        }

        static DateTime TimeStampToDateTime(long value)
        {
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddSeconds(value).ToLocalTime();
            return dt;
        }

        static List<GeoCode> SetGeoList()
        {
            var geoCodeList = new List<GeoCode>{
                new GeoCode
                {
                    KorName = "한강이남",
                    EngName = "Seoul",
                    Code = 1,
                    Position = new Point(127.03633994679214,37.27483659034889)
                },
                new GeoCode
                {
                    KorName = "한강이북",
                    EngName = "Gyeonggi-do",
                    Code = 2,
                    Position = new Point(127.08438520823772,37.64847967457447)
                },
                //new GeoCode
                //{
                //    KorName = "부산",
                //    EngName = "Busan",
                //    Code = 21
                //},
                //new GeoCode
                //{
                //    KorName = "대구",
                //    EngName = "Daegu",
                //    Code = 22
                //},
                //new GeoCode
                //{
                //    KorName = "인천",
                //    EngName = "Incheon",
                //    Code = 23
                //},
                //new GeoCode
                //{
                //    KorName = "광주",
                //    EngName = "Gwangju",
                //    Code = 24
                //},
                //new GeoCode
                //{
                //    KorName = "대전",
                //    EngName = "Daejeon",
                //    Code = 25
                //},
                //new GeoCode
                //{
                //    KorName = "울산",
                //    EngName = "Ulsan",
                //    Code = 26
                //},
                //new GeoCode
                //{
                //    KorName = "세종",
                //    EngName = "Sejong",
                //    Code = 29
                //},
                new GeoCode
                {
                    KorName = "강원",
                    EngName = "Gangwon-do",
                    Code = 32
                },
                new GeoCode
                {
                    KorName = "충북",
                    EngName = "Chungcheongbuk-do",
                    Code = 33
                },
                new GeoCode
                {
                    KorName = "충남",
                    EngName = "Chungcheongnam-do",
                    Code = 34
                },
                new GeoCode
                {
                    KorName = "전북",
                    EngName = "Jeollabuk-do",
                    Code = 35
                },
                new GeoCode
                {
                    KorName = "전남",
                    EngName = "Jeollanam-do",
                    Code = 36
                },
                new GeoCode
                {
                    KorName = "경북",
                    EngName = "Gyeongsangbuk-do",
                    Code = 37
                },
                new GeoCode
                {
                    KorName = "경남",
                    EngName = "Gyeongsangnam-do",
                    Code = 38
                },
                new GeoCode
                {
                    KorName = "제주",
                    EngName = "Jeju",
                    Code = 39
                },
            };



            return geoCodeList;
        }
    }
}
