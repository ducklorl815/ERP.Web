using ERP.Web.Service.ViewModels.Account;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.Account
{
    public partial class AccountController : Controller
    {
        /// <summary>
        /// 顯示 OTP 驗證頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VerifyOTP(string account, string? returnUrl = null)
        {
            // 檢查是否有暫存的登入資訊
            var pendingAccount = TempData["PendingLogin_Account"]?.ToString();
            if (string.IsNullOrEmpty(pendingAccount) && !string.IsNullOrEmpty(account))
            {
                pendingAccount = account;
            }

            if (string.IsNullOrEmpty(pendingAccount))
            {
                // 沒有暫存資訊，導向登入頁面
                TempData["ErrorMessage"] = "請先完成帳號密碼驗證";
                return RedirectToAction("Login");
            }

            // 檢查是否啟用 OTP
            var isOTPEnabled = await _otpService.IsOTPEnabledAsync(pendingAccount);
            if (!isOTPEnabled)
            {
                // 未啟用 OTP，直接完成登入
                var empID = TempData["PendingLogin_EmpID"]?.ToString() ?? string.Empty;
                var empName = TempData["PendingLogin_EmpName"]?.ToString() ?? string.Empty;
                var autoLogin = TempData["PendingLogin_AutoLogin"]?.ToString() == "True";
                
                await InitializeUserSessionAsync(pendingAccount, empName, empID);
                HandleAutoLoginCookie(autoLogin, pendingAccount);
                
                return RedirectToTargetPage(returnUrl);
            }

            // 檢查 Session 中是否已有 OTP 驗證記錄
            var otpVerified = HttpContext.Session.GetString("OTPVerified");
            if (otpVerified == "true")
            {
                // 已經驗證過，直接完成登入
                var empID = TempData["PendingLogin_EmpID"]?.ToString() ?? string.Empty;
                var empName = TempData["PendingLogin_EmpName"]?.ToString() ?? string.Empty;
                var autoLogin = TempData["PendingLogin_AutoLogin"]?.ToString() == "True";
                
                await InitializeUserSessionAsync(pendingAccount, empName, empID);
                HandleAutoLoginCookie(autoLogin, pendingAccount);
                
                return RedirectToTargetPage(returnUrl);
            }

            // 顯示 OTP 驗證頁面
            ViewData["Account"] = pendingAccount;
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        /// <summary>
        /// 處理 OTP 驗證請求
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> VerifyOTP(OTPVerifyViewModel model)
        {
            try
            {
                // 取得暫存的登入資訊
                var pendingAccount = TempData["PendingLogin_Account"]?.ToString() ?? model.Account;
                var empID = TempData["PendingLogin_EmpID"]?.ToString() ?? string.Empty;
                var empName = TempData["PendingLogin_EmpName"]?.ToString() ?? string.Empty;
                var returnUrl = TempData["PendingLogin_ReturnUrl"]?.ToString() ?? model.ReturnUrl;
                var autoLogin = TempData["PendingLogin_AutoLogin"]?.ToString() == "True";

                if (string.IsNullOrEmpty(pendingAccount))
                {
                    ModelState.AddModelError("", "請先完成帳號密碼驗證");
                    return RedirectToAction("Login");
                }

                // 取得 IP 位址和 User Agent
                var ipAddress = GetClientIPAddress();
                var userAgent = HttpContext.Request.Headers["User-Agent"].ToString();

                // 驗證 OTP
                var result = await _otpService.VerifyTOTPAsync(pendingAccount, model.OTPCode, ipAddress, userAgent);

                if (!result.IsSuccess)
                {
                    // 驗證失敗，保留暫存資訊並顯示錯誤
                    TempData["PendingLogin_Account"] = pendingAccount;
                    TempData["PendingLogin_EmpID"] = empID;
                    TempData["PendingLogin_EmpName"] = empName;
                    TempData["PendingLogin_ReturnUrl"] = returnUrl;
                    TempData["PendingLogin_AutoLogin"] = autoLogin.ToString();
                    
                    ModelState.Clear();
                    TempData["ErrorMessage"] = result.ErrorMessage ?? "驗證碼錯誤，請重新輸入";
                    ViewData["Account"] = pendingAccount;
                    ViewData["ReturnUrl"] = returnUrl;
                    return View();
                }

                // OTP 驗證成功，完成登入流程
                await InitializeUserSessionAsync(pendingAccount, empName, empID);
                HandleAutoLoginCookie(autoLogin, pendingAccount);

                // 導向目標頁面
                return RedirectToTargetPage(returnUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OTP 驗證處理發生錯誤: {ex.Message}");
                ModelState.Clear();
                TempData["ErrorMessage"] = "系統發生錯誤，請稍後再試";
                ViewData["Account"] = model.Account;
                ViewData["ReturnUrl"] = model.ReturnUrl;
                return View();
            }
        }

        /// <summary>
        /// 顯示 OTP 設定頁面
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> OTPSetup()
        {
            // 檢查是否已登入
            var account = HttpContext.Session.GetString("UserAccount");
            if (string.IsNullOrEmpty(account))
            {
                TempData["ErrorMessage"] = "請先登入系統";
                return RedirectToAction("Login");
            }

            // 取得現有的 OTP 設定
            var setting = await _otpService.GetOTPSettingAsync(account);
            var viewModel = new OTPSetupViewModel
            {
                Account = account,
                IsOTPEnabled = setting?.IsOTPEnabled ?? false
            };

            return View(viewModel);
        }

        /// <summary>
        /// 處理 OTP 設定請求（生成 Secret Key 和 QR Code）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> OTPSetup(string account)
        {
            try
            {
                // 檢查是否已登入
                var sessionAccount = HttpContext.Session.GetString("UserAccount");
                if (string.IsNullOrEmpty(sessionAccount) || sessionAccount != account)
                {
                    TempData["ErrorMessage"] = "請先登入系統";
                    return RedirectToAction("Login");
                }

                // 生成 TOTP Secret Key 和 QR Code
                var result = await _otpService.SetupTOTPAsync(account);

                if (!result.IsSuccess)
                {
                    var viewModel = new OTPSetupViewModel
                    {
                        Account = account,
                        ErrorMessage = result.ErrorMessage
                    };
                    return View(viewModel);
                }

                // 顯示設定結果
                var setupViewModel = new OTPSetupViewModel
                {
                    Account = account,
                    SecretKey = result.SecretKey,
                    QRCodeUrl = result.QRCodeUrl,
                    IsOTPEnabled = false // 尚未啟用，需要驗證成功後才啟用
                };

                return View(setupViewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OTP 設定處理發生錯誤: {ex.Message}");
                var viewModel = new OTPSetupViewModel
                {
                    Account = account,
                    ErrorMessage = "系統發生錯誤，請稍後再試"
                };
                return View(viewModel);
            }
        }

        /// <summary>
        /// 停用 OTP
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DisableOTP()
        {
            try
            {
                var account = HttpContext.Session.GetString("UserAccount");
                if (string.IsNullOrEmpty(account))
                {
                    return Json(new { success = false, message = "請先登入系統" });
                }

                var result = await _otpService.DisableOTPAsync(account);
                if (result)
                {
                    // 清除 Session 中的 OTP 驗證狀態
                    HttpContext.Session.Remove("OTPVerified");
                    HttpContext.Session.Remove("OTPVerifiedTime");
                    
                    return Json(new { success = true, message = "OTP 已停用" });
                }

                return Json(new { success = false, message = "停用失敗" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"停用 OTP 時發生錯誤: {ex.Message}");
                return Json(new { success = false, message = "系統發生錯誤" });
            }
        }
    }
}

