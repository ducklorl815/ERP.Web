using ERP.Web.Utility.ViewModel;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.ControllerSetting
{
    public partial class ControllerSettingController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> TreeView(Guid currentId)
        {
            //var model = new ErpMenuDataViewModel
            //{
            //    List = await _controllerSettingService.ErpTreeView(),
            //    CurrentNodeId = currentId
            //};
            //return View("ErpTreeView", model);
            var model = new LeftSidebarViewModel
            {
                List = await _controllerSettingService.TreeView(),
                CurrentNodeId = currentId
            };
            return View(model);
        }


        [HttpPost]
        public IActionResult SaveGroupPermissions([FromBody] GroupPermissionModel model)
        {
            // model.GroupName
            // model.SelectedNodeIds -> List<string> 或 List<Guid>
            // 你後續可以補完整性驗證、寫入 DB
            return Ok(new { success = true });
        }

        // 對應的 ViewModel
        public class GroupPermissionModel
        {
            public string GroupName { get; set; }

            public List<TreeNodeModel> SelectedNodes { get; set; }
        }

        public class TreeNodeModel
        {
            public Guid Id { get; set; }      // 對應 JSON id
            public string Text { get; set; }  // 對應 JSON text
            public List<TreeNodeModel> Children { get; set; } // 對應 JSON children
        }
    }
}
