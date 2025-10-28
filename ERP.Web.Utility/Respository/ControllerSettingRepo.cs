using Dapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;

namespace ERP.Web.Utility.Respository
{
    public class ControllerUtilityRepo
    {
        private readonly string _connStr;
        public ControllerUtilityRepo(string connStr)
        {
            _connStr = "Data Source=DB-ERP;Initial Catalog=erp;User ID=ducklorl815;Password=201985;Integrated Security=false;Pooling=TRUE;Application Name=ERP.Web;TrustServerCertificate=True";
        }

        public async Task<IEnumerable<dynamic>> GetMenuDataAsync(List<string> ControllerIDList)
        {

            var sqlParam = new DynamicParameters();
            sqlParam.Add("@ControllerIDList", ControllerIDList);

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
                                    AND c.ID IN @ControllerIDList

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
                                    AND c.ID IN @ControllerIDList
                                )
                                SELECT 
                                    ct.*
                                FROM ControllerTree ct
                                ORDER BY ct.Level, ct.Sort

                            ";

            using (var conn = new SqlConnection(_connStr))
            {
                return await conn.QueryAsync<dynamic>(sqlQuery, sqlParam);
            }
        }

        public async Task<string> GetBoundAccessGroupData(string employeeMainID)
        {
            employeeMainID = "4DC64990-C818-4A28-AAEC-4C726F5E6CEB";
            var sqlWhere = string.Empty;
            var sqlParam = new DynamicParameters();
            sqlParam.Add("empID", employeeMainID);

            var sqlQuery = @"
                            SELECT NodeJson
                              FROM Controller.dbo.ControllerAccessGroup cg
                              JOIN Controller.dbo.EmpBoundAccessGroup empg ON empg.ControllerAccessGroupID = cg.ID
                              WHERE empg.EmployeeMainID = @empID
                              AND empg.Enabled = 1 
                              AND empg.Deleted = 0
                              AND cg.Enabled = 1
                              AND cg.Deleted = 0
                            ";

            using (var conn = new SqlConnection(_connStr))
            {
                return await conn.QueryFirstAsync<string>(sqlQuery, sqlParam);
            }
        }
    }
}
