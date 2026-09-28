using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GolfZonWebApp.Models
{
    public static class ModelBuilderExtensions
    {
        public static void Seed(this ModelBuilder modelBuilder)
        {
            //    modelBuilder.Entity<AdminUserRole>()
            //.HasOne(aur => aur.AdminUser)
            //.WithMany(au => au.AdminUserRoles);

            modelBuilder.Entity<AdminUser>()
                .HasMany(au => au.AdminUserRoles)
                .WithOne(aur => aur.AdminUser)
                .HasForeignKey(aur => aur.UserId);

            modelBuilder.Entity<AdminRole>()
                .HasMany(ar => ar.adminUserRoles)
                .WithOne(aur => aur.AdminRole)
                .HasForeignKey(aur => aur.RoleId);

            var superPermission = new AdminRole
            {
                Name = "Super",
                NormalizedName = "SUPER",
                Type = "Super",
                EngType = "Super",
                Order = 0,
            };

            modelBuilder.Entity<AdminRole>().HasData(
                superPermission,
                new AdminRole
                {
                    Name = "UserRead",
                    NormalizedName = "USERREAD",
                    Type = "회원관리",
                    EngType = "User",
                    Order = 1,
                },
                new AdminRole
                {
                    Name = "UserWrite",
                    NormalizedName = "USERWRITE",
                    Type = "회원관리",
                    EngType = "User",
                    Order = 2,
                },
                new AdminRole
                {
                    Name = "GolfClubRead",
                    NormalizedName = "GOLFCLUBREAD",
                    Type = "골프장관리",
                    EngType = "GolfClub",
                    Order = 3,
                },
                new AdminRole
                {
                    Name = "GolfClubWrite",
                    NormalizedName = "GOLFCLUBWRITE",
                    Type = "골프장관리",
                    EngType = "GolfClub",
                    Order = 4,
                },
                new AdminRole
                {
                    Name = "ConvenienceRead",
                    NormalizedName = "CONVENIENCEREAD",
                    Type = "편의시설관리",
                    EngType = "Convenience",
                    Order = 5,
                },
                new AdminRole
                {
                    Name = "ConvenienceWrite",
                    NormalizedName = "CONVENIENCEWRITE",
                    Type = "편의시설관리",
                    EngType = "Convenience",
                    Order = 6,
                },
                new AdminRole
                {
                    Name = "WrongInfoRead",
                    NormalizedName = "WRONGINFOREAD",
                    Type = "잘못된정보관리",
                    EngType = "WrongInfo",
                    Order = 7,
                },
                new AdminRole
                {
                    Name = "WrongInfoWrite",
                    NormalizedName = "WRONGINFOWRITE",
                    Type = "잘못된정보관리",
                    EngType = "WrongInfo",
                    Order = 8,
                },
                new AdminRole
                {
                    Name = "EventRead",
                    NormalizedName = "EVENTREAD",
                    Type = "이벤트공지관리",
                    EngType = "Event",
                    Order = 9,
                },
                new AdminRole
                {
                    Name = "EventWrite",
                    NormalizedName = "EVENTWRITE",
                    Type = "이벤트공지관리",
                    EngType = "Event",
                    Order = 10,
                },
                new AdminRole
                {
                    Name = "TemplateRead",
                    NormalizedName = "TEMPLATEREAD",
                    Type = "메인목록관리",
                    EngType = "Template",
                    Order = 11,
                },
                new AdminRole
                {
                    Name = "TemplateWrite",
                    NormalizedName = "TEMPLATEWRITE",
                    Type = "메인목록관리",
                    EngType = "Template",
                    Order = 12,
                },
                new AdminRole
                {
                    Name = "ReviewRead",
                    NormalizedName = "REVIEWREAD",
                    Type = "리뷰관리",
                    EngType = "Review",
                    Order = 13,
                },
                new AdminRole
                {
                    Name = "ReviewWrite",
                    NormalizedName = "REVIEWWRITE",
                    Type = "리뷰관리",
                    EngType = "Review",
                    Order = 14,
                },
                new AdminRole
                {
                    Name = "PointRead",
                    NormalizedName = "POINTREAD",
                    Type = "포인트관리",
                    EngType = "Point",
                    Order = 15,
                },
                new AdminRole
                {
                    Name = "PointWrite",
                    NormalizedName = "POINTWRITE",
                    Type = "포인트관리",
                    EngType = "Point",
                    Order = 16,
                },
                //new AdminRole
                //{
                //    Name = "PushRead",
                //    NormalizedName = "PUSHREAD",
                //    Type = "푸쉬관리",
                //    EngType = "Push",
                //    Order = 17,
                //},
                //new AdminRole
                //{
                //    Name = "PushWrite",
                //    NormalizedName = "PUSHWRITE",
                //    Type = "푸쉬관리",
                //    EngType = "Push",
                //    Order = 18,
                //},
                new AdminRole
                {
                    Name = "StatisticsRead",
                    NormalizedName = "STATISTICSREAD",
                    Type = "통계",
                    EngType = "Statistics",
                    Order = 19,
                },
                new AdminRole
                {
                    Name = "StatisticsWrite",
                    NormalizedName = "STATISTICSWRITE",
                    Type = "통계",
                    EngType = "Statistics",
                    Order = 20,
                },
                new AdminRole
                {
                    Name = "RoleRead",
                    NormalizedName = "ROLEREAD",
                    Type = "관리자권한설정",
                    EngType = "Role",
                    Order = 21,
                },
                new AdminRole
                {
                    Name = "RoleWrite",
                    NormalizedName = "ROLEWRITE",
                    Type = "관리자권한설정",
                    EngType = "Role",
                    Order = 22,
                },
                new AdminRole
                {
                    Name = "TagRead",
                    NormalizedName = "TAGREAD",
                    Type = "추천검색어관리",
                    EngType = "Tag",
                    Order = 23,
                },
                new AdminRole
                {
                    Name = "TagWrite",
                    NormalizedName = "TAGWRITE",
                    Type = "추천검색어관리",
                    EngType = "Tag",
                    Order = 24,
                },
                new AdminRole
                {
                    Name = "RouletteWinnerRead",
                    NormalizedName = "ROULETTEWINNERREAD",
                    Type = "룰렛당첨자관리",
                    EngType = "RouletteWinner",
                    Order = 25,
                },
                new AdminRole
                {
                    Name = "RouletteWinnerWrite",
                    NormalizedName = "ROULETTEWINNERWRITE",
                    Type = "룰렛당첨자관리",
                    EngType = "RouletteWinner",
                    Order = 26,
                },
                new AdminRole
                {
                    Name = "RouletteSettingRead",
                    NormalizedName = "ROULETTESETTINGREAD",
                    Type = "룰렛설정",
                    EngType = "RouletteSetting",
                    Order = 27,
                },
                new AdminRole
                {
                    Name = "RouletteSettingWrite",
                    NormalizedName = "ROULETTESETTINGWRITE",
                    Type = "룰렛설정",
                    EngType = "RouletteSetting",
                    Order = 28,
                }
                );

            var superAdmin = new AdminUser
            {
                Id = "Super",
                Name = "Super Admin",
                UserName = "superadmin",
                NormalizedUserName = "SUPERADMIN",
                Department = "Super Admin",
                PasswordHash = "AQAAAAEAACcQAAAAEKKGcAetGzFTKCYZnbuvgsEL9e9sKNFIUporndVv9n0okVS7XmWTlI4Q4ynwMHHaYQ==",
                Status = 0,
            };

            modelBuilder.Entity<AdminUser>().HasData(
                superAdmin
            );

            modelBuilder.Entity<AdminUserRole>().HasData(
                new AdminUserRole
                {
                    UserId = superAdmin.Id,
                    RoleId = superPermission.Id,
                });
        }
    }
}
