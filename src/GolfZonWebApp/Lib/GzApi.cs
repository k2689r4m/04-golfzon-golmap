using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using GolfZonWebApp.Data;
using GolfZonWebApp.Models;
using GolfZonWebApp.Types;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NetTopologySuite.Geometries;
using Newtonsoft.Json;

namespace GolfZonWebApp.Lib
{
    public class GzApi
    {
        public static string API_DOMAIN_OAUTH = "oauth2.golfzon.com";
        public static string API_DOMAIN_GAPI = "gapi.golfzon.com";
        public static string API_MILEAGE = "https://billapi.golfzon.com";

        //public static string API_DOMAIN_OAUTH = "oauth2.qa.golfzon.com";
        //public static string API_DOMAIN_GAPI = "gapi.qa.golfzon.com";
        //public static string API_MILEAGE = "https://billapi.golfzon.com";



        public static async void UpdateWeather(GolfzonContext _context, int geoId, int golfId)
        {
            var item = _context.GeoLists.Find(geoId);

            if (item != null)
            {
                try
                {
                    var par = new KeyParams(item.Position.Centroid);

                    HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(par.GetParam());
                    myRequest.Method = "GET";

                    using (HttpWebResponse resp = (HttpWebResponse)myRequest.GetResponse())
                    {
                        HttpStatusCode status = resp.StatusCode;
                        Stream respStream = resp.GetResponseStream();

                        using (StreamReader sr = new StreamReader(respStream))
                        {
                            var reData = JsonConvert.DeserializeObject<WeatherMap>(sr.ReadToEnd());

                            SetWeather(_context, reData, golfId);

                            var nowData = GetNowWeather(par.GetNowParam());

                            if (nowData != null)
                            {
                                var tw_ = _context.Weathers.Where(w => w.GolfClubId == golfId).OrderBy(w => w.Dt).FirstOrDefault();

                                if (tw_ != null)
                                {
                                    tw_.Now = nowData.Main.Temp;
                                    tw_.Speed = nowData.Wind.Speed;
                                    tw_.Deg = nowData.Wind.Deg;
                                    tw_.WeatherId = nowData.Weather[0].Id;
                                    tw_.Main = nowData.Weather[0].Main;
                                    tw_.Icon = nowData.Weather[0].Icon;
                                    tw_.Description = nowData.Weather[0].Description;

                                    _context.Weathers.Update(tw_);
                                    _context.SaveChanges();
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
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

        static async void SetWeather(GolfzonContext _context, WeatherMap reData, int id)
        {
            try
            {
                var dWeather = _context.Weathers.Where(wt => wt.GolfClubId == id).ToList();
                _context.Weathers.RemoveRange(dWeather);

                List<Weather> we = new List<Weather>();

                foreach (var item in reData.List)
                {
                    we.Add(new Weather
                    {
                        GolfClubId = id,
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
                }

                _context.Weathers.AddRange(we);
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        //asfsdafsda
        public static GzAccessToken GetToken()
        {
            string par = "http://"+ API_DOMAIN_OAUTH +"/oauth/token?grant_type=client_credentials&scope=read";
            //string par = "http://oauth2.spazon.com/oauth/token?grant_type=client_credentials&scope=read";
            GzAccessToken reData = null;

            try
            {
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(par);
                myRequest.Method = "POST";
                myRequest.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes("contents_service:rhfvmwhs1!"));

                using (HttpWebResponse resp = (HttpWebResponse)myRequest.GetResponse())
                {
                    if (resp.StatusCode != HttpStatusCode.OK)
                    {
                        return reData;
                    }

                    HttpStatusCode status = resp.StatusCode;
                    Stream respStream = resp.GetResponseStream();

                    using (StreamReader sr = new StreamReader(respStream))
                    {
                        reData = JsonConvert.DeserializeObject<GzAccessToken>(sr.ReadToEnd());

                        return reData;
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("골프존 토큰 발급 에러 :: {0}", e);
                return reData;
            }
            
        }

        public static List<Recommend> GetRecommend(int id)
        {
            string par = "http://"+ API_DOMAIN_GAPI + "/platform-data/api/golmap/v1/recommend/users/" + id;
            //string par = "http://gapi.spazon.com/platform-data/api/golmap/v1/recommend/users/" + id;
            var token = GetToken().Access_token;
            List<Recommend> reData = new List<Recommend>();

            try
            {
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(par);
                myRequest.Method = "GET";
                myRequest.Headers["Authorization"] = "Bearer " + token;
                myRequest.Accept = "application/json";

                using (HttpWebResponse resp = (HttpWebResponse)myRequest.GetResponse())
                {
                    if (resp.StatusCode != HttpStatusCode.OK)
                    {
                        return reData;
                    }

                    HttpStatusCode status = resp.StatusCode;
                    Stream respStream = resp.GetResponseStream();

                    using (StreamReader sr = new StreamReader(respStream))
                    {
                        reData = JsonConvert.DeserializeObject<List<Recommend>>(sr.ReadToEnd());

                        return reData;
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Recommend 에러 :: {0}", e);
                return reData;
            }

        }

        public static UserRegist GetRegist(int userNo)
        {
            string par = "http://" + API_DOMAIN_GAPI + "/platform-data/api/golmap/v1/user-regist/users/" + userNo;
            //string par = "http://gapi.spazon.com/platform-data/api/golmap/v1/user-regist/users/" + userNo;
            var token = GetToken().Access_token;
            UserRegist reData = new UserRegist();

            try
            {
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(par);
                myRequest.Method = "GET";
                myRequest.Headers["Authorization"] = "Bearer " + token;
                myRequest.Accept = "application/json";

                using (HttpWebResponse resp = (HttpWebResponse)myRequest.GetResponse())
                {
                    if (resp.StatusCode != HttpStatusCode.OK)
                    {
                        return reData;
                    }

                    HttpStatusCode status = resp.StatusCode;
                    Stream respStream = resp.GetResponseStream();

                    using (StreamReader sr = new StreamReader(respStream))
                    {
                        reData = JsonConvert.DeserializeObject<UserRegist>(sr.ReadToEnd());

                        return reData;
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Recommend 에러 :: {0}", e);
                return reData;
            }

        }

        public static List<Models.LifeBestMonth> GetLifeBestMonth(int id)
        {
            string par = "http://" + API_DOMAIN_GAPI + "/platform-data/api/golmap/v1/life-best-month/users/" + id;
            var token = GetToken().Access_token;
            List<Models.LifeBestMonth> reData = new List<Models.LifeBestMonth>();

            try
            {
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(par);
                myRequest.Method = "GET";
                myRequest.Headers["Authorization"] = "Bearer " + token;
                myRequest.Accept = "application/json";

                using (HttpWebResponse resp = (HttpWebResponse)myRequest.GetResponse())
                {
                    if (resp.StatusCode != HttpStatusCode.OK)
                    {
                        return reData;
                    }

                    HttpStatusCode status = resp.StatusCode;
                    Stream respStream = resp.GetResponseStream();

                    using (StreamReader sr = new StreamReader(respStream))
                    {
                        reData = JsonConvert.DeserializeObject<List<Models.LifeBestMonth>>(sr.ReadToEnd());

                        return reData;
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Recommend 에러 :: {0}", e);
                return reData;
            }

        }


        public static List<Models.LifeBest> GetLifeBest(int id)
        {
            string par = "http://" + API_DOMAIN_GAPI + "/platform-data/api/golmap/v1/life-best/users/" + id;
            //string par = "http://gapi.spazon.com/platform-data/api/golmap/v1/life-best/users/" + id;
            var token = GetToken().Access_token;
            List<Models.LifeBest> reData = new List<Models.LifeBest>();

            try
            {
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(par);
                myRequest.Method = "GET";
                myRequest.Headers["Authorization"] = "Bearer " + token;
                myRequest.Accept = "application/json";

                using (HttpWebResponse resp = (HttpWebResponse)myRequest.GetResponse())
                {

                    if (resp.StatusCode != HttpStatusCode.OK)
                    {
                        return reData;
                    }

                    HttpStatusCode status = resp.StatusCode;
                    Stream respStream = resp.GetResponseStream();

                    using (StreamReader sr = new StreamReader(respStream))
                    {
                        reData = JsonConvert.DeserializeObject<List<Models.LifeBest>>(sr.ReadToEnd());

                        return reData;
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Recommend 에러 :: {0}", e);
                return reData;
            }

        }


        public static List<CCPlay> GetCCPlay(int id, int cId)
        {
            string par = "http://" + API_DOMAIN_GAPI + "/platform-data/api/golmap/v1/cc-play/users/" + id + "/courses/" + cId;
            //string par = "http://gapi.spazon.com/platform-data/api/golmap/v1/cc-play/users/" + id + "/courses/" + cId;
            var token = GetToken().Access_token;
            List<CCPlay> reData = new List<CCPlay>();

            Console.WriteLine(par);

            //////////로그 테스트
            //try
            //{
            //    var LOG_PATH = @"D:\wwwroot\zxzx\Upload\cc_play.log";
            //    if (!File.Exists(LOG_PATH))
            //    {
            //        File.Create(LOG_PATH);
            //    }
            
            //    using (StreamWriter sw = File.AppendText(LOG_PATH))
            //    {
            //        sw.WriteLine("[{0}] Request URL: {1} Token: {2}", DateTime.Now.ToString(), par, token);
            //    }
            //}
            //catch { }

            try
            {
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(par);
                myRequest.Method = "GET";
                myRequest.Headers["Authorization"] = "Bearer " + token;
                myRequest.Accept = "application/json";


                using (HttpWebResponse resp = (HttpWebResponse)myRequest.GetResponse())
                {
                    if (resp.StatusCode != HttpStatusCode.OK)
                    {
                        return reData;
                    }

                    HttpStatusCode status = resp.StatusCode;
                    Stream respStream = resp.GetResponseStream();

                    using (StreamReader sr = new StreamReader(respStream))
                    {
                        reData = JsonConvert.DeserializeObject<List<CCPlay>>(sr.ReadToEnd());

                        ////////로그테스트
                        //try
                        //{
                        //    using (StreamWriter sw = File.AppendText(LOG_PATH))
                        //    {
                        //        sw.WriteLine("[{0}] Response Data: {1}", DateTime.Now.ToString(), JsonConvert.SerializeObject(reData));
                        //    }
                        //}
                        //catch { }

                        return reData;
                    }
                }

            }
            catch (Exception e)
            {
                ////////로그테스트
                //try
                //{
                //    using (StreamWriter sw = File.AppendText(LOG_PATH))
                //    {
                //        sw.WriteLine("[{0}] Error Response: {1}", DateTime.Now.ToString(), JsonConvert.SerializeObject(reData));
                //        sw.WriteLine("[{0}] Error Message: {1}", DateTime.Now.ToString(), e.Message);
                //    }
                //}
                //catch { }
                Console.WriteLine("Recommend 에러 :: {0}", e);
                return reData;
            }

        }

        public static List<CCPlayBest> GetCCPlayBest(int id, long cId)
        {
            string par = "http://" + API_DOMAIN_GAPI + "/platform-data/api/golmap/v1/cc-play-best/users/" + id + "/courses/" + cId;
            var token = GetToken().Access_token;
            List<CCPlayBest> reData = new List<CCPlayBest>();

            try
            {
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(par);
                myRequest.Method = "GET";
                myRequest.Headers["Authorization"] = "Bearer " + token;
                myRequest.Accept = "application/json";

                using (HttpWebResponse resp = (HttpWebResponse)myRequest.GetResponse())
                {
                    if (resp.StatusCode != HttpStatusCode.OK)
                    {
                        return reData;
                    }

                    HttpStatusCode status = resp.StatusCode;
                    Stream respStream = resp.GetResponseStream();

                    using (StreamReader sr = new StreamReader(respStream))
                    {
                        reData = JsonConvert.DeserializeObject<List<CCPlayBest>>(sr.ReadToEnd());

                        return reData;
                    }
                }

            }
            catch (WebException e)
            {
                Console.WriteLine("Recommend 에러 :: {0}", e);
                return reData = new List<CCPlayBest>();
            }

        }

        public static List<Friend> GetFriends(int id)
        {
            string par = "http://" + API_DOMAIN_GAPI + "/platform-data/api/golmap/v1/friends/users/" + id;
            var token = GetToken().Access_token;
            List<Friend> reData = null;

            try
            {
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(par);
                myRequest.Method = "GET";
                myRequest.Headers["Authorization"] = "Bearer " + token;
                myRequest.Accept = "application/json";

                using (HttpWebResponse resp = (HttpWebResponse)myRequest.GetResponse())
                {
                    HttpStatusCode status = resp.StatusCode;
                    Stream respStream = resp.GetResponseStream();

                    using (StreamReader sr = new StreamReader(respStream))
                    {
                        reData = JsonConvert.DeserializeObject<List<Friend>>(sr.ReadToEnd());

                        return reData;
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Recommend 에러 :: {0}", e);
                return reData;
            }

        }

        //[마일리지] 나의 잔액
        public static MileageBalance GetMileageBalance(int userNo)
        {
            string par = API_MILEAGE + "/MileageV3/MileageBalance";
            MileageBalance reData = null;

            try
            {
                string sParam = "USERCLCODE=2"+"&USERNO="+ userNo;

                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(par);
                myRequest.Method = "POST";

                byte[] bytearry = Encoding.UTF8.GetBytes(sParam);
                myRequest.ContentType = "application/x-www-form-urlencoded";
                myRequest.ContentLength = bytearry.Length;

                Stream stream = myRequest.GetRequestStream();
                stream.Write(bytearry, 0, bytearry.Length);
                stream.Close();

                using (HttpWebResponse resp = (HttpWebResponse)myRequest.GetResponse())
                {
                    HttpStatusCode status = resp.StatusCode;
                    Stream respStream = resp.GetResponseStream();

                    using (StreamReader sr = new StreamReader(respStream))
                    {
                        reData = JsonConvert.DeserializeObject<MileageBalance>(sr.ReadToEnd());
                        reData.USERNO = userNo;
                        return reData;
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Recommend 에러 :: {0}", e);

                reData = new MileageBalance
                {
                    USEPOSSBMILEAGE = 0,
                    ACCMTARGETMILEAGE = 0,
                    MILEAGEAPIRSLTCODE = -1,
                    MILEAGEAPIRESULTMSG = "FAIL",
                    DATALIST = null
                };

                return reData;
            }

        }

        public static List<MileageBalance> GetMileageBalance(int[] userNoList)
        {
            string par = API_MILEAGE + "/MileageV3/MileageBalance";
            List<MileageBalance> reDataList = new List<MileageBalance>();

            foreach (var userNo in userNoList)
            {
                try
                {
                    string sParam = "USERCLCODE=2" + "&USERNO=" + userNo;

                    HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(par);
                    myRequest.Method = "POST";

                    byte[] bytearry = Encoding.UTF8.GetBytes(sParam);
                    myRequest.ContentType = "application/x-www-form-urlencoded";
                    myRequest.ContentLength = bytearry.Length;

                    Stream stream = myRequest.GetRequestStream();
                    stream.Write(bytearry, 0, bytearry.Length);
                    stream.Close();

                    using (HttpWebResponse resp = (HttpWebResponse)myRequest.GetResponse())
                    {
                        HttpStatusCode status = resp.StatusCode;
                        Stream respStream = resp.GetResponseStream();

                        using (StreamReader sr = new StreamReader(respStream))
                        {
                            var reData = JsonConvert.DeserializeObject<MileageBalance>(sr.ReadToEnd());
                            reData.USERNO = userNo;
                            reDataList.Add(reData);
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Recommend 에러 :: {0}", e);
                    reDataList.Add(new MileageBalance
                    {
                        USEPOSSBMILEAGE = 0,
                        ACCMTARGETMILEAGE = 0,
                        MILEAGEAPIRSLTCODE = -1,
                        MILEAGEAPIRESULTMSG = "FAIL",
                        DATALIST = null
                    });
                }
            }

            return reDataList;
        }

        //[마일리지] 적립 요청
        public static MileageAccm SetMileageAccm(int userNo, int mileage, string key, string pr)
        {
            string par = API_MILEAGE + "/MileageV3/MileageAccm";
            MileageAccm reData = new MileageAccm();

            var utcNow = DateTime.UtcNow.AddHours(9);
            var extDate = (utcNow.AddYears(2)).AddDays(-1);

            string AccmreqDate = string.Format("{0:yyyyMMdd}", utcNow);
            string ExttargetDate = string.Format("{0:yyyyMMdd}", extDate);
            string TranTime = string.Format("{0:yyyyMMddHHmmss}", utcNow);
            string BillParam = SHA256Hash(userNo + "|"+ mileage + "|" + key + "|" + TranTime + "|" + "rhfvm30581").ToUpper();

            try
            {
                string sParam =
                    "USERCLCODE=" + 2
                    + "&USERNO=" + userNo
                    + "&ACCMRATE=" + 1
                    + "&MILEAGE=" + mileage
                    + "&BRANDCODE=" + "G_GOLMAP"
                    + "&STORECODE=" + "GOLMAP_ONLINE"
                    + "&MILEAGEATTR=" + "A"
                    + "&TRANSCLCODE=" + "a1"
                    + "&SERVICETRANSID=" + key
                    + "&ACCMREQDATE=" + AccmreqDate
                    + "&ACCMTARGETDATE=" + AccmreqDate
                    + "&EXTTARGETDATE=" + ExttargetDate
                    + "&TRANTIME=" + TranTime
                    + "&BILLPARAM=" + BillParam
                    + "&PRODNM=" + pr;

                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(par);
                myRequest.Method = "POST";

                byte[] bytearry = Encoding.UTF8.GetBytes(sParam);
                myRequest.ContentType = "application/x-www-form-urlencoded";
                myRequest.ContentLength = bytearry.Length;

                Stream stream = myRequest.GetRequestStream();
                stream.Write(bytearry, 0, bytearry.Length);
                stream.Close();

                using (HttpWebResponse resp = (HttpWebResponse)myRequest.GetResponse())
                {
                    HttpStatusCode status = resp.StatusCode;
                    Stream respStream = resp.GetResponseStream();

                    using (StreamReader sr = new StreamReader(respStream))
                    {
                        reData = JsonConvert.DeserializeObject<MileageAccm>(sr.ReadToEnd());
                        //Console.WriteLine(reData.USERNO);
                        //Console.WriteLine(reData.MILEAGENO);
                        reData.USERNO = 1;
                        reData.TRANTIME = TranTime;
                        reData.EXTTARGETDATE = ExttargetDate;
                        return reData;
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Recommend 에러 :: {0}", e);
                return reData;
            }

        }


        public static async Task<YadageData> GetYadage(int hNum, int uNo, long ci, int gNo, int cNo)
        {
            var ccPlay = GetCCPlayBest(uNo, ci);
            YadageData reData = new YadageData();
            int rNum = 0;

            if (ccPlay.Any())
            {
                rNum = ccPlay.OrderBy(c => c.BestScore).FirstOrDefault().pgCode;
            }

            // 2021-09-27 15:54 sk 고객사에서 라이브에서 사용되는 URL로 변경요청
            // 라이브
            string par = string.Format("https://fairway.golfzon.com/bermuda/v1/mobileapi/game/hole/yadage/{0}/{1}/{2}/{3}/{4}", gNo, cNo, hNum, rNum, uNo);
            var token = "golmap-vkK3WkvPw9nec9TXcil6MiANV6eiSOLR";


            // QA
            //string par = string.Format("https://fairway.spazon.com/bermuda/v1/mobileapi/game/hole/yadage/{0}/{1}/{2}/{3}/{4}", gNo, cNo, hNum, rNum, uNo);
            //var token = "golmap-SeKsmVAbWafmM5uY3DrdNLlC5w3WcZ6s";
            //Console.WriteLine("Yadage URL: {0}", par);
            //97317538/1/1794737


            try
            {
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(par);
                myRequest.Method = "GET";
                myRequest.Headers["apikey"] = token;
                //myRequest.Headers["Authorization"] = "Bearer " + token;
                myRequest.Accept = "application/json";


                using (HttpWebResponse resp = (HttpWebResponse)myRequest.GetResponse())
                {
                    if (resp.StatusCode != HttpStatusCode.OK)
                    {
                        return reData;
                    }

                    HttpStatusCode status = resp.StatusCode;
                    Stream respStream = resp.GetResponseStream();

                    using (StreamReader sr = new StreamReader(respStream))
                    {
                        reData = JsonConvert.DeserializeObject<YadageData>(sr.ReadToEnd());

                        return reData;
                    }
                }

            }
            catch (Exception e)
            {
                Console.WriteLine("Recommend 에러 :: {0}", e);
                return reData;
            }

        }



        //스크린 데이터 가져오기
        public static async void UpdateScreenData(GolfzonContext _context, int userId, int userNo)
        {
            try
            {
                var utcNow = DateTime.UtcNow.AddHours(9).Date;
                var utcNow_ = new DateTime(utcNow.Year, utcNow.Month, 1);
                var utcNow__ = new DateTime(utcNow.Year, utcNow.Month, utcNow.Day, 9, 0, 0);


                var retObject = new List<IDictionary<string, object>>();
                List<GolfCiList> golfClList = new List<GolfCiList>();
                List<Models.LifeBestMonth> reData = new List<Models.LifeBestMonth>();

                //유저 스크린 기록 최근 데이터 하나 가져오기
                var lbm = _context.LifeBestMonths
                    .Where(lbm => lbm.UserId == userId)
                    .OrderByDescending(lbm => lbm.VisitDate)
                    .AsNoTracking()
                    .FirstOrDefault();

                if (lbm == null)
                {
                    //다 업데이트
                    var reLbmList = GetLifeBestMonth(userNo);

                    List<int> gcIdList = new List<int>();

                    for (int i=0;i<reLbmList.Count;i++)
                    {
                        gcIdList.Add(reLbmList[i].CiCode);
                    }

                    if (!gcIdList.Any())
                    {
                        return;
                    }

                    var golfList = GetGolfJoinCiCode(_context, gcIdList);

                    foreach (var item in golfList)
                    {
                        if (item.Values.ElementAt(1) != null && item.Values.ElementAt(1) != null)
                        {
                            golfClList.Add(new GolfCiList { GolfClubId = (int)item.Values.ElementAt(0), CiCode = (int)item.Values.ElementAt(1) });
                        }
                    }


                    foreach (var tg in reLbmList)
                    {
                        for (int i = 0; i < golfClList.Count; i++)
                        {
                            if (tg.CiCode == golfClList[i].CiCode)
                            {
                                reData.Add(new Models.LifeBestMonth 
                                {
                                    GolfClubId = golfClList[i].GolfClubId,
                                    CiCode = golfClList[i].CiCode,
                                    UserId = userId,
                                    VisitCnt = tg.VisitCnt,
                                    VisitDate = tg.VisitDate,
                                    BestVisitDate = tg.BestVisitDate,
                                    BestScore = tg.BestScore,
                                    TotalScore = tg.TotalScore
                                });

                                break;
                            }
                        }
                    }

                    _context.LifeBestMonths.AddRange(reData);
                    _context.SaveChanges();
                }
                else
                {
                    //고객사 요청으로 갱신 테스트 위해 주석처리해놓음 09281218
                    //if (lbm.UpdatedAt.Value.CompareTo(utcNow__) < 0)
                    if (1==1)
                    {
                        var reLbmList = GetLifeBestMonth(userNo)
                            //.Where(re =>utcNow_.CompareTo(Convert.ToDateTime(re.VisitDate).Date) <= 0)
                            .ToList();


                        List<int> gcIdList = new List<int>();

                        for (int i = 0; i < reLbmList.Count; i++)
                        {
                            gcIdList.Add(reLbmList[i].CiCode);
                        }

                        var golfList = GetGolfJoinCiCode(_context, gcIdList);

                        foreach (var item in golfList)
                        {
                            golfClList.Add(new GolfCiList { GolfClubId = (int)item.Values.ElementAt(0), CiCode = (int)item.Values.ElementAt(1) });
                        }

                        foreach (var tg in reLbmList)
                        {
                            for (int i = 0; i < golfClList.Count; i++)
                            {
                                if (tg.CiCode == golfClList[i].CiCode)
                                {
                                    reData.Add(new Models.LifeBestMonth
                                    {
                                        GolfClubId = golfClList[i].GolfClubId,
                                        CiCode = golfClList[i].CiCode,
                                        UserId = userId,
                                        VisitCnt = tg.VisitCnt,
                                        VisitDate = tg.VisitDate,
                                        BestVisitDate = tg.BestVisitDate,
                                        BestScore = tg.BestScore,
                                        TotalScore = tg.TotalScore
                                    });

                                    break;
                                }
                            }
                        }

                        var dbLbmList = _context.LifeBestMonths
                        .Where(lbm => lbm.UserId == userId)
                        //.Where(re => utcNow_.CompareTo(re.VisitDate.Date) <= 0)
                        .AsNoTracking()
                        .ToList();

                        for (int i = 0; i < reData.Count; i++)
                        {
                            var tg = dbLbmList.Where(re_ => re_.CiCode == reData[i].CiCode)
                                .Where(re_ => re_.VisitDate == reData[i].VisitDate)
                                .SingleOrDefault();

                            reData[i].Id = tg != null ? tg.Id : 0;
                        }

                        _context.LifeBestMonths.UpdateRange(reData);
                        _context.SaveChanges();
                    }
                }



                return;
            }
            catch (Exception e)
            {
                Console.WriteLine("Recommend 에러 :: {0}", e);
                return;
            }

        }


        static string SHA256Hash(string data)
        {
            SHA256 sha = new SHA256Managed();
            byte[] hash = sha.ComputeHash(Encoding.ASCII.GetBytes(data));
            StringBuilder stringBuilder = new StringBuilder();

            foreach (byte b in hash)
            {
                stringBuilder.AppendFormat("{0:x2}", b);
            }

            return stringBuilder.ToString();
        }


        static List<IDictionary<string, object>> GetGolfJoinCiCode(GolfzonContext _context, List<int> ciList)
        {
            DataTable tvp = new DataTable();
            tvp.Columns.Add(new DataColumn("code", typeof(int)));

            foreach (var item in ciList)
            {
                tvp.Rows.Add(item);
            }

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Screen_CiCode_All";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@List", "dbo.CiCodeList") { Value = tvp});

                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();

                var retObject = new List<IDictionary<string, object>>();
                using (var dataReader = cmd.ExecuteReader())
                {
                    while (dataReader.Read())
                    {
                        var dataRow = new ExpandoObject() as IDictionary<string, object>;
                        for (var iFiled = 0; iFiled < dataReader.FieldCount; iFiled++)
                        {
                            dataRow.Add(
                                dataReader.GetName(iFiled),
                                dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
                            );
                        }

                        retObject.Add(dataRow);
                    }
                }
                return retObject;
            }

        }



        public static List<dynamic> GetVisit(GolfzonContext _context, string cmdText, int userId) 
        {
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = cmdText;
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_USER_ID",
                    SqlDbType.Int)
                { Value = userId });

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
                            // one can modify the next line to
                            //   if (dataReader.IsDBNull(iFiled))
                            //       dataRow.Add(dataReader.GetName(iFiled), dataReader[iFiled]);
                            // if one want don't fill the property for NULL
                            // returned from the database
                            dataRow.Add(
                                dataReader.GetName(iFiled),
                                dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
                            );
                        }

                        retObject.Add((ExpandoObject)dataRow);
                    }
                }

                return retObject;
            }
        }

        public static List<dynamic> GetDefaultSP(GolfzonContext _context, string cmdText)
        {
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = cmdText;
                cmd.CommandType = CommandType.StoredProcedure;

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
                                dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
                            );
                        }

                        retObject.Add((ExpandoObject)dataRow);
                    }
                }

                return retObject;
            }
        }

        public static List<dynamic> GetSingleParamSP(GolfzonContext _context, string cmdText, string paramName, int param)
        {
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = cmdText;
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter(paramName,
                    SqlDbType.Int)
                { Value = param });

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

                return retObject;
            }
        }

        public static List<dynamic> GetSingleParamSP(GolfzonContext _context, string cmdText, string paramName, string param)
        {
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = cmdText;
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter(paramName,
                    SqlDbType.NVarChar)
                { Value = param });

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

                return retObject;
            }
        }


        public static async void SetAdminEditHistory(GolfzonContext _context, List<AdminEditList> editList)
        {
            
            DataTable tvp = new DataTable();
            
            tvp.Columns.Add(new DataColumn("AdminId", typeof(string)));
            tvp.Columns.Add(new DataColumn("RoleType", typeof(bool)));
            tvp.Columns.Add(new DataColumn("RoleName", typeof(string)));

            foreach (var item in editList)
            {
                var t = tvp.NewRow();
                t["AdminId"] = item.AdminId;
                t["RoleType"] = item.RoleType;
                t["RoleName"] = item.RoleName;
                tvp.Rows.Add(t);
            }

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Admin_Edit_history_Insert";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add(new SqlParameter("@List", "dbo.EditList") { Value = tvp });

                if (cmd.Connection.State != ConnectionState.Open)
                    cmd.Connection.Open();


                cmd.ExecuteReader();

                //var retObject = new List<IDictionary<string, object>>();
                //using (var dataReader = cmd.ExecuteReader())
                //{
                //    while (dataReader.Read())
                //    {
                //        var dataRow = new ExpandoObject() as IDictionary<string, object>;
                //        for (var iFiled = 0; iFiled < dataReader.FieldCount; iFiled++)
                //        {
                //            dataRow.Add(
                //                dataReader.GetName(iFiled),
                //                dataReader.IsDBNull(iFiled) ? null : dataReader[iFiled] // use null instead of {}
                //            );
                //        }

                //        retObject.Add(dataRow);
                //    }
                //}
                //return retObject;
            }

        }

        public static async void GetDefaultSPAsync(GolfzonContext _context, string cmdText)
        {
            try
            {
                using (var cmd = _context.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = cmdText;
                    cmd.CommandType = CommandType.StoredProcedure;

                    if (cmd.Connection.State != ConnectionState.Open)
                        cmd.Connection.Open();

                    cmd.ExecuteNonQuery();
                }
            }
            catch { }
        }

        public class AdminEditList
        {
            public string AdminId { get; set; }
            public bool RoleType { get; set; }
            public string RoleName { get; set; }
        }

        public class GzAccessToken
        {
            public string Access_token { get; set; }
            public string Token_type { get; set; }
            public int Expires_in { get; set; }
            public string Scope { get; set; }
            public string Jti { get; set; }
        }

        public class Recommend
        {
            public int CiCode { get; set; }
            public int PlayCnt { get; set; }
            public int BestScore { get; set; }
        }

        //public class LifeBestMonth
        //{
        //    public int CiCode { get; set; }             //CC번호
        //    public string VisitDate { get; set; }       //방문년월
        //    public int VisitCnt { get; set; }           //방문횟수
        //    public int TotalScore { get; set; }     //전체타수
        //    public int BestScore { get; set; }      //최저타수
        //    public string BestVisitDate { get; set; }      //최저타수일자
        //}

        public class UserRegist
        {
            public int UsrNo { get; set; }             //CC번호
            public DateTime Regdt { get; set; }       //방문년월
            public DateTime UpdateDate { get; set; }           //타수
        }


        public class LifeBest
        {
            public int CiCode { get; set; }             //CC번호
            public int VisitCnt { get; set; }           //방문횟수
            public int TotalScore { get; set; }     //전체타수
            public int BestScore { get; set; }      //최저타수
            public string BestVisitDate { get; set; }      //최저타수일자
        }

        public class CCPlay
        {
            public int CiCode { get; set; }             //CC번호
            public string VisitDate { get; set; }       //방문년월
            public int Score { get; set; }           //타수
        }

        public class CCPlayBest
        {
            public int CiCode { get; set; }             //CC번호
            public int BestScore { get; set; }      //최저타수
            public string VisitDate { get; set; }       //방문년월
            public int pgCode { get; set; }           //게임번호
        }

        public class Friend
        {
            public int UiCode { get; set; }             //소스유저 번호
            public int TargetCode { get; set; }      //골친유저 번호
            public string UsrNickName { get; set; }       //골친유저 닉네임
            public long Cnt { get; set; }           //함계한 게임 횟수
        }

        public class MileageBalance
        {
            public int USERNO { get; set; } = 0;
            public int USEPOSSBMILEAGE { get; set; }        //사용 가능 마일리지
            public int ACCMTARGETMILEAGE { get; set; }      //적립 예정 마일리지
            public int EXTTARGETMILEAGE { get; set; }       //소멸 예정 마일리지
            public int MILEAGEAPIRSLTCODE { get; set; }     //마일리지API결과코드
            public string MILEAGEAPIRESULTMSG { get; set; } //마일리지API결과메시지
            public List<DATALIST> DATALIST { get; set;}     //마일리지 속성 리스트 
        }

        public class DATALIST
        {
            public string MILEAGEATTR { get; set; }         //마일리지 속성
            public int UNITPOSSBMILEAGE {get; set;}         //속성별 사용가능 마일리지
            public int UNITACCMTGMILEAGE { get; set; }      //속성별 적립예정 마일리지
            public int UNITEXTTGMILEAGE { get; set; }       //속성별 소멸예정 마일리지
        }


        public class MileageAccm
        {
            public int USERNO { get; set; } = 0;
            public string TRANTIME { get; set; } = "0";            //적립일
            public string EXTTARGETDATE { get; set; } = "0";       //소멸날
            public Int64 MILEAGENO { get; set; }        //마일리지 적립 발행번호 MILEAGEAPIRSLTCODE 성공(0)일 때 만 발행
            public int MILEAGEAPIRSLTCODE { get; set; }        //성공(0), 그 외에는 실패 코드
            public string MILEAGEAPIRESULTMSG { get; set; }        //성공(OK) 그 외에는 실패 메시지
            public string BILLPARAM { get; set; }        //SHA256 
        }

        public class GolfCiList
        {
            public int GolfClubId { get; set; }
            public int CiCode { get; set; }
        }

        public class YadageData
        {
            public YadageEntity ENTITY { get; set; }
            public string CODE { get; set; }
            public string codeMessage { get; set; }
            public string status { get; set; }
            public string statusMessage { get; set; }
        }

        public class YadageEntity
        {
            public Int64 holeCode { get; set; }
            public int roundNo { get; set; }
            public int holeNo { get; set; }
            public int basicParCount { get; set; }
            public int hitCount { get; set; }
            public int score { get; set; }
            public int putCount { get; set; }
            public string skipYn { get; set; }
            public string yadageImageUrl { get; set; }
            public int teePosition { get; set; }
            public string teePositionX { get; set; }
            public string teePositionY { get; set; }
            public string holePositionX { get; set; }
            public string holePositionY { get; set; }
            public string mapPositionTop { get; set; }
            public string mapPositionLeft { get; set; }
            public string mapPositionRight { get; set; }
            public string mapPositionBottom { get; set; }
            public int distanceBackTee { get; set; }
            public int distanceChampTee { get; set; }
            public int distanceFrontTee { get; set; }
            public int distanceLadyTee { get; set; }
            public int distanceSeniorTee { get; set; }
            public float heightBackTee { get; set; }
            public float heightChampTee { get; set; }
            public float heightFrontTee { get; set; }
            public float heightLadyTee { get; set; }
            public float heightSeniorTee { get; set; }
            public List<YadageShot> shot { get; set; }
        }

        public class YadageShot
        {
            public int shotNo { get; set; }
            public float distance { get; set; }
            public float remain { get; set; }
            public int ground { get; set; }
            public string positionX { get; set; }
            public string positionY { get; set; }
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

        static DateTime TimeStampToDateTime(long value)
        {
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddSeconds(value).ToLocalTime();
            return dt;
        }
    }
}
