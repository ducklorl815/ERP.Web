using ERP.Web.Utility.Respository;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace ERP.Web.Utility.Services
{
    /// <summary>
    /// 權限檢查服務實作
    /// 提供快取機制以提升效能
    /// </summary>
    public class PermissionService : IPermissionService
    {
        private readonly ControllerUtilityRepo _controllerUtilityRepo;
        private readonly IConfiguration _configuration;

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30); // 快取 30 分鐘

        public PermissionService(
            IConfiguration configuration,
            IMemoryCache cache,
            IHttpContextAccessor httpContextAccessor)
        {
            _configuration = configuration;
            _controllerUtilityRepo = new ControllerUtilityRepo(configuration.GetConnectionString("UtilityERP"));
            _httpContextAccessor = httpContextAccessor;
        }

        private bool IsBypassEnabled()
        {
            return _configuration.GetValue<bool>("Permission:Bypass");
        }

        /// <summary>
        /// 檢查使用者是否有特定 Controller Action 的權限
        /// </summary>
        public async Task<bool> HasPermissionAsync(string userName, string controller, string action)
        {
            if (IsBypassEnabled())
                return true;

            if (string.IsNullOrEmpty(userName) || string.IsNullOrEmpty(controller) || string.IsNullOrEmpty(action))
                return false;

            var permissions = await GetUserPermissionsAsync(userName);
            
            // 檢查格式：Controller.Action
            var permissionKey = $"{controller}.{action}".ToLower();
            return permissions.Contains(permissionKey);
        }

        /// <summary>
        /// 取得使用者的所有權限清單（使用 Cookie 快取）
        /// </summary>
        public async Task<HashSet<string>> GetUserPermissionsAsync(string userName)
        {
            if (IsBypassEnabled())
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "*" };

            if (string.IsNullOrEmpty(userName))
                return new HashSet<string>();

            var cacheKey = $"UserPermissions_{userName}";
            var context = _httpContextAccessor.HttpContext;

            // 先從 Cookie 嘗試取得
            if (context.Request.Cookies.TryGetValue(cacheKey, out string cookieValue))
            {
                try
                {
                    var cookiePermissions = JsonConvert.DeserializeObject<HashSet<string>>(cookieValue);
                    if (cookiePermissions != null && cookiePermissions.Any())
                    {
                        return cookiePermissions;
                    }
                }
                catch
                {
                    // Cookie 損壞或無效時略過
                }
            }

            // Cookie 無資料，從資料庫重新取得
            var permissions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // 取得使用者的群組節點 JSON
                string nodeJson = await _controllerUtilityRepo.GetBoundAccessGroupData(userName);
                if (string.IsNullOrEmpty(nodeJson))
                {
                    WriteCookie(context, cacheKey, permissions);
                    return permissions;
                }

                // 解析節點並取得所有 Controller ID
                var nodes = JsonConvert.DeserializeObject<List<TreeNodeUtilityModel>>(nodeJson);
                var controllerIDList = await UtilityFlattenIDs(nodes);

                // 取得選單資料
                var menuData = await _controllerUtilityRepo.GetMenuDataAsync(controllerIDList);
                if (menuData == null || !menuData.Any())
                {
                    WriteCookie(context, cacheKey, permissions);
                    return permissions;
                }

                // 建立權限清單（Controller.Action 格式）
                foreach (var item in menuData)
                {
                    if (item == null) continue;

                    var controller = item.Controller as string;
                    var action = (item.ActName ?? item.Action) as string;

                    if (!string.IsNullOrEmpty(controller) && !string.IsNullOrEmpty(action))
                        permissions.Add($"{controller}.{action}".ToLower());
                }

                // 寫回 Cookie
                WriteCookie(context, cacheKey, permissions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"取得使用者權限時發生錯誤: {ex.Message}");
            }

            return permissions;
        }

        /// <summary>
        /// 將權限集合寫入 Cookie
        /// </summary>
        private void WriteCookie(HttpContext context, string key, HashSet<string> permissions)
        {
            try
            {
                var options = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // 若是 HTTPS 環境
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddMinutes(30) // 有效 30 分鐘
                };

                var json = JsonConvert.SerializeObject(permissions);
                context.Response.Cookies.Append(key, json, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"寫入 Cookie 失敗: {ex.Message}");
            }
        }


        /// <summary>
        /// 檢查使用者是否有特定功能 ID 的權限
        /// </summary>
        public async Task<bool> HasPermissionByIdAsync(string userName, Guid controllerMainID)
        {
            if (IsBypassEnabled())
                return true;

            if (string.IsNullOrEmpty(userName) || controllerMainID == Guid.Empty)
                return false;

            try
            {
                // 取得使用者的群組節點 JSON
                string nodeJson = await _controllerUtilityRepo.GetBoundAccessGroupData(userName);
                
                if (string.IsNullOrEmpty(nodeJson))
                    return false;

                var nodes = JsonConvert.DeserializeObject<List<TreeNodeUtilityModel>>(nodeJson);
                var controllerIDList = await UtilityFlattenIDs(nodes);

                // 檢查 ID 是否在清單中
                return controllerIDList.Contains(controllerMainID.ToString());
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 清除使用者的權限快取（用於權限變更時）
        /// </summary>
        public void ClearUserPermissionsCache(string userName)
        {
            if (string.IsNullOrWhiteSpace(userName))
                return;

            // 測試繞過模式下不需要清快取，但仍保留原本流程以避免呼叫端行為改變
            var context = _httpContextAccessor.HttpContext;
            if (context == null)
                return;

            var cacheKey = $"UserPermissions_{userName}";

            // 刪除 Cookie
            context.Response.Cookies.Delete(cacheKey);
        }

        #region Private Methods

        /// <summary>
        /// 遞迴展平樹狀節點，取得所有 ID
        /// </summary>
        private async Task<List<string>> UtilityFlattenIDs(List<TreeNodeUtilityModel> nodes)
        {
            var result = new List<string>();

            if (nodes == null)
                return result;

            foreach (var node in nodes)
            {
                result.Add(node.ID.ToString());

                if (node.Children != null && node.Children.Any())
                {
                    result.AddRange(await UtilityFlattenIDs(node.Children));
                }
            }

            return result;
        }

        #endregion

        #region Helper Models

        /// <summary>
        /// 樹狀節點模型（用於解析 JSON）
        /// </summary>
        private class TreeNodeUtilityModel
        {
            public Guid ID { get; set; }
            public string DisplayName { get; set; }
            public List<TreeNodeUtilityModel> Children { get; set; }
        }

        #endregion
    }
}

