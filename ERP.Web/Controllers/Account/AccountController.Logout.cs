using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.Account
{
    public partial class AccountController : Controller
    {
        /// <summary>
        /// 登出處理
        /// 清除所有 Session、Cookie 和權限快取
        /// </summary>
        public IActionResult Logout()
        {
            try
            {
                // 1. 取得使用者帳號
                var userAccount = HttpContext.Session.GetString("UserAccount");
                
                // 2. 清除使用者權限 Cookie
                if (!string.IsNullOrEmpty(userAccount))
                {
                    _permissionService.ClearUserPermissionsCache(userAccount);
                }

                // 3. 清除所有 Session
                HttpContext.Session.Clear();

                // 4. 清除自動登入 Cookie
                Response.Cookies.Delete("AutoLogin_Account");
                Response.Cookies.Delete("AutoLogin_Token");

                // 5. 登出驗證（如果有使用 ASP.NET Core Authentication）
                // await HttpContext.SignOutAsync();
            }
            catch (Exception ex)
            {
                // 記錄錯誤但繼續登出流程
                Console.WriteLine($"登出處理發生錯誤: {ex.Message}");
            }

            // 導向登入頁面
            return RedirectToAction("Login", "Account");
        }
    }
}
