using ERP.Web.Models.Models;
using ERP.Web.Models.Respository;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;

namespace ERP.Web.Service.Service
{
    public class ExamService
    {
        private readonly ExamRespo _examRepo;
        public ExamService(
            ExamRespo examRepo
            )
        {
            _examRepo = examRepo;
        }
        public async Task GetUploadFileAsync(IFormFile file)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // 設定 EPPlus 授權

            var vocabularies = new List<Vocabulary>();
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheets = package.Workbook.Worksheets;
                    if (worksheets.Count <= 1) return; // 如果只有一個 (範本)，則不做任何處理
                    // **跳過第一個工作表**
                    for (int i = 1; i < worksheets.Count; i++)
                    {
                        var worksheet = worksheets[i]; // 從第二個工作表開始讀取

                        if (worksheet.Dimension == null) continue; // 略過空的工作表

                        int rowCount = worksheet.Dimension.Rows;
                        string className = worksheet.Name; // 以工作表名稱作為課程名稱

                        for (int row = 2; row <= rowCount; row++) // 從第 2 行開始，因為第 1 行是標題
                        {
                            string word = worksheet.Cells[row, 1].Text.Trim();
                            string meaning = worksheet.Cells[row, 2].Text.Trim();

                            if (!string.IsNullOrEmpty(word) && !string.IsNullOrEmpty(meaning))
                            {
                                vocabularies.Add(new Vocabulary
                                {
                                    Class = i, // 以工作表名稱作為課程名稱
                                    Word = word,
                                    Meaning = meaning
                                });
                            }
                        }
                    }
                }
            }

            // 儲存到資料庫
            await SaveToDatabase(vocabularies);
        }

        public async Task<bool> SaveToDatabase(List<Vocabulary> vocabularies)
        {
            try
            {
                foreach (var vocab in vocabularies)
                {
                    // 檢查是否已有相同單字
                    bool checkWord = await _examRepo.chkSameWord(vocab.Word);
                    if (!checkWord)
                        await _examRepo.InsertWord(vocab);
                }
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }
    }
}
