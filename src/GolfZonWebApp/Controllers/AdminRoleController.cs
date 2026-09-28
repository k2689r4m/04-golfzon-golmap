using GolfZonWebApp.Data;
using GolfZonWebApp.Models;
using GolfZonWebApp.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using GolfZonWebApp.Lib;
using GolfZonWebApp.Types;

namespace GolfZonWebApp.Controllers
{
    [ApiController]
    public class AdminRoleController : Controller
    {
        private readonly RoleManager<AdminRole> roleManager;
        private readonly SignInManager<AdminUser> signInManager;
        private readonly UserManager<AdminUser> userManager;
        private readonly GolfzonContext context;

        public AdminRoleController(RoleManager<AdminRole> roleManager, SignInManager<AdminUser> signInManager,
                                                    UserManager<AdminUser> userManager, GolfzonContext context)
        {
            this.roleManager = roleManager;
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.context = context;
        }

        [HttpGet("api/[controller]/all")]
        public async Task<ActionResult<List<AdminUserViewModel>>> GetAllAdminUsers(string option, string content, int page)
        {
            string error = "no result";

            if (page <= 0)
            {
                page = 1;
            }

            IQueryable<AdminUserViewModel> data = null;

            if (content == null && option == null)
            {
                data =
                    from adminUsers in context
                            .AdminUsers
                            .OrderByDescending(au => au.CreatedAt)
                            .Select(au => new AdminUserViewModel
                            {
                                Id = au.Id,
                                UserName = au.UserName,
                                Name = au.Name,
                                Department = au.Department,
                                Status = au.Status,
                                LoggedAt = context.AdminLoginHistorys.Count(a => a.AdminId == au.Id) > 0 ? context.AdminLoginHistorys.Where(a => a.AdminId == au.Id).Max(a => a.CreatedAt) : null,
                                AdminUserRoles = au.AdminUserRoles,
                            })
                            .AsNoTracking()
                    select adminUsers;

            }
            else if (option != null && content != null)
            {
                string[] options = { "아이디", "이름" };
                if (Array.Exists(options, element => element == option))
                {
                    if (content != "")
                    {
                        data =
                        from adminUsers in context.AdminUsers
                            .OrderByDescending(au => au.CreatedAt)
                            .Select(au => new AdminUserViewModel
                            {
                                Id = au.Id,
                                UserName = au.UserName,
                                Name = au.Name,
                                Department = au.Department,
                                Status = au.Status,
                                LoggedAt = context.AdminLoginHistorys.Count(a => a.AdminId == au.Id) > 0 ? context.AdminLoginHistorys.Where(a => a.AdminId == au.Id).Max(a => a.CreatedAt) : null,
                                AdminUserRoles = au.AdminUserRoles,
                            })
                            .AsNoTracking()
                        where EF.Functions.Like(
                            option == "아이디" ? adminUsers.UserName : (
                                option == "이름" ? adminUsers.Name : "" ), "%" + content + "%")
                        select adminUsers;
                    }
                    else
                    {
                        error = "content is too short";
                    }
                }
                else
                {
                    error = "invalid option";
                }
            }
            else
            {
                if (content == null)
                {
                    error = "content is too short";
                }
                else if (option == null)
                {
                    error = "invalid option";
                }
            }

            if (data == null)
            {
                return Json(PaginatedList<int>.nothing(error));
            }

            PaginatedList<AdminUserViewModel> reviews = await PaginatedList<AdminUserViewModel>.CreateAsync(data, page);

            return Json(reviews.paginate());



            //return Json(context.AdminUsers.OrderByDescending(au => au.CreatedAt).Select(au => new AdminUserViewModel
            //{
            //    Id = au.Id,
            //    UserName = au.UserName,
            //    Name = au.Name,
            //    Department = au.Department,
            //    Status = au.Status,
            //    AdminUserRoles = au.AdminUserRoles,
            //}));
        }

        [HttpGet("api/[controller]/get/{id}")]
        public async Task<ActionResult> GetAdminUserById(string id)
        {
            return Json(context.AdminUsers.Where(au => au.Id == id)
                .Include(au => au.AdminUserRoles)
                .ThenInclude(aur => aur.AdminRole)
                .Select(au => new
                {
                    Id = au.Id,
                    UserName = au.UserName,
                    Name = au.Name,
                    Department = au.Department,
                    Status = au.Status,
                    AdminUserRoles = au.AdminUserRoles.Select(e => new
                    {
                        e.AdminRole,
                        e.RoleId,
                        e.UserId
                    }),
                })
                .FirstOrDefault());
        }

