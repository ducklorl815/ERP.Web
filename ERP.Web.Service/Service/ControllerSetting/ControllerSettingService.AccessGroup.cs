using ERP.Web.Models.Models.ControllerSetting;
using ERP.Web.Service.ViewModels.ControllerSetting;
using Newtonsoft.Json;

namespace ERP.Web.Service.Service.ControllerSetting
{
    public partial class ControllerSettingService
    {

        public async Task<bool> SaveAccessGroup(AccessGroupModel model)
        {
            var GroupName = model.GroupName;
            var NodeJson = JsonConvert.SerializeObject(model.SelectedNodes);

            Guid InsertID = await _controllerSettingRepo.SaveAccessGroup(GroupName, NodeJson);

            // 🔹 將樹狀結構攤平成一維清單
            var allNodes = FlattenNodes(model.SelectedNodes);

            // 🔹 組成要寫入 DB 的模型
            var groupData = new AccessGroupMainModel
            {
                GroupName = model.GroupName,
                Nodes = allNodes
            };
            // 存入權限群組資料庫
            return true;

        }
        // 🔹 遞迴攤平 TreeNode 結構
        private List<NodeRecord> FlattenNodes(List<TreeNodeModel> nodes)
        {
            var result = new List<NodeRecord>();

            foreach (var node in nodes)
            {
                result.Add(new NodeRecord
                {
                    ID = node.ID,
                    DisplayName = node.DisplayName
                });

                if (node.Children != null && node.Children.Any())
                {
                    result.AddRange(FlattenNodes(node.Children));
                }
            }

            return result;
        }
        private List<TreeNodeModel> RestoreTree(List<NodeRecord> flatList)
        {
            var lookup = flatList.ToDictionary(x => x.ID, x => new TreeNodeModel
            {
                ID = x.ID,
                DisplayName = x.DisplayName,
                Children = new List<TreeNodeModel>()
            });

            var roots = new List<TreeNodeModel>();

            foreach (var node in flatList)
            {
                if (node.ParentControllerMainID == null)
                {
                    roots.Add(lookup[node.ID]);
                }
                else if (lookup.ContainsKey(node.ParentControllerMainID))
                {
                    lookup[node.ParentControllerMainID].Children.Add(lookup[node.ID]);
                }
            }

            return roots;
        }
    }
}
