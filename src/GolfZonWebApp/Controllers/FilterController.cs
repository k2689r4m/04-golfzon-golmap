using GolfZonWebApp.Data;
using GolfZonWebApp.Models;
using GolfZonWebApp.Lib;
using GolfZonWebApp.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GolfZonWebApp.Controllers
{
    [ApiController]
    [Route("api/filters")]
    public class FilterController : Controller
    {
        private readonly GolfzonContext _context;
        public FilterController(GolfzonContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<List<FilterObject>>> GetAll()
        {
            return Json(await _context.FilterObjects.ToArrayAsync()); 
        }

        [HttpPost]
        public async Task<IActionResult> Update([FromBody] List<FilterObject> param)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var dFilterObjects = await _context.FilterObjects.AsNoTracking().ToListAsync();


            for (int i = 0; i < dFilterObjects.Count; i++)
            {
                //삭제
                if (param.Where(f => f.Id == dFilterObjects[i].Id).FirstOrDefault() == null)
                {
                    _context.Remove(_context.FilterObjects.Find(dFilterObjects[i].Id));
                }
            }

            _context.FilterObjects.UpdateRange(param);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
