using ERP.Web.Service.ViewModels;
using Microsoft.AspNetCore.Mvc;

public class ExamController : Controller
{
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
