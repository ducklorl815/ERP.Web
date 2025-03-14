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

        public async Task<List<Vocabulary>> GetExamDataAsync(string ClassName, int ClassNum)
        {
            var listVocabulary = await _examRepo.GetExamDataAsync(ClassName, ClassNum);

            // 依 ClassNum 降序排列（最新的在前）
            var groupedByClass = listVocabulary
                .GroupBy(x => x.ClassNum)
                .OrderByDescending(g => g.Key)
                .ToList();

            // 設定權重（依課程遠近分配）
            var distribution = new Dictionary<int, double>
            {
                { 0, 0.8 }, // 目前課程
                { 1, 0.3 }, // 前一個課程
                { 2, 0.2 }, // 前前一個課程
                { 3, 0.1 }  // 其餘課程
            };

            int totalWordQuestions = 20;   // 單字 10 題
            int totalPhraseQuestions = 20; // 片語 10 題

            List<Vocabulary> finalWordQuestions = new List<Vocabulary>();
            List<Vocabulary> finalPhraseQuestions = new List<Vocabulary>();

            int usedWordQuestions = 0;
            int usedPhraseQuestions = 0;

            // 先處理前三個課程（依據 distribution 權重）
            for (int i = 0; i < Math.Min(3, groupedByClass.Count); i++)
            {
                double percentage = distribution[i];
                int wordCount = (int)Math.Round(totalWordQuestions * percentage);
                int phraseCount = (int)Math.Round(totalPhraseQuestions * percentage);
                usedWordQuestions += wordCount;
                usedPhraseQuestions += phraseCount;

                var words = groupedByClass[i].Where(x => x.Type == "Word").ToList();
                var phrases = groupedByClass[i].Where(x => x.Type == "Phrase").ToList();

                if (words.Count > wordCount)
                    finalWordQuestions.AddRange(words.OrderBy(x => Guid.NewGuid()).Take(wordCount));
                else
                    finalWordQuestions.AddRange(words);

                if (phrases.Count > phraseCount)
                    finalPhraseQuestions.AddRange(phrases.OrderBy(x => Guid.NewGuid()).Take(phraseCount));
                else
                    finalPhraseQuestions.AddRange(phrases);
            }

            // 剩餘課程（全部佔 10%）
            int remainingWordQuestions = totalWordQuestions - usedWordQuestions;
            int remainingPhraseQuestions = totalPhraseQuestions - usedPhraseQuestions;

            var olderWords = groupedByClass.Skip(3).SelectMany(g => g).Where(x => x.Type == "單字_Word").ToList();
            var olderPhrases = groupedByClass.Skip(3).SelectMany(g => g).Where(x => x.Type == "片語_Phrase").ToList();

            if (olderWords.Count > remainingWordQuestions)
                finalWordQuestions.AddRange(olderWords.OrderBy(x => Guid.NewGuid()).Take(remainingWordQuestions));
            else
                finalWordQuestions.AddRange(olderWords);

            if (olderPhrases.Count > remainingPhraseQuestions)
                finalPhraseQuestions.AddRange(olderPhrases.OrderBy(x => Guid.NewGuid()).Take(remainingPhraseQuestions));
            else
                finalPhraseQuestions.AddRange(olderPhrases);

            // 合併兩個題目清單，隨機排序
            var finalQuestions = finalWordQuestions.Concat(finalPhraseQuestions)
                                                   .OrderBy(x => Guid.NewGuid())
                                                   .ToList();

            return finalQuestions.OrderByDescending(x => x.Type).ToList();
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
                            string Type = worksheet.Cells[row, 1].Text.Trim();
                            string Question = worksheet.Cells[row, 2].Text.Trim();
                            string Answer = worksheet.Cells[row, 3].Text.Trim();

                            if (!string.IsNullOrEmpty(Question) && !string.IsNullOrEmpty(Answer))
                            {
                                vocabularies.Add(new Vocabulary
                                {
                                    Type = Type,
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
