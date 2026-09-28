using GolfZonWebApp.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.SqlServer;

namespace GolfZonWebApp.Data
{
    public class HelloWorldJobContext: DbContext
    {
        public HelloWorldJobContext()
        {
            this.ChangeTracker.LazyLoadingEnabled = false;

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            //192.168.20.63
            optionsBuilder.UseSqlServer(@"uid=Trams;pwd=@TABK&bHND1DJKB#;database=Golmap;server=192.168.20.63,8279;", x => x.UseNetTopologySuite());
            //optionsBuilder.UseSqlServer(@"uid=Trams;pwd=@TABK&bHND1DJKB#;database=Golmap;server=172.20.43.112,8279;", x => x.UseNetTopologySuite());
            //services.AddControllers().AddJsonOptions(x => {
            //    x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
            //    x.JsonSerializerOptions.Converters.Add(new NetTopologySuite.IO.Converters.GeoJsonConverterFactory());
            //});
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
            var utcNow = DateTime.UtcNow;

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
        public DbSet<ReviewGood> ReviewGoods { get; set; }
        public DbSet<WrongInfo> WrongInfos { get; set; }
        public DbSet<WrongInfoImage> WrongInfoImages { get; set; }
        public DbSet<GeoList> GeoLists { get; set; }
        public DbSet<Weather> Weathers { get; set; }
        public DbSet<WeatherArea> WeatherAreas { get; set; }
        public DbSet<ConUpdateList> ConUpdateLists { get; set; }
        public DbSet<WeatherCity> WeatherCities { get; set; }

        public DbSet<RefGolfList> RefGolfLists { get; set; }
    }
}
