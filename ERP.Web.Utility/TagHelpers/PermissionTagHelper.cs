using ERP.Web.Utility.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Razor.TagHelpers;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ERP.Web.Utility.TagHelpers
{
    /// <summary>
    /// 權限檢查 TagHelper
    /// 根據使用者權限決定是否顯示元素
    /// 
    /// 使用方式：
    /// <div permission-controller="ControllerSetting" permission-action="TreeView">
    ///     <a href="/ControllerSetting/TreeView">新增群組</a>
    /// </div>
    /// 
    /// 或簡化版：
    /// <a permission-check="ControllerSetting.TreeView" asp-action="TreeView">新增群組</a>
    /// </summary>
    [HtmlTargetElement("*", Attributes = "permission-controller,permission-action")]
    [HtmlTargetElement("*", Attributes = "permission-check")]
    public class PermissionTagHelper : TagHelper
    {
        private readonly IPermissionService _permissionService;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConfiguration _configuration;

        public PermissionTagHelper(
            IPermissionService permissionService,
            IHttpContextAccessor httpContextAccessor,
            IConfiguration configuration)
        {
            _permissionService = permissionService;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        /// <summary>
        /// Controller 名稱
        /// </summary>
        [HtmlAttributeName("permission-controller")]
        public string Controller { get; set; }

        /// <summary>
        /// Action 名稱
        /// </summary>
        [HtmlAttributeName("permission-action")]
        public string Action { get; set; }

        /// <summary>
        /// 簡化版權限檢查（格式：Controller.Action）
        /// </summary>
        [HtmlAttributeName("permission-check")]
        public string PermissionCheck { get; set; }

        /// <summary>
        /// 當沒有權限時是否完全移除元素（預設 true）
        /// 如果設為 false，則只是添加 disabled 屬性和隱藏樣式
        /// </summary>
        [HtmlAttributeName("permission-remove")]
        public bool RemoveElement { get; set; } = true;

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            // 測試階段：允許透過設定繞過權限檢查，全部顯示
            if (_configuration.GetValue<bool>("Permission:Bypass"))
                return;

            // 取得當前使用者
            var userName = _httpContextAccessor.HttpContext?.User?.Identity?.Name;
            userName = "4DC64990-C818-4A28-AAEC-4C726F5E6CEB";
            if (string.IsNullOrEmpty(userName))
            {
                // 沒有登入，移除元素
                SuppressOutput(output);
                return;
            }

            // 解析權限參數
            string controller, action;
            if (!string.IsNullOrEmpty(PermissionCheck))
            {
                // 簡化版：Controller.Action
                var parts = PermissionCheck.Split('.');
                if (parts.Length != 2)
                {
                    // 格式錯誤，移除元素
                    SuppressOutput(output);
                    return;
                }
                controller = parts[0];
                action = parts[1];
            }
            else
            {
                // 完整版：分別指定 Controller 和 Action
                controller = Controller;
                action = Action;
            }

            // 檢查權限
            var hasPermission = await _permissionService.HasPermissionAsync(userName, controller, action);

            if (!hasPermission)
            {
                output.Attributes.SetAttribute("disabled", "disabled");
                output.Attributes.SetAttribute("style", "display:none;");
                if (RemoveElement)
                {
                    // 完全移除元素
                    SuppressOutput(output);
                }
                else
                {
                    // 保留元素但禁用
                    output.Attributes.SetAttribute("disabled", "disabled");
                    output.Attributes.SetAttribute("style", "display:none;");
                }
            }
        }

        /// <summary>
        /// 抑制輸出（移除元素）
        /// </summary>
        private void SuppressOutput(TagHelperOutput output)
        {
            output.SuppressOutput();
        }
    }
}

