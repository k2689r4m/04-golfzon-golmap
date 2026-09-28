using GolfZonWebApp.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace GolfZonWebApp.Data
{
    public class GolfzonContext: IdentityDbContext<AdminUser>
    {
        public GolfzonContext(DbContextOptions<GolfzonContext> options): base(options)
        {
            this.ChangeTracker.LazyLoadingEnabled = false;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<GeoList>()
            //    .Property(x => x.Position).
            //    HasColumnType("geometry");


            // Fluent API 
            // Manual Relationship Configuration
            //modelBuilder.Entity<Course>()
            //   .Ignore(c => c.GolfClub);

            //modelBuilder.Entity<AdminUserRole>().HasKey(p => new { p.UserId, p.RoleId });
            //modelBuilder.Entity<IdentityUserClaim<Guid>>().HasKey(p => new { p.Id });
            //modelBuilder.Entity<IdentityUserRole<Guid>>().HasKey(p => new { p.UserId, p.RoleId });

            base.OnModelCreating(modelBuilder);
            modelBuilder.Seed();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            OnBeforeSaving();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override async Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default(CancellationToken)
        )
        {
            OnBeforeSaving();
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void OnBeforeSaving()
        {
            var entries = ChangeTracker.Entries();
            var utcNow = DateTime.UtcNow.AddHours(9);

            foreach (var entry in entries)
            {
                // for entities that inherit from BaseEntity,
                // set UpdatedOn / CreatedOn appropriately
                if (entry.Entity is BaseEntity trackable)
                {
                    switch (entry.State)
                    {
                        case EntityState.Modified:
                            // set the updated date to "now"
                            trackable.UpdatedAt = utcNow;

                            // mark property as "don't touch"
                            // we don't want to update on a Modify operation
                            entry.Property("CreatedAt").IsModified = false;
                            break;

                        case EntityState.Added:
                            // set both updated and created date to "now"
                            trackable.CreatedAt = utcNow;
                            trackable.UpdatedAt = utcNow;
                            
                            break;
                    }
                }
            }
        }

        public DbSet<Admin> Admins { get; set; }
        public DbSet<GolfClub> GolfClubs { get; set; }
        public DbSet<GolfClubImage> GolfClubImages { get; set; }
        public DbSet<Course> Courses { get; set; }
        public DbSet<Hole> Holes { get; set; }
        public DbSet<ClubHouse> ClubHouses { get; set; }
        public DbSet<ClubHouseMenu> ClubHouseMenus { get; set; }
        public DbSet<ShadeHouse> ShadeHouses { get; set; }
        public DbSet<ShadeHouseMenu> ShadeHouseMenus { get; set; }
        public DbSet<Convenience> Conveniences { get; set; }
        public DbSet<ConvenienceImage> ConvenienceImages { get; set; }
        public DbSet<ConvenienceMenu> ConvenienceMenus { get; set; }
        public DbSet<FilterList> FilterLists { get; set; }
        public DbSet<FilterObject> FilterObjects { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<PointHistory> PointHistorys { get; set; }
        public DbSet<PointObject> PointObjects { get; set; }
        public DbSet<UserLoginHistory> UserLoginHistorys { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<EventTemplate> EventTemplates { get; set; }
        public DbSet<EventTemplateGolfClub> EventTemplatesGolfClubs { get; set; }
        public DbSet<GolfClubTemplate> GolfClubTemplates { get; set; }
        public DbSet<ConvenienceTemplate> ConvenienceTemplates { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<ReviewImage> ReviewImages { get; set; }
        public DbSet<ReviewGood> ReviewGoods { get; set; }
        public DbSet<ReviewGrade> ReviewGrades { get; set; }
        public DbSet<ReviewReport> ReviewReports { get; set; }
        public DbSet<ReviewBlackUser> ReviewBlackUsers { get; set; }
        public DbSet<WrongInfo> WrongInfos { get; set; }
        public DbSet<WrongInfoImage> WrongInfoImages { get; set; }
        public DbSet<GeoList> GeoLists { get; set; }
        public DbSet<Weather> Weathers { get; set; }
        public DbSet<WeatherArea> WeatherAreas { get; set; }
        public DbSet<GolfClubSearchHistory> GolfClubSearchHistories { get; set; }
        public DbSet<GolfFavorites> GolfFavorites { get; set; }
        public DbSet<ConvenienceFavorites> ConvenienceFavorites { get; set; }
        public DbSet<Notification> Notifications { get;  set; }
        public DbSet<Tag> Tags { get;  set; }

        public DbSet<ReviewTag> ReviewTags { get;  set; }
        public DbSet<RefCourseList> RefCourseLists { get;  set; }
        public DbSet<RefGolfList> RefGolfLists { get;  set; }
        public DbSet<Recommend> Recommends { get;  set; }
        public DbSet<LifeBestMonth> LifeBestMonths { get;  set; }
        public DbSet<LifeBest> LifeBests { get;  set; }
        public DbSet<CCPlay> CCPlays { get;  set; }
        public DbSet<CCPlayBest> CCPlayBests { get;  set; }
        public DbSet<Friend> Friends { get;  set; }
        public DbSet<ConUpdateList> ConUpdateLists { get;  set; }
        public DbSet<WeatherCity> WeatherCities { get;  set; }
        public DbSet<OpenPomotion> OpenPomotion { get; set; }

        public DbSet<Roulette> Roulettes { get; set; }
        public DbSet<RouletteItem> RouletteItems { get; set; }
        public DbSet<RouletteResult> RouletteResults { get; set; }
        public DbSet<RouletteActionOption> RouletteActionOptions { get; set; }

        //admin
        public DbSet<AdminUser> AdminUsers { get; set; }
        public DbSet<AdminRole> AdminRoles { get; set; }
        public DbSet<AdminUserRole> AdminUserRoles { get; set; }
        public DbSet<AdminLoginHistory> AdminLoginHistorys { get; set; }
        public DbSet<AdminLoginFailHistory> AdminLoginFailHistorys { get; set; }

        public DbSet<Maintenance> Maintenances { get; set; }
        public DbSet<MaintenanceUser> MaintenanceUsers { get; set; }

        //public DbSet<AdminUserClaim> AdminUserClaims { get; set; }
        //public DbSet<AdminRoleClaim> AdminRoleClaims { get; set; }
        //public DbSet<AdminUserToken> AdminUserTokens { get; set; }

        //userSetting
        public DbSet<Setting> Settings { get; set; } 
        public DbSet<SettingFilter> SettingFilters { get; set; } 



        //
        //public DbSet<Microsoft.AspNetCore.Identity.IdentityUserClaim<Guid>> IdentityUserClaims { get; set; }
    }
}
