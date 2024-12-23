using Dapper;
using ERP.Web.Models.Models;
using ERP.Web.Utility.Models;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;

namespace ERP.Web.Models.Respository
{
    public class SeatMapRespo
    {
        private readonly DBList _dBList;

        public SeatMapRespo
            (
             IOptions<DBList> dbList)
        {
            _dBList = dbList.Value;
        }

        public async Task<Guid> ChkExistSeatMap(SeatMapMainModel param)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Col", param.Col);
            sqlparam.Add("Row", param.Row);
            var sql = @"
                        SELECT ID
                          FROM erp.dbo.SeatMap
                          WHERE Row = @Row
                           AND  Col = @Col
                           AND enabled = 1
                           AND Deleted = 0
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
        public async Task<bool> GetInsertSeatMap(SeatMapMainModel param)
        {
            var sqlparam = new DynamicParameters();
            foreach (var property in param.GetType().GetProperties())
            {
                sqlparam.Add(property.Name, property.GetValue(param));
            }
            var sql = @"
            INSERT INTO dbo.SeatMap
                        (
                        ID
                        ,Row
                        ,Col
                        ,Border
                        ,BoxNumber
                        ,Location
                        ,Colorcode
                        ,Status
                        ,Etc
                        ,ModifyDate
                        ,ModifyUser
                        ,Enabled
                        ,Deleted
                        )
                    VALUES
                        (
                        NEWID()
                        ,@Row
                        ,@Col
                        ,@Border
                        ,''
                        ,''
                        ,''
                        ,@Status
                        ,''
                        ,GETDATE()
                        ,@ModifyUser
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
        public async Task<bool> GetUpdateSeatMap(SeatMapMainModel param)
        {
            var sqlparam = new DynamicParameters();
            foreach (var property in param.GetType().GetProperties())
            {
                sqlparam.Add(property.Name, property.GetValue(param));
            }
            var sql = @"
                        UPDATE dbo.SeatMap
                           SET Border = @Border
                              ,Status = @Status
                              ,ModifyDate = @ModifyDate
                              ,ModifyUser = @ModifyUser
                         WHERE ID = @ID
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
