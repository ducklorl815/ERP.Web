using Dapper;
using ERP.Web.Models.Models.ControllerSetting;
using ERP.Web.Models.Models.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;

namespace ERP.Web.Models.Respository.Tools
{
    public partial class ToolsRespo
    {
        public async Task<bool> chkInsetData(FontAwesomeMainModel param)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("IconClass", param.IconClass);
            sqlparam.Add("IconStyle", param.IconStyle);

            var sql = $@"
                        SELECT TOP 1 1
                          FROM Controller.dbo.FontAwesomeMain
                          WHERE IconClass = @IconClass
                          AND IconStyle = @IconStyle
                        ";

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.QueryFirstOrDefaultAsync<int>(sql, sqlparam);
                return result > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<bool> InsertFontAwesome(FontAwesomeMainModel param)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("IconClass", param.IconClass);
            sqlparam.Add("IconStyle", param.IconStyle);

            var sql = $@"
                    INSERT INTO Controller.dbo.FontAwesomeMain
                               (
		                       ID
                               ,IconClass
                               ,IconStyle
                               ,Enabled
                               ,Deleted
		                       )
                         VALUES
                               (
		                       NEWID()
                               ,@IconClass
                               ,@IconStyle
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
