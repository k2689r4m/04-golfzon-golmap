using GolfZonWebApp.Data;
using GolfZonWebApp.Models;
using GolfZonWebApp.Types;
using System.IO;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization;
using System.Text.Json;
using Quartz;
using Quartz.Spi;
using GolfZonWebApp.Scheduler;
using Quartz.Impl;
using NetTopologySuite.Geometries;
using System.Collections.Generic;
using Microsoft.AspNetCore.Antiforgery;
using System;

using Microsoft.IdentityModel.Logging;

using GolfZonWebApp.Middlewares;
using GolfZonWebApp.Scheduler.Repositories;
using GolfZonWebApp.Scheduler.Managers;
using GolfZonWebApp.Repositories;

namespace GolfZonWebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddAntiforgery((options) =>
            //{
            //    options.FormFieldName = "x-xsrf-token";
            //    options.HeaderName = "x-xsrf-token";
            //    //same orgin
            //    options.SuppressXFrameOptionsHeader = false;
            //});

            //services.AddControllersWithViews(options =>
            //    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

            //services.AddHttpContextAccessor();
            services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

            services.AddControllers().AddJsonOptions(x => {
                //x.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve;
                x.JsonSerializerOptions.Converters.Add(new NetTopologySuite.IO.Converters.GeoJsonConverterFactory());
            });//.AddNewtonsoftJson(options => options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

            //services.AddMvc()
            //    .AddJsonOptions(options => {

            //    });
            services.AddHttpContextAccessor();


            services.AddDbContext<GolfzonContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("RemoteDevSQLServerConnection"),
                    //Configuration.GetConnectionString("ConnectionString"),
                    x => x.UseNetTopologySuite()));

            services.AddIdentity<AdminUser, AdminRole>()
                .AddEntityFrameworkStores<GolfzonContext>();

            //services.AddSingleton<ISessionRepository, MockSessionRepository>();
            services.AddSingleton<IManagerRepository, MockManagerRepository>();
            //services.AddSingleton<IPointRepository, MockPointRepository>();

            IdentityModelEventSource.ShowPII = true;

            services.Configure<IdentityOptions>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 3;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
            });

            services.AddDatabaseDeveloperPageExceptionFilter();

            //services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
            //    .AddEntityFrameworkStores<ApplicationDbContext>();

            //services.AddIdentityServer()
            //    .AddApiAuthorization<ApplicationUser, ApplicationDbContext>();

            //services.AddAuthentication()
            //    .AddIdentityServerJwt();

            services.AddControllersWithViews();
            services.AddRazorPages();

            //행정도시 위경도 로드
            var settings = new Newtonsoft.Json.JsonSerializerSettings { ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore };
            foreach (var conv in NetTopologySuite.IO.GeoJsonSerializer.Create(settings, new GeometryFactory(new PrecisionModel(), 4326)).Converters)
            {
                settings.Converters.Add(conv);
            }
            var _geo = Newtonsoft.Json.JsonConvert.DeserializeObject<Geo>(
                System.IO.File.ReadAllText(Directory.GetCurrentDirectory() + "\\features.json")
                , settings);

            var _geoMore = Newtonsoft.Json.JsonConvert.DeserializeObject<GeoMore>(
                System.IO.File.ReadAllText(Directory.GetCurrentDirectory() + "\\featuresMore.json")
                , settings);

            services.Configure<Types.Geo>(Configuration =>
            {
                Configuration.features = _geo.features;
            });

            services.Configure<Types.GeoMore>(Configuration =>
            { 
                Configuration.features = _geoMore.features; 
            });

            services.Configure<AppSettings>(Configuration.GetSection("AppSettings"));


            //services.AddControllers().AddJsonOptions(x => x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.Preserve);

            //services.AddControllers().AddNewtonsoftJson();

            // CORS (For Debug) It has to be disabled in production.
            services.AddCors(options =>
            {
                options.AddPolicy(name: "Development", builder => builder.
                    //WithOrigins("https://localhost:3000")
                    WithOrigins("*")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    //.AllowCredentials()
                    .WithExposedHeaders("access-token")
                    .WithExposedHeaders("stamp")
                );
            });

            //services.AddSession((options) =>
            //{
            //    options.IdleTimeout = System.TimeSpan.FromMinutes(10);
            //    options.Cookie.HttpOnly = false;
            //    options.Cookie.IsEssential = true;
            //    //options.Cookie.SameSite = SameSiteMode.None;
            //    //options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            //    //options.Cookie.Name = "R-TOKEN";
            //});

            // Add Quartz services
            services.AddSingleton<IJobFactory, SingletonJobFactory>();
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();

            // Add our job
            services.AddSingleton<ConSchedule>();
            services.AddSingleton<ConSchedule2>();
            services.AddSingleton<MileageSchedule>();

            services.AddSpaStaticFiles(config => config.RootPath = "D:/wwwroot/web");

            //편의시설
            //services.AddSingleton(new JobSchedule(
            //    jobType: typeof(ConSchedule),
            //    cronExpression: "0 1 22 * * ?")); // run every 5 seconds


            //날씨 라이브
            services.AddSingleton(new JobSchedule(
                jobType: typeof(ConSchedule2),
                cronExpression: "0 0 0-23/3 * * ?"));




            //날씨 Qa
            //services.AddSingleton(new JobSchedule(
            //    jobType: typeof(ConSchedule2),
            //    cronExpression: "0 7 13 * * ?"));





            //services.AddSingleton(new JobSchedule(
            //    jobType: typeof(ConSchedule2),
            //    cronExpression: "0/5 * * * * ?"));

            //마일리지
            //services.AddSingleton(new JobSchedule(
            //    jobType: typeof(MileageSchedule),
            //    cronExpression: "0 0/24 * * * ?"));

            services.AddHostedService<QuartzHostedService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, GolfzonContext gcon, IAntiforgery antiforgery)
        {
            if (env.IsDevelopment())
            {
                //app.UseDeveloperExceptionPage();
                app.UseExceptionHandler("/Error");
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseStatusCodePages();
            app.UseStatusCodePages(async context =>
            {
                context.HttpContext.Response.ContentType = "text/plain";

                await context.HttpContext.Response.WriteAsync(
                    "Status code page, status code: " +
                    context.HttpContext.Response.StatusCode);
            });

            //웹소켓
            app.UseWebSockets();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            //app.UseStaticFiles(new StaticFileOptions
            //{
            //    FileProvider = new PhysicalFileProvider(
            //        Path.Combine(env.ContentRootPath, "Upload/Images")),
            //    RequestPath = "/images",
            //    ServeUnknownFileTypes = true,
            //    DefaultContentType = "image/png"
            //});

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(
                    Path.Combine(env.ContentRootPath, "Upload/Temp")),
                RequestPath = "/temp",
                ServeUnknownFileTypes = true
                //DefaultContentType = "image/png"
            });
            //app.UseSpaStaticFiles();


            app.UseRouting();

            app.UseCors("Development");

            app.UseAuthentication();
            //app.UseIdentityServer();
            app.UseAuthorization();

            //routing 이후 endpoint 이전
            //app.UseSession();


            app.UseUserSessionMiddleware();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });

            const string spaUserPath = "/user";
            const string spaAdminPath = "/admin";

            //if (!env.IsDevelopment())
            //{
                app.UseSpaStaticFiles(new StaticFileOptions()
                {
                    FileProvider = new PhysicalFileProvider(Directory.GetCurrentDirectory())
                });

                app.Map("/admin", con =>
                {
                    con.UseSpaStaticFiles(new StaticFileOptions()
                    {
                        FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "AdminApp/build"))
                    });

                    con.UseSpa(spa =>
                    {
                        spa.Options.DefaultPageStaticFileOptions = new StaticFileOptions()
                        {
                            FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "AdminApp/build"))
                        };

                        spa.Options.SourcePath = "AdminApp/build";
                    });
                });

                app.Map("/user", con =>
                {
                    con.UseSpaStaticFiles(new StaticFileOptions()
                    {
                        FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "ClientApp/build"))
                    });

                    con.UseSpa(spa =>
                    {
                        spa.Options.DefaultPageStaticFileOptions = new StaticFileOptions()
                        {
                            FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "ClientApp/build"))
                        };

                        spa.Options.SourcePath = "ClientApp/build";
                    });
                });

        


            //디비 초기화
            //gcon.Database.EnsureDeleted();
            //디비 인잇
            gcon.Database.EnsureCreated();
        }
    }
}
