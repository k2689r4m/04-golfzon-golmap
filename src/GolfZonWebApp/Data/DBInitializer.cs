using GolfZonWebApp.Models;
using System;
using System.Linq;
using System.Collections.Generic;


namespace GolfZonWebApp.Data
{
    public static class DBInitializer
    {
        // 골프장 임시 리스트 생성 메소드로 현재 모델 컬럼의 잦은 변화로 주석 처리
        // private static void CreateGolfClubSampleData(GolfzonContext context)
        // {
        //     if (context.GolfClubs.Any())
        //     {
        //         return;
        //     }

        //     var golfClubs = new GolfClub[]
        //     {
        //         new GolfClub{Name="강원골프클럽", GcNum="1000", Contact="02-1234-1234", Status=1, Hours="24시간",  Address="경기도 광주시 오포읍 문형리", Lat=37.3609124, Lng=127.1462686},
        //         new GolfClub{Name="경기골프클럽", GcNum="1001", Contact="02-1234-1234", Status=1, Hours="24시간",  Address="경기도 광주시 오포읍 문형리", Lat=37.3509124, Lng=127.1562686},
        //         new GolfClub{Name="샘플골프클럽", GcNum="1002", Contact="02-1234-1234", Status=1, Hours="24시간", Address="경기도 광주시 오포읍 문형리", Lat=37.3509124, Lng=127.1762686},
        //         new GolfClub{Name="잘되는 골프클럽", GcNum="1003", Contact="02-1234-1234", Status=1, Hours="24시간", Address="경기도 광주시 오포읍 문형리", Lat=37.3609124, Lng=127.1662686},
        //         new GolfClub{Name="멋잇는 골프클럽", GcNum="1004", Contact="02-1234-1234", Status=1, Hours="24시간", Address="경기도 광주시 오포읍 문형리", Lat=37.3409124, Lng=127.1662686},
        //         new GolfClub{Name="필드 좋은 골프클럽", GcNum="1005", Contact="02-1234-1234", Status=1, Hours="24시간", Address="경기도 광주시 오포읍 문형리", Lat=37.3809124, Lng=127.1662686},
        //         new GolfClub{Name="학동 골드 클럽", GcNum="1006", Contact="02-1234-1234", Status=1, Hours="24시간", Address="서울특별시 강남구 서초동", Lat=37.5146635, Lng=127.0399246},
        //         new GolfClub{Name="반포 골드 클럽", GcNum="1007", Contact="02-1234-1234", Status=1, Hours="24시간", Address="서울특별시 강남구 서초동", Lat=37.5070527, Lng=127.0192043},
        //         new GolfClub{Name="용산 골드 클럽", GcNum="1008", Contact="02-1234-1234", Status=1, Hours="24시간", Address="서울특별시 강남구 서초동", Lat=37.5253597, Lng=126.9962429},
        //         new GolfClub{Name="동작 골드 클럽", GcNum="1009", Contact="02-1234-1234", Status=1, Hours="24시간", Address="서울특별시 강남구 서초동", Lat=37.500822, Lng=126.9854529},
        //         new GolfClub{Name="숭실 골드 클럽", GcNum="1010", Contact="02-1234-1234", Status=1, Hours="24시간", Address="서울특별시 강남구 서초동", Lat=37.4976709, Lng=126.9587941},
        //         new GolfClub{Name="마포 골드 클럽", GcNum="1011", Contact="02-1234-1234", Status=1, Hours="24시간", Address="서울특별시 강남구 서초동", Lat=37.5367079, Lng=126.9564911},
        //         new GolfClub{Name="송현 골드 클럽", GcNum="1012", Contact="02-1234-1234", Status=1, Hours="24시간", Address="인천광역시 강남구 서초동", Lat=37.4814798, Lng=126.6570733},
        //         new GolfClub{Name="동인천 골드 클럽", GcNum="1013", Contact="02-1234-1234", Status=1, Hours="24시간", Address="인천광역시 강남구 서초동", Lat=37.485651, Lng=126.6962322},
        //         new GolfClub{Name="숭덕 골드 클럽", GcNum="1014", Contact="02-1234-1234", Status=1, Hours="24시간", Address="인천광역시 강남구 서초동", Lat=37.4621294, Lng=126.7361449},
        //         new GolfClub{Name="논곡 골드 클럽", GcNum="1015", Contact="02-1234-1234", Status=1, Hours="24시간", Address="인천광역시 강남구 서초동", Lat=37.410682, Lng=126.712085},
        //         new GolfClub{Name="해송 골드 클럽", GcNum="1017", Contact="02-1234-1234", Status=1, Hours="24시간", Address="인천광역시 강남구 서초동", Lat=37.3816412, Lng=126.6500174},
        //         new GolfClub{Name="송도 골드 클럽", GcNum="1016", Contact="02-1234-1234", Status=1, Hours="24시간", Address="인천광역시 강남구 서초동", Lat=37.4101283, Lng=126.6570331},
        //     };

