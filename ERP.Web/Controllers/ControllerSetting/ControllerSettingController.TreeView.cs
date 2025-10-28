using ERP.Web.Service.ViewModels.ControllerSetting;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.ControllerSetting
{

    public partial class ControllerSettingController : Controller
    {

        /// <summary>
        /// 查詢AccessGroup
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> AccessGroupSearchList(AccessGroupSearchListViewModel_param param)
        {

            var result = await _controllerSettingService.AccessGroupSearchList(param);
            if (Request.IsAjaxRequest())
                return PartialView("_AccessGroupSearchList", result.List);

            return View(result);
        }
        /// <summary>
        /// 權限群組樹狀圖
        /// </summary>
        /// <param name="ModuleIDs"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> TreeView(string ModuleIDs)
        {
            //var result = await _controllerSettingService.ErpTreeView(ModuleIDs);
            //if (Request.IsAjaxRequest())
            //    return PartialView("_TreePartial", result.List);

            //return View("ErpTreeView", result);

            var result = await _controllerSettingService.TreeView(ModuleIDs);
            if (Request.IsAjaxRequest())
                return PartialView("_TreePartial", result.List);

            return View(result);
        }
        /// <summary>
        /// 樹狀結構維護
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public async Task<IActionResult> TreeDataMaintain(string ID)
        {
            //ModuleID = "171ec9c6-4ca1-49f8-b5bb-3ec3e1e9da5a".ToUpper();
            var result = await _controllerSettingService.TreeDataMaintain(ID);
            return View("TreeViewDataMaintain", result);
        }
        /// <summary>
        /// 新增AccessGroup
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 更新AccessGroup
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
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
        /// <summary>
        /// 刪除AccessGroup
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> DeleteAccessGroup(string ID)
        {

            bool DeleteAccessGroup = await _controllerSettingService.DeleteAccessGroup(ID);

            return Ok(new
            {
                success = DeleteAccessGroup
            });
        }
    }
}
