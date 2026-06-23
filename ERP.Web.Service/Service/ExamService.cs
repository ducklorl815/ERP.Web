using Azure.Core;
using ERP.Web.Models.Models;
using ERP.Web.Models.Respository;
using ERP.Web.Service.ViewModels;
using ERP.Web.Utility.Paging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering; // ASP.NET Core 的 SelectListItem
using OfficeOpenXml;

namespace ERP.Web.Service.Service
{
    public class ExamService
    {
        private readonly ExamRespo _examRepo;
        /// <summary>複習考（答錯題）固定課程名稱，用於累計出卷次數</summary>
        private const string WrongExamLessionName = "複習考";
        public ExamService(
            ExamRespo examRepo
            )
        {
            _examRepo = examRepo;
        }
        public async Task<ExamSearchListViewModel_result> GetNewTestAsync(ExamSearchListViewModel_param param)
        {
            var result = new ExamSearchListViewModel_result
            {
                TestType = param.TestType,
                CorrectType = param.CorrectType,
                KidID = param.KidID,
                ClassNameList = param.ClassNameList
            };
            //分頁功能
            var ExamKeyword = new ExamMainKeyword
            {
                ClassNameList = param.ClassNameList,
                CorrectType = param.CorrectType,
                TestType = param.TestType, // 加入 TestType 篩選
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
            var result = new ExamSearchListViewModel_result
            {
                SearchText = param.SearchText,
                KidID = param.KidID,
                CorrectType = param.CorrectType,
                ClassNameList = param.ClassNameList,
                TestType = param.TestType
            };

            var examKeyword = new ExamMainKeyword
            {
                ClassNameList = param.ClassNameList,
                CorrectType = param.CorrectType,
                KidID = param.KidID,
                TestType = param.TestType,
                SearchText = param.SearchText
            };

            var datacount = string.IsNullOrEmpty(param.KidID)
                ? 0
                : await _examRepo.GetReTestCountAsync(examKeyword);
            var pager = new Paging(param.Page, param.PageSize, datacount);

            result.Pager = pager;
            result.ExamDataList = string.IsNullOrEmpty(param.KidID)
                ? new List<ExamMainModel>()
                : await _examRepo.GetReTestSearchListAsync(pager, examKeyword);

            await PublicTaskAsync(result, param);

            if (!string.IsNullOrEmpty(param.KidID))
            {
                result.ClassNameStats = await _examRepo.GetReTestClassNameStatsAsync(param.KidID, param.TestType);
            }

            result.ClassNameSelectList = BuildReTestClassNameSelectList(result.ClassNameList, result.ClassNameStats);
            return result;
        }

        /// <summary>課程下拉：Value 為 ClassName，Text 顯示辭庫統計</summary>
        private static List<SelectListItem> BuildReTestClassNameSelectList(List<string> allClassNames, List<ClassNameStatModel> stats)
        {
            if (allClassNames == null || allClassNames.Count == 0)
                return new List<SelectListItem>();

            var statMap = stats?.ToDictionary(s => s.ClassName, StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, ClassNameStatModel>(StringComparer.OrdinalIgnoreCase);

            return allClassNames
                .Select(c =>
                {
                    var text = c;
                    if (statMap.TryGetValue(c, out var s))
                        text = $"{c}（已考:{s.TestedCount} 對:{s.CorrectCount} 錯:{s.WrongCount}）";
                    return new SelectListItem { Value = c, Text = text };
                })
                .ToList();
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

            Guid reExamLessionId = await _examRepo.GetLessionID(result.Title);
            if (reExamLessionId == Guid.Empty && !string.IsNullOrWhiteSpace(param.ClassName))
                reExamLessionId = await _examRepo.ChkLessionID(param.ClassName);

            if (KidTestIndexID != Guid.Empty)
            {
                result.VocabularyList = await _examRepo.GetExamFromExamIndex(KidTestIndexID);
                await CalculateScore(result.VocabularyList, result);
                await PopulateExamPaperMetaAsync(result, param.KidID, reExamLessionId, param.TestType, KidTestIndexID);
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
            if (reExamLessionId == Guid.Empty)
                reExamLessionId = LessionID;

            Guid NewKidTestID = await _examRepo.InsertKidTestIndex(LessionID, param.KidID);

            foreach (var word in result.VocabularyList)
            {
                ExamRcdModel ExamRcdData = await _examRepo.GetExamRcd(word.WordID);
                ExamRcdData.WordID = word.WordID;
                ExamRcdData.NewKidTestID = NewKidTestID;
                await _examRepo.InsertExamIndex(ExamRcdData);
            }

            await PopulateExamPaperMetaAsync(result, param.KidID, reExamLessionId, param.TestType, NewKidTestID);
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
            var lessionId = await _examRepo.GetLessionID(result.Title);

            Guid KidTestIndexID = await _examRepo.ChkKidTest(result.Title, param.TestType, param.KidID);

            if (KidTestIndexID != Guid.Empty)
            {
                result.VocabularyList = await _examRepo.GetExamFromExamIndex(KidTestIndexID);
                await CalculateScore(result.VocabularyList, result);
                await PopulateExamPaperMetaAsync(result, param.KidID, lessionId, param.TestType, KidTestIndexID);
                return result;
            }

            //取得考試資料
            result.VocabularyList = await GetExamData(param);

            // 題目為空：不計分、不寫入考卷紀錄，直接回傳避免拋例外
            if (result.VocabularyList == null || result.VocabularyList.Count == 0)
            {
                result.VocabularyList = new List<Vocabulary>();
                result.scoreTable.WordScore = 0;
                result.scoreTable.PhraseScore = 0;
                result.scoreTable.MentalMathScore = 0;
                return result;
            }

            await CalculateScore(result.VocabularyList, result);

            if (lessionId == Guid.Empty)
                lessionId = await _examRepo.GetLessionID(result.Title);

            Guid NewKidTestID = await _examRepo.InsertKidTestIndex(lessionId, param.KidID);

            foreach (var word in result.VocabularyList)
            {
                var ExamRcdData = await _examRepo.GetExamRcd(word.WordID) ?? new ExamRcdModel();
                if (ExamRcdData != null)
                    ExamRcdData.ReTest = ExamRcdData.ReTest + 1;
                ExamRcdData.NewKidTestID = NewKidTestID;
                ExamRcdData.WordID = word.WordID;
                await _examRepo.InsertExamIndex(ExamRcdData);
            }

            await PopulateExamPaperMetaAsync(result, param.KidID, lessionId, param.TestType, NewKidTestID);
            return result;
        }

        /// <summary>設定考卷標題用的日期與第幾次考試（KidTestIndex × Lession）</summary>
        private async Task PopulateExamPaperMetaAsync(
            ExamDataViewModel_result result,
            string kidId,
            Guid lessionId,
            string testType,
            Guid? kidTestIndexId)
        {
            result.ExamTypeLabel = string.Equals(testType, "Math", StringComparison.OrdinalIgnoreCase) ? "數學" : "英文";
            result.ExamAttemptNumber = await _examRepo.GetKidTestAttemptNumberAsync(kidId, lessionId, kidTestIndexId);

            if (kidTestIndexId.HasValue && kidTestIndexId.Value != Guid.Empty)
                result.ExamDate = await _examRepo.GetKidTestDateAsync(kidTestIndexId.Value) ?? DateTime.Today;
            else
                result.ExamDate = DateTime.Today;
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
                case "Math":
                    finalQuestions = await GetVocabularyExamDataAsync(param);
                    return finalQuestions;
                default:
                    return finalQuestions;
            }

        }

        /// <summary>英文／數學辭庫出題（依 ClassNameList 與 TestType）</summary>
        private async Task<List<Vocabulary>> GetVocabularyExamDataAsync(ExamSearchListViewModel_param param)
        {
            List<Vocabulary> finalQuestions = new List<Vocabulary>(), listVocabulary = new List<Vocabulary>();
            string FirstClassName = string.Empty;

            if (param?.ClassNameList == null || param.ClassNameList.Count == 0)
                return finalQuestions;

            for (int i = 0; i < param.ClassNameList.Count(); i++)
            {
                string ClassName = param.ClassNameList[i];

                // 若需要紀錄第一筆的 ClassName 跟 Num，可這樣做（可選）
                if (i == 0)
                {
                    FirstClassName = ClassName;
                }

                var NewVocabulary = await _examRepo.GetExamDataAsync(ClassName, param.KidID, param.TestType);

                if (NewVocabulary != null)
                    listVocabulary.AddRange(NewVocabulary);
            }

            if (listVocabulary.Count == 0)
                return finalQuestions;

            // 依 ClassNum 降序排列（最新的在前）
            var groupedByClass = listVocabulary
                .GroupBy(x => new { x.ClassName, x.ClassNum, x.Category }) // 依 ClassNum 和 Category 分組
                .OrderByDescending(g => g.Key.ClassName == FirstClassName)
                .ToList();

            // 設定權重（依課程遠近分配）
            var distribution = Enumerable.Range(0, groupedByClass.Count())
                .ToDictionary(
                    i => i,
                    i => (param.ClassNameList.Count() == 1 && i == 0)
                        ? 1.0                                  // 只有一個課程且是第一個 => 權重 1.0
                        : 1.0 / groupedByClass.Count()         // 用 1.0 確保浮點數除法
                );

            int totalQuestions = param.TestNumber > 0
                ? param.TestNumber
                : (
                    param.ClassNameList.Count() == 1
                        ? groupedByClass.FirstOrDefault()?.Count() ?? 0
                        : 0
                  );

            if (totalQuestions <= 0)
                return finalQuestions;


            // 先處理前三個課程（依據 distribution 權重）
            for (int i = 0; i < Math.Min(4, groupedByClass.Count); i++)
            {
                double percentage = distribution[i];
                int totalCount = (int)Math.Round(totalQuestions * percentage);

                // 上次答錯 (LastExamCorrect==0) 優先 → ReTest 較小 → 被考次數較少 → 同序內亂數
                var words = OrderEnglishExamPool(groupedByClass[i]);

                // i = 0 時，不過濾 Correct，其他情況下過濾 Correct > 0，並依 Correct 降序排列
                if (i != 0)
                {
                    words = words.Where(x => x.Correct > 0 && x.KidID == Guid.Parse(param.KidID))
                                 .OrderBy(x => x.Correct) // 優先拿 Correct = 0
                                 .OrderBy(x => x.ReTest)
                                 .ToList();
                }

                // i = 0 時，優先拿 Correct = 0
                List<Vocabulary> selectedWords;

                if (i == 0)
                {
                    // 同一 WordID 只取一題，避免 Correct=0 與其餘題目 Concat 造成重複
                    selectedWords = TakeUniqueFromEnglishPool(words, totalCount);
                }
                else
                {
                    selectedWords = words
                        .GroupBy(x => x.WordID)
                        .Select(g => g.First())
                        .OrderBy(_ => Guid.NewGuid())
                        .Take(totalCount)
                        .ToList();
                }

                // 確保不超過 totalQuestions
                int remainingSlots = totalQuestions - finalQuestions.Count;
                finalQuestions.AddRange(selectedWords.Take(remainingSlots));

                // 題目滿 20 題就跳出
                if (finalQuestions.Count >= totalQuestions) break;
            }

            // 題目不足時：先從同批課程辭庫補，仍不足則從歷史答錯題補（不與已選重複）
            if (finalQuestions.Count < totalQuestions)
            {
                var takenIds = new HashSet<Guid>(finalQuestions.Select(x => x.WordID));

                int remainingSlots = totalQuestions - finalQuestions.Count;
                var fromSamePool = TakeUniqueFromEnglishPool(
                    listVocabulary.Where(x => !takenIds.Contains(x.WordID)),
                    remainingSlots);
                foreach (var word in fromSamePool)
                {
                    finalQuestions.Add(word);
                    takenIds.Add(word.WordID);
                }

                remainingSlots = totalQuestions - finalQuestions.Count;
                if (remainingSlots > 0)
                {
                    var fromWrongPool = await FillFromWrongAnswerPoolAsync(param, takenIds, remainingSlots);
                    finalQuestions.AddRange(fromWrongPool);
                }
            }

            return finalQuestions;
        }

        /// <summary>從課程辭庫依出題優先序選題，同一 WordID 只取一題。</summary>
        private static List<Vocabulary> TakeUniqueFromEnglishPool(IEnumerable<Vocabulary> words, int count)
        {
            if (count <= 0)
                return new List<Vocabulary>();

            return OrderEnglishExamPool(words)
                .GroupBy(x => x.WordID)
                .Select(g => g.First())
                .Take(count)
                .OrderBy(_ => Guid.NewGuid())
                .ToList();
        }

        /// <summary>
        /// 從歷史答錯題補足出題：ReTest 較少 → 前兩次考卷未出現 → 同序亂數；排除已選 WordID。
        /// </summary>
        private async Task<List<Vocabulary>> FillFromWrongAnswerPoolAsync(
            ExamSearchListViewModel_param param,
            HashSet<Guid> excludeWordIds,
            int count)
        {
            if (count <= 0 || string.IsNullOrWhiteSpace(param.KidID))
                return new List<Vocabulary>();

            var pool = await _examRepo.GetWrongVocabularyByKid(param.KidID, param.TestType);
            if (pool == null || pool.Count == 0)
                return new List<Vocabulary>();

            var recentSet = (await _examRepo.GetRecentExamWordIdsAsync(param.KidID, param.TestType, 2))?.ToHashSet()
                ?? new HashSet<Guid>();

            return OrderWrongExamPool(
                    pool.Where(x => !excludeWordIds.Contains(x.WordID)),
                    recentSet)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// 英文正式考出題順序：上次答錯優先 → ReTest 較小 → 被考次數較少，同序內亂數打散。
        /// </summary>
        private static List<Vocabulary> OrderEnglishExamPool(IEnumerable<Vocabulary> words)
        {
            return words
                .OrderBy(x => x.LastExamCorrect == 0 ? 0 : 1)
                .ThenBy(x => x.ReTest)
                .ThenBy(x => x.ExamTimes)
                .ThenBy(_ => Guid.NewGuid())
                .ToList();
        }

        public async Task<ExamDataViewModel_result> GetWrongExamDataAsync(ExamSearchListViewModel_param param)
        {
            var result = new ExamDataViewModel_result
            {
                scoreTable = new ScoreTable(),
                VocabularyList = new List<Vocabulary>(),
                Title = WrongExamLessionName + "_" + DateTime.Now.ToString("yyyyMMdd"),
                ExamDate = DateTime.Today,
                ExamTypeLabel = string.Equals(param?.TestType, "Math", StringComparison.OrdinalIgnoreCase) ? "數學" : "英文"
            };

            if (param == null || string.IsNullOrWhiteSpace(param.KidID))
                return result;

            // 依「學生」抓取目前仍答錯（最新一筆 Correct=0）的題目
            var take = param.TestNumber > 0 ? param.TestNumber : 18;
            result.VocabularyList = await FillFromWrongAnswerPoolAsync(param, new HashSet<Guid>(), take);
            if (result.VocabularyList.Count == 0)
                return result;

            await CalculateScore(result.VocabularyList, result);

            // 複習考累計出卷次數（含 ClassName「複習考」與舊版「複習考_日期」）
            Guid lessionId = await GetLessionID(WrongExamLessionName, param.TestType);
            result.ExamAttemptNumber = await _examRepo.GetWrongExamAttemptNumberAsync(param.KidID, param.TestType);

            Guid newKidTestId = await _examRepo.InsertKidTestIndex(lessionId, param.KidID);
            foreach (var word in result.VocabularyList)
            {
                await _examRepo.InsertExamIndex(new ExamRcdModel
                {
                    WordID = word.WordID,
                    NewKidTestID = newKidTestId,
                    ReTest = word.ReTest + 1,
                    Correct = 1
                });
            }

            return result;
        }

        /// <summary>
        /// 複習考（答錯題）出題順序：ReTest 較少 → 前兩次考卷未出現 → 同序內亂數。
        /// </summary>
        private static List<Vocabulary> OrderWrongExamPool(IEnumerable<Vocabulary> words, HashSet<Guid> recentExamWordIds)
        {
            return words
                .OrderBy(x => x.ReTest)
                .ThenBy(x => recentExamWordIds.Contains(x.WordID) ? 1 : 0)
                .ThenBy(_ => Guid.NewGuid())
                .ToList();
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
            Guid LessionID = await _examRepo.GetLessionID(ClassName, TestType);
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

            // Correct：true＝最終答對（DB Correct=1）
            var correctUpdated = await _examRepo.UpdateExamWord(
                param.WordID,
                param.KidID,
                param.TestDate ?? string.Empty,
                param.Correct,
                param.TestType);

            var wordUpdated = true;
            if (!string.IsNullOrWhiteSpace(param.Question) && !string.IsNullOrWhiteSpace(param.Answer))
                wordUpdated = await _examRepo.UpdateWord(param.WordID, param.Question.Trim(), param.Answer.Trim());

            return correctUpdated && wordUpdated;
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

        /// <summary>學生下拉選單（考券頁面共用）</summary>
        public async Task<List<SelectListItem>> GetKidSelectListAsync()
        {
            var kids = await _examRepo.GetKidListAsync();
            if (kids == null || kids.Count == 0)
                return new List<SelectListItem>();

            return kids
                .Select(k => new SelectListItem { Value = k.Item1.ToString(), Text = k.Item2 })
                .ToList();
        }

        private async Task PublicTaskAsync(ExamSearchListViewModel_result result, ExamSearchListViewModel_param param)
        {
            // 並行非同步請求
            var kidListTask = _examRepo.GetKidListAsync();
            var testDateListTask = _examRepo.GetTestDateList(param.KidID);
            List<ExamListModel> ExamList = await _examRepo.GetExamListAsync();

            // 根據 TestType 篩選課程清單（忽略大小寫，相容 Excel 上傳的 math / english）
            if (!string.IsNullOrEmpty(param.TestType))
            {
                ExamList = ExamList
                    .Where(x => string.Equals((x.TestType ?? string.Empty).Trim(), param.TestType.Trim(), StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            #region todo 製作排序
            // Step 1: 動態取得主題群組（同樣邏輯）
            var grouped = ExamList
                .GroupBy(x =>
                {
                    var parts = x.ClassName.Split(' ');
                    if (parts.Length >= 3 && (parts[^2] == "HW" || parts[^2] == "Sp"))
                        return string.Join(" ", parts.Take(parts.Length - 1));
                    else
                        return x.ClassName.Split('_')[0];
                })
                .ToDictionary(
                    g => g.Key,
                    g => g.OrderByDescending(x => x.LessionSort).ToList()
                );

            // Step 2: 根據群組中最大 LessionSort 排序主題（最新優先）
            var topicOrder = grouped
                .OrderBy(g => g.Value.Max(x => x.ClassName.Contains("MentalMath")))
                .Select(g => g.Key)
                .ToList();

            // Step 3: 展平
            var sortedList = topicOrder
                .Where(topic => grouped.ContainsKey(topic))
                .SelectMany(topic => grouped[topic])
                .ToList();
            var classNameListTask = sortedList.Select(x => x.ClassName).ToList();
            #endregion

            await Task.WhenAll(kidListTask, testDateListTask);

            result.KidList = kidListTask.Result
                .Select(x => new SelectListItem
                {
                    Text = x.Item2,
                    Value = x.Item1.ToString().Trim()
                }).ToList();

            result.TestDateList = testDateListTask.Result
                .Select(x => new SelectListItem
                {
                    Text = x.Item1.ToString("yyyy-MM-dd") + " : " + x.Item2,
                    Value = x.Item1.ToString("yyyy-MM-dd")
                }).ToList();

            result.ClassNameList = classNameListTask;
        }
        public async Task<ExamDataViewModel_result> CalculateScore(List<Vocabulary> vocabularyList, ExamDataViewModel_result result)
        {
            if (vocabularyList == null || vocabularyList.Count == 0)
            {
                result.scoreTable.WordScore = 0;
                result.scoreTable.PhraseScore = 0;
                result.scoreTable.MentalMathScore = 0;
                return result;
            }

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
                MentalMathScore = FloorScorePerQuestion(result.scoreTable.totalScore / MentalMathCount);
            }

            // 若只有單一題型，100 分平均分配（每題分數無條件捨去）
            if (wordCount == 0 && phraseCount > 0)
            {
                phraseScore = FloorScorePerQuestion(result.scoreTable.totalScore / phraseCount);
            }
            else if (phraseCount == 0 && wordCount > 0)
            {
                wordScore = FloorScorePerQuestion(result.scoreTable.totalScore / wordCount);
            }
            else if (wordCount > 0 && phraseCount > 0)
            {
                // 單字、片語混合：依題型權重 2:3 計算每題分數
                // 總權重 = 單字題數×2 + 片語題數×3；每題分數 = 總分 × 該題型權重 ÷ 總權重（無條件捨去）
                int wordTypeWeight = result.scoreTable.wordWeight;
                int phraseTypeWeight = result.scoreTable.phraseWeight;
                int totalQuestionWeight = wordCount * wordTypeWeight + phraseCount * phraseTypeWeight;

                if (totalQuestionWeight <= 0)
                    throw new InvalidOperationException("題型權重或題數異常，無法計算分數。");

                wordScore = FloorScorePerQuestion((double)result.scoreTable.totalScore * wordTypeWeight / totalQuestionWeight);
                phraseScore = FloorScorePerQuestion((double)result.scoreTable.totalScore * phraseTypeWeight / totalQuestionWeight);
            }
            else
            {
                result.scoreTable.WordScore = 0;
                result.scoreTable.PhraseScore = 0;
                result.scoreTable.MentalMathScore = MentalMathScore;
                return result;
            }

            result.scoreTable.WordScore = wordScore;
            result.scoreTable.PhraseScore = phraseScore;
            result.scoreTable.MentalMathScore = MentalMathScore;

            return result;
        }

        /// <summary>每題分數：無條件捨去小數，避免全對時總分超過滿分。</summary>
        private static int FloorScorePerQuestion(double value) => (int)Math.Floor(value);

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
                result.VocabularyList = await _examRepo.GetExamFromExamIndex(KidTestIndexID);
                await CalculateScore(result.VocabularyList, result);
                result.Title = ClassName;
                await PopulateExamPaperMetaAsync(result, KidID, LessionID, TestType, KidTestIndexID);
                return result;
            }

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
                ExamRcdModel ExamRcdData = await _examRepo.GetExamRcd(word.WordID);
                ExamRcdData.WordID = word.WordID;
                ExamRcdData.NewKidTestID = NewKidTestID;
                await _examRepo.InsertExamIndex(ExamRcdData);
            }
            result.Title = ClassName;
            await PopulateExamPaperMetaAsync(result, KidID, LessionID, TestType, NewKidTestID);
            return result;
        }

        public async Task<List<SelectListItem>> GetTestDateList(string kidID, string testType)
        {
            var dates = await _examRepo.GetTestDateOnlyList(kidID, testType);
            return dates.Select(d => new SelectListItem
            {
                Text = d.ToString("yyyy-MM-dd"),
                Value = d.ToString("yyyy-MM-dd")
            }).ToList();
        }

        public async Task<List<SelectListItem>> GetClassNameListByDate(string kidID, string testDate, string testType)
        {
            if (string.IsNullOrWhiteSpace(kidID) || string.IsNullOrWhiteSpace(testDate))
                return new List<SelectListItem>();

            if (!DateTime.TryParse(testDate, out var parsedDate))
                return new List<SelectListItem>();

            var classNames = await _examRepo.GetClassNameListByDate(kidID, parsedDate, testType);
            return classNames.Select(c => new SelectListItem
            {
                Text = c,
                Value = c
            }).ToList();
        }
    }

}
