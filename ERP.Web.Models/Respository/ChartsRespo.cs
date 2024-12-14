using Dapper;
using ERP.Web.Models.Models;
using ERP.Web.Utility.Models;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;

namespace ERP.Web.Models.Respository
{
    public class ChartsRespo
    {
        private readonly DBList _dBList;
        public ChartsRespo
            (
             IOptions<DBList> dbList)
        {
            _dBList = dbList.Value;
        }
        public async Task<List<OrdersAmountMainModel>> GetOrdersAmount()
        {
            var sql = @"
                    SELECT ID
                          ,OrderDate
                          ,Amount
                          ,Count
                      FROM dbo.ChartOrder
                      ORDER BY OrderDate
                        ";
            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.QueryAsync<OrdersAmountMainModel>(sql);
                return result.ToList();

            }
            catch
            {
                return null;
            }
        }
    }
}
