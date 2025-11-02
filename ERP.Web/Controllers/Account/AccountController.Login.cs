using ERP.Web.Service.ViewModels.Account;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.Account
{
    public partial class AccountController : Controller
    {
        /// <summary>
        /// 顯示登入頁面
        /// 如果有自動登入 Cookie，則直接登入並導向首頁
        /// </summary>
        public async Task<IActionResult> Login(string? returnUrl = null)
        {
            // 檢查是否有自動登入 Cookie
            if (Request.Cookies.TryGetValue("AutoLogin_Account", out string autoLoginAccount) 
                && !string.IsNullOrEmpty(autoLoginAccount))
            {
                // 執行自動登入流程
                var autoLoginResult = await PerformAutoLoginAsync(autoLoginAccount, returnUrl);
                if (autoLoginResult != null)
                {
                    return autoLoginResult; // 自動登入成功，導向首頁
                }
                
                // 自動登入失敗，清除無效的 Cookie，顯示登入表單
                Response.Cookies.Delete("AutoLogin_Account");
            }

            // 顯示登入表單
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// 處理手動登入請求
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Login(AccountLoginViewModel_param model)
        {
            try
            {
                // 執行登入驗證
                var result = await _accountLoginService.LoginAsync(model.Account, model.Password, model.AutoLogin);

                // 登入失敗處理
                if (!result.IsSuccess)
                {
                    ModelState.Clear();
                    TempData["ErrorMessage"] = result.Msg ?? "帳號或密碼錯誤，請重新輸入";
                    return View();
                }

                // 登入成功 - 初始化使用者資料和權限
                await InitializeUserSessionAsync(result.Account, result.EmpName, result.EmpID);

                // 處理自動登入 Cookie（只記錄帳號，供下次自動登入使用）
                HandleAutoLoginCookie(model.AutoLogin, result.Account);

                // 導向目標頁面
                return RedirectToTargetPage(model.ReturnUrl);
            }
            catch (Exception ex)
            {
                // 記錄錯誤
                Console.WriteLine($"登入處理發生錯誤: {ex.Message}");
                
                ModelState.Clear();
                TempData["ErrorMessage"] = "系統發生錯誤，請稍後再試";
                return View();
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// 執行自動登入流程
        /// </summary>
        private async Task<IActionResult?> PerformAutoLoginAsync(string account, string? returnUrl)
        {
            try
            {
                // 從 Service 層取得使用者資料（會檢查帳號是否有效）
                var userData = await _accountLoginService.GetUserDataByAccountAsync(account);

                if (userData == null || !userData.IsSuccess)
                {
                    // 帳號無效或已停用
                    return null;
                }

                // 初始化使用者資料和權限
                await InitializeUserSessionAsync(userData.Account, userData.EmpName, userData.EmpID);

                // 導向目標頁面
                return RedirectToTargetPage(returnUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"自動登入失敗: {ex.Message}");
                return null; // 返回 null 表示自動登入失敗
            }
        }

        /// <summary>
        /// 初始化使用者 Session 和權限
        /// </summary>
        private async Task InitializeUserSessionAsync(string account, string empName, string empID)
        {
            // 1. 設定 Session 儲存使用者資訊
            HttpContext.Session.SetString("UserAccount", account ?? string.Empty);
            HttpContext.Session.SetString("UserName", empName ?? string.Empty);
            HttpContext.Session.SetString("UserEmpID", empID ?? string.Empty);
            HttpContext.Session.SetString("LoginTime", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            // 2. 初始化使用者權限（透過 PermissionService）
            // 根據帳號取得權限，並自動寫入 Cookie
            if (!string.IsNullOrEmpty(account))
            {
                var permissions = await _permissionService.GetUserPermissionsAsync(account);
                HttpContext.Session.SetInt32("PermissionCount", permissions?.Count ?? 0);
            }
        }

        /// <summary>
        /// 處理自動登入 Cookie
        /// 只記錄帳號，供下次進入登入頁面時自動登入
        /// </summary>
        private void HandleAutoLoginCookie(bool autoLogin, string account)
        {
            if (autoLogin && !string.IsNullOrEmpty(account))
            {
                // 設定自動登入 Cookie（有效期 30 天）
                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true, // HTTPS 環境使用
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(30)
                };

                Response.Cookies.Append("AutoLogin_Account", account, cookieOptions);
            }
            else
            {
                // 清除自動登入 Cookie
                Response.Cookies.Delete("AutoLogin_Account");
            }
        }

        /// <summary>
        /// 導向目標頁面（ReturnUrl 或首頁）
        /// </summary>
        private IActionResult RedirectToTargetPage(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        #endregion
    }
}