        [HttpGet("api/[controller]/role/all")]
        public async Task<ActionResult> GetRoles()
        {
            return Json(context.AdminRoles.Where(ar => ar.Type != "Super").ToList().OrderBy(ar => ar.Order).GroupBy(ar => new { ar.Type }));
        }

        [HttpPost("api/[controller]/AdminUser")]
        public async Task<ActionResult> PostAdminUser([FromBody] AdminUserViewModel model)
        {
            List<string> errors = new List<string>();
            List<GzApi.AdminEditList> editList = new List<GzApi.AdminEditList>();

            if (ModelState.IsValid)
            {
                if (model.Id == null)
                {
                    if (model.Password != null && model.Password.Length > 0)
                    {
                        var user = new AdminUser
                        {
                            UserName = model.UserName,
                            Name = model.Name,
                            Department = model.Department,
                            Status = model.Status,
                            CreatedAt = DateTime.Now
                        };
                        var result = await userManager.CreateAsync(user, model.Password);



                        if (result.Succeeded)
                        {
                            var adminUserRoles = model.AdminUserRoles;

                            if (adminUserRoles.Count > 0)
                            {
                                for (int i = 0; i < adminUserRoles.Count; i++)
                                {

                                    adminUserRoles.ElementAt(i).UserId = user.Id;
                                    context.AdminUserRoles.Add(adminUserRoles.ElementAt(i));



                                    //editList.Add(new GzApi.AdminEditList
                                    //{
                                    //    AdminId = user.Id,
                                    //    RoleType = adminUserRoles.ElementAt(i).AdminRole

                                    //});
                                }

                                await context.SaveChangesAsync();


                                editList = model.AdminUserRoles
                               .Join(context.AdminRoles, user => user.RoleId, admin => admin.Id, (ar, ur) => new GzApi.AdminEditList
                               {
                                   AdminId = ar.UserId,
                                   RoleType = ur.Order % 2 == 0 ? true : false,
                                   RoleName = ur.Type +
                                   (ur.Order % 2 == 0 ? " 편집" : " 조회")
                                   + " 권한 추가"
                               })
                               .ToList();

                                GzApi.SetAdminEditHistory(context, editList);
                            }

                            return Ok();
                        }

                        foreach (var error in result.Errors)
                        {
                            errors.Add(error.Description);
                        }
                    }
                    else
                    {
                        //required when register admin
                        errors.Add("비밀번호를 입력해 주세요.");
                    }
                }
                else
                {
                    //do update

                    var adminUser = context.AdminUsers.Find(model.Id);

                    if (adminUser == null)
                    {
                        return BadRequest();
                    }

                    adminUser.Name = model.Name;
                    adminUser.UserName = model.UserName;
                    adminUser.Department = model.Department;
                    adminUser.Status = model.Status;

                    if (model.Password != null && model.Password.Length > 0)
                    {
                        //password manudally change
                        PasswordHasher<AdminUser> hasher = new PasswordHasher<AdminUser>();
                        var hasedPassword = hasher.HashPassword(adminUser, model.Password);
                        adminUser.SecurityStamp = Guid.NewGuid().ToString();
                        adminUser.PasswordHash = hasedPassword;
                    }



                    var adminUserRole = context.AdminUserRoles.Where(aur => aur.UserId == adminUser.Id).AsNoTracking().FirstOrDefault();
                    if (adminUserRole != null)
                    {
                        var adminRole = context.AdminRoles.AsNoTracking().Where(ar => ar.Id == adminUserRole.RoleId).FirstOrDefault();

                        if (adminRole != null)
                        {
                            if (adminRole.Name == "Super")
                            {
                                await context.SaveChangesAsync();
                                return Ok();
                            }
                        }
                    }
                    

                    var adminUserRoles = model.AdminUserRoles;

                    if (adminUserRoles.Count > 0)
                    {
                        var _adminUserRoles = context.AdminUserRoles.Where(aur => aur.UserId == adminUser.Id);
                        var _adminUserRolesRoleId = _adminUserRoles.Select(aur => aur.RoleId).AsNoTracking().ToList();

                        List<string> adminUserRolesRoleId = new List<string>();
                        List<string> adminUserRolesRoleId2 = new List<string>();
                        for (int i = 0; i < adminUserRoles.Count; i++)
                        {
                            adminUserRoles.ElementAt(i).UserId = adminUser.Id;
                            var roleId = adminUserRoles.ElementAt(i).RoleId;
                            adminUserRolesRoleId.Add(roleId);

                            if (_adminUserRolesRoleId.Find(str => str == roleId) != null)
                            {
                                //context.AdminUserRoles.Update(adminUserRoles.ElementAt(i));
                            }
                            else
                            {
                                AdminUserRole newAdminUserRole = new AdminUserRole
                                {
                                    UserId = adminUser.Id,
                                    RoleId = adminUserRoles.ElementAt(i).RoleId
                                };
                                context.AdminUserRoles.Add(newAdminUserRole);

                                adminUserRolesRoleId2.Add(roleId);
                                //context.AdminUserRoles.Add(adminUserRoles.ElementAt(i));
                            }
                        }



                        var removeRoleId = _adminUserRolesRoleId.Except(adminUserRolesRoleId);

                        foreach (var roleId in removeRoleId)
                        {
                            var remove = _adminUserRoles.Where(aur => aur.RoleId == roleId).FirstOrDefault();
                            if (remove != null)
                            {
                                context.AdminUserRoles.Remove(remove);
                            }
                        }


                        await context.SaveChangesAsync();

                        if (adminUserRolesRoleId2.Count > 0)
                        {
                            GzApi.SetAdminEditHistory(context, adminUserRolesRoleId2
                             .Join(context.AdminRoles, user => user, admin => admin.Id, (ar, ur) => new GzApi.AdminEditList
                             {
                                 AdminId = adminUser.Id,
                                 RoleType = ur.Order % 2 == 0 ? true : false,
                                 RoleName = ur.Type +
                                 (ur.Order % 2 == 0 ? " 편집" : " 조회")
                                 + " 권한 추가"
                             })
                             .ToList());
                        }

                        if (removeRoleId.Count() > 0)
                        {
                            GzApi.SetAdminEditHistory(context, removeRoleId
                             .Join(context.AdminRoles, user => user, admin => admin.Id, (ar, ur) => new GzApi.AdminEditList
                             {
                                 AdminId = adminUser.Id,
                                 RoleType = ur.Order % 2 == 0 ? true : false,
                                 RoleName = ur.Type +
                                 (ur.Order % 2 == 0 ? " 편집" : " 조회")
                                 + " 권한 삭제"
                             })
                             .ToList());
                        }

                        
                    }
                    else
                    {
                        var _adminUserRoles = context.AdminUserRoles.Where(aur => aur.UserId == adminUser.Id);

                        foreach (var aur in _adminUserRoles)
                        {
                            context.AdminUserRoles.Remove(aur);
                        }
                    }

                    await context.SaveChangesAsync();

                    return Ok();
                }













            }
            else
            {
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }
            }


            return Json(errors);
        }


        [HttpGet("api/[controller]/accesshistories")]
        public async Task<ActionResult> GetAccessHistories(string id, int page = 1)
        {
            if (page < 1)
            {
                page = 1;
            }

            Console.WriteLine(id);

            using (var cmd = context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Admin_Login_History_All";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@P_PAGE",
                    SqlDbType.Int)
                { Value = page });
                cmd.Parameters.Add(new SqlParameter("@P_ADMIN_ID",
                    SqlDbType.NVarChar)
                { Value = id });

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

                return Json(retObject);
            }
        }



        [HttpGet("api/[controller]/edithistories")]
        public async Task<ActionResult> GetEditHistories(string id, int page = 1)
        {
            if (page < 1)
            {
                page = 1;
            }

            Console.WriteLine(id);

            using (var cmd = context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Admin_Edit_History_All";
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.Parameters.Add(new SqlParameter("@P_PAGE",
                    SqlDbType.Int)
                { Value = page });
                cmd.Parameters.Add(new SqlParameter("@P_ADMIN_ID",
                    SqlDbType.NVarChar)
                { Value = id });

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

                return Json(retObject);
            }
        }
    }
}
