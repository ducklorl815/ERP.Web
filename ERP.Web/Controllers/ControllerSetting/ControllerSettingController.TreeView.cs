using ERP.Web.Models.Models.ControllerSetting;
using ERP.Web.Service.ViewModels.ControllerSetting;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.ControllerSetting
{
    public partial class ControllerSettingController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> TreeView(string ModuleIDs)
        {
            var result = await _controllerSettingService.ErpTreeView(ModuleIDs);
            if (Request.IsAjaxRequest())
                return PartialView("_TreePartial", result.List);

            return View("ErpTreeView", result);

            //var result = await _controllerSettingService.TreeView(ModuleIDs);
            //if (Request.IsAjaxRequest())
            //    return PartialView("_TreePartial", result.List);

            //return View(result);
        }

        public async Task<IActionResult> TreeDataMaintain(string ModuleID)
        {
            ModuleID = "171ec9c6-4ca1-49f8-b5bb-3ec3e1e9da5a".ToUpper();
            var result = await _controllerSettingService.TreeDataMaintain(ModuleID);
            return View("TreeViewDataMaintain", result);
        }
        [HttpPost]
        public async Task<IActionResult> SaveAccessGroup([FromBody] AccessGroupViewModel model)
        {
            if (model == null)
                return BadRequest("無效的資料");

            bool SaveAccessGroup = await _controllerSettingService.SaveAccessGroup(model);

            return Ok(new
            {
                success = SaveAccessGroup
            });
        }
        [HttpPost]
        public async Task<IActionResult> UpdateAccessGroup([FromBody] AccessGroupViewModel model)
        {
            if (model == null)
                return BadRequest("無效的資料");

            bool UpdateAccessGroup = await _controllerSettingService.UpdateAccessGroup(model);

            return Ok(new
            {
                success = UpdateAccessGroup
            });
        }
    }
}
