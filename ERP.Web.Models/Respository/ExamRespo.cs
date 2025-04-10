﻿using Dapper;
using ERP.Web.Models.Models;
using ERP.Web.Utility.Models;
using ERP.Web.Utility.Paging;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;

namespace ERP.Web.Models.Respository
{
    public class ExamRespo
    {
        private readonly DBList _dBList;
        public ExamRespo
            (
             IOptions<DBList> dbList)
        {
            _dBList = dbList.Value;
        }

        public async Task<Guid> ChkKidTest(string Class, string TestType, string KidID)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Class", Class);
            sqlparam.Add("TestType", TestType);
            if (Guid.TryParse(KidID, out Guid KidMainID))
                sqlparam.Add("KidMainID", KidMainID);
            var sql = @"
                    SELECT ID
                    FROM KidsWorld.dbo.KidTestIndex
                    WHERE Class = @Class
                    AND TestType = @TestType
                    AND CONVERT(DATE, TestDate) = CONVERT(DATE, GETDATE())
                    AND KidMainID = @KidMainID
            ";



            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.QueryFirstOrDefaultAsync<Guid>(sql, sqlparam);
                return result;
            }
            catch
            {
                return Guid.Empty;
            }
        }

        public async Task<bool> chkSameWord(Vocabulary param)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Question", param.Question);
            sqlparam.Add("Answer", param.Answer);
            sqlparam.Add("ClassName", param.ClassName);
            sqlparam.Add("Category", param.Category);

            var sql = @"
                        SELECT TOP 1 1 
                          FROM KidsWorld.dbo.Vocabulary
                          WHERE Question = @Question
                          AND Answer = @Answer
                          AND ClassName = @ClassName
                          AND Category = @Category
                        "
            ;

            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.QueryFirstOrDefaultAsync<int>(sql, sqlparam);
                return result > 0;

            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Vocabulary>> GetExamDataAsync(string ClassName, int ClassNum, string Category)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("ClassName", ClassName);
            sqlparam.Add("ClassNum", ClassNum);
            sqlparam.Add("Category", Category);

            var sql = @"
                DECLARE @targetClass INT = @ClassNum;
                DECLARE @minRange INT = CASE WHEN @targetClass - 10 < 0 THEN 0 ELSE @targetClass - 10 END; 
                DECLARE @MaxRange INT = @targetClass + 10; 

                SELECT DISTINCT w.ID as WordID
				, Type
				, ClassNum
				, ClassName
				, Category
				, Question
				, Answer
				, Focus
				, km.ID as KidID
				,wi.Correct
                FROM KidsWorld.dbo.Vocabulary w
                LEFT JOIN KidsWorld.dbo.KidExamWordIndex wi ON wi.ExamID = w.ID
                LEFT JOIN KidsWorld.dbo.KidTestIndex kti ON kti.ID = wi.KidTestIndexID
                LEFT JOIN KidsWorld.dbo.KidMain km ON km.ID = kti.KidMainID
                WHERE ClassName = @ClassName
                AND Category = @Category
                AND ClassNum BETWEEN @minRange AND @MaxRange
            ";


            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.QueryAsync<Vocabulary>(sql, sqlparam);
                return result.ToList();
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<Vocabulary>> GetExamFromExamIndex(Guid KidTestIndexID)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("KidTestIndexID", KidTestIndexID);

            var sql = @"
                SELECT 
                v.ID,
                v.Type,
                v.ClassNum,
                v.Category,
                v.ClassName,
                v.Question,
                v.Answer
                FROM KidsWorld.dbo.Vocabulary v
                JOIN KidsWorld.dbo.KidExamWordIndex ke ON v.ID = ke.ExamID
                JOIN KidsWorld.dbo.KidTestIndex kt ON ke.KidTestIndexID = kt.ID
                WHERE kt.ID = @KidTestIndexID 
                AND ke.Enabled = 1 AND ke.Deleted = 0
                AND kt.Enabled = 1 AND kt.Deleted = 0
            ";


            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.QueryAsync<Vocabulary>(sql, sqlparam);
                return result.ToList();
            }
            catch
            {
                return null;
            }
        }

        public async Task<List<string>> GetExamListAsync()
        {
            var sql = @"
                SELECT DISTINCT TOP 1000 ClassName  + ' ' + Category + ' ' + ClassNum as ClassName
                  FROM KidsWorld.dbo.Vocabulary
            ";


            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.QueryAsync<string>(sql);
                return result.ToList();

            }
            catch
            {
                return null;
            }
        }

        public async Task<List<(Guid, string)>> GetKidListAsync()
        {
            var sql = @"
                SELECT ID
                      ,Cname
                  FROM KidsWorld.dbo.KidMain
            ";


            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.QueryAsync<(Guid, string)>(sql);
                return result.ToList();

            }
            catch
            {
                return null;
            }
        }

        public async Task<int> GetListCountAsync(ExamMainKeyword param)
        {
            var sqlparam = new DynamicParameters();

            var sql = $@"
                    SELECT  count(*)
                      FROM KidsWorld.dbo.KidExamWordIndex wl
                      JOIN KidsWorld.dbo.Vocabulary w ON w.ID = wl.ExamID
                      JOIN KidsWorld.dbo.KidTestIndex kti ON kti.ID = wl.KidTestIndexID 
                      JOIN KidsWorld.dbo.KidMain km ON km.ID = kti.KidMainID  
                      WHERE wl.Enabled = 1
                      AND wl.Deleted = 0
                      AND kti.Enabled = 1
                      AND kti.Deleted = 0
                        ";

            #region 關鍵字搜尋
            if (param.ClassNameList?.Any() == true)
            {
                sql += " AND kti.Class IN @Class";
                sqlparam.Add("Class", param.ClassNameList);
            }
            if (!string.IsNullOrEmpty(param.KidID))
            {
                sqlparam.Add("KidID", param.KidID);
                sql += $" AND km.ID = @KidID";
            }
            if (!string.IsNullOrEmpty(param.CorrectType))
            {
                sqlparam.Add("Correct", param.CorrectType);
                sql += $" AND Correct = @Correct";
            }
            if (param.TestDate != DateTime.MinValue)
            {
                sqlparam.Add("TestDate", param.TestDate);
                sql += $" AND CONVERT(DATE, kti.TestDate) = @TestDate";
            }
            #endregion

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.QueryFirstOrDefaultAsync<int>(sql, sqlparam);
                return result;
            }
            catch (Exception)
            {
                return int.MinValue;
            }
        }

        public async Task<int> GetNewTestCountAsync(ExamMainKeyword param)
        {
            var sqlparam = new DynamicParameters();

            var sql = $@"
                    SELECT  count(*)
                    FROM KidsWorld.dbo.Vocabulary
                        ";

            #region 關鍵字搜尋
            //if (param.ClassNameList?.Any() == true)
            //{
            //    sql += " AND kti.Class IN @Class";
            //    sqlparam.Add("Class", param.ClassNameList);
            //}
            //if (!string.IsNullOrEmpty(param.KidID))
            //{
            //    sqlparam.Add("KidID", param.KidID);
            //    sql += $" AND km.ID = @KidID";
            //}
            //if (!string.IsNullOrEmpty(param.CorrectType))
            //{
            //    sqlparam.Add("Correct", param.CorrectType);
            //    sql += $" AND Correct = @Correct";
            //}
            //if (param.TestDate != DateTime.MinValue)
            //{
            //    sqlparam.Add("TestDate", param.TestDate);
            //    sql += $" AND CONVERT(DATE, kti.TestDate) = @TestDate";
            //}
            #endregion

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.QueryFirstOrDefaultAsync<int>(sql, sqlparam);
                return result;
            }
            catch (Exception)
            {
                return int.MinValue;
            }
        }

        public async Task<List<ExamMainModel>> GetSearchListAsync(Paging pager, ExamMainKeyword param)
        {
            var sqlparam = new DynamicParameters();

            var sql = $@"
                    SELECT 
	                       kti.Class
						  ,w.Category
	                      ,kti.TestType
	                      ,w.ID as WordID
	                      ,w.Question
	                      ,w.Answer
	                      ,km.Cname
                          ,Correct
	                      ,(CONVERT(DATE,kti.TestDate)) as TestDate
                      FROM KidsWorld.dbo.KidExamWordIndex wl
                      JOIN KidsWorld.dbo.Vocabulary w ON w.ID = wl.ExamID
                      JOIN KidsWorld.dbo.KidTestIndex kti ON kti.ID = wl.KidTestIndexID
                      JOIN KidsWorld.dbo.KidMain km ON km.ID = kti.KidMainID
                      WHERE wl.Enabled = 1
                      AND wl.Deleted = 0
                      AND kti.Enabled = 1
                      AND kti.Deleted = 0
                        ";

            #region 關鍵字搜尋
            if (param.ClassNameList?.Any() == true)
            {
                sql += " AND kti.Class IN @Class";
                sqlparam.Add("Class", param.ClassNameList);
            }

            if (!string.IsNullOrEmpty(param.KidID))
            {
                sqlparam.Add("KidID", param.KidID);
                sql += $" AND km.ID = @KidID";
            }
            if (!string.IsNullOrEmpty(param.CorrectType))
            {
                sqlparam.Add("Correct", param.CorrectType);
                sql += $" AND Correct = @Correct";
            }
            if (param.TestDate != DateTime.MinValue)
            {
                sqlparam.Add("TestDate", param.TestDate);
                sql += $" AND CONVERT(DATE, kti.TestDate) = @TestDate";
            }
            #endregion

            sql += " ORDER BY (CONVERT(DATE,kti.TestDate)) desc ";

            //分頁功能
            sqlparam.Add("Offset", pager.ItemStart - 1);
            sqlparam.Add("Fetch", pager.PageSize);
            sql += "offset @Offset Rows ";
            sql += "fetch next @Fetch Rows Only ";

            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.QueryAsync<ExamMainModel>(sql, sqlparam);
                return result.ToList();
            }
            catch (Exception)
            {
                return null;
            }

        }

        public async Task<List<DateTime>> GetTestDateList(string KidID)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("KidMainID", KidID);

            var sql = @"
                      SELECT DISTINCT CONVERT(date, TestDate)
                      FROM KidsWorld.dbo.KidTestIndex
                      WHERE KidMainID = @KidMainID
                      AND Enabled = 1
                      AND Deleted = 0
                        "
            ;

            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.QueryAsync<DateTime>(sql, sqlparam);
                return result.ToList();

            }
            catch
            {
                return null;
            }
        }

        public async Task<List<ExamMainModel>> GetNewTestListAsync(Paging pager, ExamMainKeyword param)
        {
            var sqlparam = new DynamicParameters();

            var sql = $@"
                        SELECT ID
                              ,seq
                              ,Type
                              ,Category
                              ,ClassName
                              ,ClassNum
                              ,Question
                              ,Answer
                              ,Focus
                          FROM KidsWorld.dbo.Vocabulary
                        ";

            #region 關鍵字搜尋
            //if (param.ClassNameList?.Any() == true)
            //{
            //    sql += " AND kti.Class IN @Class";
            //    sqlparam.Add("Class", param.ClassNameList);
            //}
            //if (!string.IsNullOrEmpty(param.KidID))
            //{
            //    sqlparam.Add("KidID", param.KidID);
            //    sql += $" AND km.ID = @KidID";
            //}
            //if (!string.IsNullOrEmpty(param.CorrectType))
            //{
            //    sqlparam.Add("Correct", param.CorrectType);
            //    sql += $" AND Correct = @Correct";
            //}
            //if (param.TestDate != DateTime.MinValue)
            //{
            //    sqlparam.Add("TestDate", param.TestDate);
            //    sql += $" AND CONVERT(DATE, kti.TestDate) = @TestDate";
            //}
            #endregion

            sql += " ORDER BY seq desc ";

            //分頁功能
            sqlparam.Add("Offset", pager.ItemStart - 1);
            sqlparam.Add("Fetch", pager.PageSize);
            sql += "offset @Offset Rows ";
            sql += "fetch next @Fetch Rows Only ";

            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.QueryAsync<ExamMainModel>(sql, sqlparam);
                return result.ToList();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<bool> InsertExamIndex(Guid ExamID, Guid KidTestIndexID)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("ExamID", ExamID);
            sqlparam.Add("KidTestIndexID", KidTestIndexID);

            var sql = @"
                    INSERT INTO KidsWorld.dbo.KidExamWordIndex
                               (
		                       ID
                               ,ExamID
                               ,KidTestIndexID
                               ,CreateDate
                               ,ModifyDate
                               ,Enabled
                               ,Deleted
		                       )
                         VALUES
                               (
		                       NEWID()
                               ,@ExamID
                               ,@KidTestIndexID
                               ,GETDATE()
                               ,GETDATE()
                               ,1
                               ,0
		                       )
                        "
            ;

            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.ExecuteAsync(sql, sqlparam);
                return result > 0;

            }
            catch
            {
                return false;
            }
        }

        public async Task<Guid> InsertKidTestIndex(string Class, string TestType, string KidID)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Class", Class);
            sqlparam.Add("TestType", TestType);
            if (Guid.TryParse(KidID, out Guid KidMainID))
                sqlparam.Add("KidMainID", KidMainID);

            var sql = @"
                    INSERT INTO KidsWorld.dbo.KidTestIndex
                               (ID
                               ,KidMainID
                               ,Class
                               ,TestType
                               ,TestDate
                               ,Enabled
                               ,Deleted)
                         OUTPUT INSERTED.ID
                         VALUES
                               (
		                       NEWID()
                               ,@KidMainID
                               ,@Class
                               ,@TestType
                               ,GETDATE()
                               ,1
                               ,0
		                       )
                        "
            ;

            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.QueryFirstOrDefaultAsync<Guid>(sql, sqlparam);
                return result;

            }
            catch
            {
                return Guid.Empty;
            }
        }

        public async Task<bool> InsertWord(Vocabulary param)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Type", param.Type);
            sqlparam.Add("Question", param.Question);
            sqlparam.Add("Answer", param.Answer);
            sqlparam.Add("ClassName", param.ClassName);
            sqlparam.Add("ClassNum", param.ClassNum.ToString("D2"));
            sqlparam.Add("Category", param.Category);

            //sqlparam.Add("Class", $"{param.ClassName} Sp {param.ClassNum.ToString("D2")}");
            var sql = @"
                    INSERT INTO KidsWorld.dbo.Vocabulary
                               (
                                ID
                               ,Type
                               ,ClassName
                               ,ClassNum
                               ,Category
                               ,Question
                               ,Answer
                                )
                         VALUES
                               (
                                newID()
                               ,@Type
                               ,@ClassName
                               ,@ClassNum
                               ,@Category
                               ,@Question
                               ,@Answer
                                )
                        "
            ;

            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.ExecuteAsync(sql, sqlparam);
                return result > 0;

            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> UpdateExamWord(string WordID, string KidID, string TestDate, bool Correct)
        {

            var sqlparam = new DynamicParameters();
            sqlparam.Add("ExamID", WordID);
            sqlparam.Add("KidMainID", KidID);
            sqlparam.Add("TestDate", DateTime.Parse(TestDate));
            sqlparam.Add("Correct", Correct);
            var sql = @"
                    UPDATE KEWI
                    SET 
                        KEWI.Correct = @Correct, 
                        KEWI.ModifyDate = GETDATE() 
                    FROM 
                        KidsWorld.dbo.KidExamWordIndex KEWI
                    JOIN 
                        KidsWorld.dbo.KidTestIndex KTI 
                        ON KTI.ID = KEWI.KidTestIndexID  
                    WHERE 
                        CAST(KTI.TestDate AS DATE) = CAST(@TestDate AS DATE) 
                        AND KTI.KidMainID = @KidMainID  
                        AND KEWI.ExamID = @ExamID  
                        "
            ;

            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.ExecuteAsync(sql, sqlparam);
                return result > 0;

            }
            catch
            {
                return false;
            }
        }
    }
}
