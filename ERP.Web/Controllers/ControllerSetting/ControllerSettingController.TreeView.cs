using ERP.Web.Utility.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.ControllerSetting
{
    public partial class ControllerSettingController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> TreeView(Guid currentId)
        {
            var model = new LeftSidebarViewModel
            {
                List = await _controllerSettingService.TreeView(),
                CurrentNodeId = currentId
            };
            return View(model);
        }

    }
}
