using GolfZonWebApp.Data;
using GolfZonWebApp.Lib;
using Microsoft.Data.SqlClient;
using System.Dynamic;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace GolfZonWebApp.Controllers
{
    public class StatisticsController : Controller
    {
        private readonly GolfzonContext _context;

        public StatisticsController(GolfzonContext context)
        {
            _context = context;
        }

        [HttpGet("api/[controller]/today")]
        public async Task<ActionResult> GetToday()
        {
            return Json(GzApi.GetDefaultSP(_context, "Admin_Statistics_Today"));
        }


        [HttpGet("api/[controller]/visit")]
        public async Task<ActionResult> GetVisit(int type, string startDate, string endDate)
        {
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Admin_Statistics_Visit";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_TYPE",
                    SqlDbType.Int)
                { Value = type });
                cmd.Parameters.Add(new SqlParameter("@P_START_DATE",
                    SqlDbType.DateTime2)
                { Value = startDate });
                cmd.Parameters.Add(new SqlParameter("@P_END_DATE",
                    SqlDbType.DateTime2)
                { Value = endDate });

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

        [HttpGet("api/[controller]/visitall")]
        public async Task<ActionResult> GetVisitAll(int type, string startDate, string endDate)
        {
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Admin_Statistics_Visit_All";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_TYPE",
                    SqlDbType.Int)
                { Value = type });
                cmd.Parameters.Add(new SqlParameter("@P_START_DATE",
                    SqlDbType.DateTime2)
                { Value = startDate });
                cmd.Parameters.Add(new SqlParameter("@P_END_DATE",
                    SqlDbType.DateTime2)
                { Value = endDate });

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

        [HttpGet("api/[controller]/visitnew")]
        public async Task<ActionResult> GetVisitNew(int type, string startDate, string endDate)
        {
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "Admin_Statistics_Visit_New";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_TYPE",
                    SqlDbType.Int)
                { Value = type });
                cmd.Parameters.Add(new SqlParameter("@P_START_DATE",
                    SqlDbType.DateTime2)
                { Value = startDate });
                cmd.Parameters.Add(new SqlParameter("@P_END_DATE",
                    SqlDbType.DateTime2)
                { Value = endDate });

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


        [HttpGet("api/[controller]/visitre")]
        public async Task<ActionResult> GetVisitRe(int type, string startDate, string endDate)
        {
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                if (type == 2)
                {
                    startDate += "-01";
                    endDate += "-01";
                }
                

                cmd.CommandText = "Admin_Statistics_Visit_Re";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_TYPE",
                    SqlDbType.Int)
                { Value = type });
                cmd.Parameters.Add(new SqlParameter("@P_START_DATE",
                    SqlDbType.DateTime2)
                { Value = startDate });
                cmd.Parameters.Add(new SqlParameter("@P_END_DATE",
                    SqlDbType.DateTime2)
                { Value = endDate });

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


        [HttpGet("api/[controller]/visitavg")]
        public async Task<ActionResult> GetVisitAvg(int type, string startDate, string endDate)
        {
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                if (type == 1 || type == 3)
                {
                    startDate += "-01";
                    endDate += "-01";
                }

                cmd.CommandText = "Admin_Statistics_Visit_Avg";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_TYPE",
                    SqlDbType.Int)
                { Value = type });
                cmd.Parameters.Add(new SqlParameter("@P_START_DATE",
                    SqlDbType.DateTime2)
                { Value = startDate });
                cmd.Parameters.Add(new SqlParameter("@P_END_DATE",
                    SqlDbType.DateTime2)
                { Value = endDate });

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

        [HttpGet("api/[controller]/newuser")]
        public async Task<ActionResult> GetNewUser(int type, string startDate, string endDate)
        {
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                if (type == 3)
                {
                    startDate += "-01";
                    endDate += "-01";
                }

                cmd.CommandText = "Admin_Statistics_NewUser";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_TYPE",
                    SqlDbType.Int)
                { Value = type });
                cmd.Parameters.Add(new SqlParameter("@P_START_DATE",
                    SqlDbType.DateTime2)
                { Value = startDate });
                cmd.Parameters.Add(new SqlParameter("@P_END_DATE",
                    SqlDbType.DateTime2)
                { Value = endDate });

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

        [HttpGet("api/[controller]/alluser")]
        public async Task<ActionResult> GetAllUser(int type, string startDate, string endDate)
        {
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                if (type == 1)
                {
                    startDate += "-01";
                    endDate += "-01";
                }

                cmd.CommandText = "Admin_Statistics_AllUser";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_TYPE",
                    SqlDbType.Int)
                { Value = type });
                cmd.Parameters.Add(new SqlParameter("@P_START_DATE",
                    SqlDbType.DateTime2)
                { Value = startDate });
                cmd.Parameters.Add(new SqlParameter("@P_END_DATE",
                    SqlDbType.DateTime2)
                { Value = endDate });

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

        [HttpGet("api/[controller]/mileage")]
        public async Task<ActionResult> GetMileage(int type, string startDate, string endDate)
        {
            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                if (type == 1)
                {
                    startDate += "-01";
                    endDate += "-01";
                }

                cmd.CommandText = "Admin_Statistics_Mileage";
                cmd.CommandType = CommandType.StoredProcedure;
                // set some parameters of the stored procedure
                cmd.Parameters.Add(new SqlParameter("@P_TYPE",
                    SqlDbType.Int)
                { Value = type });
                cmd.Parameters.Add(new SqlParameter("@P_START_DATE",
                    SqlDbType.DateTime2)
                { Value = startDate });
                cmd.Parameters.Add(new SqlParameter("@P_END_DATE",
                    SqlDbType.DateTime2)
                { Value = endDate });

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