        // context.GolfClubs.AddRange(golfClubs);
        // context.SaveChanges();
        // }

        private static void CreateAdminAccounts(GolfzonContext context)
        {
            if (context.Admins.Any())
            {
                return;
            }

            var admins = new Admin[]
            {
                new Admin{Username="Admin", Password="1234", Name="관리자", Level=1}
            };
            
            context.Admins.AddRange(admins);
            context.SaveChanges();
        }

        // 디비 테스트용으로 필요할 때 주석을 해제 후 사용
        private static void DatabaseTestCode(GolfzonContext context)
        {
            //var users = new List<User>
            //{
            //    new User
            //    {
            //        UserNum = "1",
            //        Username = "test1",
            //        Fullname = "fullname1",
            //        Nickname = "nickname1",
            //        Phone = "010-0000-0001",
            //        Email = "test1@oo.com",
            //        Point = 10
            //    },
            //    new User
            //    {
            //        UserNum = "2",
            //        Username = "test2",
            //        Fullname = "fullname2",
            //        Nickname = "nickname2",
            //        Phone = "010-0000-0002",
            //        Email = "test2@oo.com",
            //        Point = 100
            //    },
            //    new User
            //    {
            //        UserNum = "3",
            //        Username = "test3",
            //        Fullname = "fullname3",
            //        Nickname = "nickname3",
            //        Phone = "010-0000-0003",
            //        Email = "test3@oo.com",
            //        Point = 40
            //    },
            //    new User
            //    {
            //        UserNum = "4",
            //        Username = "test4",
            //        Fullname = "fullname4",
            //        Nickname = "nickname4",
            //        Phone = "010-0000-0004",
            //        Email = "test4@oo.com",
            //        Point = 104
            //    },
            //    new User
            //    {
            //        UserNum = "5",
            //        Username = "test5",
            //        Fullname = "fullname5",
            //        Nickname = "nickname5",
            //        Phone = "010-0000-0005",
            //        Email = "test5@oo.com",
            //        Point = 62
            //    },
            //};

            //context.Users.AddRange(users);
            //context.SaveChanges();

            //var userLogin = new List<UserLoginHistory>
            //{
            //    new UserLoginHistory
            //    {
            //        UserId = 1
            //    },
            //    new UserLoginHistory
            //    {
            //        UserId = 1
            //    },
            //    new UserLoginHistory
            //    {
            //        UserId = 2
            //    },
            //    new UserLoginHistory
            //    {
            //        UserId = 2
            //    },
            //    new UserLoginHistory
            //    {
            //        UserId = 2
            //    },
            //    new UserLoginHistory
            //    {
            //        UserId = 3
            //    },
            //    new UserLoginHistory
            //    {
            //        UserId = 4
            //    }
            //};

            //context.UserLoginHistorys.AddRange(userLogin);
            //context.SaveChanges();

            //var pointHistorys = new List<PointHistory>
            //{
            //    new PointHistory
            //    {
            //        UserId = 1,
            //        Memo = "호호호",
            //        Point = 5.5
            //    },
            //    new PointHistory
            //    {
            //        UserId = 1,
            //        Memo = "gjgjgj",
            //        Point = 55.5
            //    },
            //    new PointHistory
            //    {
            //        UserId = 2,
            //        Memo = "zxzxz",
            //        Point = 75
            //    },
            //    new PointHistory
            //    {
            //        UserId = 4,
            //        Memo = "이것은 무엇",
            //        Point = 750
            //    },
            //    new PointHistory
            //    {
            //        UserId = 4,
            //        Memo = "ㄴㅇㄹㄴㄹ",
            //        Point = 10
            //    },
            //};

            //context.PointHistorys.AddRange(pointHistorys);
            //context.SaveChanges();

            //var sampleTemplates = new List<Template>
            //{
            //    new Template
            //    {
            //        TemplateType = 1,
            //        Type = "이벤트공지형",
            //        MenuName = "테스트A",
            //    },
            //    new Template
            //    {
            //        TemplateType = 2,
            //        Type = "골프장소개형",
            //        MenuName = "테스트B",
            //    },
            //    new Template
            //    {
            //        TemplateType = 3,
            //        Type = "업체소개형",
            //        MenuName = "테스트C",
            //        IsActive = true,
            //    }
            //};

            //context.Templates.AddRange(sampleTemplates);
            //context.SaveChanges();

            //GolfClub sampleGolfClub = new GolfClub
            //{
            //    GcNum = "100000",
            //    Name = "테스트 골프클럽",
            //    Status = 1,
            //    Grade = 1,
            //    SelfRound = 1,
            //    HoleLength = 1
            //};
            //context.GolfClubs.Add(sampleGolfClub);
            //context.SaveChanges();

            //Convenience sampleConvenience = new Convenience
            //{
            //    Name = "테스트 편의시설"
            //};
            //context.Conveniences.Add(sampleConvenience);
            //context.SaveChanges();

            //var sampleEventTemplate = new EventTemplate
            //{
            //    TemplateId = sampleTemplates[0].Id,
            //    Title = "제목",
            //    Status = 1,
            //    IsPush = false,
            //    IsMainExposure = false,
            //    IsTemp = false,
            //};
            //context.EventTemplates.Add(sampleEventTemplate);
            //context.SaveChanges();

            //EventTemplateGolfClub sampleEventTemplateGolfClub = new EventTemplateGolfClub
            //{
            //    EventTemplateId = sampleEventTemplate.Id,
            //    GolfClubId = sampleGolfClub.Id,
            //};

            //context.EventTemplatesGolfClubs.Add(sampleEventTemplateGolfClub);
            //context.SaveChanges();

            //GolfClubTemplate sampleGolfClubTemplate = new GolfClubTemplate
            //{
            //    TemplateId = sampleTemplates[1].Id,
            //    GolfClubId = sampleGolfClub.Id,
            //};

            //ConvenienceTemplate sampleConvenienceTemplate = new ConvenienceTemplate
            //{
            //    TemplateId = sampleTemplates[2].Id,
            //    ConvenienceId = sampleConvenience.Id,
            //};

            //context.GolfClubTemplates.Add(sampleGolfClubTemplate);
            //context.ConvenienceTemplates.Add(sampleConvenienceTemplate);

            //context.SaveChanges();






            //var golfClub = new GolfClub
            //{
            //    Name = "golfclub",
            //    GcNum = "1000",
            //    ClubHouse = new ClubHouse { MFileName = "asdasd" },
            //    GolfClubImages = new List<GolfClubImage>
            //    {
            //        new GolfClubImage
            //        {
            //            OriginalName = "1",
            //            Name = "1",
            //            Uri = "1"
            //        },
            //        new GolfClubImage
            //        {
            //            OriginalName = "2",
            //            Name = "2",
            //            Uri = "2"
            //        },
            //    }
            //};

            //context.GolfClubs.Add(golfClub);
            //context.SaveChanges();

            //var golfClubImage = new GolfClubImage
            //{
            //    Name="a.jpg",
            //    OriginalName="b.jpg",
            //    Uri= "b.jpg",
            //    GolfClubId =golfClub.Id
            //};

            //context.GolfClubImages.Add(golfClubImage);
            //context.SaveChanges();

            //var clubHouse = new ClubHouse
            //{
            //    GolfClubId=golfClub.Id,
            //    MFileName="test.jpg"
            //};

            //context.ClubHouses.Add(clubHouse);
            //context.SaveChanges();

            //Console.WriteLine("GolfClubId: {0}", golfClub.Id);
            //Console.WriteLine("GolfClubImageId From GolfClub: {0}", golfClub.GolfClubImages.First().Id);
            //Console.WriteLine("GolfClubImageUri From GolfClub: {0}", golfClub.GolfClubImages.First().Uri);
            //Console.WriteLine("GolfClubImageId: {0}", golfClubImage.Id);
            //Console.WriteLine("ClubHouseId From GolfClub: {0}", golfClub.ClubHouse.Id);
            //Console.WriteLine("MFileName From GolfClub: {0}", golfClub.ClubHouse.MFileName);
            //Console.WriteLine("GolfClubName From GolfClubImage: {0}", golfClubImage.GolfClub.Name);
            //Console.WriteLine("GolfClubName From ClubHouse: {0}", clubHouse.GolfClub.Name);
        }

        public static void Initialize(GolfzonContext context)
        {
            // 임시 코드, DB 매번 삭제
            // context.Database.EnsureDeleted();

            context.Database.EnsureCreated();

            DatabaseTestCode(context);
            // CreateGolfClubSampleData(context);
            CreateAdminAccounts(context);
        }
    }
}
