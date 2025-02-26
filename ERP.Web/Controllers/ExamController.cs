using ERP.Web.Service.Service;
using ERP.Web.Service.ViewModels;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver.Core.Configuration;
using OfficeOpenXml;
using System.Collections.Generic;
using System.Data.SqlClient;

public class ExamController : Controller
{
    private readonly ExamService _examService;
    public ExamController(ExamService examService)
    {
        _examService = examService;
    }
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("請上傳 Excel 檔案");
        }

        await _examService.GetUploadFileAsync(file);
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
            await SaveToDatabase(vocabularies);

            return Ok("Excel 資料成功匯入");
        }
        catch (Exception ex)
        {
            return StatusCode(500, "發生錯誤：" + ex.Message);
        }
    }

    private void SaveToDatabase(List<Vocabulary> vocabularies)
    {
        using (SqlConnection conn = new SqlConnection(_connectionString))
        {
            conn.Open();

            foreach (var vocab in vocabularies)
            {
                // 檢查是否已有相同單字
                string checkSql = "SELECT COUNT(1) FROM Vocabulary WHERE Course = @Course AND Word = @Word";
                using (SqlCommand checkCmd = new SqlCommand(checkSql, conn))
                {
                    checkCmd.Parameters.AddWithValue("@Course", vocab.Course);
                    checkCmd.Parameters.AddWithValue("@Word", vocab.Word);

                    int count = (int)checkCmd.ExecuteScalar();
                    if (count > 0) continue; // 若已存在，則跳過
                }

                // 插入新單字
                string insertSql = "INSERT INTO Vocabulary (Course, Word, Meaning) VALUES (@Course, @Word, @Meaning)";
                using (SqlCommand insertCmd = new SqlCommand(insertSql, conn))
                {
                    insertCmd.Parameters.AddWithValue("@Course", vocab.Course);
                    insertCmd.Parameters.AddWithValue("@Word", vocab.Word);
                    insertCmd.Parameters.AddWithValue("@Meaning", vocab.Meaning);
                    insertCmd.ExecuteNonQuery();
                }
            }
        }
    }


    public IActionResult Index()
    {
        // 死資料 (未來可改為從資料庫撈取)
        var questions = new List<ExamQuestionViewModel>
        {
            new ExamQuestionViewModel { Id = 1, Question = "She ____ (be) a teacher.", Answer = "is" },
            new ExamQuestionViewModel { Id = 2, Question = "They ____ (go) to school every day.", Answer = "go" },
            new ExamQuestionViewModel { Id = 3, Question = "The sun ____ (rise) in the east.", Answer = "rises" }
        };

        return View(questions);
    }
    [HttpPost]
    public IActionResult Submit(List<string> answers)
    {
        var correctAnswers = new List<string> { "is", "go", "rises" };
        int score = 0;

        for (int i = 0; i < correctAnswers.Count; i++)
        {
            if (answers[i].Trim().ToLower() == correctAnswers[i])
            {
                score++;
            }
        }

        ViewBag.Score = score;
        return View("Result");
    }
}
