using GolfZonWebApp.Data;
using GolfZonWebApp.Models;
using GolfZonWebApp.Scheduler.Managers;
using GolfZonWebApp.Scheduler.Repositories;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using GolfZonWebApp.Lib;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System.Dynamic;
using System.Data;
using Microsoft.Data.SqlClient;

namespace GolfZonWebApp.Controllers
{
    [ApiController]
    public class MaintenanceController : Controller
    {
        private readonly GolfzonContext context;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly HttpContext httpContext;

        public MaintenanceController(GolfzonContext context, IHttpContextAccessor httpContextAccessor)
        {
            this.context = context;
            this.httpContextAccessor = httpContextAccessor;
            this.httpContext = httpContextAccessor.HttpContext;
        }

        [HttpGet("api/user/[controller]")]
        public IActionResult MaintenancePage()
        {
            return Json(GzApi.GetDefaultSP(context, "MaintenancePage"));
        }

        [HttpGet("api/[controller]/get/all")]
        public IActionResult MaintenanceGetAll(int page = 1)
        {
            if (page < 1) page = 1;

            int mCount = context.Maintenances.Count();

            var mList = context.Maintenances.Skip((page - 1) * 10).Take(10).ToList();

            return Json(new
            {
                lastPage = ((int)((mCount - 1) / 10)) + 1,
                items = mList
            });
        }

        [HttpGet("api/[controller]/get")]
        public IActionResult MaintenanceGet(int id)
        {
            var m = context.Maintenances.Include(m => m.MaintenanceUsers).Where(m => m.Id == id).FirstOrDefault();

            return Json(m);
        }


        [HttpPost("api/[controller]")]
        public async Task<ActionResult> MaintenanceSet([FromBody] Maintenance m)
        {
            if (m.StartDate >= m.EndDate) return BadRequest();

            if (m.Message == null) return BadRequest();

            if (m.Status)
            {
                var sCount = context.Maintenances.Where(mm => !(mm.StartDate > m.EndDate || mm.EndDate < m.StartDate)).Where(mm => mm.Id != m.Id).Where(mm => mm.Status).Count();
                if (sCount > 0) return BadRequest();
            }

            var mu = m.MaintenanceUsers.ToList();
            m.MaintenanceUsers = new List<MaintenanceUser>();

            var _m = context.Maintenances.Find(m.Id);
            if (_m == null)
            {
                context.Maintenances.Add(m);

                await context.SaveChangesAsync();
            }
            else
            {
                _m.StartDate = m.StartDate;
                _m.EndDate = m.EndDate;
                _m.Status = m.Status;
                _m.Message = m.Message;
            }

            for (int i = 0; i < mu.Count; i++)
            {
                mu.ElementAt(i).Id = 0;
                mu.ElementAt(i).MaintenanceId = m.Id;
            }

            var muList = context.MaintenanceUsers.Where(mu => mu.MaintenanceId == m.Id).ToList();
            if (muList.Count > 0)
            {
                context.MaintenanceUsers.RemoveRange(muList);
            }

            context.MaintenanceUsers.AddRange(mu);

            await context.SaveChangesAsync();

            return Ok();
        }
    }
}
