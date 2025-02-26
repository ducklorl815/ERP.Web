using ERP.Web.Models.Models;
using ERP.Web.Models.Respository;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;

namespace ERP.Web.Service.Service
{
    public class ExamService
    {
        private readonly ExamRepo _examRepo;
        public ExamService(
            ExamRepo examRepo
            ) 
        { 
            _examRepo = examRepo;
        }
        public async Task GetUploadFileAsync(IFormFile file)
        {
            try
            {
                var vocabularies = new List<Vocabulary>();

                using (var stream = new MemoryStream())
                {
                    file.CopyTo(stream);
                    using (var package = new ExcelPackage(stream))
                    {
                        var worksheet = package.Workbook.Worksheets[0]; // 讀取第一個工作表
                        int rowCount = worksheet.Dimension.Rows;

                        for (int row = 2; row <= rowCount; row++) // 從第 2 行開始，因為第 1 行是標題
                        {
                            int course = int.Parse(worksheet.Cells[row, 1].Text.Trim());
                            string word = worksheet.Cells[row, 2].Text.Trim();
                            string meaning = worksheet.Cells[row, 3].Text.Trim();

                            if (!string.IsNullOrEmpty(word) && !string.IsNullOrEmpty(meaning))
                            {
                                vocabularies.Add(new Vocabulary
                                {
                                    Class = course,
                                    Word = word,
                                    Meaning = meaning
                                });
                            }
                        }
                    }
                }

                // 存入資料庫
                await _examRepo.SaveToDatabase(vocabularies);

                return Ok("Excel 資料成功匯入");
            }
            catch (Exception ex)
            {
                return StatusCode(500, "發生錯誤：" + ex.Message);
            }
        }
    }
}
