using Dapper;
using ERP.Web.Models.Models.ControllerSetting;
using ERP.Web.Utility.Models;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;

namespace ERP.Web.Models.Respository.ControllerSetting
{
    public class ControllerSettingRepo
    {
        private readonly DBList _dBList;
        private readonly string CreateUser;
        private readonly string CreateDept;

        public ControllerSettingRepo
            (
             IOptions<DBList> dBList
            )
        {
            _dBList = dBList.Value;
            CreateUser = "C2C93ACC-74AF-4CD6-8D73-70FE1E981041";
            CreateDept = "35FE68C1-9F3F-458D-A832-D53A4B96C6EE";
        }
        /// <summary>
        /// 新增Action
        /// </summary>
        /// <param name="ActionName"></param>
        /// <returns></returns>
        public async Task<Guid> ControllerActionDataMaintain(string ActionName)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("ActionName", ActionName);
            sqlparam.Add("CreateUser", CreateUser);
            sqlparam.Add("CreateDept", CreateDept);
            sqlparam.Add("ModifyUser", CreateUser);
            sqlparam.Add("ModifyDept", CreateDept);
            var sql = $@"
                    INSERT INTO Controller.dbo.ControllerAction
                               (ID
                               ,ActionName
                               ,ActionDesc
                               ,AbandonReason
                               ,CreateDate
                               ,CreateUser
                               ,CreateDept
                               ,ModifyDate
                               ,ModifyUser
                               ,ModifyDept
                               ,Enabled
                               ,Deleted)
                         OUTPUT inserted.ID 
                         VALUES
                               (NEWID()
                               ,@ActionName
                               ,''
                               ,''
                               ,GETDATE()
                               ,@CreateUser
                               ,@CreateDept
                               ,GETDATE()
                               ,@ModifyUser
                               ,@ModifyDept
                               ,1
                               ,0)
                        ";

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.QueryFirstOrDefaultAsync<Guid>(sql, sqlparam);
                return result;
            }
            catch (Exception)
            {
                return Guid.Empty;
            }
        }
        /// <summary>
        /// 新增Controller
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> ControllerDataMaintain(ControllerMainModel param)
        {
            var sqlparam = new DynamicParameters();
            //foreach (var property in param.GetType().GetProperties())
            //{
            //    sqlparam.Add(property.Name, property.GetValue(param));
            //}
            sqlparam.Add("StationMainID", param.StationMainID);
            sqlparam.Add("DisplayName", param.DisplayName);
            sqlparam.Add("Controller", param.Controller);
            sqlparam.Add("ControllerActionID", param.ControllerActionID);
            sqlparam.Add("HttpMethod", param.HttpMethod);
            sqlparam.Add("ParentControllerMainID", param.ParentControllerMainID);
            sqlparam.Add("PageNumber", param.PageNumber);
            sqlparam.Add("IconClass", param.IconClass);
            sqlparam.Add("FrontNumber", param.FrontNumber);
            sqlparam.Add("Sort", param.Sort);
            sqlparam.Add("IsMenu", param.IsMenu);
            sqlparam.Add("IsBlank", param.IsBlank);
            sqlparam.Add("Level", param.Level);
            sqlparam.Add("ControllerDesc", param.ControllerDesc);

            sqlparam.Add("CreateUser", CreateUser);
            sqlparam.Add("CreateDept", CreateDept);
            sqlparam.Add("ModifyUser", CreateUser);
            sqlparam.Add("ModifyDept", CreateDept);
            var sql = $@"
                        INSERT INTO Controller.dbo.ControllerMain
                                   (ID
                                   ,Controller
                                   ,DisplayName
                                   ,ControllerDesc
                                   ,HttpMethod
                                   ,ParentControllerMainID
                                   ,ControllerActionID
                                   ,StationMainID
                                   ,Level
                                   ,Sort
                                   ,IsMenu
                                   ,FrontNumber
                                   ,IconClass
                                   ,isBlank
                                   ,AbandonReason
                                   ,CreateDate
                                   ,CreateUser
                                   ,CreateDept
                                   ,ModifyDate
                                   ,ModifyUser
                                   ,ModifyDept
                                   ,Enabled
                                   ,Deleted)
                             VALUES
                                   (NEWID()
                                   ,@Controller
                                   ,@DisplayName
                                   ,@ControllerDesc
                                   ,@HttpMethod
                                   ,@ParentControllerMainID
                                   ,@ControllerActionID
                                   ,@StationMainID
                                   ,@Level
                                   ,@Sort
                                   ,@IsMenu
                                   ,@FrontNumber
                                   ,@IconClass
                                   ,@isBlank
                                   ,''
                                   ,GETDATE()
                                   ,@CreateUser
                                   ,@CreateDept
                                   ,GETDATE()
                                   ,@ModifyUser
                                   ,@ModifyDept
                                   ,1
                                   ,0)
                        ";

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.ExecuteAsync(sql, sqlparam);
                return result > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<ControllerSettingActionMainModel>> ControllerSettingActionList(ControllerSettingActionMainModel_param actionParam)
        {
            var sql = $@"
                        SELECT 
                            ca.ID as ActionID
		                    ,cm.Controller
		                    ,cm.ID as ControllerID
		                    ,cm.HttpMethod
		                    ,ActionName
		                    ,ActionDesc
		                    ,ca.Enabled
		                    ,ca.Deleted
                        FROM Controller.dbo.ControllerAction ca
                        JOIN  Controller.dbo.ControllerMain cm ON cm.ControllerActionID = ca.ID
                        ";

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.QueryAsync<ControllerSettingActionMainModel>(sql);
                return result.ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// 撈取Controller,Action關聯
        /// </summary>
        /// <returns></returns>
        public async Task<List<ControllerListMainModel>> ControllerSettingSearchList()
        {
            var sql = $@"
                    SELECT cm.ID
                          ,cm.Seq
	                      ,stat.StationName
	                      ,stat.StationCode
                          ,cm.Controller
	                      ,ca.ActionName
                          ,cm.HttpMethod
                          ,cm.Level
                          ,cm.Sort
	                      ,cm2.Seq as ParentSeq
                          ,cm.ParentControllerMainID
                          ,cm.StationMainID
                          ,cm.IsMenu
	                      ,ca.ActionDesc
                      FROM Controller.dbo.ControllerMain cm
                      LEFT JOIN Controller.dbo.ControllerStationMain stat ON stat.ID = cm.StationMainID
                      LEFT JOIN Controller.dbo.ControllerAction ca ON ca.ID = cm.ControllerActionID
                      LEFT JOIN Controller.dbo.ControllerMain cm2 ON cm2.ID = cm.ParentControllerMainID
                        ";

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.QueryAsync<ControllerListMainModel>(sql);
                return result.ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }
        /// <summary>
        /// 取得控制器資訊
        /// </summary>
        /// <param name="Domain"></param>
        /// <returns></returns>
        public async Task<List<StationMainModel>> GetControllerDataAsync(string StationMainID)
        {
            var sqlparam = new DynamicParameters();

            var sql = @"
                        SELECT cm.ID
                              ,cm.Controller
	                          ,case when ca.ActionName is NULL then '' else  ca.ActionName end as ActionName
                              ,DisplayName
                              ,HttpMethod
                              ,cs.Domain
							  ,cm.StationMainID
                          FROM Controller.dbo.ControllerMain cm
                          LEFT JOIN Controller.dbo.ControllerAction ca ON ca.ID = cm.ControllerActionID AND ca.Enabled = 1 AND ca.Deleted = 0
                          JOIN Controller.dbo.ControllerStationMain cs ON cs.ID = cm.StationMainID AND cs.Enabled = 1 AND cs.Deleted = 0
                          WHERE cm.Enabled = 1
                          AND cm.Deleted = 0
                            ";

            if (!string.IsNullOrEmpty(StationMainID))
            {
                sqlparam.Add("StationMainID", StationMainID.ToUpper());
                sql += " AND cm.StationMainID = @StationMainID";
            }
            sql += " ORDER BY CASE WHEN cm.Controller = '' THEN 0 ELSE 1 END, cm.CreateDate DESC";

            try
            {
                using (var conn = new SqlConnection(_dBList.erp))
                {
                    var result = await conn.QueryAsync<StationMainModel>(sql, sqlparam);
                    return result.ToList();
                }
            }
            catch (Exception)
            {

                throw;
            }

        }

        public async Task<List<StationMainModel>> GetStationMainDataAsync()
        {
            var sqlQuery = @"select ID 
                                   ,StationCode
                                   ,StationName
                                   ,Domain
                             from Controller.dbo.ControllerStationMain
                             where Deleted = 0";

            using (var conn = new SqlConnection(_dBList.erp))
            {
                var result = await conn.QueryAsync<StationMainModel>(sqlQuery);
                return result.ToList();
            }

        }

        /// <summary>
        /// 新增站台
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> StationDataMaintain(StationMainModel param)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("StationName", param.StationName);
            sqlparam.Add("Domain", param.Domain);
            sqlparam.Add("StationCode", param.StationCode);

            sqlparam.Add("CreateUser", CreateUser);
            sqlparam.Add("CreateDept", CreateDept);
            sqlparam.Add("ModifyUser", CreateUser);
            sqlparam.Add("ModifyDept", CreateDept);

            var sql = $@"
                        INSERT INTO Controller.dbo.ControllerStationMain
                                   (
		                           ID
                                   ,StationCode
                                   ,StationName
                                   ,Domain
                                   ,AbandonReason
                                   ,CreateDate
                                   ,CreateUser
                                   ,CreateDept
                                   ,ModifyDate
                                   ,ModifyUser
                                   ,ModifyDept
                                   ,Enabled
                                   ,Deleted
		                           )
                             VALUES
                                   (
		                           NEWID()
                                   ,@StationCode
                                   ,@StationName
                                   ,@Domain
                                   ,''
                                   ,GETDATE()
                                   ,@CreateUser
                                   ,@CreateDept
                                   ,GETDATE()
                                   ,@ModifyUser
                                   ,@ModifyDept
                                   ,1
                                   ,0
		                           )
                        ";

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.ExecuteAsync(sql, sqlparam);
                return result > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
