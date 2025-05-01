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
            var pager = new Paging(param.Page, param.PageSize, datacount);

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
        public async Task<ExamDataViewModel_result> GetExamDataAsync(ExamSearchListViewModel_param param)
        {

            var result = new ExamDataViewModel_result
            {
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
                return result;
            }

            //取得考試資料
            result.VocabularyList = await GetExamData(param);

            Guid LessionID = await _examRepo.GetLessionID(result.Title);

            // 出過的題目存入資料庫
            Guid NewKidTestID = await _examRepo.InsertKidTestIndex(LessionID, param.TestType, param.KidID);

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

            int totalQuestions = param.ClassNameList.Count() == 1 ? groupedByClass[0].Count() : 20;


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

                        //string Category = string.Empty;
                        //List<string> ClassArrey = new List<string>();
                        //if (ClassName.Contains("Sp"))
                        //{
                        //    ClassArrey = ClassName.Split("Sp").ToList();
                        //    Category = "Sp";
                        //}
                        //if (ClassName.Contains("HW"))
                        //{
                        //    ClassArrey = ClassName.Split("HW").ToList();
                        //    Category = "HW";
                        //}
                        //var ClassNumChk = ClassArrey[1].Trim();
                        //if (!int.TryParse(ClassNumChk, out int ClassNum))
                        //    return false;

                        for (int row = 2; row <= rowCount; row++) // 從第 2 行開始，因為第 1 行是標題
                        {
                            string CategoryType = worksheet.Cells[row, 1].Text.Trim();
                            string Question = worksheet.Cells[row, 2].Text.Trim();
                            string Answer = worksheet.Cells[row, 3].Text.Trim();
                            string ChkDone = worksheet.Cells[row, 4].Text.Trim().ToLower();
                            string Grade = worksheet.Cells[row, 5].Text.Trim().ToLower();
                            string TestType = worksheet.Cells[row, 6].Text.Trim().ToLower();


                            if (!string.IsNullOrEmpty(Question) && !string.IsNullOrEmpty(Answer))
                            {
                                vocabularies.Add(new Vocabulary
                                {
                                    CategoryType = CategoryType,
                                    //Class = ClassArrey[0].Trim(),
                                    ClassName = ClassName,
                                    //ClassNum = ClassNum,
                                    //Category = Category,
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

                    // 檢查是否已有相同單字
                    bool checkWord = await _examRepo.chkSameWord(vocab);

                    if (!checkWord)
                    {
                        Guid LessionID = await GetLessionID(vocab);
                        if (LessionID == Guid.Empty)
                            return false;
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
        private async Task<Guid> GetLessionID(Vocabulary param)
        {
            Guid LessionID = await _examRepo.ChkLessionID(param);
            if (LessionID == Guid.Empty)
            {
                int LessionSort = await _examRepo.GetLessionSort();
                var LessionData = new LessionModel
                {
                    ClassName = param.ClassName,
                    ClassNum = param.ClassNum,
                    TestType = "English",
                    Category = param.Category,
                    CategoryType = param.CategoryType,
                    LessionSort = LessionSort,
                };
                LessionID = await _examRepo.InsertLessionID(LessionData);
                return LessionID;
            }
            return LessionID;
        }
        private async Task<string> GetCategory(string Category)
        {
            switch (Category.ToUpper())
            {
                case "HW":
                    return "Phrase";
                case "SP":
                    return "Word";
                default:
                    return "NONE";
            }
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


    }

}
