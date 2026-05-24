using Dapper;
using ERP.Web.Models.Models;
using ERP.Web.Utility.Models;
using ERP.Web.Utility.Paging;
using Microsoft.Extensions.Options;
using Microsoft.Data.SqlClient;

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

        /// <summary>
        /// 取得指定課程可供測驗的單字清單。
        /// </summary>
        /// <param name="className">課程名稱</param>
        /// <param name="kidId">學生 ID；若可解析為 Guid，則會帶入該生最近一次作答與累計考次，供出題排序。</param>
        /// <param name="testType">測驗類型（如 English），與 kidId 一併用於篩選歷史紀錄。</param>
        public async Task<List<Vocabulary>> GetExamDataAsync(string className, string? kidId = null, string? testType = null)
        {
            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                if (!string.IsNullOrWhiteSpace(kidId) && Guid.TryParse(kidId, out var kidMainId))
                {
                    var sqlparam = new DynamicParameters();
                    sqlparam.Add("ClassName", className);
                    sqlparam.Add("KidMainID", kidMainId);
                    sqlparam.Add("TestType", string.IsNullOrWhiteSpace(testType) ? "English" : testType);

                    var sqlKid = @"
                SELECT DISTINCT
                    w.ID AS WordID,
                    les.TestType,
                    w.CategoryType,
                    les.ClassName,
                    w.Question,
                    w.Answer,
                    CASE WHEN wi.Focus IS NOT NULL THEN wi.Focus ELSE 0 END AS Focus,
                    @KidMainID AS KidID,
                    ISNULL(latest.LastCorrect, 0) AS Correct,
                    ISNULL(latest.LastReTest, 0) AS ReTest,
                    latest.LastCorrect AS LastExamCorrect,
                    ISNULL(examCnt.Cnt, 0) AS ExamTimes
                FROM KidsWorld.dbo.Vocabulary w
                JOIN KidsWorld.dbo.Lession les ON les.ID = w.LessionID
                LEFT JOIN KidsWorld.dbo.VocabularyIndex wi ON wi.WordID = w.ID
                OUTER APPLY (
                    SELECT TOP 1
                        kwi.Correct AS LastCorrect,
                        kwi.ReTest AS LastReTest
                    FROM KidsWorld.dbo.KidExamWordIndex kwi
                    INNER JOIN KidsWorld.dbo.KidTestIndex kti ON kti.ID = kwi.KidTestIndexID
                        AND kti.Enabled = 1 AND kti.Deleted = 0
                    INNER JOIN KidsWorld.dbo.Lession les_k ON les_k.ID = kti.LessionID
                    WHERE kwi.ExamID = w.ID
                      AND kwi.Enabled = 1 AND kwi.Deleted = 0
                      AND kti.KidMainID = @KidMainID
                      AND LOWER(les_k.TestType) = LOWER(@TestType)
                    ORDER BY kti.TestDate DESC, kwi.ModifyDate DESC, kwi.CreateDate DESC
                ) latest
                OUTER APPLY (
                    SELECT COUNT_BIG(*) AS Cnt
                    FROM KidsWorld.dbo.KidExamWordIndex kwi
                    INNER JOIN KidsWorld.dbo.KidTestIndex kti ON kti.ID = kwi.KidTestIndexID
                        AND kti.Enabled = 1 AND kti.Deleted = 0
                    INNER JOIN KidsWorld.dbo.Lession les_k ON les_k.ID = kti.LessionID
                    WHERE kwi.ExamID = w.ID
                      AND kwi.Enabled = 1 AND kwi.Deleted = 0
                      AND kti.KidMainID = @KidMainID
                      AND LOWER(les_k.TestType) = LOWER(@TestType)
                ) examCnt
                WHERE les.ClassName = @ClassName
                ";

                    var resultKid = await conn.QueryAsync<Vocabulary>(sqlKid, sqlparam);
                    return resultKid.ToList();
                }

                var sqlparamLegacy = new DynamicParameters();
                sqlparamLegacy.Add("ClassName", className);

                var sql = @"
                SELECT DISTINCT w.ID as WordID,
				les.TestType,
				w.CategoryType,
				ClassName,
				Question,
				Answer,
				case when wi.Focus is not null then wi.Focus else 0 end as Focus,
				km.ID as KidID,
				case when kwi.Correct is not null then kwi.Correct else 0 end as Correct,
				case when kwi.ReTest is not null then kwi.ReTest else 0 end as ReTest,
                CAST(NULL AS int) AS LastExamCorrect,
                0 AS ExamTimes
                FROM KidsWorld.dbo.Vocabulary w
				LEFT JOIN KidsWorld.dbo.VocabularyIndex wi ON wi.WordID = w.ID 
				JOIN KidsWorld.dbo.Lession les ON les.ID = w.LessionID
                LEFT JOIN KidsWorld.dbo.KidExamWordIndex kwi ON kwi.ExamID = w.ID
                LEFT JOIN KidsWorld.dbo.KidTestIndex kti ON kti.ID = kwi.KidTestIndexID
                LEFT JOIN KidsWorld.dbo.KidMain km ON km.ID = kti.KidMainID
                WHERE ClassName = @ClassName
		        ORDER BY Correct , ReTest 
            ";

                var result = await conn.QueryAsync<Vocabulary>(sql, sqlparamLegacy);
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

        /// <summary>
        /// 補齊 KidExamWordIndex（當只有 KidTestIndex 但沒有題目明細時）
        /// </summary>
        /// <remarks>
        /// ReTest 的資料來源是 KidExamWordIndex + Vocabulary + KidTestIndex。
        /// 若系統曾在產生考卷時中途失敗，可能會只留下 KidTestIndex，導致 ReTest 查不到任何資料。
        /// 僅在該考卷索引尚無任何明細時才補入；已存在部分題目（例如依 TestNumber 抽題）時不得再補滿整課單字。
        /// </remarks>
        public async Task<int> BackfillKidExamWordIndexAsync(string kidID, DateTime testDate, string testType)
        {
            if (!Guid.TryParse(kidID, out var kidMainId))
                return 0;

            var sqlparam = new DynamicParameters();
            sqlparam.Add("KidMainID", kidMainId);
            sqlparam.Add("TestDate", testDate.Date);
            sqlparam.Add("TestType", (testType ?? string.Empty).ToLower());

            // 只針對「指定孩子 + 指定日期 + 指定 TestType」的考卷索引補明細。
            // 重要：僅在該 KidTestIndex 尚「完全沒有」KidExamWordIndex 明細時才整批補入課程單字。
            // 若考卷刻意只抽部分題（例如 TestNumber），已有明細時不可再補其餘單字，否則 ReTest 會變成整課題數。
            var sql = @"
                ;WITH TargetKidTest AS (
                    SELECT kti.ID, kti.LessionID
                    FROM KidsWorld.dbo.KidTestIndex kti
                    JOIN KidsWorld.dbo.Lession les ON les.ID = kti.LessionID
                    WHERE kti.KidMainID = @KidMainID
                      AND kti.Enabled = 1 AND kti.Deleted = 0
                      AND les.Enabled = 1 AND les.Deleted = 0
                      AND les.TestType = @TestType
                      AND CONVERT(date, kti.TestDate) = @TestDate
                ),
                KidTestNeedingBackfill AS (
                    SELECT t.ID, t.LessionID
                    FROM TargetKidTest t
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM KidsWorld.dbo.KidExamWordIndex kwi
                        WHERE kwi.KidTestIndexID = t.ID
                          AND kwi.Enabled = 1 AND kwi.Deleted = 0
                    )
                )
                INSERT INTO KidsWorld.dbo.KidExamWordIndex
                (
                    ID,
                    ExamID,
                    KidTestIndexID,
                    Correct,
                    ReTest,
                    CreateDate,
                    ModifyDate,
                    Enabled,
                    Deleted
                )
                SELECT
                    NEWID(),
                    v.ID AS ExamID,
                    t.ID AS KidTestIndexID,
                    0 AS Correct,
                    0 AS ReTest,
                    GETDATE(),
                    GETDATE(),
                    1 AS Enabled,
                    0 AS Deleted
                FROM KidTestNeedingBackfill t
                JOIN KidsWorld.dbo.Vocabulary v ON v.LessionID = t.LessionID
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM KidsWorld.dbo.KidExamWordIndex kwi
                    WHERE kwi.KidTestIndexID = t.ID
                      AND kwi.ExamID = v.ID
                      AND kwi.Enabled = 1 AND kwi.Deleted = 0
                );

                SELECT @@ROWCOUNT;
            ";

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                return await conn.QueryFirstOrDefaultAsync<int>(sql, sqlparam);
            }
            catch
            {
                return 0;
            }
        }

        public async Task<List<ExamListModel>> GetExamListAsync()
        {
            var sql = @"
                    SELECT ClassName,LessionSort,TestType,CreateDate
                      FROM KidsWorld.dbo.Lession
                      WHERE Enabled = 1
                      AND Deleted = 0
                      Order by LessionSort desc
            ";


            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.QueryAsync<ExamListModel>(sql);
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
                sql += " AND les.ClassName IN @Class";
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
            if (!string.IsNullOrEmpty(param.TestType))
            {
                sqlparam.Add("TestType", param.TestType);
                sql += $" AND les.TestType = @TestType";
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
                        WHERE 1 = 1
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
            if (!string.IsNullOrEmpty(param.TestType))
            {
                sqlparam.Add("TestType", param.TestType);
                sql += $" AND les.TestType = @TestType";
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
                            ROW_NUMBER() OVER(
                            PARTITION BY CONVERT(DATE, kti.TestDate) 
                            ORDER BY w.ID
                            ) AS RowNum
                            ,les.ClassName
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
/*
SELECT DISTINCT
        wd.seq,
        les.ClassName
      ,les.TestType
      ,wd.ID as WordID
      ,wd.Question
      ,wd.[Answer]
      ,km.Cname
      ,Correct
      ,ReTest
      ,wd.[CategoryType]
  FROM [KidsWorld].[dbo].[Vocabulary] wd
  JOIN [KidsWorld].[dbo].Lession les ON les.ID = wd.LessionID
  JOIN KidsWorld.dbo.KidExamWordIndex wl ON wl.ExamID = wd.ID
  JOIN KidsWorld.dbo.KidTestIndex kti ON kti.ID = wl.KidTestIndexID
  JOIN KidsWorld.dbo.KidMain km ON km.ID = kti.KidMainID
  ORDER BY les.ClassName,wd.seq
*/
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
            if (!string.IsNullOrEmpty(param.TestType))
            {
                sqlparam.Add("TestType", param.TestType);
                sql += $" AND les.TestType = @TestType";
            }
            #endregion
            sql += " ORDER BY TestDate desc,RowNum ";

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
                return new List<ExamMainModel>();
            }

        }

        public async Task<List<(DateTime, string)>> GetTestDateList(string KidID)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("KidMainID", KidID);

            var sql = @"
                      SELECT DISTINCT CONVERT(date, TestDate) as TestDate,ls.ClassName
                      FROM KidsWorld.dbo.KidTestIndex kti
					  JOIN KidsWorld.dbo.Lession ls ON ls.ID = kti.LessionID 
                      WHERE KidMainID = @KidMainID
                      AND kti.Enabled = 1
                      AND kti.Deleted = 0
                      Order by TestDate desc
                        "
            ;

            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.QueryAsync<(DateTime, string)>(sql, sqlparam);
                return result.ToList();

            }
            catch
            {
                return null;
            }
        }

        public async Task<List<DateTime>> GetTestDateOnlyList(string KidID, string testType)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("KidMainID", KidID);
            sqlparam.Add("TestType", (testType ?? string.Empty).ToLower());

            var sql = @"
                SELECT DISTINCT CONVERT(date, kti.TestDate) as TestDate
                FROM KidsWorld.dbo.KidTestIndex kti
                JOIN KidsWorld.dbo.Lession ls ON ls.ID = kti.LessionID
                WHERE kti.KidMainID = @KidMainID
                  AND kti.Enabled = 1 AND kti.Deleted = 0
                  AND ls.Enabled = 1 AND ls.Deleted = 0
                  AND ls.TestType = @TestType
                ORDER BY TestDate desc
            ";

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.QueryAsync<DateTime>(sql, sqlparam);
                return result.ToList();
            }
            catch
            {
                return new List<DateTime>();
            }
        }

        public async Task<List<string>> GetClassNameListByDate(string KidID, DateTime testDate, string testType)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("KidMainID", KidID);
            sqlparam.Add("TestDate", testDate.Date);
            sqlparam.Add("TestType", (testType ?? string.Empty).ToLower());

            var sql = @"
                SELECT DISTINCT ls.ClassName
                FROM KidsWorld.dbo.KidTestIndex kti
                JOIN KidsWorld.dbo.Lession ls ON ls.ID = kti.LessionID
                WHERE kti.KidMainID = @KidMainID
                  AND kti.Enabled = 1 AND kti.Deleted = 0
                  AND ls.Enabled = 1 AND ls.Deleted = 0
                  AND ls.TestType = @TestType
                  AND CONVERT(date, kti.TestDate) = @TestDate
                ORDER BY ls.ClassName
            ";

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.QueryAsync<string>(sql, sqlparam);
                return result.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        public async Task<List<Vocabulary>> GetWrongVocabularyByDate(string kidID, DateTime testDate, string testType)
        {
            if (!Guid.TryParse(kidID, out var kidMainId))
                return new List<Vocabulary>();

            var sqlparam = new DynamicParameters();
            sqlparam.Add("KidMainID", kidMainId);
            sqlparam.Add("TestDate", testDate.Date);
            sqlparam.Add("TestType", (testType ?? string.Empty).ToLower());

            // 只撈「指定孩子 + 指定日期 + 指定 TestType」且 Correct=0（答錯）的題目
            var sql = @"
                SELECT 
                    w.ID as WordID,
                    les.TestType,
                    w.CategoryType,
                    les.ClassName,
                    w.Question,
                    w.Answer,
                    km.ID as KidID,
                    wl.Correct
                FROM KidsWorld.dbo.KidExamWordIndex wl
                LEFT JOIN KidsWorld.dbo.Vocabulary w ON w.ID = wl.ExamID
                LEFT JOIN KidsWorld.dbo.Lession les ON les.ID = w.LessionID
                LEFT JOIN KidsWorld.dbo.KidTestIndex kti ON kti.ID = wl.KidTestIndexID
                LEFT JOIN KidsWorld.dbo.KidMain km ON km.ID = kti.KidMainID
                WHERE wl.Enabled = 1
                  AND wl.Deleted = 0
                  AND kti.Enabled = 1
                  AND kti.Deleted = 0
                  AND km.ID = @KidMainID
                  AND wl.Correct = 0
                  AND les.TestType = @TestType
                  AND CONVERT(date, kti.TestDate) = @TestDate
            ";

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.QueryAsync<Vocabulary>(sql, sqlparam);
                return result?.ToList() ?? new List<Vocabulary>();
            }
            catch
            {
                return new List<Vocabulary>();
            }
        }

        public async Task<List<Vocabulary>> GetWrongVocabularyByKid(string kidID, string testType)
        {
            if (!Guid.TryParse(kidID, out var kidMainId))
                return new List<Vocabulary>();

            var sqlparam = new DynamicParameters();
            sqlparam.Add("KidMainID", kidMainId);
            sqlparam.Add("TestType", (testType ?? string.Empty).ToLower());

            // 撈「指定孩子 + 指定 TestType」且 Correct=0（答錯）的歷史題目（不限定日期）
            // 重要：以 ExamID 去重，避免同一題在不同考卷索引出現多筆影響排序
            var sql = @"
                ;WITH WrongPool AS (
                    SELECT
                        wl.ExamID,
                        MIN(ISNULL(wl.ReTest, 0)) AS ReTest
                    FROM KidsWorld.dbo.KidExamWordIndex wl
                    JOIN KidsWorld.dbo.KidTestIndex kti ON kti.ID = wl.KidTestIndexID
                    JOIN KidsWorld.dbo.KidMain km ON km.ID = kti.KidMainID
                    JOIN KidsWorld.dbo.Vocabulary w ON w.ID = wl.ExamID
                    JOIN KidsWorld.dbo.Lession les ON les.ID = w.LessionID
                    WHERE wl.Enabled = 1 AND wl.Deleted = 0
                      AND kti.Enabled = 1 AND kti.Deleted = 0
                      AND km.ID = @KidMainID
                      AND wl.Correct = 0
                      AND les.TestType = @TestType
                    GROUP BY wl.ExamID
                )
                SELECT
                    w.ID AS WordID,
                    les.TestType,
                    w.CategoryType,
                    les.ClassName,
                    w.Question,
                    w.Answer,
                    @KidMainID AS KidID,
                    0 AS Correct,
                    p.ReTest
                FROM WrongPool p
                JOIN KidsWorld.dbo.Vocabulary w ON w.ID = p.ExamID
                JOIN KidsWorld.dbo.Lession les ON les.ID = w.LessionID
            ";

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                var result = await conn.QueryAsync<Vocabulary>(sql, sqlparam);
                return result?.ToList() ?? new List<Vocabulary>();
            }
            catch
            {
                return new List<Vocabulary>();
            }
        }

        public async Task<int> IncreaseReTestAsync(string kidID, List<Guid> examIDs, string testType)
        {
            if (!Guid.TryParse(kidID, out var kidMainId))
                return 0;
            if (examIDs == null || examIDs.Count == 0)
                return 0;

            var sqlparam = new DynamicParameters();
            sqlparam.Add("KidMainID", kidMainId);
            sqlparam.Add("ExamIDs", examIDs);
            sqlparam.Add("TestType", (testType ?? string.Empty).ToLower());

            // 針對每個 ExamID 更新「最新的一筆」KidExamWordIndex：ReTest + 1
            var sql = @"
                ;WITH Latest AS (
                    SELECT
                        wl.ID,
                        ROW_NUMBER() OVER (
                            PARTITION BY wl.ExamID
                            ORDER BY wl.ModifyDate DESC, wl.CreateDate DESC
                        ) AS rn
                    FROM KidsWorld.dbo.KidExamWordIndex wl
                    JOIN KidsWorld.dbo.KidTestIndex kti ON kti.ID = wl.KidTestIndexID
                    JOIN KidsWorld.dbo.KidMain km ON km.ID = kti.KidMainID
                    JOIN KidsWorld.dbo.Vocabulary w ON w.ID = wl.ExamID
                    JOIN KidsWorld.dbo.Lession les ON les.ID = w.LessionID
                    WHERE wl.Enabled = 1 AND wl.Deleted = 0
                      AND kti.Enabled = 1 AND kti.Deleted = 0
                      AND km.ID = @KidMainID
                      AND wl.ExamID IN @ExamIDs
                      AND les.TestType = @TestType
                )
                UPDATE wl
                SET wl.ReTest = ISNULL(wl.ReTest, 0) + 1,
                    wl.ModifyDate = GETDATE()
                FROM KidsWorld.dbo.KidExamWordIndex wl
                JOIN Latest l ON l.ID = wl.ID
                WHERE l.rn = 1;

                SELECT @@ROWCOUNT;
            ";

            using var conn = new SqlConnection(_dBList.erp);
            try
            {
                return await conn.QueryFirstOrDefaultAsync<int>(sql, sqlparam);
            }
            catch
            {
                return 0;
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
            if (!string.IsNullOrEmpty(param.TestType))
            {
                sqlparam.Add("TestType", param.TestType);
                sql += $" AND les.TestType = @TestType";
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

        public async Task<bool> InsertExamIndex(ExamRcdModel param)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("ExamID", param.WordID);
            sqlparam.Add("KidTestIndexID", param.NewKidTestID);

            var sql = @"
                    INSERT INTO KidsWorld.dbo.KidExamWordIndex
                               (
		                       ID
                               ,ExamID
                               ,KidTestIndexID
                               ,Correct
                               ,ReTest
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
                               ,1
                               ,0
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

        public async Task<ExamRcdModel> GetExamRcd(Guid WordID)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("ExamID", WordID);
            var sql = @"
                        SELECT
	                           Correct
                              ,ReTest
                          FROM KidsWorld.dbo.KidExamWordIndex
                          WHERE ExamID = @ExamID
                          AND Enabled = 1
                          AND Deleted = 0
                          Order by ReTest desc
                        "
            ;

            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.QueryFirstOrDefaultAsync<ExamRcdModel>(sql, sqlparam);
                return result;

            }
            catch
            {
                return null;
            }
        }
    }
}
