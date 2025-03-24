using ERP.Web.Service.Service;
using ERP.Web.Service.ViewModels;
using Microsoft.AspNetCore.Mvc;

public class ExamController : Controller
{
    private readonly ExamService _examService;
    public ExamController(ExamService examService)
    {
        _examService = examService;
    }

    public async Task<IActionResult> Index()
    {
        ExamSearchListViewModel_result result = await _examService.GetIndexAsync();
        return View(result);
    }
    public async Task<IActionResult> GetList(ExamSearchListViewModel_param param)
    {
        ExamSearchListViewModel_result result = await _examService.GetListAsync(param);
        return View(result);
    }
    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("請上傳 Excel 檔案");
        }

        var chkUpload = await _examService.GetUploadFileAsync(file);
        return Json("true");
    }
    public async Task<IActionResult> Test(string Class,string KidID)
    {
        var result = await _examService.GetExamDataAsync(Class, KidID);

        return View(result);
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
