using ERP.Web.Service.ViewModels.ControllerSetting;
using ERP.Web.Utility.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.ControllerSetting
{
    public partial class ControllerSettingController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> TreeView(Guid CurrentId)
        {
            //var model = new ErpMenuDataViewModel
            //{
            //    List = await _controllerSettingService.ErpTreeView(),
            //    CurrentNodeId = currentId
            //};
            //return View("ErpTreeView", model);
            var model = new LeftSidebarViewModel
            {
                List = await _controllerSettingService.TreeView(CurrentId),
                CurrentNodeId = CurrentId
            };
            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> SaveAccessGroup([FromBody] AccessGroupViewModel model)
        {
            if (model == null)
                return BadRequest("無效的資料");

            if (string.IsNullOrWhiteSpace(model.GroupName))
                return BadRequest("群組名稱不得為空");

            if (model.SelectedNodes == null || !model.SelectedNodes.Any())
                return BadRequest("請至少選擇一個節點");

            bool SaveAccessGroup = await _controllerSettingService.SaveAccessGroup(model);

            return Ok(new
            {
                success = SaveAccessGroup
            });
        }
    }
}
