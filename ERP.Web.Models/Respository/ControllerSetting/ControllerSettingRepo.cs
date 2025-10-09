using Dapper;
using ERP.Web.Models.Models.ControllerSetting;
using ERP.Web.Utility.Models;
using ERP.Web.Utility.Paging;
using ERP.Web.Utility.ViewModel;
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
        /// 新增Controller
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<Guid> ControllerCreate(ControllerMainModel param)
        {
            var sqlparam = new DynamicParameters();

            sqlparam.Add("Controller", string.IsNullOrEmpty(param.Controller) ? "" : param.Controller);
            sqlparam.Add("Action", string.IsNullOrEmpty(param.Action) ? "" : param.Action);
            sqlparam.Add("HttpMethod", string.IsNullOrEmpty(param.HttpMethod) ? "" : param.HttpMethod);
            sqlparam.Add("IconClass", string.IsNullOrEmpty(param.IconClass) ? "" : param.IconClass);
            sqlparam.Add("FrontNumber", string.IsNullOrEmpty(param.FrontNumber) ? "" : param.FrontNumber);

            sqlparam.Add("StationMainID", param.StationMainID);
            sqlparam.Add("DisplayName", param.DisplayName);
            sqlparam.Add("ParentControllerMainID", param.ParentControllerMainID);
            sqlparam.Add("Sort", param.Sort);
            sqlparam.Add("IsMenu", param.IsMenu);
            sqlparam.Add("IsBlank", param.IsBlank);
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
                                   ,Action
                                   ,StationMainID
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
                                   OUTPUT INSERTED.ID
                             VALUES
                                   (NEWID()
                                   ,@Controller
                                   ,@DisplayName
                                   ,@ControllerDesc
                                   ,@HttpMethod
                                   ,@ParentControllerMainID
                                   ,@Action
                                   ,@StationMainID
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
                var result = await conn.ExecuteScalarAsync<Guid>(sql, sqlparam);
                return result;
            }
            catch (Exception)
            {
                return Guid.Empty;
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
	                      ,stat.Domain
	                      ,stat.StationCode
                          ,cm.Controller
	                      ,cm.Action
						  ,cm.DisplayName
                          ,cm.HttpMethod
						  ,cm.FrontNumber
						  ,cm.IconClass
                          ,cm.Sort
	                      ,cm2.Seq as ParentSeq
                          ,cm.ParentControllerMainID
                          ,cm.StationMainID
                          ,cm.IsMenu
                      FROM Controller.dbo.ControllerMain cm
                      LEFT JOIN Controller.dbo.ControllerStationMain stat ON stat.ID = cm.StationMainID
                      LEFT JOIN Controller.dbo.ControllerMain cm2 ON cm2.ID = cm.ParentControllerMainID
					  ORDER BY StationMainID, ParentSeq ,IsMenu desc
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

        public async Task<ControllerMainModel> GetActDataMaintain(string ID)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("ID", ID);

            var sql = @"
                        SELECT ID
                              ,Controller
                              ,Action
                              ,DisplayName
                              ,ControllerDesc
                              ,HttpMethod
                              ,ParentControllerMainID
                              ,StationMainID
                              ,Sort
                              ,IsMenu
                              ,FrontNumber
                              ,IconClass
                              ,IsBlank
                              ,ModifyDate
                              ,ModifyUser
                              ,ModifyDept
                          FROM Controller.dbo.ControllerMain
                          WHERE ID = @ID
                          AND Enabled = 1
                          AND Deleted = 0
                        ";
            try
            {
                using (var conn = new SqlConnection(_dBList.erp))
                {
                    var result = await conn.QueryFirstOrDefaultAsync<ControllerMainModel>(sql, sqlparam);
                    return result;
                }
            }
            catch (Exception ex)
            {

                throw;
            }

        }
        /// <summary>
        /// 更新ActDataMaintain
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<bool> UpdateActDataMaintain(ControllerMainModel param)
        {
            var sqlparam = new DynamicParameters();

            sqlparam.Add("Controller", string.IsNullOrEmpty(param.Controller) ? "" : param.Controller);
            sqlparam.Add("Action", string.IsNullOrEmpty(param.Action) ? "" : param.Action);
            sqlparam.Add("HttpMethod", string.IsNullOrEmpty(param.HttpMethod) ? "" : param.HttpMethod);
            sqlparam.Add("IconClass", string.IsNullOrEmpty(param.IconClass) ? "" : param.IconClass);
            sqlparam.Add("FrontNumber", string.IsNullOrEmpty(param.FrontNumber) ? "" : param.FrontNumber);

            sqlparam.Add("ID", param.ID);
            sqlparam.Add("StationMainID", param.StationMainID);
            sqlparam.Add("DisplayName", param.DisplayName);
            sqlparam.Add("ParentControllerMainID", param.ParentControllerMainID);
            sqlparam.Add("Sort", param.Sort);
            sqlparam.Add("IsMenu", param.IsMenu);
            sqlparam.Add("IsBlank", param.IsBlank);
            sqlparam.Add("ControllerDesc", param.ControllerDesc);

            sqlparam.Add("ModifyUser", CreateUser);
            sqlparam.Add("ModifyDept", CreateDept);
            var sql = @"
                        UPDATE Controller.dbo.ControllerMain
                           SET 
	                           Controller = @Controller
                              ,Action = @Action
                              ,DisplayName = @DisplayName
                              ,ControllerDesc = @ControllerDesc
                              ,HttpMethod = @HttpMethod
                              ,ParentControllerMainID = @ParentControllerMainID
                              ,StationMainID = @StationMainID
                              ,Sort = @Sort
                              ,IsMenu = @IsMenu
                              ,FrontNumber = @FrontNumber
                              ,IconClass = @IconClass
                              ,IsBlank = @IsBlank
                              ,ModifyDate = GETDATE()
                              ,ModifyUser = @ModifyUser
                              ,ModifyDept = @ModifyDept
                         WHERE ID = @ID
                        ";

            try
            {
                using (var conn = new SqlConnection(_dBList.erp))
                {
                    var result = await conn.ExecuteAsync(sql, sqlparam);
                    return result > 0;
                }
            }
            catch (Exception ex)
            {

                throw;
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
							  ,cs.StationCode
	                          ,cm.Action
                              ,cm.DisplayName
                              ,cm.HttpMethod
                              ,cs.Domain
							  ,cm.StationMainID
							  ,cm.ParentControllerMainID
							  ,cm1.DisplayName as ParentDisplayName
                          FROM Controller.dbo.ControllerMain cm
                          JOIN Controller.dbo.ControllerStationMain cs ON cs.ID = cm.StationMainID AND cs.Enabled = 1 AND cs.Deleted = 0
						  LEFT JOIN Controller.dbo.ControllerMain cm1 ON cm1.ID = cm.ParentControllerMainID 
                          WHERE cm.Enabled = 1
                          AND cm.Deleted = 0
                            ";

            if (!string.IsNullOrEmpty(StationMainID))
            {
                sqlparam.Add("StationMainID", StationMainID.ToUpper());
                sql += " AND cm.StationMainID = @StationMainID";
            }
            sql += " ORDER BY CASE WHEN cm.Controller = '' THEN 0 ELSE 1 END, cm.ParentControllerMainID , cm.CreateDate DESC";

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
        public async Task<int> GetIconCountAsync()
        {
            var sql = @"
                        SELECT COUNT(*)
                        FROM Controller.dbo.FontAwesomeMain
                        WHERE Deleted = 0 AND Enabled = 1
                        ";

            using (var conn = new SqlConnection(_dBList.erp))
            {
                var result = await conn.QueryFirstOrDefaultAsync<int>(sql);
                return result;
            }
        }
        /// <summary>
        /// 取得IconClass清單
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public async Task<List<IconUtilityModel>> GetIconList(Paging pager)
        {
            var sqlparam = new DynamicParameters();
            var sql = @"
                        SELECT ID
                              ,seq
                              ,IconClass
                              ,REPLACE(IconClass, 'fa-', '') AS IconName
                              ,IconStyle
                              ,version
                              ,Enabled
                              ,Deleted
                        FROM Controller.dbo.FontAwesomeMain
                        WHERE Deleted = 0 AND Enabled = 1
                        ORDER BY seq ASC
                        ";
            //分頁功能
            sqlparam.Add("Offset", pager.ItemStart - 1);
            sqlparam.Add("Fetch", pager.PageSize);

            sql += "offset @Offset Rows ";
            sql += "fetch next @Fetch Rows Only ";
            using (var conn = new SqlConnection(_dBList.erp))
            {
                var result = await conn.QueryAsync<IconUtilityModel>(sql, sqlparam);
                return result.ToList();
            }
        }
        /// <summary>
        /// 撈取站台清單
        /// </summary>
        /// <returns></returns>
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

        public async Task<List<MenuData>> GetMenuDataAsync()
        {
            var sqlQuery = @"
                                WITH ControllerTree AS
                                (
                                    -- Root 節點
                                    SELECT
                                        c.ID,
                                        c.Controller,
                                        c.DisplayName,
                                        c.Action,
                                        c.ControllerDesc,
                                        c.HttpMethod,
                                        c.StationMainID,
                                        0 AS Level,
                                        c.Sort,
                                        c.IsMenu,
                                        c.FrontNumber,
                                        c.IconClass,
                                        c.IsBlank,
                                        c.AbandonReason,
                                        c.Enabled,
                                        c.Deleted,
                                        c.ParentControllerMainID
                                    FROM Controller.dbo.ControllerMain c
                                    WHERE c.ParentControllerMainID = '00000000-0000-0000-0000-000000000000'

                                    UNION ALL

                                    -- 遞迴子節點
                                    SELECT
                                        c.ID,
                                        c.Controller,
                                        c.DisplayName,
                                        c.Action,
                                        c.ControllerDesc,
                                        c.HttpMethod,
                                        c.StationMainID,
                                        ct.Level + 1,
                                        c.Sort,
                                        c.IsMenu,
                                        c.FrontNumber,
                                        c.IconClass,
                                        c.IsBlank,
                                        c.AbandonReason,
                                        c.Enabled,
                                        c.Deleted,
                                        c.ParentControllerMainID
                                    FROM Controller.dbo.ControllerMain c
                                    INNER JOIN ControllerTree ct ON c.ParentControllerMainID = ct.ID
                                )
                                SELECT 
                                    ct.*
                                FROM ControllerTree ct
                                ORDER BY ct.Level, ct.Sort

                            ";


            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.QueryAsync<MenuData>(sqlQuery);
                return result.ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<List<ErpMenuData>> GetErpMenuDataAsync()
        {
            var sqlQuery = @"
                                WITH ControllerTree AS
                                (
                                    -- Root 節點
                                    SELECT
                                        c.ID,
                                        c.ControllerName,
                                        c.Name,
                                        c.ActName,
                                        c.ControllerDesc,
                                        c.HttpMethod,
                                        c.StationMainID,
                                        0 AS Level,
                                        c.Sort,
                                        c.IsMenu,
                                        c.FrontNumber,
                                        c.IconClass,
                                        c.AbandonReason,
                                        c.Enabled,
                                        c.Deleted,
                                        c.ControllerMainID
                                    FROM erp.dbo.ControllerMain c
                                    WHERE c.ControllerMainID = '00000000-0000-0000-0000-000000000000'

                                    UNION ALL

                                    -- 遞迴子節點
                                    SELECT
                                        c.ID,
                                        c.ControllerName,
                                        c.Name,
                                        c.ActName,
                                        c.ControllerDesc,
                                        c.HttpMethod,
                                        c.StationMainID,
                                        ct.Level + 1,
                                        c.Sort,
                                        c.IsMenu,
                                        c.FrontNumber,
                                        c.IconClass,
                                        c.AbandonReason,
                                        c.Enabled,
                                        c.Deleted,
                                        c.ControllerMainID
                                    FROM erp.dbo.ControllerMain c
                                    INNER JOIN ControllerTree ct ON c.ControllerMainID = ct.ID
                                )
                                SELECT 
                                    ct.*
                                FROM ControllerTree ct
                                ORDER BY ct.Level, ct.Sort
                            ";


            using var conn = new SqlConnection("Data Source=DBA-uERP1;Initial Catalog=erp;User ID=master2;Password=MasteR2;Integrated Security=false;Pooling=TRUE;Application Name=ERP.Web");
            try
            {
                var result = await conn.QueryAsync<ErpMenuData>(sqlQuery);
                return result.ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
