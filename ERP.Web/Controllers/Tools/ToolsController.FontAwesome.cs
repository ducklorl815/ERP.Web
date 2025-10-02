using ERP.Web.Models.Models.Tools;
using ERP.Web.Service.ViewModels.Tools;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace ERP.Web.Controllers.Tools
{
    public partial class ToolsController : Controller
    {
        public async Task<IActionResult> ImportFontJson(IFormFile JsonFile)
        {
            var result = new FontAwesomeViewModel_result();
            if (JsonFile == null || JsonFile.Length == 0)
            {
                result.IsSuccess = false;
                result.Msg = "請選擇一個 JSON 檔案";
                return Json(result);
            }
            result = await _toolsService.ImportFontJson(JsonFile);
            return Json(result);
        }
    }
}
