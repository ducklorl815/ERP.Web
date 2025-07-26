using ERP.Web.Models.Models;
using ERP.Web.Models.Respository;
using ERP.Web.Service.ViewModels;
using ERP.Web.Utility.Paging;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using System.Web.Mvc;

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
        public async Task<ExamSearchListViewModel_result> GetNewTestAsync(ExamSearchListViewModel_param param)
        {
            var result = new ExamSearchListViewModel_result();
            //分頁功能
            var ExamKeyword = new ExamMainKeyword
            {
                ClassNameList = param.ClassNameList,
                CorrectType = param.CorrectType,
            };

            var datacount = await _examRepo.GetNewTestCountAsync(ExamKeyword);
            var pager = new Paging(param.Page, param.PageSize, datacount);
            result.Pager = pager;
            result.ExamDataList = await _examRepo.GetNewTestListAsync(pager, ExamKeyword);

            await PublicTaskAsync(result, param);

            return result;
        }
        public async Task<ExamSearchListViewModel_result> GetReTestAsync(ExamSearchListViewModel_param param)
        {
            var result = new ExamSearchListViewModel_result()
            {
                TestDateList = new List<SelectListItem>(),
                TestDate = param.TestDate
            };
            //分頁功能
            var ExamKeyword = new ExamMainKeyword
            {
                ClassNameList = param.ClassNameList,
                CorrectType = param.CorrectType,
                KidID = param.KidID,
            };
            if (param.TestDate != null)
                ExamKeyword.TestDate = DateTime.Parse(param.TestDate);

            var datacount = await _examRepo.GetReTestCountAsync(ExamKeyword);
            var pager = new Paging(param.Page, datacount != 0 ? datacount : param.PageSize, datacount);

            result.Pager = pager;
            result.ExamDataList = await _examRepo.GetReTestSearchListAsync(pager, ExamKeyword);
            var kidListTask = await _examRepo.GetKidListAsync();
            var testDateListTask = await _examRepo.GetTestDateList(param.KidID);
            var classNameListTask = await _examRepo.GetExamListAsync();

            //await Task.WhenAll(kidListTask, testDateListTask, classNameListTask);

            result.KidList = kidListTask
                .Select(x => new SelectListItem
                {
                    Text = x.Item2,
                    Value = x.Item1.ToString().Trim()
                }).ToList();

            result.TestDateList = testDateListTask
                .Select(x => new SelectListItem
                {
                    Text = x.Date.ToString("yyyy-MM-dd"),
                    Value = x.Date.ToString("yyyy-MM-dd")
                }).ToList();

            result.ClassNameList = classNameListTask;
            //await PublicTaskAsync(result, param);

            return result;
        }
        public async Task<ExamDataViewModel_result> GetReExamDataAsync(ReExamSearchListViewModel_param param)
        {

            var result = new ExamDataViewModel_result
            {
                scoreTable = new ScoreTable(),
                VocabularyList = new List<Vocabulary>(),
                Title = string.Empty
            };
            result.Title = param.ClassName;

            // 判斷今天是否出過考券
            Guid KidTestIndexID = await _examRepo.ChkKidTest(result.Title, param.TestType, param.KidID);

            if (KidTestIndexID != Guid.Empty)
            {
                // 取得今天出過的考試資料
                result.VocabularyList = await _examRepo.GetExamFromExamIndex(KidTestIndexID);
                await CalculateScore(result.VocabularyList, result);
                return result;
            }
            var VocabularyList = new List<Vocabulary>();
            //取得考試資料
            foreach (var item in param.selectedWordIDs)
            {
                var Vocabulary = await _examRepo.GetReExamVocab(item);
                VocabularyList.Add(Vocabulary);
            }

            result.VocabularyList = VocabularyList.OrderByDescending(x => Guid.NewGuid())
                .OrderByDescending(x => x.CategoryType.ToLower() == "word")
                .ToList();

            await CalculateScore(result.VocabularyList, result);

            int LessionSort = await _examRepo.GetLessionSort();
            var LessionData = new LessionModel
            {
                ClassName = param.ClassName,
                TestType = param.TestType,
                LessionSort = LessionSort,
            };
            Guid LessionID = await _examRepo.InsertLessionID(LessionData);

            // 出過的題目存入資料庫
            Guid NewKidTestID = await _examRepo.InsertKidTestIndex(LessionID, param.KidID);

            foreach (var word in result.VocabularyList)
            {
                await _examRepo.InsertExamIndex(word.WordID, NewKidTestID);
            }

            return result;
        }
        public async Task<ExamDataViewModel_result> GetExamDataAsync(ExamSearchListViewModel_param param)
        {

            var result = new ExamDataViewModel_result
            {
                scoreTable = new ScoreTable(),
                VocabularyList = new List<Vocabulary>(),
                Title = string.Empty
            };
            result.Title = param.ClassNameList.Count() > 1 ? "聯合試題_" + DateTime.Now.ToString("yyyyMMdd") : param.ClassNameList[0];

            // 判斷今天是否出過考券
            Guid KidTestIndexID = await _examRepo.ChkKidTest(result.Title, param.TestType, param.KidID);

            if (KidTestIndexID != Guid.Empty)
            {
                // 取得今天出過的考試資料
                result.VocabularyList = await _examRepo.GetExamFromExamIndex(KidTestIndexID);
                await CalculateScore(result.VocabularyList, result);
                return result;
            }

            //取得考試資料
            result.VocabularyList = await GetExamData(param);

            await CalculateScore(result.VocabularyList, result);

            Guid LessionID = await _examRepo.GetLessionID(result.Title);

            // 出過的題目存入資料庫
            Guid NewKidTestID = await _examRepo.InsertKidTestIndex(LessionID, param.KidID);

            foreach (var word in result.VocabularyList)
            {
                await _examRepo.InsertExamIndex(word.WordID, NewKidTestID);
            }

            return result;
        }
        /// <summary>
        /// 取得考試資料
        /// </summary>
        /// <param name="Class"></param>
        /// <returns></returns>
        private async Task<List<Vocabulary>> GetExamData(ExamSearchListViewModel_param param)
        {

            List<Vocabulary> finalQuestions = new List<Vocabulary>();
            switch (param.TestType)
            {
                case "English":
                    finalQuestions = await GetEnglishExamData(param);
                    return finalQuestions;
                case "Math":
                    return finalQuestions;
                default:
                    return finalQuestions;
            }

        }

        private async Task<List<Vocabulary>> GetEnglishExamData(ExamSearchListViewModel_param param)
        {
            List<Vocabulary> finalQuestions = new List<Vocabulary>(), listVocabulary = new List<Vocabulary>();
            string FirstClassName = string.Empty;

            for (int i = 0; i < param.ClassNameList.Count(); i++)
            {
                string ClassName = param.ClassNameList[i];

                // 若需要紀錄第一筆的 ClassName 跟 Num，可這樣做（可選）
                if (i == 0)
                {
                    FirstClassName = ClassName;
                }

                var NewVocabulary = await _examRepo.GetExamDataAsync(ClassName);

                listVocabulary.AddRange(NewVocabulary);
            }

            // 依 ClassNum 降序排列（最新的在前）
            var groupedByClass = listVocabulary
                .GroupBy(x => new { x.ClassName, x.ClassNum, x.Category }) // 依 ClassNum 和 Category 分組
                .OrderByDescending(g => g.Key.ClassName == FirstClassName)
                .ToList();

            // 設定權重（依課程遠近分配）
            var distribution = Enumerable.Range(0, groupedByClass.Count()).ToDictionary(i => i,
                                i => (param.ClassNameList.Count() == 1 && i == 0) ? 1.0 : 1 / groupedByClass.Count());

            int totalQuestions = param.ClassNameList.Count() == 1 ? groupedByClass[0].Count() : 30;


            // 先處理前三個課程（依據 distribution 權重）
            for (int i = 0; i < Math.Min(4, groupedByClass.Count); i++)
            {
                double percentage = distribution[i];
                int totalCount = (int)Math.Round(totalQuestions * percentage);

                var words = groupedByClass[i].ToList();

                // i = 0 時，不過濾 Correct，其他情況下過濾 Correct > 0，並依 Correct 降序排列
                if (i != 0)
                {
                    words = words.Where(x => x.Correct > 0 && x.KidID == Guid.Parse(param.KidID))
                                 .OrderByDescending(x => x.Correct) // 優先拿 Correct 較大的
                                 .OrderByDescending(y => y.Focus)
                                 .ToList();
                }

                // 隨機排序後取前 totalCount 筆
                var selectedWords = words.OrderBy(x => Guid.NewGuid()).Take(totalCount).ToList();

                // 確保不超過 totalQuestions
                int remainingSlots = totalQuestions - finalQuestions.Count;
                finalQuestions.AddRange(selectedWords.Take(remainingSlots));

                // 題目滿 20 題就跳出
                if (finalQuestions.Count >= totalQuestions) break;
            }

            // 如果題目不足 20 題，補滿
            if (finalQuestions.Count < totalQuestions)
            {
                int remainingSlots = totalQuestions - finalQuestions.Count;
                var remainingWords = listVocabulary.Where(x => !finalQuestions.Contains(x))
                                                   .OrderBy(x => Guid.NewGuid())
                                                   .Take(remainingSlots) // 只補足夠的數量
                                                   .ToList();

                finalQuestions.AddRange(remainingWords);
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
                        string ClassName = worksheet.Cells[1, 1].Text; // 以工作表名稱作為課程名稱
                        string ChkDone = worksheet.Cells[1, 4].Text.Trim().ToLower();
                        string Grade = worksheet.Cells[1, 5].Text.Trim().ToLower();
                        string TestType = worksheet.Cells[1, 6].Text.Trim().ToLower();

                        if (ChkDone == "done")
                            continue;

                        for (int row = 2; row <= rowCount; row++) // 從第 2 行開始，因為第 1 行是標題
                        {
                            string CategoryType = worksheet.Cells[row, 1].Text.Trim();
                            string Question = worksheet.Cells[row, 2].Text.Trim();
                            string Answer = worksheet.Cells[row, 3].Text.Trim();

                            if (!string.IsNullOrEmpty(Question) && !string.IsNullOrEmpty(Answer))
                            {
                                vocabularies.Add(new Vocabulary
                                {
                                    CategoryType = CategoryType,
                                    ClassName = ClassName,
                                    TestType = TestType,
                                    Question = Question,
                                    Answer = Answer
                                });
                            }
                        }
                    }
                }
            }

            // 儲存到資料庫
            return vocabularies.Count > 0 ? await SaveToDatabase(vocabularies) : false;
        }

        public async Task<bool> SaveToDatabase(List<Vocabulary> vocabularies)
        {
            try
            {

                foreach (var vocab in vocabularies)
                {
                    #region debug
                    //await _examRepo.chkUpdateWord(vocab);
                    #endregion
                    Guid LessionID = await GetLessionID(vocab.ClassName, vocab.TestType);
                    if (LessionID == Guid.Empty)
                        return false;
                    // 檢查是否已有相同單字
                    bool checkWord = await _examRepo.chkSameWord(vocab);

                    if (!checkWord)
                    {
                        vocab.LessionID = LessionID;
                        await _examRepo.InsertWord(vocab);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        private async Task<Guid> GetLessionID(string ClassName, string TestType)
        {
            Guid LessionID = await _examRepo.GetLessionID(ClassName);
            if (LessionID != Guid.Empty)
            {
                return LessionID;
            }
            int LessionSort = await _examRepo.GetLessionSort();
            var LessionData = new LessionModel
            {
                ClassName = ClassName,
                TestType = TestType,
                LessionSort = LessionSort,
            };
            LessionID = await _examRepo.InsertLessionID(LessionData);
            return LessionID;
        }

        public async Task<bool> UpdateExamWord(ExamSearchListViewModel_param param)
        {
            if (string.IsNullOrEmpty(param.WordID))
                return false;

            bool ChkUpdate = await _examRepo.UpdateExamWord(param.WordID, param.KidID, param.TestDate, param.Correct);

            return ChkUpdate;
        }
        public async Task<bool> UpdateNewTestWord(ExamSearchListViewModel_param param)
        {
            if (string.IsNullOrEmpty(param.WordID))
                return false;

            bool ChkFocusInsert = await _examRepo.ChkVocabIndex(param.WordID, param.KidID);
            bool ChkFocusUpdate = ChkFocusInsert ? await _examRepo.UpdateFocusWord(param.WordID, param.KidID, param.Focus) : await _examRepo.InsertFocusWord(param.WordID, param.KidID, param.Focus);
            bool ChkUpdateWord = false;
            if (!string.IsNullOrEmpty(param.Question) && !string.IsNullOrEmpty(param.Answer))
                ChkUpdateWord = await _examRepo.UpdateWord(param.WordID, param.Question, param.Answer);

            return (ChkFocusUpdate && ChkUpdateWord);
        }

        private async Task PublicTaskAsync(ExamSearchListViewModel_result result, ExamSearchListViewModel_param param)
        {
            // 並行非同步請求
            var kidListTask = _examRepo.GetKidListAsync();
            var testDateListTask = _examRepo.GetTestDateList(param.KidID);
            var classNameListTask = _examRepo.GetExamListAsync();

            await Task.WhenAll(kidListTask, testDateListTask, classNameListTask);

            result.KidList = kidListTask.Result
                .Select(x => new SelectListItem
                {
                    Text = x.Item2,
                    Value = x.Item1.ToString().Trim()
                }).ToList();

            result.TestDateList = testDateListTask.Result
                .Select(x => new SelectListItem
                {
                    Text = x.Date.ToString("yyyy-MM-dd"),
                    Value = x.Date.ToString("yyyy-MM-dd")
                }).ToList();

            result.ClassNameList = classNameListTask.Result;
        }
        public async Task<ExamDataViewModel_result> CalculateScore(List<Vocabulary> vocabularyList, ExamDataViewModel_result result)
        {
            if (vocabularyList == null || vocabularyList.Count == 0)
                throw new ArgumentException("Vocabulary list cannot be empty.");

            // 題型分類
            int wordCount = vocabularyList.Count(x => x.CategoryType.Equals("word", StringComparison.OrdinalIgnoreCase));
            int phraseCount = vocabularyList.Count(x => x.CategoryType.Equals("phrase", StringComparison.OrdinalIgnoreCase));
            int MentalMathCount = vocabularyList.Count(x => x.CategoryType.Equals("MentalMath", StringComparison.OrdinalIgnoreCase));
            int totalCount = wordCount + phraseCount + MentalMathCount;

            int wordScore = 0;
            int phraseScore = 0;
            int MentalMathScore = 0;

            if (MentalMathCount > 0)
            {
                MentalMathScore = RoundToEven(result.scoreTable.totalScore / MentalMathCount);
            }

            //若只有單一題型，平均分數
            if (wordCount == 0 && phraseCount > 0)
            {
                phraseScore = RoundToEven(result.scoreTable.totalScore / phraseCount);
            }
            else if (phraseCount == 0 && wordCount > 0)
            {
                wordScore = RoundToEven(result.scoreTable.totalScore / wordCount);
            }
            else
            {
                //有兩種題型時，依比重分配
                double totalWeight = result.scoreTable.wordWeight + result.scoreTable.phraseWeight;
                if (Math.Abs(totalWeight - 1.0) > 0.0001)
                    throw new InvalidOperationException($"比重總和必須為 1.0，目前為 {totalWeight:F2}，請調整比重。");

                wordScore = RoundToEven(result.scoreTable.totalScore * result.scoreTable.wordWeight / wordCount);
                phraseScore = RoundToEven(result.scoreTable.totalScore * result.scoreTable.phraseWeight / phraseCount);
            }

            result.scoreTable.WordScore = wordScore;
            result.scoreTable.PhraseScore = phraseScore;
            result.scoreTable.MentalMathScore = MentalMathScore;

            return result;
        }

        //四捨五入為「偶數」整數
        private int RoundToEven(double value)
        {
            int rounded = (int)Math.Round(value);
            return rounded % 2 == 0 ? rounded : rounded + 1;
        }

        public async Task<ExamDataViewModel_result> GenerateQuestions(int MentalLevel, string KidID)
        {
            var result = new ExamDataViewModel_result
            {
                scoreTable = new ScoreTable(),
                VocabularyList = new List<Vocabulary>(),
                Title = string.Empty
            };
            var entries = new List<Vocabulary>();
            var rnd = new Random();
            var ClassName = $"MentalMath{MentalLevel}_{DateTime.Now.ToString("yyyyMMdd")}";
            var TestType = "Math";
            Guid LessionID = await _examRepo.ChkLessionID(ClassName);
            LessionID = await GetLessionID(ClassName, TestType);
            // 判斷今天是否出過考券
            Guid KidTestIndexID = await _examRepo.ChkKidTest(ClassName, TestType, KidID);

            if (KidTestIndexID != Guid.Empty)
            {
                // 取得今天出過的考試資料
                result.VocabularyList = await _examRepo.GetExamFromExamIndex(KidTestIndexID);
                await CalculateScore(result.VocabularyList, result);
                return result;
            }

            // 出過的題目存入資料庫
            Guid NewKidTestID = await _examRepo.InsertKidTestIndex(LessionID, KidID);

            for (int i = 0; i < 30; i++)
            {
                int count = (MentalLevel == 10) ? 4 : 6;
                List<int> numbers = new List<int>();
                int currentSum = 0;

                // 第1個數字只能是正數
                int firstNumber = rnd.Next(1, 10);
                numbers.Add(firstNumber);
                currentSum = firstNumber;

                for (int j = 1; j < count; j++)
                {
                    int nextNumber = 0;

                    // 如果 currentSum 為 0，就不能做減法
                    if (currentSum == 0 || rnd.Next(0, 2) == 0) // 加法
                    {
                        nextNumber = rnd.Next(1, 10); // +1~+9
                    }
                    else // 減法
                    {
                        int maxSubtract = Math.Min(9, currentSum); // 最多減掉 currentSum
                        nextNumber = -rnd.Next(1, maxSubtract + 1); // -1 ~ -maxSubtract
                    }

                    currentSum += nextNumber;
                    numbers.Add(nextNumber);
                }

                var questionText = string.Join(" + ", numbers.Select(n => n < 0 ? $"({n})" : n.ToString()));

                var entry = new Vocabulary
                {
                    CategoryType = "MentalMath",
                    Question = questionText,
                    Answer = currentSum.ToString(),
                    ClassName = ClassName,
                    TestType = TestType
                };

                entries.Add(entry);
            }


            foreach (var question in entries)
            {
                question.LessionID = LessionID;
                await _examRepo.InsertWord(question);
                Guid WordID = await _examRepo.GetWordID(question.Question, question.Answer);
                question.WordID = WordID;
            }

            result.VocabularyList = entries;
            await CalculateScore(result.VocabularyList, result);

            foreach (var word in result.VocabularyList)
            {
                await _examRepo.InsertExamIndex(word.WordID, NewKidTestID);
            }
            result.Title = ClassName;
            return result;
        }

    }

}
