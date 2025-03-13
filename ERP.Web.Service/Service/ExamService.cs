using ERP.Web.Models.Models;
using ERP.Web.Models.Respository;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.Security.Claims;

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

        public async Task<List<Vocabulary>> GetExamDataAsync(string ClassName, int ClassNum)
        {
            var listVocabulary = await _examRepo.GetExamDataAsync(ClassName, ClassNum);

            // 按 ClassNum 降序排列（最新的在前）
            var groupedByClass = listVocabulary
                .GroupBy(x => x.ClassNum)
                .OrderByDescending(g => g.Key)
                .ToList();

            // 設定權重
            var distribution = new Dictionary<int, double>
            {
                { 0, 0.6 }, // 目前課程
                { 1, 0.2 }, // 前一個課程
                { 2, 0.1 }  // 前前一個課程
            };

            int totalQuestions = 100;
            List<Vocabulary> finalQuestions = new List<Vocabulary>();

            // 先處理前三個課程
            int usedQuestions = 0;
            for (int i = 0; i < Math.Min(3, groupedByClass.Count); i++)
            {
                double percentage = distribution[i];
                int questionCount = (int)Math.Round(totalQuestions * percentage);
                usedQuestions += questionCount;

                var availableQuestions = groupedByClass[i].ToList();
                if (availableQuestions.Count > questionCount)
                {
                    finalQuestions.AddRange(availableQuestions.OrderBy(x => Guid.NewGuid()).Take(questionCount));
                }
                else
                {
                    finalQuestions.AddRange(availableQuestions);
                }
            }

            // 剩餘的課程（全部佔 10%）
            int remainingQuestions = totalQuestions - usedQuestions;
            var olderClasses = groupedByClass.Skip(3).SelectMany(g => g).ToList();

            if (olderClasses.Count > remainingQuestions)
            {
                finalQuestions.AddRange(olderClasses.OrderBy(x => Guid.NewGuid()).Take(remainingQuestions));
            }
            else
            {
                finalQuestions.AddRange(olderClasses); // 題目不足就全拿
            }

            return finalQuestions;
        }



        public async Task<bool> GetUploadFileAsync(IFormFile file)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial; // 設定 EPPlus 授權

            var vocabularies = new List<Vocabulary>();
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheets = package.Workbook.Worksheets;
                    if (worksheets.Count <= 1) return false; // 如果只有一個 (範本)，則不做任何處理
                    // **跳過第一個工作表**
                    for (int i = 1; i < worksheets.Count; i++)
                    {
                        var worksheet = worksheets[i]; // 從第二個工作表開始讀取

                        if (worksheet.Dimension == null) continue; // 略過空的工作表

                        int rowCount = worksheet.Dimension.Rows;

                        string Class = worksheet.Name; // 以工作表名稱作為課程名稱
                        var ClassArrey = Class.Split("Sp");
                        var ClassName = ClassArrey[0];
                        var ClassNumChk = ClassArrey[1].Trim();
                        if (!int.TryParse(ClassNumChk, out int ClassNum))
                            return false;

                        for (int row = 2; row <= rowCount; row++) // 從第 2 行開始，因為第 1 行是標題
                        {
                            string Question = worksheet.Cells[row, 2].Text.Trim();
                            string Answer = worksheet.Cells[row, 3].Text.Trim();

                            if (!string.IsNullOrEmpty(Question) && !string.IsNullOrEmpty(Answer))
                            {
                                vocabularies.Add(new Vocabulary
                                {
                                    ClassNum = ClassNum,
                                    ClassName = ClassName,
                                    Question = Question,
                                    Answer = Answer
                                });
                            }
                        }
                    }
                }
            }

            // 儲存到資料庫
            var result = await SaveToDatabase(vocabularies);
            return vocabularies.Count > 0 ? await SaveToDatabase(vocabularies) : false;
        }

        public async Task<bool> SaveToDatabase(List<Vocabulary> vocabularies)
        {
            try
            {
                foreach (var vocab in vocabularies)
                {
                    // 檢查是否已有相同單字
                    bool checkWord = await _examRepo.chkSameWord(vocab.Question);
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
