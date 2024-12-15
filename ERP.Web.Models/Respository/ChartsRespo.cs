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
        public async Task<List<OrdersAmountMainModel>> GetOrdersAmount(int targetYear, int targetMonth)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("targetYear", targetYear);

            var sql = @"
                        WITH DailyAmounts AS (
                            SELECT 
                                OrderDate,  -- 取出日期部分
                                SUM(OrderAmount) AS TotalAmount,            -- 當天金額加總
		                        COUNT(*) AS OrderCount  
                            FROM erp.dbo.ChartOrder
                            WHERE YEAR(OrderDate) = @targetYear
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
                        AND YEAR(R.OrderDate) = @targetYear
                        ";
            if (targetMonth > 0)
            {
                sqlparam.Add("targetMonth", targetMonth);
                sql += @" AND Month(R.OrderDate) = @targetMonth";
            }
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
    }
}
