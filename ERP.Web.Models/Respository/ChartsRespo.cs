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

        public async Task<bool> chkExistDaily(DateTime OrderDate)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("OrderDate", OrderDate);
            var sql = @"
                        SELECT TOP 1 1
                        FROM erp.dbo.ChartOrder
                        WHERE OrderDate = CONVERT(DATE, @OrderDate);
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

        public async Task<List<OrdersAmountMainModel>> GetOrdersAmount(int targetYear, int targetMonth, Guid SalerID)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("targetYear", targetYear);
            sqlparam.Add("targetMonth", targetMonth);
            sqlparam.Add("SalerID", SalerID);
            var sql = @"
                        WITH DailyAmounts AS (
                            SELECT 
                                OrderDate,  -- 取出日期部分
                                SUM(OrderAmount) AS TotalAmount,            -- 當天金額加總
		                        COUNT(*) AS OrderCount  
                            FROM erp.dbo.ChartOrder
                            WHERE YEAR(OrderDate) = @targetYear
                            AND Month(OrderDate) = @targetMonth
                            AND SalerID = @SalerID
                            GROUP BY OrderDate
                        ),
                        RankedOrders AS (
                            SELECT 
                                OrderID,
                                OrderAmount,
                                OrderDate,
                                SalerID,
                                ROW_NUMBER() OVER (
                                    PARTITION BY CONVERT(DATE, OrderDate) 
                                    ORDER BY OrderAmount DESC
                                ) AS OrderRank
                            FROM erp.dbo.ChartOrder
                            WHERE YEAR(OrderDate) = @targetYear
                            AND Month(OrderDate) = @targetMonth
                            AND SalerID = @SalerID
                        )
                        SELECT 
                            R.OrderID,
                            R.OrderAmount as TopAmount,
                            D.TotalAmount AS TotalAmount, -- 當天金額總和
	                        D.OrderCount,
                            R.OrderDate,
                            R.SalerID
                        FROM RankedOrders R
                        JOIN DailyAmounts D
                            ON R.OrderDate = D.OrderDate
                        WHERE R.OrderRank = 1 -- 只選取當天金額最高的單
                        ";

            sql += @" ORDER BY R.OrderDate";
            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.QueryAsync<OrdersAmountMainModel>(sql, sqlparam);
                return result.ToList();

            }
            catch
            {
                return null;
            }
        }

        public async Task<List<Guid>> GetSalerList()
        {
            var sql = @"
                    SELECT ID
                      FROM erp.dbo.EmployeeMain
                      WHERE Enabled = 1
                      AND Deleted =0
                        ";
            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.QueryAsync<Guid>(sql);
                return result.ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> InsertDailyData(ChartOrderMainModel param)
        {
            var sqlparam = new DynamicParameters();
            foreach (var property in param.GetType().GetProperties())
            {
                sqlparam.Add(property.Name, property.GetValue(param));
            }
            var sql = @"
                    INSERT INTO dbo.ChartOrder
                               (
                                ID
                               ,OrderID
                               ,OrderAmount
                               ,OrderDate
                               ,SalerID
                               ,ModifyDate
                               ,ModifyUser
                               )
                         VALUES
                               (
		                        newid()
                               ,@OrderID
                               ,@OrderAmount
                               ,@OrderDate
                               ,@SalerID
                               ,@ModifyDate
                               ,@ModifyUser
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
