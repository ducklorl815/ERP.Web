
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
        public async Task<bool> chkSameWord(string Word)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Word", Word);
            var sql = @"
                        SELECT TOP 1 1 
                          FROM KidsWorld.dbo.Vocabulary
                          WHERE Word = @Word
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

        public async Task<List<Vocabulary>> GetExamDataAsync(int Class)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Class", Class);
            var sql = @"
                    DECLARE @targetClass INT = @Class;

                    SELECT Class, Word, Meaning
                    FROM KidsWorld.dbo.Vocabulary
                    WHERE @Class BETWEEN @targetClass - 10;
                        "
            ;

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

        public async Task<bool> InsertWord(Vocabulary vocab)
        {
            var sqlparam = new DynamicParameters();
            sqlparam.Add("Word", vocab.Word);
            sqlparam.Add("Class", vocab.Class);
            sqlparam.Add("Meaning", vocab.Meaning);
            var sql = @"
                    INSERT INTO KidsWorld.dbo.Vocabulary
                               (
                                ID
                               ,Class
                               ,Word
                               ,Meaning
                                )
                         VALUES
                               (
                                newID()
                               ,@Class
                               ,@Word
                               ,@Meaning
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

        public async Task SaveToDatabase(Vocabulary param)
        {
            throw new NotImplementedException();
        }
    }
}
