using Dapper;
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

        public async Task<Guid> ChkKidTest(string ClassName, string TestType, string KidID)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("ClassName", ClassName);
            sqlparam.Add("TestType", TestType.ToLower());
            if (Guid.TryParse(KidID, out Guid KidMainID))
                sqlparam.Add("KidMainID", KidMainID);
            var sql = @"
                    SELECT kti.ID
                    FROM KidsWorld.dbo.KidTestIndex kti
					JOIN KidsWorld.dbo.Lession les ON kti.LessionID = les.ID
                    WHERE les.ClassName = @ClassName
                    AND les.TestType = @TestType
                    AND CONVERT(DATE, kti.TestDate) = CONVERT(DATE, GETDATE())
                    AND kti.KidMainID = @KidMainID
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
        public async Task<bool> chkUpdateWord(Vocabulary param)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Question", param.Question);
            sqlparam.Add("Answer", param.Answer);
            sqlparam.Add("CategoryType", param.CategoryType);

            var sql = @"
                        UPDATE KidsWorld.dbo.Vocabulary
                        SET CategoryType = @CategoryType
                        WHERE Question = @Question
                        AND Answer = @Answer
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
        public async Task<bool> chkSameWord(Vocabulary param)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Question", param.Question);
            sqlparam.Add("Answer", param.Answer);

            var sql = @"
                        SELECT TOP 1 1 
                          FROM KidsWorld.dbo.Vocabulary
                          WHERE Question = @Question
                          AND Answer = @Answer
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

        public async Task<List<Vocabulary>> GetExamDataAsync(string ClassName)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("ClassName", ClassName);

            var sql = @"
                SELECT DISTINCT w.ID as WordID,
				les.TestType,
				w.CategoryType,
				ClassName,
				Question,
				Answer,
				case when wi.Focus is not null then wi.Focus else 0 end as Focus,
				km.ID as KidID,
				case when kwi.Correct is not null then kwi.Correct else 0 end as Correct
                FROM KidsWorld.dbo.Vocabulary w
				LEFT JOIN KidsWorld.dbo.VocabularyIndex wi ON wi.WordID = w.ID 
				JOIN KidsWorld.dbo.Lession les ON les.ID = w.LessionID
                LEFT JOIN KidsWorld.dbo.KidExamWordIndex kwi ON kwi.ExamID = w.ID
                LEFT JOIN KidsWorld.dbo.KidTestIndex kti ON kti.ID = kwi.KidTestIndexID
                LEFT JOIN KidsWorld.dbo.KidMain km ON km.ID = kti.KidMainID
                WHERE ClassName = @ClassName
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
                les.TestType,
				v.CategoryType,
                les.ClassName,
                v.Question,
                v.Answer
                FROM KidsWorld.dbo.Vocabulary v
                JOIN KidsWorld.dbo.KidExamWordIndex ke ON v.ID = ke.ExamID
                JOIN KidsWorld.dbo.KidTestIndex kti ON ke.KidTestIndexID = kti.ID
				JOIN KidsWorld.dbo.Lession les ON kti.LessionID = les.ID
                WHERE kti.ID = @KidTestIndexID 
                AND ke.Enabled = 1 AND ke.Deleted = 0
                AND kti.Enabled = 1 AND kti.Deleted = 0
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
                    SELECT ClassName
                      FROM KidsWorld.dbo.Lession
                      WHERE Enabled = 1
                      AND Deleted = 0
                      Order by LessionSort desc
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

        public async Task<int> GetReTestCountAsync(ExamMainKeyword param)
        {
            var sqlparam = new DynamicParameters();

            var sql = $@"
                    SELECT  count(*)
                      FROM KidsWorld.dbo.KidExamWordIndex wl
                      JOIN KidsWorld.dbo.Vocabulary w ON w.ID = wl.ExamID
					  JOIN KidsWorld.dbo.Lession les ON les.ID = w.LessionID
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
			            SELECT COUNT(*)
			            FROM KidsWorld.dbo.Vocabulary w
			            JOIN KidsWorld.dbo.Lession les ON les.ID = w.LessionID
						LEFT JOIN KidsWorld.dbo.VocabularyIndex wi ON wi.WordID = w.ID
                        ";

            #region 關鍵字搜尋
            if (param.ClassNameList?.Any() == true)
            {
                sql += " AND les.ClassName IN @Class";
                sqlparam.Add("Class", param.ClassNameList);
            }
            if (!string.IsNullOrEmpty(param.KidID))
            {
                sqlparam.Add("KidID", param.KidID);
                sql += $" AND km.ID = @KidID";
            }
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

        public async Task<List<ExamMainModel>> GetReTestSearchListAsync(Paging pager, ExamMainKeyword param)
        {
            var sqlparam = new DynamicParameters();

            var sql = $@"
                    SELECT 
	                       les.ClassName
	                      ,les.TestType
	                      ,w.ID as WordID
	                      ,w.Question
	                      ,w.Answer
	                      ,km.Cname
                          ,Correct
	                      ,(CONVERT(DATE,kti.TestDate)) as TestDate
                      FROM KidsWorld.dbo.KidExamWordIndex wl
                      JOIN KidsWorld.dbo.Vocabulary w ON w.ID = wl.ExamID
					  JOIN KidsWorld.dbo.Lession les ON les.ID = w.LessionID
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
                sql += " AND les.ClassName IN @ClassName";
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
                      SELECT DISTINCT CONVERT(date, TestDate),TestDate
                      FROM KidsWorld.dbo.KidTestIndex
                      WHERE KidMainID = @KidMainID
                      AND Enabled = 1
                      AND Deleted = 0
                      Order by TestDate desc
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
						SELECT w.ID as WordID,
				               les.ClassName,
				               CategoryType,
				               les.LessionSort,
				               les.TestType,
				               Question,
				               Answer,
				               case when wi.Focus is NULL then 0
									ELSE wi.Focus 
							   end as 'Focus'
			            FROM KidsWorld.dbo.Vocabulary w
			            JOIN KidsWorld.dbo.Lession les ON les.ID = w.LessionID
						LEFT JOIN KidsWorld.dbo.VocabularyIndex wi ON wi.WordID = w.ID
                        where 1 = 1
                        ";

            #region 關鍵字搜尋
            if (param.ClassNameList?.Any() == true)
            {
                sqlparam.Add("Class", param.ClassNameList);
                sql += " AND les.ClassName IN @Class";
            }
            if (!string.IsNullOrEmpty(param.KidID))
            {
                sqlparam.Add("KidID", param.KidID);
                sql += $" AND km.ID = @KidID";
            }
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

            sql += " ORDER BY les.LessionSort DESC ";

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
                               ,15
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

        public async Task<Guid> InsertKidTestIndex(Guid LessionID, string KidID)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("LessionID", LessionID);
            if (Guid.TryParse(KidID, out Guid KidMainID))
                sqlparam.Add("KidMainID", KidMainID);

            var sql = @"
                    INSERT INTO KidsWorld.dbo.KidTestIndex
                               (
                                ID
                               ,KidMainID
                               ,LessionID
                               ,TestDate
                               ,Enabled
                               ,Deleted)
                         OUTPUT INSERTED.ID
                         VALUES
                               (NEWID()
                               ,@KidMainID
                               ,@LessionID
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
            sqlparam.Add("LessionID", param.LessionID);
            sqlparam.Add("Question", param.Question);
            sqlparam.Add("Answer", param.Answer);
            sqlparam.Add("CategoryType", param.CategoryType);

            var sql = @"
                    INSERT INTO KidsWorld.dbo.Vocabulary
                               (
                                ID
                               ,LessionID
                               ,CategoryType
                               ,Question
                               ,Answer
                                )
                         VALUES
                               (
                                newID()
                               ,@LessionID
                               ,@CategoryType
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

        public async Task<Guid> ChkLessionID(string ClassName)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("ClassName", ClassName);

            var sql = @"
                    SELECT ID
                      FROM KidsWorld.dbo.Lession
                      WHERE ClassName = @ClassName
                      AND Enabled = 1
                      AND Deleted = 0
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

        public async Task<Guid> InsertLessionID(LessionModel param)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("ClassName", param.ClassName);
            sqlparam.Add("TestType", param.TestType);
            sqlparam.Add("LessionSort", param.LessionSort);
            var sql = @"
                INSERT INTO KidsWorld.dbo.Lession
                           (ID
                           ,ClassName
                           ,TestType
                           ,LessionSort
                           ,CreateDate
                           ,ModifyDate
                           ,Enabled
                           ,Deleted)
                     OUTPUT INSERTED.ID
                     VALUES
                           (NewID()
                           ,@ClassName
                           ,@TestType
                           ,@LessionSort
                           ,GETDATE()
                           ,GETDATE()
                           ,1
                           ,0)
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

        public async Task<int> GetClassNum(string ClassName)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("ClassName", ClassName);

            var sql = @"
                    SELECT ClassNum
                      FROM KidsWorld.dbo.Lession
                      WHERE Enabled = 1
                      AND Deleted = 0
                      AND ClassName = @ClassName
                        "
            ;

            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.QueryFirstOrDefaultAsync<int>(sql, sqlparam);
                if (result == 0)
                {
                    result = 1;
                    return result;
                }

                return result + 1;

            }
            catch
            {
                return 0;
            }
        }

        public async Task<int> GetLessionSort()
        {
            var sqlparam = new DynamicParameters();

            var sql = @"
                    SELECT LessionSort
                      FROM KidsWorld.dbo.Lession
                      WHERE Enabled = 1
                      AND Deleted = 0
                      Order by LessionSort desc
                        "
            ;

            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.QueryFirstOrDefaultAsync<int>(sql);
                if (result == 0)
                {
                    result = 1;
                    return result;
                }
                return result + 1;

            }
            catch
            {
                return 0;
            }
        }

        public async Task<Guid> GetLessionID(string ClassName)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("ClassName", ClassName);
            var sql = @"
                        SELECT ID
                          FROM KidsWorld.dbo.Lession
                          WHERE ClassName = @ClassName
                          AND Enabled = 1
                          AND Deleted = 0
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

        public async Task<bool> UpdateFocusWord(string WordID, string KidMainID, bool Focus)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("WordID", WordID);
            sqlparam.Add("KidMainID", KidMainID);
            sqlparam.Add("Focus", Focus);
            var sql = @"
						 UPDATE KidsWorld.dbo.VocabularyIndex
                           SET Focus = @Focus
                              ,FocusDate = GETDATE()
                         WHERE WordID = @WordID
                         AND KidMainID = @KidMainID
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

        public async Task<bool> ChkVocabIndex(string WordID, string KidMainID)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("WordID", WordID);
            sqlparam.Add("KidMainID", KidMainID);
            var sql = @"
                        SELECT TOP 1 1
                          FROM KidsWorld.dbo.VocabularyIndex
                          WHERE KidMainID = @KidMainID
                          AND WordID = @WordID
                          AND Enabled = 1
                          AND Deleted = 0
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

        public async Task<bool> InsertFocusWord(string WordID, string KidMainID, bool Focus)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("WordID", WordID);
            sqlparam.Add("KidMainID", KidMainID);
            sqlparam.Add("Focus", Focus);
            var sql = @"
                INSERT INTO KidsWorld.dbo.VocabularyIndex
                           (ID
                           ,KidMainID
                           ,WordID
                           ,Focus
                           ,FocusDate
                           ,Enabled
                           ,Deleted)
                     VALUES
                           (NEWID()
                           ,@KidMainID
                           ,@WordID
                           ,@Focus
                           ,GETDATE()
                           ,1
                           ,0)
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

        public async Task<bool> UpdateWord(string ID, string Question, string Answer)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("ID", ID);
            sqlparam.Add("Question", Question);
            sqlparam.Add("Answer", Answer);
            var sql = @"
                    UPDATE KidsWorld.dbo.Vocabulary
                       SET Question = @Question
                          ,Answer = @Answer
                     WHERE ID = @ID
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

        public async Task<Vocabulary> GetReExamVocab(string ID)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("ID", ID);
            var sql = @"
                    SELECT ID as WordID
	                      ,CategoryType
                          ,Question
                          ,Answer
                      FROM KidsWorld.dbo.Vocabulary
                      where ID =@ID
                        "
            ;

            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.QueryFirstOrDefaultAsync<Vocabulary>(sql, sqlparam);
                return result;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Guid> GetWordID(string Question, string Answer)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Question", Question);
            sqlparam.Add("Answer", Answer);
            var sql = @"
                        SELECT ID
                          FROM KidsWorld.dbo.Vocabulary
                          WHERE Question = @Question
                          AND Answer = @Answer
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
    }
}
