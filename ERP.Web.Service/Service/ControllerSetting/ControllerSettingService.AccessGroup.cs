using ERP.Web.Models.Models.ControllerSetting;
using ERP.Web.Models.Respository.ControllerSetting;
using ERP.Web.Service.ViewModels.ControllerSetting;
using ERP.Web.Utility.Paging;
using ERP.Web.Utility.ViewModel;
using Newtonsoft.Json;

namespace ERP.Web.Service.Service.ControllerSetting
{
    public partial class ControllerSettingService
    {
        /// <summary>   
        /// 取得權限群組清單
        /// </summary>
        /// <param name="param"></param>
        /// <returns></returns>
        public async Task<AccessGroupSearchListViewModel_result> AccessGroupSearchList(AccessGroupSearchListViewModel_param param)
        {
            var result = new AccessGroupSearchListViewModel_result(){
                Keyword = new AccessGroupKeyword(),
            };
            var AccessGroupMainKeyword = new ControllerSettingRepo.AccessGroupMainKeyword
            {
                GroupName = param?.Keyword?.GroupName,
                GroupDesc = param?.Keyword?.GroupDesc,
            };
            var datacount = await _controllerSettingRepo.GetAccessGroupListCountAsync(AccessGroupMainKeyword);
            var pager = new Paging(param.Page, param.PageSize, datacount);
            result.Pager = pager;
            result.List = await _controllerSettingRepo.GetAccessGroupSearchListAsync(pager, AccessGroupMainKeyword);
            return result;
        }


        /// <summary>
        /// 儲存新的模組
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<bool> SaveAccessGroup(AccessGroupViewModel model)
        {
            var GroupName = model.GroupName;
            var GroupDesc = model.GroupDesc;
            var NodeJson = JsonConvert.SerializeObject(model.SelectedNodes);

            var InsertID = await _controllerSettingRepo.SaveAccessGroup(GroupName, GroupDesc, NodeJson);

            // 🔹 將樹狀結構攤平成一維清單
            var allNodes = await FlattenNodes(model.SelectedNodes);

            // 🔹 組成要寫入 DB 的模型
            var groupData = new AccessGroupParallelModel
            {
                GroupName = model.GroupName,
                Nodes = allNodes
            };
            // 存入權限群組資料庫
            return true;

        }
        /// <summary>
        /// 更新新的模組
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public async Task<bool> UpdateAccessGroup(AccessGroupViewModel model)
        {
            var ID = model.ID;
            var GroupName = model.GroupName;
            var GroupDesc = model.GroupDesc;
            var NodeJson = JsonConvert.SerializeObject(model.SelectedNodes);

            bool ChkUpdateAccess = string.IsNullOrEmpty(ID)?
                await _controllerSettingRepo.SaveAccessGroup(GroupName, GroupDesc, NodeJson):
                await _controllerSettingRepo.UpdateAccessGroup(ID, GroupName, GroupDesc, NodeJson);

            return ChkUpdateAccess;
        }
        /// <summary>
        /// 刪除AccessGroup
        /// </summary>
        /// <param name="ID"></param>
        /// <returns></returns>
        public async Task<bool> DeleteAccessGroup(string ID)
        {
            bool DeleteAccessGroup = await _controllerSettingRepo.DeleteAccessGroup(ID);

            return DeleteAccessGroup;
        }
        
        // 🔹 遞迴攤平 TreeNode 結構
        /// <summary>
        /// 遞迴攤平 TreeNode 結構
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        private async Task<List<NodeRecord>> FlattenNodes(List<TreeNodeModel> nodes)
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
                    result.AddRange(await FlattenNodes(node.Children));
                }
            }

            return result;
        }
        /// <summary>
        /// 更新ErpMenuCheckStatus
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="menuData"></param>
        /// <param name="updatedNodeIds"></param>
        /// <returns></returns>
        private async Task<List<ErpMenuData>> UpdateErpMenuCheckStatus(List<TreeNodeModel> nodes, List<ErpMenuData> menuData, HashSet<Guid> updatedNodeIds)
        {
            if (nodes == null || !nodes.Any() || menuData == null || !menuData.Any())
                return menuData;

            foreach (var node in nodes)
            {
                if (!Guid.TryParse(node.ID.ToString(), out Guid nodeId))
                    continue;

                // 如果該節點還沒更新過，才執行設定
                if (!updatedNodeIds.Contains(nodeId))
                {
                    var target = menuData.FirstOrDefault(m => m.ID == nodeId);
                    if (target != null)
                    {
                        target.IsMenu = true;
                        updatedNodeIds.Add(nodeId); // ✅ 記錄這個節點已處理
                    }
                }

                // 遞迴處理子節點
                if (node.Children != null && node.Children.Any())
                {
                    await UpdateErpMenuCheckStatus(node.Children, menuData, updatedNodeIds);
                }
            }

            return menuData;
        }
        /// <summary>
        /// 遞迴比對節點清單，更新 menuData 的勾選狀態。
        /// 為避免不同群組重複節點重複設定，使用 HashSet 過濾。
        /// </summary>
        private async Task<List<MenuData>> UpdateMenuCheckStatus(List<TreeNodeModel> nodes, List<MenuData> menuData, HashSet<Guid> updatedNodeIds)
        {
            if (nodes == null || !nodes.Any() || menuData == null || !menuData.Any())
                return menuData;

            foreach (var node in nodes)
            {
                if (!Guid.TryParse(node.ID.ToString(), out Guid nodeId))
                    continue;

                // 如果該節點還沒更新過，才執行設定
                if (!updatedNodeIds.Contains(nodeId))
                {
                    var target = menuData.FirstOrDefault(m => m.ID == nodeId);
                    if (target != null)
                    {
                        target.IsCheck = true;
                        updatedNodeIds.Add(nodeId); // ✅ 記錄這個節點已處理
                    }
                }

                // 遞迴處理子節點
                if (node.Children != null && node.Children.Any())
                {
                    await UpdateMenuCheckStatus(node.Children, menuData, updatedNodeIds);
                }
            }

            return menuData;
        }
        /// <summary>
        /// 恢復Tree結構
        /// </summary>
        /// <param name="flatList"></param>
        /// <returns></returns>

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
