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

        public async Task<Guid> ChkKidTest(string Class, string TestType)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Class", Class);
            sqlparam.Add("TestType", TestType);

            var sql = @"
                    SELECT ID
                    FROM KidsWorld.dbo.KidTestIndex
                    WHERE Class = @Class
                    AND TestType = @TestType
                    AND CONVERT(DATE, TestDate) = CONVERT(DATE, GETDATE());
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
            var sql = @"
                        SELECT TOP 1 1 
                          FROM KidsWorld.dbo.Vocabulary
                          WHERE Question = @Question
                          AND Answer = @Answer
                          AND ClassName = @ClassName
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

                SELECT Type, ClassNum, ClassName, Category, Question, Answer
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

        public async Task<List<Vocabulary>> GetExamFromExamIndex(Guid KidTestID)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("KidTestID", KidTestID);

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
                WHERE kt.ID = @KidTestID 
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
            var sqlparam = new DynamicParameters();

            var sql = @"
                SELECT DISTINCT TOP 1000 ClassName  + ' ' + Category + ' ' + ClassNum as ClassName
                  FROM KidsWorld.dbo.Vocabulary
            ";


            using var conn = new SqlConnection(_dBList.erp);

            try
            {
                var result = await conn.QueryAsync<string>(sql, sqlparam);
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
                    INSERT INTO dbo.KidExamWordIndex
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

        public async Task<Guid> InsertKidTestIndex(string Class, string TestType)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Class", Class);
            sqlparam.Add("TestType", TestType);

            var sql = @"
                    INSERT INTO dbo.KidTestIndex
                               (ID
                               ,Class
                               ,TestType
                               ,TestDate
                               ,Enabled
                               ,Deleted)
                         OUTPUT INSERTED.ID
                         VALUES
                               (
		                       NEWID()
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
