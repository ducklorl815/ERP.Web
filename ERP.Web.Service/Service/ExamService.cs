using ERP.Web.Models.Models;
using ERP.Web.Models.Respository;
using ERP.Web.Service.ViewModels;
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
        public async Task<ExamSearchListViewModel_result> GetListAsync()
        {
            var result = new ExamSearchListViewModel_result();
            result.ClassNameList = await _examRepo.GetExamListAsync();
            return result;
        }
        public async Task<ExamDataViewModel_result> GetExamDataAsync(string ClassName, int ClassNum, string Category)
        {
            var result = new ExamDataViewModel_result
            {
                VocabularyList = new List<Vocabulary>()
            };
            var listVocabulary = await _examRepo.GetExamDataAsync(ClassName, ClassNum);

            // 依 ClassNum 降序排列（最新的在前）
            var groupedByClass = listVocabulary
                .GroupBy(x => new { x.ClassNum, x.Category }) // 依 ClassNum 和 Category 分組
                .OrderByDescending(g => g.Key.ClassNum == ClassNum) // 讓指定 ClassNum 優先
                .ThenByDescending(g => g.Key.Category == Category) // 其他 Category 維持降序
                .ThenByDescending(g => g.Key.ClassNum) // 其他 ClassNum 維持降序
                .ThenBy(g => g.Key.Category)// 在相同 ClassNum 下依 Category 升序排列
                .ToList();

            // 設定權重（依課程遠近分配）
            var distribution = new Dictionary<int, double>
            {
                { 0, 1 },   // 目前課程
                { 1, 0.3 }, // 前一個課程
                { 2, 0.2 }, // 前前一個課程
                { 3, 0.1 }  // 其餘課程
            };

            int totalWordQuestions = 20;
            int totalPhraseQuestions = 16;

            List<Vocabulary> finalWordQuestions = new List<Vocabulary>();
            List<Vocabulary> finalPhraseQuestions = new List<Vocabulary>();

            // 先處理前三個課程（依據 distribution 權重）
            for (int i = 0; i < Math.Min(3, groupedByClass.Count); i++)
            {
                double percentage = distribution[i];
                int wordCount = (int)Math.Round(totalWordQuestions * percentage);
                int phraseCount = (int)Math.Round(totalPhraseQuestions * percentage);

                var words = groupedByClass[i].Where(x => x.Type == "Word").OrderBy(x => Guid.NewGuid()).ToList();
                var phrases = groupedByClass[i].Where(x => x.Type == "Phrase").OrderBy(x => Guid.NewGuid()).ToList();

                finalWordQuestions.AddRange(words.Take(wordCount));
                finalPhraseQuestions.AddRange(phrases.Take(phraseCount));
            }

            // 如果題目不足 15 題，補滿
            if (finalWordQuestions.Count < totalWordQuestions)
            {
                var remainingWords = listVocabulary.Where(x => x.Type == "Word" && !finalWordQuestions.Contains(x))
                                                   .OrderBy(x => Guid.NewGuid())
                                                   .ToList();
                finalWordQuestions.AddRange(remainingWords);
            }

            if (finalPhraseQuestions.Count < totalPhraseQuestions)
            {
                var remainingPhrases = listVocabulary.Where(x => x.Type == "Phrase" && !finalPhraseQuestions.Contains(x))
                                                     .OrderBy(x => Guid.NewGuid())
                                                     .ToList();
                finalPhraseQuestions.AddRange(remainingPhrases);
            }

            // **嚴格限制單字和片語各 15 題**
            finalWordQuestions = finalWordQuestions.Take(totalWordQuestions).ToList();
            finalPhraseQuestions = finalPhraseQuestions.Take(totalPhraseQuestions).ToList();

            // **最終合併並隨機排序**
            result.VocabularyList = finalWordQuestions.Concat(finalPhraseQuestions)
                                     .OrderBy(x => Guid.NewGuid())
                                     .ToList();

            return result;
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

                        string Class = worksheet.Cells[1, 1].Text; // 以工作表名稱作為課程名稱
                        string Category = string.Empty;
                        List<string> ClassArrey = new List<string>();
                        if (Class.Contains("Sp"))
                        {
                            ClassArrey = Class.Split("Sp").ToList();
                            Category = "Sp";
                        }
                        if (Class.Contains("HW"))
                        {
                            ClassArrey = Class.Split("HW").ToList();
                            Category = "HW";
                        }

                        var ClassName = ClassArrey[0];
                        var ClassNumChk = ClassArrey[1].Trim();
                        if (!int.TryParse(ClassNumChk, out int ClassNum))
                            return false;

                        for (int row = 2; row <= rowCount; row++) // 從第 2 行開始，因為第 1 行是標題
                        {
                            string Type = worksheet.Cells[row, 1].Text.Trim();
                            string Question = worksheet.Cells[row, 2].Text.Trim();
                            string Answer = worksheet.Cells[row, 3].Text.Trim();

                            if (!string.IsNullOrEmpty(Question) && !string.IsNullOrEmpty(Answer))
                            {
                                vocabularies.Add(new Vocabulary
                                {
                                    Type = Type,
                                    ClassNum = ClassNum,
                                    Category = Category,
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
                    bool checkWord = await _examRepo.chkSameWord(vocab);
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
