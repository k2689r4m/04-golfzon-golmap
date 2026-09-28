using GolfZonWebApp.Data;
using GolfZonWebApp.Models;
using GolfZonWebApp.Types;
using NetTopologySuite.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GolfZonWebApp.Lib
{
    public class MileageManager
    {
        //private readonly GolfzonContext _context;

        //public MileageManager(GolfzonContext context)
        //{
        //    _context = context;
        //}

        //public static string S_ID = "G_M_2_";
        public static string S_ID = "G_M_QA_2";

        public static int NOTI_TYPE = 100;
        public static int EVENT_TYPE_GOLF = 100;
        public static int EVENT_TYPE_GOLF_4 = 104;
        public static int EVENT_TYPE_GOLF_3 = 103;
        public static int EVENT_TYPE_GOLF_2 = 102;
        public static int EVENT_TYPE_GOLF_1 = 101;
        public static int EVENT_TYPE_CON = 200;
        public static int EVENT_TYPE_CON_2 = 202;
        public static int EVENT_TYPE_CON_1 = 201;
        public static int EVENT_TYPE_RELOGIN = 300;
        public static int EVENT_TYPE_SHAKING = 400;
        public static int EVENT_TYPE_FIRST = 500;
        public static int EVENT_TYPE_REPORT = 600;
        public static int EVENT_TYPE_RESERVATION = 700;
        public static int EVENT_TYPE_ROULETTE = 1000;

        public static int EVENT_GROUP_REVIEW = 100;
        public static int EVENT_GROUP_LOGIN = 200;
        public static int EVENT_GROUP_ETC = 300;
        public static int EVENT_GROUP_ROULETTE = 1000;


        public static int GetEventType(int eventType, bool text, bool itemText, int imgCount)
        {
            if (eventType == EVENT_TYPE_GOLF)
            {
                Console.WriteLine("EVENT_TYPE_GOLF");

                if (text && itemText && 3 <= imgCount)
                {
                    return EVENT_TYPE_GOLF_4;
                }
                else if (text && itemText && 1 <= imgCount)
                {
                    return EVENT_TYPE_GOLF_3;
                }
                else if (text && 1 <= imgCount)
                {
                    return EVENT_TYPE_GOLF_2;
                }
                else if (text)
                {
                    return EVENT_TYPE_GOLF_1;
                }
            }
            else if (eventType == EVENT_TYPE_CON)
            {
                Console.WriteLine("EVENT_TYPE_CON");
                if (text && 1 <= imgCount)
                {
                    return EVENT_TYPE_CON_2;
                }
                else if (text)
                {
                    return EVENT_TYPE_CON_1;
                }
            }

            return 0;
        }

        public async static Task<bool> SetEvent(GolfzonContext _context, int userNo, int userId, int eventType, int group)
        {
            var trans = _context.Database.BeginTransaction();
            var date = DateTime.UtcNow.AddHours(9);
            var nowDate = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
            trans.CreateSavepoint("BeforePoint");

            try
            {

                var pointObjectList = _context.PointObjects
                    .Where(po => po.Type == eventType)
                    .ToList();

                PointObject pointObject = null;

                Console.WriteLine(eventType);
                foreach (var item in pointObjectList)
                {
                    Console.WriteLine("StartDate: {0}", item.StartDate);
                    Console.WriteLine("EndDate: {0}", item.EndDate);
                    if ((item.StartDate.CompareTo(nowDate) <= 0 && item.EndDate.CompareTo(nowDate) >= 0))
                    {
                        pointObject = item;
                        break;
                    }
                }

                if (pointObject == null)
                {
                    Console.WriteLine("1111111");
                    trans.Dispose();
                    return false;
                }


               


                List<PointHistory> ckPointHistory = new List<PointHistory>();
                if (group == EVENT_GROUP_REVIEW)
                {
                    //ckPointHistory = _context.PointHistorys
                    //.Where(po => po.UserId == userId)
                    //.Where(po => po.Group == group)
                    //.ToList();
                }
                else if(group == EVENT_GROUP_LOGIN)
                {
                    ckPointHistory = _context.PointHistorys
                    .Where(po => po.UserId == userId)
                    .Where(po => po.Group == group)
                    .Where(po => po.Type == eventType)
                    .ToList();

                    if (ckPointHistory.Any())
                    {
                        Console.WriteLine("이거 이미 있음");
                        trans.Dispose();
                        return false;
                    }
                }
                else
                {
                    trans.Dispose();
                    return false;
                }

                PointHistory pointHistory = new PointHistory
                {
                    UserId = userId,
                    Point = pointObject.Point,
                    Group = group,
                    Type = pointObject.Type,
                    Accumulate = pointObject.Accumulate
                };

                _context.PointHistorys.Add(pointHistory);
                _context.SaveChanges();

                if ( pointHistory.Id == 0)
                {
                    Console.WriteLine("22222222222");
                    trans.RollbackToSavepoint("BeforePoint");
                    trans.Dispose();

                    return false;
                }

                var pr = GetProdnmTypeComment(eventType, group);
                GzApi.MileageAccm reMileage = GzApi.SetMileageAccm(userNo, pointObject.Point, S_ID + pointHistory.Id, pr);

                

                Console.WriteLine("---------------------------");
                Console.WriteLine(eventType);
                Console.WriteLine(group);
                Console.WriteLine(reMileage.MILEAGENO);
                Console.WriteLine(reMileage.MILEAGEAPIRSLTCODE);
                Console.WriteLine(reMileage.MILEAGEAPIRESULTMSG);
                Console.WriteLine("---------------------------");

                if (reMileage.MILEAGEAPIRSLTCODE != 0)
                {
                    trans.RollbackToSavepoint("BeforePoint");
                    trans.Dispose();

                    return false;
                }
                else
                {
                    trans.Commit();

                    string contents = GetEventTypeComment(eventType, group, pointObject.Point);
                    UpdateNotification.Update(_context, NOTI_TYPE, userId, contents);

                    return true;
                }
            }
            catch (Exception e)
            {
                trans.RollbackToSavepoint("BeforePoint");
                trans.Dispose();

                Console.WriteLine(e);
                return false;
            }
        }


        public async static Task<bool> SetOneEvent(GolfzonContext _context, int userNo, int userId, int eventType, int group)
        {
            var trans = _context.Database.BeginTransaction();
            var date = DateTime.UtcNow.AddHours(9);
            var nowDate = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);
            trans.CreateSavepoint("BeforePoint");
            try
            {
                var pointObjectList = _context.PointObjects
                    .Where(po => po.Type == eventType)
                    .ToList();

                PointObject pointObject = null;

                Console.WriteLine(eventType);
                foreach (var item in pointObjectList)
                {
                    Console.WriteLine("StartDate: {0}", item.StartDate);
                    Console.WriteLine("EndDate: {0}", item.EndDate);
                    if ((item.StartDate.CompareTo(nowDate) <= 0 && item.EndDate.CompareTo(nowDate) >= 0))
                    {
                        pointObject = item;
                        break;
                    }
                }

                if (pointObject == null)
                {
                    Console.WriteLine("1111111");
                    trans.RollbackToSavepoint("BeforePoint");
                    trans.Dispose();
                    return false;
                }


                //trans.CreateSavepoint("BeforePoint");

                List<PointHistory> ckPointHistory = new List<PointHistory>();
                if (group == EVENT_GROUP_REVIEW)
                {
                    ckPointHistory = _context.PointHistorys
                    .Where(po => po.UserId == userId)
                    .Where(po => po.Group == group)
                    //.Where(po => po.Type == eventType)
                    .ToList();

                    if (ckPointHistory.Any())
                    {
                        Console.WriteLine("이미 리뷰가 있음");
                        trans.Dispose();
                        return false;
                    }
                }
                else
                {
                    trans.Dispose();
                    return false;
                }

                Console.WriteLine("첫리뷰 통과");

                PointHistory pointHistory = new PointHistory
                {
                    UserId = userId,
                    Point = pointObject.Point,
                    Group = group,
                    Type = pointObject.Type,
                    Accumulate = pointObject.Accumulate
                };

                _context.PointHistorys.Add(pointHistory);
                _context.SaveChanges();

                if (pointHistory.Id == 0)
                {
                    Console.WriteLine("22222222222");
                    trans.RollbackToSavepoint("BeforePoint");
                    trans.Dispose();
                    return false;
                }

                var pr = GetProdnmTypeComment(eventType, group);
                GzApi.MileageAccm reMileage = GzApi.SetMileageAccm(userNo, pointObject.Point, S_ID + pointHistory.Id, pr);



                Console.WriteLine("---------------------------");
                Console.WriteLine(eventType);
                Console.WriteLine(group);
                Console.WriteLine(reMileage.MILEAGENO);
                Console.WriteLine(reMileage.MILEAGEAPIRSLTCODE);
                Console.WriteLine(reMileage.MILEAGEAPIRESULTMSG);
                Console.WriteLine("---------------------------");

                if (reMileage.MILEAGEAPIRSLTCODE != 0)
                {
                    trans.RollbackToSavepoint("BeforePoint");
                    trans.Dispose();
                    return false;
                }
                else
                {
                    trans.Commit();

                    string contents = GetEventTypeComment(eventType, group, pointObject.Point);
                    UpdateNotification.Update(_context, NOTI_TYPE, userId, contents);

                    return true;
                }
            }
            catch (Exception e)
            {
                trans.RollbackToSavepoint("BeforePoint");
                trans.Dispose();
                Console.WriteLine(e);
                return false;
            }
        }


        public static bool SetReservationEvent(GolfzonContext _context, int userNo, int userId, int eventType, int group)
        {
            var trans = _context.Database.BeginTransaction();
            var date = DateTime.UtcNow.AddHours(9);
            var nowDate = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);

            try
            {

                var pointObjectList = _context.PointObjects
                    .Where(po => po.Type == eventType)
                    .ToList();

                PointObject pointObject = null;

                Console.WriteLine(eventType);
                foreach (var item in pointObjectList)
                {
                    Console.WriteLine("StartDate: {0}", item.StartDate);
                    Console.WriteLine("EndDate: {0}", item.EndDate);
                    if ((item.StartDate.CompareTo(nowDate) <= 0 && item.EndDate.CompareTo(nowDate) >= 0))
                    {
                        pointObject = item;
                        break;
                    }
                }

                if (pointObject == null)
                {
                    Console.WriteLine("1111111");
                    return false;
                }

                var dbOp = _context.OpenPomotion
                    .Where(op => op.AttendUser == userNo)
                    .Where(op => op.AttendStatus == 1)
                    .SingleOrDefault();

                if (dbOp == null)
                {
                    Console.WriteLine("사전예약자가 아님");
                    return false;
                }

                trans.CreateSavepoint("BeforePoint");


                PointHistory pointHistory = new PointHistory
                {
                    UserId = userId,
                    Point = pointObject.Point,
                    Group = group,
                    Type = pointObject.Type,
                    Accumulate = pointObject.Accumulate
                };

                _context.PointHistorys.Add(pointHistory);
                _context.SaveChanges();

                dbOp.AttendStatus = 2;
                dbOp.ProvideDate = pointHistory.CreatedAt;
                _context.Update(dbOp);
                _context.SaveChanges();

                if (pointHistory.Id == 0)
                {
                    Console.WriteLine("22222222222");
                    trans.RollbackToSavepoint("BeforePoint");
                    trans.Dispose();
                    return false;
                }

                var pr = GetProdnmTypeComment(eventType, group);
                GzApi.MileageAccm reMileage = GzApi.SetMileageAccm(userNo, pointObject.Point, S_ID + pointHistory.Id, pr);


                Console.WriteLine("---------------------------");
                Console.WriteLine(eventType);
                Console.WriteLine(group);
                Console.WriteLine(reMileage.MILEAGENO);
                Console.WriteLine(reMileage.MILEAGEAPIRSLTCODE);
                Console.WriteLine(reMileage.MILEAGEAPIRESULTMSG);
                Console.WriteLine("---------------------------");

                if (reMileage.MILEAGEAPIRSLTCODE != 0)
                {
                    trans.RollbackToSavepoint("BeforePoint");
                    trans.Dispose();
                    return false;
                }
                else
                {
                    trans.Commit();

                    string contents = GetEventTypeComment(eventType, group, pointObject.Point);
                    UpdateNotification.Update(_context, NOTI_TYPE, userId, contents);

                    return true;
                }
            }
            catch (Exception e)
            {
                trans.RollbackToSavepoint("BeforePoint");
                trans.Dispose();
                Console.WriteLine(e);
                return false;
            }
        }

        public static bool SetReport(GolfzonContext _context, int userNo, int userId)
        {
            int eventType = EVENT_TYPE_REPORT;
            int group = EVENT_GROUP_ETC;

            var trans = _context.Database.BeginTransaction();
            var date = DateTime.UtcNow.AddHours(9);
            var nowDate = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);

            try
            {

                var pointObjectList = _context.PointObjects
                    .Where(po => po.Type == eventType)
                    .ToList();

                PointObject pointObject = null;

                Console.WriteLine(eventType);
                foreach (var item in pointObjectList)
                {
                    Console.WriteLine("StartDate: {0}", item.StartDate);
                    Console.WriteLine("EndDate: {0}", item.EndDate);
                    if ((item.StartDate.CompareTo(nowDate) <= 0 && item.EndDate.CompareTo(nowDate) >= 0))
                    {
                        pointObject = item;
                        break;
                    }
                }

                if (pointObject == null)
                {
                    Console.WriteLine("1111111");
                    return false;
                }


                trans.CreateSavepoint("BeforePoint");

                PointHistory pointHistory = new PointHistory
                {
                    UserId = userId,
                    Point = pointObject.Point,
                    Group = group,
                    Type = pointObject.Type,
                    Accumulate = pointObject.Accumulate
                };

                _context.PointHistorys.Add(pointHistory);
                _context.SaveChanges();

                if (pointHistory.Id == 0)
                {
                    Console.WriteLine("22222222222");
                    trans.RollbackToSavepoint("BeforePoint");
                    trans.Dispose();
                    return false;
                }

                var pr = GetProdnmTypeComment(eventType, group);
                GzApi.MileageAccm reMileage = GzApi.SetMileageAccm(userNo, pointObject.Point, S_ID + pointHistory.Id, pr);


                Console.WriteLine("---------------------------");
                Console.WriteLine(eventType);
                Console.WriteLine(group);
                Console.WriteLine(reMileage.MILEAGENO);
                Console.WriteLine(reMileage.MILEAGEAPIRSLTCODE);
                Console.WriteLine(reMileage.MILEAGEAPIRESULTMSG);
                Console.WriteLine("---------------------------");

                if (reMileage.MILEAGEAPIRSLTCODE != 0)
                {
                    trans.RollbackToSavepoint("BeforePoint");
                    trans.Dispose();
                    return false;
                }
                else
                {
                    trans.Commit();

                    string contents = GetEventTypeComment(eventType, group, pointObject.Point);
                    UpdateNotification.Update(_context, NOTI_TYPE, userId, contents);

                    return true;
                }
            }
            catch (Exception e)
            {
                trans.RollbackToSavepoint("BeforePoint");
                trans.Dispose();
                Console.WriteLine(e);
                return false;
            }
        }

        public static bool SetRouletteEvent(GolfzonContext _context, int userNo, int userId, int po)
        {
            var trans = _context.Database.BeginTransaction();
            var date = DateTime.UtcNow.AddHours(9);
            var nowDate = new DateTime(date.Year, date.Month, date.Day, 0, 0, 0);

            try
            {
                trans.CreateSavepoint("BeforePoint");
                

                PointHistory pointHistory = new PointHistory
                {
                    UserId = userId,
                    Point = po,
                    Group = EVENT_GROUP_ROULETTE,
                    Type = EVENT_TYPE_ROULETTE,
                    Accumulate = "룰렛 이벤트"
                };

                _context.PointHistorys.Add(pointHistory);
                _context.SaveChanges();

                if (pointHistory.Id == 0)
                {
                    trans.RollbackToSavepoint("BeforePoint");
                    trans.Dispose();
                    return false;
                }

                var pr = GetProdnmTypeComment(EVENT_TYPE_ROULETTE, EVENT_GROUP_ROULETTE);
                GzApi.MileageAccm reMileage = GzApi.SetMileageAccm(userNo, po, S_ID + pointHistory.Id, pr);



                Console.WriteLine("---------------------------");
                Console.WriteLine(EVENT_TYPE_ROULETTE);
                Console.WriteLine(EVENT_GROUP_ROULETTE);
                Console.WriteLine(reMileage.MILEAGENO);
                Console.WriteLine(reMileage.MILEAGEAPIRSLTCODE);
                Console.WriteLine(reMileage.MILEAGEAPIRESULTMSG);
                Console.WriteLine("---------------------------");

                if (reMileage.MILEAGEAPIRSLTCODE != 0)
                {
                    trans.RollbackToSavepoint("BeforePoint");
                    trans.Dispose();
                    return false;
                }
                else
                {
                    trans.Commit();

                    string contents = GetEventTypeComment(EVENT_TYPE_ROULETTE, EVENT_GROUP_ROULETTE, po);
                    UpdateNotification.Update(_context, NOTI_TYPE, userId, contents);

                    return true;
                }
            }
            catch (Exception e)
            {
                trans.RollbackToSavepoint("BeforePoint");
                trans.Dispose();
                Console.WriteLine(e);
                return false;
            }
        }


        public static string GetEventTypeComment(int eventType, int group, int point)
        {
            if (eventType == EVENT_TYPE_FIRST)
            {
                return string.Format("첫리뷰를 작성하여 {0} 마일리지가 적립되었습니다.", point);
            }
            else if (eventType == EVENT_TYPE_CON_1)
            {
                return string.Format("편의시설에 간단리뷰를 등록하여 {0} 마일리지가 적립되었습니다.", point);
            }
            else if (eventType == EVENT_TYPE_CON_2)
            {
                return string.Format("편의시설에 간단리뷰/사진을 등록하여 {0} 마일리지가 적립되었습니다.", point);
            }
            else if (eventType == EVENT_TYPE_GOLF_1)
            {
                return string.Format("골프장에 간단리뷰를 등록하여 {0} 마일리지가 적립되었습니다.", point);
            }
            else if (eventType == EVENT_TYPE_GOLF_2)
            {
                return string.Format("골프장에 간단리뷰/사진을 등록하여 {0} 마일리지가 적립되었습니다.", point);
            }
            else if (eventType == EVENT_TYPE_GOLF_3)
            {
                return string.Format("골프장에 항목별 상세리뷰/사진을 등록하여 {0} 마일리지가 적립되었습니다.", point);
            }
            else if (eventType == EVENT_TYPE_GOLF_4)
            {
                return string.Format("골프장에 항목별 상세리뷰/사진 3장 이상을 등록하여 {0} 마일리지가 적립되었습니다.", point);
            }
            else if (group == EVENT_GROUP_LOGIN && eventType == EVENT_TYPE_RELOGIN)
            {
                return string.Format("골맵에 재방문 하여 {0} 마일리지가 적립되었습니다.", point);
            }
            else if (group == EVENT_GROUP_LOGIN && eventType == EVENT_TYPE_SHAKING)
            {
                return string.Format("쉐이킹 기능으로 골프장을 방문하여 {0} 마일리지가 적립되었습니다.", point);
            }
            else if (group == EVENT_GROUP_ETC && eventType == EVENT_TYPE_REPORT)
            {
                return string.Format("잘못된 정보 수정요청하여 {0} 마일리지가 적립되었습니다.", point);
            }
            else if (group == EVENT_GROUP_ETC && eventType == EVENT_TYPE_RESERVATION)
            {
                return string.Format("사전 예약 {0} 마일리지가 적립되었습니다.", point);
            }
            else if (group == EVENT_GROUP_ROULETTE)
            {
                return string.Format("룰렛 이벤트 {0} 마일리지가 적립되었습니다.", point);
            }


            return "none";
        }

        public static string GetProdnmTypeComment(int eventType, int group)
        {
            
            if (eventType == EVENT_TYPE_RELOGIN)
            {
                return "재방문 보상";
            }
            else if (eventType == EVENT_TYPE_SHAKING)
            {
                return "방문 쉐이킹 보상";
            }
            else if (eventType == EVENT_TYPE_FIRST)
            {
                return "첫 리뷰 보상";
            }
            else if (eventType == EVENT_TYPE_REPORT)
            {
                return "정보 수정 요청 보상";
            }
            else if (eventType == EVENT_TYPE_RESERVATION)
            {
                return "사전 알림 이벤트";
            }
            else if (eventType == EVENT_TYPE_ROULETTE)
            {
                return "룰렛 이벤트";
            }
            else if (group == EVENT_GROUP_REVIEW)
            {
                if (eventType >= EVENT_TYPE_CON)
                {
                    return "업체 리뷰 등록 보상";
                }
                else
                {
                    return "골프장 리뷰 등록 보상";
                }
            }

            return "none";
        }
    }
}
