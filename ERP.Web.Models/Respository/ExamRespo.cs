using Dapper;
using ERP.Web.Models.Models;
using ERP.Web.Utility.Models;
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

        public async Task<List<Vocabulary>> GetExamDataAsync(string ClassName, int ClassNum)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("ClassName", ClassName);
            sqlparam.Add("ClassNum", ClassNum);

            var sql = @"
                DECLARE @targetClass INT = @ClassNum;
                DECLARE @minRange INT = CASE WHEN @targetClass - 10 < 0 THEN 0 ELSE @targetClass - 10 END; 
                DECLARE @MaxRange INT = @targetClass + 10; 

                SELECT ID,Type, ClassNum, ClassName, Category, Question, Answer
                FROM KidsWorld.dbo.Vocabulary
                WHERE ClassName = @ClassName
                AND ClassNum BETWEEN @minRange AND @MaxRange;
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

    }
}
