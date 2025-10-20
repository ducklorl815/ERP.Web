using ERP.Web.Service.ViewModels.ControllerSetting;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.ControllerSetting
{
    public partial class ControllerSettingController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> TreeView(string ModuleIDs)
        {
            //var model = new ErpMenuDataViewModel
            //{
            //    List = await _controllerSettingService.ErpTreeView(),
            //    CurrentNodeId = currentId
            //};
            //return View("ErpTreeView", model);

            var result = await _controllerSettingService.TreeView(ModuleIDs);
            if (Request.IsAjaxRequest())
                return PartialView("_TreePartial", result.List);

            return View("TreeViewDataMaintain", result);
        }

        public async Task<IActionResult> TreeDataMaintain([FromBody] AccessGroupViewModel model)
        {

            return View("TreeViewDataMaintain");
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
