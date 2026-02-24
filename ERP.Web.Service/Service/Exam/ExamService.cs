using ERP.Web.Models.Models;
using ERP.Web.Models.Respository.Exam;
using ERP.Web.Service.ViewModels;
using ERP.Web.Utility.Paging;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using Microsoft.AspNetCore.Mvc.Rendering; // ASP.NET Core 的 SelectListItem

namespace ERP.Web.Service.Service.Exam
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
                TestType = param.TestType, // 加入 TestType 篩選
            };
            if (param.TestDate != null)
                ExamKeyword.TestDate = DateTime.Parse(param.TestDate);
            else
                ExamKeyword.CorrectType = "1";

            var datacount = await _examRepo.GetReTestCountAsync(ExamKeyword);
            var pager = new Paging(param.Page, param.PageSize, datacount);

            result.Pager = pager;
            result.ExamDataList = await _examRepo.GetReTestSearchListAsync(pager, ExamKeyword);
            var kidListTask = await _examRepo.GetKidListAsync();
            var testDateListTask = await _examRepo.GetTestDateList(param.KidID);
            var ExamList = await _examRepo.GetExamListAsync();

            // 根據 TestType 篩選課程清單
            if (!string.IsNullOrEmpty(param.TestType))
            {
                ExamList = ExamList.Where(x => x.TestType.Equals(param.TestType, StringComparison.OrdinalIgnoreCase)).ToList();
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
                    Text = x.Item1.ToString("yyyy-MM-dd") + " : " + x.Item2,
                    Value = x.Item1.ToString("yyyy-MM-dd")
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

                ExamRcdModel ExamRcdData = await _examRepo.GetExamRcd(word.WordID);
                ExamRcdData.WordID = word.WordID;
                ExamRcdData.NewKidTestID = NewKidTestID;
                await _examRepo.InsertExamIndex(ExamRcdData);
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
                var ExamRcdData = await _examRepo.GetExamRcd(word.WordID) ?? new ExamRcdModel();
                ExamRcdData.WordID = word.WordID;
                ExamRcdData.NewKidTestID = NewKidTestID;
                await _examRepo.InsertExamIndex(ExamRcdData);
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

                if (i == 0)
                {
                    FirstClassName = ClassName;
                }

                var NewVocabulary = await _examRepo.GetExamDataAsync(ClassName);

                // 讓 Question / Answer 有機會對調（中翻英 / 英翻中）
                var random = new Random();
                foreach (var item in NewVocabulary)
                {
                    if (random.Next(0, 2) == 1) // 50% 機率為英翻中
                    {
                        var origQuestion = item.Question;
                        var origAnswer = item.Answer;
                        // 英翻中：題目改為英文（單字時改為音標顯示，讓小孩用念的方式想出中文）
                        if (item.CategoryType != null && item.CategoryType.Equals("word", StringComparison.OrdinalIgnoreCase))
                            item.Question = ToPhoneticDisplay(origAnswer);
                        else
                            item.Question = origAnswer;
                        item.Answer = origQuestion;
                    }
                }

                listVocabulary.AddRange(NewVocabulary);
            }


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

            int totalQuestions = param.ClassNameList.Count() == 1 ? groupedByClass[0].Count() : param.TestNumber;


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

        /// <summary>
        /// 將英文單字轉成「第一個母音加短音記號 breve」的顯示用音標，供英翻中時出題（小孩用念的方式想出中文）。
        /// 例如：elephant → ĕlephant，duck → dŭck。
        /// </summary>
        private static string ToPhoneticDisplay(string englishWord)
        {
            if (string.IsNullOrWhiteSpace(englishWord)) return englishWord;
            // 第一個母音對應的 breve 字元（小寫）
            const string vowels = "aeiouAEIOU";
            const string withBreve = "ăĕĭŏŭĂĔĬŎŬ";
            for (int i = 0; i < englishWord.Length; i++)
            {
                int idx = vowels.IndexOf(englishWord[i]);
                if (idx >= 0)
                    return englishWord.Substring(0, i) + withBreve[idx] + englishWord.Substring(i + 1);
            }
            return englishWord;
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
            List<ExamListModel> ExamList = await _examRepo.GetExamListAsync();

            // 根據 TestType 篩選課程清單
            if (!string.IsNullOrEmpty(param.TestType))
            {
                ExamList = ExamList.Where(x => x.TestType.Equals(param.TestType, StringComparison.OrdinalIgnoreCase)).ToList();
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

        /// <summary>
        /// 產生珠心算題目（舊版：使用級數）
        /// </summary>
        [Obsolete("請使用 GenerateMentalMathQuestions 方法，可自定義位數")]
        public async Task<ExamDataViewModel_result> GenerateQuestions(int MentalLevel, string KidID)
        {
            // 轉換為新的方法
            int digitCount = 1; // 1位數
            int numberCount = (MentalLevel == 10) ? 4 : 6;
            int questionCount = 30;
            
            return await GenerateMentalMathQuestions(KidID, questionCount, digitCount, numberCount);
        }

        /// <summary>
        /// 產生珠心算題目（新版：可自定義位數和數量）
        /// </summary>
        /// <param name="KidID">學生ID</param>
        /// <param name="questionCount">題數</param>
        /// <param name="digitCount">數字位數（1=個位數, 2=十位數, 3=百位數...）</param>
        /// <param name="numberCount">加減數數量</param>
        /// <param name="allowNegativeResult">是否允許結果為負數</param>
        public async Task<ExamDataViewModel_result> GenerateMentalMathQuestions(
            string KidID, 
            int questionCount, 
            int digitCount, 
            int numberCount,
            bool allowNegativeResult = false)
        {
            var result = new ExamDataViewModel_result
            {
                scoreTable = new ScoreTable(),
                VocabularyList = new List<Vocabulary>(),
                Title = string.Empty
            };
            var entries = new List<Vocabulary>();
            var rnd = new Random();
            
            // 計算數字範圍
            int minNumber = (int)Math.Pow(10, digitCount - 1); // 例如：1位數=1, 2位數=10, 3位數=100
            int maxNumber = (int)Math.Pow(10, digitCount) - 1; // 例如：1位數=9, 2位數=99, 3位數=999
            
            if (digitCount == 1)
            {
                minNumber = 1; // 個位數從 1 開始
            }
            
            var ClassName = $"珠心算_{digitCount}位數_{numberCount}數_{DateTime.Now.ToString("yyyyMMdd")}";
            var TestType = "Math";
            result.Title = ClassName;
            
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

            // 產生題目
            for (int i = 0; i < questionCount; i++)
            {
                List<int> numbers = new List<int>();
                int currentSum = 0;

                // 第1個數字只能是正數
                int firstNumber = rnd.Next(minNumber, maxNumber + 1);
                numbers.Add(firstNumber);
                currentSum = firstNumber;

                // 產生其他數字（可正可負）
                for (int j = 1; j < numberCount; j++)
                {
                    int nextNumber = 0;

                    // 決定是加法還是減法
                    bool isAddition = rnd.Next(0, 2) == 0;
                    
                    if (isAddition)
                    {
                        // 加法
                        nextNumber = rnd.Next(minNumber, maxNumber + 1);
                    }
                    else
                    {
                        // 減法
                        nextNumber = -rnd.Next(minNumber, maxNumber + 1);
                        
                        // 如果不允許負數結果，且會產生負數，則改為加法
                        if (!allowNegativeResult && (currentSum + nextNumber) < 0)
                        {
                            nextNumber = rnd.Next(minNumber, maxNumber + 1);
                        }
                    }

                    currentSum += nextNumber;
                    numbers.Add(nextNumber);
                }

                // 組合題目文字
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

            // 儲存題目到資料庫
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
                var ExamRcdData = await _examRepo.GetExamRcd(word.WordID) ?? new ExamRcdModel();
                ExamRcdData.WordID = word.WordID;
                ExamRcdData.NewKidTestID = NewKidTestID;
                await _examRepo.InsertExamIndex(ExamRcdData);
            }
            result.Title = ClassName;

            return result;
        }

        public async Task<List<SelectListItem>> GetTestDateList(string kidID)
        {
            var testDateListTask = await _examRepo.GetTestDateList(kidID);

            var TestDateList = testDateListTask
            .Select(x => new SelectListItem
            {
                Text = x.Item1.ToString("yyyy-MM-dd") + " : " + x.Item2,
                Value = x.Item1.ToString("yyyy-MM-dd")
            }).ToList();
            if (TestDateList != null)
                return TestDateList;
            return null;
        }

        /// <summary>
        /// 取得學生清單
        /// </summary>
        public async Task<List<SelectListItem>> GetKidList()
        {
            var kidListTask = await _examRepo.GetKidListAsync();

            var KidList = kidListTask
                .Select(x => new SelectListItem
                {
                    Text = x.Item2,
                    Value = x.Item1.ToString().Trim()
                }).ToList();

            return KidList;
        }

        #region 四則運算題目生成

        /// <summary>
        /// 產生加法題目
        /// </summary>
        /// <param name="KidID">學生ID</param>
        /// <param name="questionCount">題數</param>
        /// <param name="firstNumberDigits">第一個數位數（被加數）</param>
        /// <param name="addendDigits">其他加數位數</param>
        /// <param name="numberCount">加數數量</param>
        /// <param name="resultDigitCount">結果位數限制</param>
        public async Task<ExamDataViewModel_result> GenerateAdditionQuestions(
            string KidID,
            int questionCount,
            int firstNumberDigits,
            int addendDigits,
            int numberCount,
            int resultDigitCount)
        {
            var result = new ExamDataViewModel_result
            {
                scoreTable = new ScoreTable(),
                VocabularyList = new List<Vocabulary>(),
                Title = string.Empty
            };

            var entries = new List<Vocabulary>();
            var rnd = new Random();
            var ClassName = $"Addition_{firstNumberDigits}x{addendDigits}digit_{DateTime.Now:yyyyMMdd_HHmmss}";
            var TestType = "Math";

            // 建立課程ID
            Guid LessionID = await GetLessionID(ClassName, TestType);

            // 判斷今天是否出過同樣的考券
            Guid KidTestIndexID = await _examRepo.ChkKidTest(ClassName, TestType, KidID);

            if (KidTestIndexID != Guid.Empty)
            {
                // 取得已存在的考試資料
                result.VocabularyList = await _examRepo.GetExamFromExamIndex(KidTestIndexID);
                await CalculateScore(result.VocabularyList, result);
                result.Title = ClassName;
                return result;
            }

            // 建立新的測驗索引
            Guid NewKidTestID = await _examRepo.InsertKidTestIndex(LessionID, KidID);

            // 數字範圍：第一個數（被加數）和其他加數使用不同的位數範圍
            int minFirstNumber = firstNumberDigits == 1 ? 1 : (int)Math.Pow(10, firstNumberDigits - 1);
            int maxFirstNumber = (int)Math.Pow(10, firstNumberDigits) - 1;
            int minAddend = addendDigits == 1 ? 1 : (int)Math.Pow(10, addendDigits - 1);
            int maxAddend = (int)Math.Pow(10, addendDigits) - 1;
            int maxResult = (int)Math.Pow(10, resultDigitCount) - 1;

            // 產生題目
            for (int i = 0; i < questionCount; i++)
            {
                List<int> numbers = new List<int>();
                int sum = 0;
                bool validQuestion = false;

                // 重試機制，確保結果符合位數限制
                int retryCount = 0;
                while (!validQuestion && retryCount < 100)
                {
                    numbers.Clear();
                    sum = 0;

                    // 第一個數（被加數）使用 firstNumberDigits 位數
                    int firstNumber = rnd.Next(minFirstNumber, maxFirstNumber + 1);
                    numbers.Add(firstNumber);
                    sum = firstNumber;

                    // 產生其他加數（使用 addendDigits 位數）
                    for (int j = 1; j < numberCount; j++)
                    {
                        int addend = rnd.Next(minAddend, maxAddend + 1);
                        numbers.Add(addend);
                        sum += addend;
                    }

                    // 檢查結果是否符合位數限制
                    if (sum <= maxResult)
                    {
                        validQuestion = true;
                    }

                    retryCount++;
                }

                // 如果重試100次仍不符合，降低數字範圍
                if (!validQuestion)
                {
                    numbers.Clear();
                    sum = 0;
                    
                    // 第一個數使用較小的範圍
                    int reducedMaxFirst = Math.Min(maxFirstNumber, maxResult / numberCount);
                    int firstNumber = rnd.Next(minFirstNumber, reducedMaxFirst + 1);
                    numbers.Add(firstNumber);
                    sum = firstNumber;
                    
                    // 其他加數使用較小的範圍
                    int remainingResult = maxResult - firstNumber;
                    int reducedMaxAddend = Math.Min(maxAddend, remainingResult / (numberCount - 1));
                    for (int j = 1; j < numberCount; j++)
                    {
                        int addend = rnd.Next(minAddend, reducedMaxAddend + 1);
                        numbers.Add(addend);
                        sum += addend;
                    }
                }

                // 產生橫式排列的題目格式：123+234+456=
                var questionParts = new List<string>();
                for (int j = 0; j < numbers.Count; j++)
                {
                    if (j == 0)
                    {
                        questionParts.Add(numbers[j].ToString());
                    }
                    else
                    {
                        questionParts.Add($"+{numbers[j]}");
                    }
                }
                var questionText = string.Join("", questionParts) + "=";

                // 建立題目物件
                var entry = new Vocabulary
                {
                    CategoryType = "Addition",
                    Question = questionText,
                    Answer = sum.ToString(),
                    ClassName = ClassName,
                    TestType = TestType,
                    LessionID = LessionID
                };

                entries.Add(entry);
            }

            // 儲存題目到資料庫
            foreach (var question in entries)
            {
                await _examRepo.InsertWord(question);
                Guid WordID = await _examRepo.GetWordID(question.Question, question.Answer);
                question.WordID = WordID;

                // 建立測驗記錄
                var ExamRcdData = new ExamRcdModel
                {
                    WordID = WordID,
                    NewKidTestID = NewKidTestID
                };
                await _examRepo.InsertExamIndex(ExamRcdData);
            }

            result.VocabularyList = entries;
            await CalculateScore(result.VocabularyList, result);
            result.Title = $"加法運算 - {firstNumberDigits}位數 + {addendDigits}位數 × {numberCount - 1}個加數";

            return result;
        }

        /// <summary>
        /// 產生減法題目
        /// </summary>
        /// <param name="KidID">學生ID</param>
        /// <param name="questionCount">題數</param>
        /// <param name="minuendDigits">被減數位數</param>
        /// <param name="subtrahendDigits">減數位數</param>
        /// <param name="numberCount">減數數量</param>
        /// <param name="resultDigitCount">結果位數限制</param>
        /// <param name="allowNegative">是否允許負數結果</param>
        public async Task<ExamDataViewModel_result> GenerateSubtractionQuestions(
            string KidID,
            int questionCount,
            int minuendDigits,
            int subtrahendDigits,
            int numberCount,
            int resultDigitCount,
            bool allowNegative = false)
        {
            var result = new ExamDataViewModel_result
            {
                scoreTable = new ScoreTable(),
                VocabularyList = new List<Vocabulary>(),
                Title = string.Empty
            };

            var entries = new List<Vocabulary>();
            var rnd = new Random();
            var ClassName = $"Subtraction_{minuendDigits}x{subtrahendDigits}digit_{DateTime.Now:yyyyMMdd_HHmmss}";
            var TestType = "Math";

            // 建立課程ID
            Guid LessionID = await GetLessionID(ClassName, TestType);

            // 判斷今天是否出過同樣的考券
            Guid KidTestIndexID = await _examRepo.ChkKidTest(ClassName, TestType, KidID);

            if (KidTestIndexID != Guid.Empty)
            {
                result.VocabularyList = await _examRepo.GetExamFromExamIndex(KidTestIndexID);
                await CalculateScore(result.VocabularyList, result);
                result.Title = ClassName;
                return result;
            }

            // 建立新的測驗索引
            Guid NewKidTestID = await _examRepo.InsertKidTestIndex(LessionID, KidID);

            // 數字範圍：被減數和減數使用不同的位數範圍
            int minMinuend = minuendDigits == 1 ? 1 : (int)Math.Pow(10, minuendDigits - 1);
            int maxMinuend = (int)Math.Pow(10, minuendDigits) - 1;
            int minSubtrahend = subtrahendDigits == 1 ? 1 : (int)Math.Pow(10, subtrahendDigits - 1);
            int maxSubtrahend = (int)Math.Pow(10, subtrahendDigits) - 1;
            int maxResult = (int)Math.Pow(10, resultDigitCount) - 1;
            int minResult = allowNegative ? -(int)Math.Pow(10, resultDigitCount) + 1 : 0;

            // 產生題目
            for (int i = 0; i < questionCount; i++)
            {
                List<int> numbers = new List<int>();
                int difference = 0;
                bool validQuestion = false;

                // 重試機制
                int retryCount = 0;
                while (!validQuestion && retryCount < 100)
                {
                    numbers.Clear();

                    // 第一個數字（被減數）使用 minuendDigits 位數
                    int minuend = rnd.Next(minMinuend, maxMinuend + 1);
                    numbers.Add(minuend);
                    difference = minuend;

                    // 產生減數（使用 subtrahendDigits 位數）
                    for (int j = 1; j < numberCount; j++)
                    {
                        int subtrahend = rnd.Next(minSubtrahend, maxSubtrahend + 1);
                        numbers.Add(subtrahend);
                        difference -= subtrahend;
                    }

                    // 檢查結果是否符合限制
                    if (difference >= minResult && difference <= maxResult)
                    {
                        validQuestion = true;
                    }

                    retryCount++;
                }

                // 如果無法生成有效題目，調整策略
                if (!validQuestion)
                {
                    numbers.Clear();
                    int minuend = rnd.Next(minMinuend, maxMinuend + 1);
                    numbers.Add(minuend);
                    difference = minuend;

                    // 確保結果為正數（如果不允許負數）
                    int remainingSubtract = allowNegative ? 
                        minuend + maxResult : 
                        Math.Min(minuend, maxMinuend);

                    for (int j = 1; j < numberCount; j++)
                    {
                        int maxSubtract = remainingSubtract / (numberCount - j);
                        int subtrahend = rnd.Next(minSubtrahend, Math.Min(maxSubtrahend, maxSubtract) + 1);
                        numbers.Add(subtrahend);
                        difference -= subtrahend;
                        remainingSubtract -= subtrahend;
                    }
                }

                // 產生橫式排列的題目格式：999-234-123=
                var questionParts = new List<string>();
                for (int j = 0; j < numbers.Count; j++)
                {
                    if (j == 0)
                    {
                        questionParts.Add(numbers[j].ToString());
                    }
                    else
                    {
                        questionParts.Add($"-{numbers[j]}");
                    }
                }
                var questionText = string.Join("", questionParts) + "=";

                // 建立題目物件
                var entry = new Vocabulary
                {
                    CategoryType = "Subtraction",
                    Question = questionText,
                    Answer = difference.ToString(),
                    ClassName = ClassName,
                    TestType = TestType,
                    LessionID = LessionID
                };

                entries.Add(entry);
            }

            // 儲存題目到資料庫
            foreach (var question in entries)
            {
                await _examRepo.InsertWord(question);
                Guid WordID = await _examRepo.GetWordID(question.Question, question.Answer);
                question.WordID = WordID;

                var ExamRcdData = new ExamRcdModel
                {
                    WordID = WordID,
                    NewKidTestID = NewKidTestID
                };
                await _examRepo.InsertExamIndex(ExamRcdData);
            }

            result.VocabularyList = entries;
            await CalculateScore(result.VocabularyList, result);
            result.Title = $"減法運算 - {minuendDigits}位數 - {subtrahendDigits}位數 × {numberCount - 1}個減數{(allowNegative ? " (含負數)" : "")}";

            return result;
        }

        /// <summary>
        /// 產生乘法題目
        /// </summary>
        /// <param name="KidID">學生ID</param>
        /// <param name="questionCount">題數</param>
        /// <param name="multiplicandDigits">被乘數位數</param>
        /// <param name="multiplierDigits">乘數位數</param>
        /// <param name="resultDigitCount">結果位數限制</param>
        public async Task<ExamDataViewModel_result> GenerateMultiplicationQuestions(
            string KidID,
            int questionCount,
            int multiplicandDigits,
            int multiplierDigits,
            int resultDigitCount)
        {
            var result = new ExamDataViewModel_result
            {
                scoreTable = new ScoreTable(),
                VocabularyList = new List<Vocabulary>(),
                Title = string.Empty
            };

            var entries = new List<Vocabulary>();
            var rnd = new Random();
            var ClassName = $"Multiplication_{multiplicandDigits}x{multiplierDigits}_{DateTime.Now:yyyyMMdd_HHmmss}";
            var TestType = "Math";

            // 建立課程ID
            Guid LessionID = await GetLessionID(ClassName, TestType);

            // 判斷今天是否出過同樣的考券
            Guid KidTestIndexID = await _examRepo.ChkKidTest(ClassName, TestType, KidID);

            if (KidTestIndexID != Guid.Empty)
            {
                result.VocabularyList = await _examRepo.GetExamFromExamIndex(KidTestIndexID);
                await CalculateScore(result.VocabularyList, result);
                result.Title = ClassName;
                return result;
            }

            // 建立新的測驗索引
            Guid NewKidTestID = await _examRepo.InsertKidTestIndex(LessionID, KidID);

            // 數字範圍
            int minMultiplicand = (int)Math.Pow(10, multiplicandDigits - 1);
            int maxMultiplicand = (int)Math.Pow(10, multiplicandDigits) - 1;
            int minMultiplier = (int)Math.Pow(10, multiplierDigits - 1);
            int maxMultiplier = (int)Math.Pow(10, multiplierDigits) - 1;
            int maxResult = (int)Math.Pow(10, resultDigitCount) - 1;

            // 確保結果位數至少為2
            if (resultDigitCount < 2)
            {
                resultDigitCount = 2;
                maxResult = 99;
            }

            // 產生題目
            for (int i = 0; i < questionCount; i++)
            {
                int multiplicand = 0;
                int multiplier = 0;
                int product = 0;
                bool validQuestion = false;

                // 重試機制
                int retryCount = 0;
                while (!validQuestion && retryCount < 100)
                {
                    multiplicand = rnd.Next(minMultiplicand, maxMultiplicand + 1);
                    multiplier = rnd.Next(minMultiplier, maxMultiplier + 1);
                    product = multiplicand * multiplier;

                    // 檢查結果是否符合位數限制
                    if (product <= maxResult && product >= 10) // 確保至少2位數
                    {
                        validQuestion = true;
                    }

                    retryCount++;
                }

                // 如果無法生成有效題目，調整範圍
                if (!validQuestion)
                {
                    // 使用較小的數字確保結果符合限制
                    int adjustedMaxMultiplicand = (int)Math.Sqrt(maxResult);
                    multiplicand = rnd.Next(
                        Math.Min(minMultiplicand, adjustedMaxMultiplicand),
                        Math.Min(maxMultiplicand, adjustedMaxMultiplicand) + 1);
                    
                    int maxPossibleMultiplier = maxResult / multiplicand;
                    multiplier = rnd.Next(
                        Math.Min(minMultiplier, maxPossibleMultiplier),
                        Math.Min(maxMultiplier, maxPossibleMultiplier) + 1);
                    
                    product = multiplicand * multiplier;
                }

                // 產生橫式排列的題目格式：3×8=
                var questionText = $"{multiplicand}×{multiplier}=";

                // 建立題目物件
                var entry = new Vocabulary
                {
                    CategoryType = "Multiplication",
                    Question = questionText,
                    Answer = product.ToString(),
                    ClassName = ClassName,
                    TestType = TestType,
                    LessionID = LessionID
                };

                entries.Add(entry);
            }

            // 儲存題目到資料庫
            foreach (var question in entries)
            {
                await _examRepo.InsertWord(question);
                Guid WordID = await _examRepo.GetWordID(question.Question, question.Answer);
                question.WordID = WordID;

                var ExamRcdData = new ExamRcdModel
                {
                    WordID = WordID,
                    NewKidTestID = NewKidTestID
                };
                await _examRepo.InsertExamIndex(ExamRcdData);
            }

            result.VocabularyList = entries;
            await CalculateScore(result.VocabularyList, result);
            result.Title = $"乘法運算 - {multiplicandDigits}位數 × {multiplierDigits}位數";

            return result;
        }

        /// <summary>
        /// 產生除法題目
        /// </summary>
        /// <param name="KidID">學生ID</param>
        /// <param name="dividendDigits">被除數位數</param>
        /// <param name="divisorDigits">除數位數</param>
        /// <param name="exactCount">整除題數</param>
        /// <param name="remainderCount">有餘數題數</param>
        public async Task<ExamDataViewModel_result> GenerateDivisionQuestions(
            string KidID,
            int dividendDigits,
            int divisorDigits,
            int exactCount,
            int remainderCount)
        {
            var result = new ExamDataViewModel_result
            {
                scoreTable = new ScoreTable(),
                VocabularyList = new List<Vocabulary>(),
                Title = string.Empty
            };

            var entries = new List<Vocabulary>();
            var rnd = new Random();
            int totalQuestions = exactCount + remainderCount;
            var ClassName = $"Division_{dividendDigits}div{divisorDigits}_E{exactCount}R{remainderCount}_{DateTime.Now:yyyyMMdd_HHmmss}";
            var TestType = "Math";

            // 建立課程ID
            Guid LessionID = await GetLessionID(ClassName, TestType);

            // 判斷今天是否出過同樣的考券
            Guid KidTestIndexID = await _examRepo.ChkKidTest(ClassName, TestType, KidID);

            if (KidTestIndexID != Guid.Empty)
            {
                result.VocabularyList = await _examRepo.GetExamFromExamIndex(KidTestIndexID);
                await CalculateScore(result.VocabularyList, result);
                result.Title = ClassName;
                return result;
            }

            // 建立新的測驗索引
            Guid NewKidTestID = await _examRepo.InsertKidTestIndex(LessionID, KidID);

            // 數字範圍
            int minDivisor = (int)Math.Pow(10, divisorDigits - 1);
            int maxDivisor = (int)Math.Pow(10, divisorDigits) - 1;

            // 建立題目類型列表（整除 + 有餘數）
            var questionTypes = new List<bool>(); // true = 整除, false = 有餘數
            
            // 加入整除題目
            for (int i = 0; i < exactCount; i++)
            {
                questionTypes.Add(true);
            }
            
            // 加入有餘數題目
            for (int i = 0; i < remainderCount; i++)
            {
                questionTypes.Add(false);
            }

            // 隨機打亂題目順序
            questionTypes = questionTypes.OrderBy(x => Guid.NewGuid()).ToList();

            // 產生題目
            for (int i = 0; i < totalQuestions; i++)
            {
                bool isExactDivision = questionTypes[i];
                int divisor = 0;
                int quotient = 0;
                int dividend = 0;
                int remainder = 0;
                bool validQuestion = false;

                // 數字範圍
                int minDividend = dividendDigits == 1 ? 1 : (int)Math.Pow(10, dividendDigits - 1);
                int maxDividend = (int)Math.Pow(10, dividendDigits) - 1;

                // 重試機制，確保題目符合位數要求
                int retryCount = 0;
                while (!validQuestion && retryCount < 100)
                {
                    // 避免除數為0
                    do
                    {
                        divisor = rnd.Next(minDivisor, maxDivisor + 1);
                    } while (divisor == 0);

                    if (isExactDivision)
                    {
                        // 整除：先決定商，再計算被除數，確保被除數符合位數要求
                        // 計算商的合理範圍：被除數 = 除數 × 商
                        // 被除數範圍：[minDividend, maxDividend]
                        // 商範圍：[minDividend / maxDivisor, maxDividend / minDivisor]
                        int minQuotient = Math.Max(1, minDividend / maxDivisor);
                        int maxQuotient = maxDividend / divisor;
                        
                        // 如果商範圍不合理，調整
                        if (minQuotient < 1) minQuotient = 1;
                        if (maxQuotient < minQuotient)
                        {
                            // 如果被除數位數小於等於除數位數，商只能是1（或0，但0不合理）
                            minQuotient = 1;
                            maxQuotient = 1;
                        }

                        quotient = rnd.Next(minQuotient, maxQuotient + 1);
                        dividend = divisor * quotient;
                        remainder = 0;

                        // 檢查被除數是否符合位數要求
                        if (dividend >= minDividend && dividend <= maxDividend)
                        {
                            validQuestion = true;
                        }
                    }
                    else
                    {
                        // 有餘數：產生被除數，計算商和餘數，確保有餘數且符合位數要求
                        dividend = rnd.Next(minDividend, maxDividend + 1);
                        quotient = dividend / divisor;
                        remainder = dividend % divisor;

                        // 確保有餘數且商不為0（如果被除數 >= 除數）
                        if (remainder > 0 && quotient > 0)
                        {
                            validQuestion = true;
                        }
                        else if (dividend < divisor)
                        {
                            // 如果被除數 < 除數，商為0，餘數 = 被除數，這也算有餘數
                            quotient = 0;
                            remainder = dividend;
                            validQuestion = true;
                        }
                    }

                    retryCount++;
                }

                // 如果仍然無法生成有效題目，使用更保守的策略
                if (!validQuestion)
                {
                    // 避免除數為0
                    do
                    {
                        divisor = rnd.Next(minDivisor, maxDivisor + 1);
                    } while (divisor == 0);

                    if (isExactDivision)
                    {
                        // 整除：使用最小的合理值
                        if (dividendDigits >= divisorDigits)
                        {
                            // 被除數位數 >= 除數位數，可以使用商 >= 1
                            quotient = Math.Max(1, minDividend / divisor);
                            if (quotient < 1) quotient = 1;
                        }
                        else
                        {
                            // 被除數位數 < 除數位數，只能嘗試商 = 1（如果可能）
                            if (divisor <= maxDividend)
                            {
                                quotient = 1;
                            }
                            else
                            {
                                // 無法整除，改為有餘數題目
                                quotient = 0;
                            }
                        }
                        
                        dividend = divisor * quotient;
                        
                        // 如果被除數超出範圍，調整
                        if (dividend > maxDividend)
                        {
                            // 調整為最大的合理被除數
                            dividend = (maxDividend / divisor) * divisor;
                            quotient = dividend / divisor;
                        }
                        
                        if (dividend < minDividend)
                        {
                            dividend = Math.Max(minDividend, divisor);
                            quotient = dividend / divisor;
                            remainder = dividend % divisor;
                            // 如果無法整除，改為有餘數
                            if (remainder > 0)
                            {
                                isExactDivision = false;
                            }
                        }
                        
                        remainder = 0;
                        validQuestion = true;
                    }
                    else
                    {
                        // 有餘數：確保被除數不是除數的倍數
                        dividend = rnd.Next(minDividend, maxDividend + 1);
                        
                        // 確保不是整除
                        while (dividend % divisor == 0 && dividend < maxDividend)
                        {
                            dividend++;
                        }
                        
                        // 如果達到最大值仍整除，減1
                        if (dividend % divisor == 0 && dividend > minDividend)
                        {
                            dividend--;
                        }
                        
                        quotient = dividend / divisor;
                        remainder = dividend % divisor;
                        
                        // 如果還是整除（極端情況），手動添加餘數
                        if (remainder == 0 && dividend < maxDividend)
                        {
                            int addRemainder = rnd.Next(1, Math.Min(divisor, maxDividend - dividend));
                            dividend += addRemainder;
                            quotient = dividend / divisor;
                            remainder = dividend % divisor;
                        }
                        
                        validQuestion = true;
                    }
                }

                // 產生題目格式（簡潔格式，不提示答案欄位）
                string questionText = $"{dividend} ÷ {divisor} =";
                string answer = remainder == 0 
                    ? quotient.ToString() 
                    : $"{quotient} ... {remainder}";

                // 建立題目物件
                var entry = new Vocabulary
                {
                    CategoryType = "Division",
                    Question = questionText,
                    Answer = answer,
                    ClassName = ClassName,
                    TestType = TestType,
                    LessionID = LessionID
                };

                entries.Add(entry);
            }

            // 儲存題目到資料庫
            foreach (var question in entries)
            {
                await _examRepo.InsertWord(question);
                Guid WordID = await _examRepo.GetWordID(question.Question, question.Answer);
                question.WordID = WordID;

                var ExamRcdData = new ExamRcdModel
                {
                    WordID = WordID,
                    NewKidTestID = NewKidTestID
                };
                await _examRepo.InsertExamIndex(ExamRcdData);
            }

            result.VocabularyList = entries;
            await CalculateScore(result.VocabularyList, result);
            result.Title = $"除法運算 - {dividendDigits}位數 ÷ {divisorDigits}位數 (整除{exactCount}題 + 有餘數{remainderCount}題)";

            return result;
        }

        #endregion
    }

}
