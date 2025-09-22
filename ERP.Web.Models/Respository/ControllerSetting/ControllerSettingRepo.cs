using Dapper;
using ERP.Web.Models.Models.ControllerSetting;
using ERP.Web.Utility.Models;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;

namespace LifeTech.FlashWolf.ERP.Web.Models.Repo.ControllerSetting
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

        public async Task<bool> ControllerDataMaintain(ControllerMainModel param)
        {
            var sqlparam = new DynamicParameters();
            foreach (var property in param.GetType().GetProperties())
            {
                sqlparam.Add(property.Name, property.GetValue(param));
            }
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
    }
}
