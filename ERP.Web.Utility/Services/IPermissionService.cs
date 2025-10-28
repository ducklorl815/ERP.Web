namespace ERP.Web.Utility.Services
{
    /// <summary>
    /// 權限檢查服務介面
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
        /// 檢查使用者是否有特定 Controller Action 的權限
        /// </summary>
        /// <param name="userName">使用者名稱</param>
        /// <param name="controller">Controller 名稱</param>
        /// <param name="action">Action 名稱</param>
        /// <returns>是否有權限</returns>
        Task<bool> HasPermissionAsync(string userName, string controller, string action);

        /// <summary>
        /// 取得使用者的所有權限清單
        /// </summary>
        /// <param name="userName">使用者名稱</param>
        /// <returns>權限清單（Controller.Action 格式）</returns>
        Task<HashSet<string>> GetUserPermissionsAsync(string userName);

        /// <summary>
        /// 檢查使用者是否有特定功能 ID 的權限
        /// </summary>
        /// <param name="userName">使用者名稱</param>
        /// <param name="controllerMainID">Controller Main ID</param>
        /// <returns>是否有權限</returns>
        Task<bool> HasPermissionByIdAsync(string userName, Guid controllerMainID);

        void ClearUserPermissionsCache(string userName);
    }
}

