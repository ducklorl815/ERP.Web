using ERP.Web.Service.ViewModels.Account;
using Microsoft.AspNetCore.Mvc;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

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

            // 檢查是否有 OTP 設定（不檢查是否啟用，因為第一次驗證成功後會自動啟用）
            var otpSetting = await _otpService.GetOTPSettingAsync(pendingAccount);
            if (otpSetting == null || string.IsNullOrEmpty(otpSetting.SecretKey))
            {
                // 尚未設定 OTP，需要先設定
                // 保留登入資訊到 TempData，以便設定完成後繼續登入流程
                TempData["PendingLogin_Account"] = pendingAccount;
                TempData["PendingLogin_EmpID"] = TempData["PendingLogin_EmpID"]?.ToString() ?? string.Empty;
                TempData["PendingLogin_EmpName"] = TempData["PendingLogin_EmpName"]?.ToString() ?? string.Empty;
                TempData["PendingLogin_ReturnUrl"] = returnUrl;
                TempData["PendingLogin_AutoLogin"] = TempData["PendingLogin_AutoLogin"]?.ToString() ?? "False";
                
                TempData["InfoMessage"] = "請先設定 OTP 驗證器";
                return RedirectToAction("OTPSetup");
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
        /// 支援從登入流程進入（從 TempData 或 URL 參數取得帳號）或已登入使用者（從 Session 取得帳號）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> OTPSetup(string? account = null)
        {
            // 優先從 TempData 取得帳號（從登入流程進入的情況）
            var pendingAccount = TempData["PendingLogin_Account"]?.ToString();
            
            // 如果 TempData 沒有，則從 URL 參數取得（從 VerifyOTP 頁面連結進入的情況）
            if (string.IsNullOrEmpty(pendingAccount) && !string.IsNullOrEmpty(account))
            {
                pendingAccount = account;
            }
            
            // 如果還是沒有，則從 Session 取得（已登入使用者的情況）
            if (string.IsNullOrEmpty(pendingAccount))
            {
                pendingAccount = HttpContext.Session.GetString("UserAccount");
            }
            
            // 如果都沒有，導向登入頁面
            if (string.IsNullOrEmpty(pendingAccount))
            {
                TempData["ErrorMessage"] = "請先登入系統";
                return RedirectToAction("Login");
            }

            // 保留 TempData（如果有的話），以便後續流程使用
            // 即使 TempData 已經被讀取過，也要嘗試保留
            if (TempData.ContainsKey("PendingLogin_Account"))
            {
                TempData.Keep("PendingLogin_Account");
                TempData.Keep("PendingLogin_EmpID");
                TempData.Keep("PendingLogin_EmpName");
                TempData.Keep("PendingLogin_ReturnUrl");
                TempData.Keep("PendingLogin_AutoLogin");
            }
            // 如果 TempData 已經被讀取過，但我們有 account 參數，重新設定 TempData
            else if (!string.IsNullOrEmpty(pendingAccount) && !string.IsNullOrEmpty(account))
            {
                // 從 VerifyOTP 頁面進入，重新設定 TempData 以便後續流程使用
                TempData["PendingLogin_Account"] = pendingAccount;
                // 其他 TempData 可能已經遺失，但至少保留 Account
            }

            // 取得現有的 OTP 設定
            var setting = await _otpService.GetOTPSettingAsync(pendingAccount);
            
            // 正確判斷是否已啟用：必須有設定且 IsOTPEnabled = true
            var isOTPEnabled = setting != null && setting.IsOTPEnabled && !string.IsNullOrEmpty(setting.SecretKey);
            
            var viewModel = new OTPSetupViewModel
            {
                Account = pendingAccount,
                IsOTPEnabled = isOTPEnabled
            };

            // 直接生成 QR Code（不判斷是否已有設定，每次進入都生成新的）
            Console.WriteLine($"開始為帳號 {pendingAccount} 生成 OTP QR Code...");
            var setupResult = await _otpService.SetupTOTPAsync(pendingAccount);
            if (setupResult.IsSuccess && !string.IsNullOrEmpty(setupResult.QRCodeUrl))
            {
                Console.WriteLine($"SetupTOTPAsync 成功，QRCodeUrl 長度: {setupResult.QRCodeUrl.Length}");
                viewModel.SecretKey = setupResult.SecretKey;
                viewModel.QRCodeUrl = setupResult.QRCodeUrl;
                var qrCodeImage = GenerateQRCodeImage(setupResult.QRCodeUrl);
                if (!string.IsNullOrEmpty(qrCodeImage))
                {
                    viewModel.QRCodeImageUrl = qrCodeImage;
                    Console.WriteLine($"QR Code 圖片生成成功，Base64 長度: {qrCodeImage.Length}");
                }
                else
                {
                    var urlPreview = setupResult.QRCodeUrl?.Length > 50 
                        ? setupResult.QRCodeUrl.Substring(0, 50) + "..." 
                        : setupResult.QRCodeUrl ?? "null";
                    Console.WriteLine($"警告：QR Code 圖片生成失敗，但 QRCodeUrl 存在: {urlPreview}");
                }
            }
            else
            {
                var urlPreview = setupResult?.QRCodeUrl?.Length > 50 
                    ? setupResult.QRCodeUrl.Substring(0, 50) + "..." 
                    : setupResult?.QRCodeUrl ?? "null";
                Console.WriteLine($"SetupTOTPAsync 失敗或 QRCodeUrl 為空。IsSuccess: {setupResult?.IsSuccess}, QRCodeUrl: {urlPreview}");
                if (!string.IsNullOrEmpty(setupResult?.ErrorMessage))
                {
                    Console.WriteLine($"錯誤訊息: {setupResult.ErrorMessage}");
                    viewModel.ErrorMessage = setupResult.ErrorMessage;
                }
            }

            return View(viewModel);
        }

        /// <summary>
        /// 取得 OTP 設定頁面的 PartialView（用於 Modal 顯示）
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> OTPSetupPartial(string? account = null)
        {
            // 優先從 TempData 取得帳號（從登入流程進入的情況）
            var pendingAccount = TempData["PendingLogin_Account"]?.ToString();
            
            // 如果 TempData 沒有，則從 URL 參數取得（從 VerifyOTP 頁面連結進入的情況）
            if (string.IsNullOrEmpty(pendingAccount) && !string.IsNullOrEmpty(account))
            {
                pendingAccount = account;
            }
            
            // 如果還是沒有，則從 Session 取得（已登入使用者的情況）
            if (string.IsNullOrEmpty(pendingAccount))
            {
                pendingAccount = HttpContext.Session.GetString("UserAccount");
            }
            
            // 如果都沒有，返回錯誤訊息
            if (string.IsNullOrEmpty(pendingAccount))
            {
                return PartialView("_OTPSetupPartial", new OTPSetupViewModel
                {
                    ErrorMessage = "請先登入系統"
                });
            }

            // 保留 TempData（如果有的話），以便後續流程使用
            if (TempData.ContainsKey("PendingLogin_Account"))
            {
                TempData.Keep("PendingLogin_Account");
                TempData.Keep("PendingLogin_EmpID");
                TempData.Keep("PendingLogin_EmpName");
                TempData.Keep("PendingLogin_ReturnUrl");
                TempData.Keep("PendingLogin_AutoLogin");
            }
            else if (!string.IsNullOrEmpty(pendingAccount) && !string.IsNullOrEmpty(account))
            {
                TempData["PendingLogin_Account"] = pendingAccount;
            }

            // 取得現有的 OTP 設定
            var setting = await _otpService.GetOTPSettingAsync(pendingAccount);
            
            // 正確判斷是否已啟用：必須有設定且 IsOTPEnabled = true
            var isOTPEnabled = setting != null && setting.IsOTPEnabled && !string.IsNullOrEmpty(setting.SecretKey);
            
            var viewModel = new OTPSetupViewModel
            {
                Account = pendingAccount,
                IsOTPEnabled = isOTPEnabled
            };

            // 直接生成 QR Code（不判斷是否已有設定，每次進入都生成新的）
            Console.WriteLine($"開始為帳號 {pendingAccount} 生成 OTP QR Code (PartialView)...");
            var setupResult = await _otpService.SetupTOTPAsync(pendingAccount);
            if (setupResult.IsSuccess && !string.IsNullOrEmpty(setupResult.QRCodeUrl))
            {
                Console.WriteLine($"SetupTOTPAsync 成功，QRCodeUrl 長度: {setupResult.QRCodeUrl.Length}");
                viewModel.SecretKey = setupResult.SecretKey;
                viewModel.QRCodeUrl = setupResult.QRCodeUrl;
                var qrCodeImage = GenerateQRCodeImage(setupResult.QRCodeUrl);
                if (!string.IsNullOrEmpty(qrCodeImage))
                {
                    viewModel.QRCodeImageUrl = qrCodeImage;
                    Console.WriteLine($"QR Code 圖片生成成功，Base64 長度: {qrCodeImage.Length}");
                }
                else
                {
                    var urlPreview = setupResult.QRCodeUrl?.Length > 50 
                        ? setupResult.QRCodeUrl.Substring(0, 50) + "..." 
                        : setupResult.QRCodeUrl ?? "null";
                    Console.WriteLine($"警告：QR Code 圖片生成失敗，但 QRCodeUrl 存在: {urlPreview}");
                }
            }
            else
            {
                var urlPreview = setupResult?.QRCodeUrl?.Length > 50 
                    ? setupResult.QRCodeUrl.Substring(0, 50) + "..." 
                    : setupResult?.QRCodeUrl ?? "null";
                Console.WriteLine($"SetupTOTPAsync 失敗或 QRCodeUrl 為空。IsSuccess: {setupResult?.IsSuccess}, QRCodeUrl: {urlPreview}");
                if (!string.IsNullOrEmpty(setupResult?.ErrorMessage))
                {
                    Console.WriteLine($"錯誤訊息: {setupResult.ErrorMessage}");
                    viewModel.ErrorMessage = setupResult.ErrorMessage;
                }
            }

            return PartialView("_OTPSetupPartial", viewModel);
        }

        /// <summary>
        /// 處理 OTP 設定請求（生成 Secret Key 和 QR Code）
        /// 支援從登入流程進入（從 TempData 取得帳號）或已登入使用者（從 Session 取得帳號）
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> OTPSetup()
        {
            try
            {
                // 優先從 TempData 取得帳號（從登入流程進入的情況）
                var pendingAccount = TempData["PendingLogin_Account"]?.ToString();
                
                // 如果 TempData 沒有，則從表單取得（從視圖表單提交）
                if (string.IsNullOrEmpty(pendingAccount))
                {
                    pendingAccount = Request.Form["account"].ToString();
                }
                
                // 如果還是沒有，則從 Session 取得（已登入使用者的情況）
                if (string.IsNullOrEmpty(pendingAccount))
                {
                    pendingAccount = HttpContext.Session.GetString("UserAccount");
                }
                
                // 如果都沒有帳號，導向登入頁面
                if (string.IsNullOrEmpty(pendingAccount))
                {
                    TempData["ErrorMessage"] = "請先登入系統";
                    return RedirectToAction("Login");
                }

                // 保留 TempData（如果有的話），以便後續流程使用
                if (TempData["PendingLogin_Account"] != null)
                {
                    TempData.Keep("PendingLogin_Account");
                    TempData.Keep("PendingLogin_EmpID");
                    TempData.Keep("PendingLogin_EmpName");
                    TempData.Keep("PendingLogin_ReturnUrl");
                    TempData.Keep("PendingLogin_AutoLogin");
                }

                // 生成 TOTP Secret Key 和 QR Code
                var result = await _otpService.SetupTOTPAsync(pendingAccount);

                if (!result.IsSuccess)
                {
                    var viewModel = new OTPSetupViewModel
                    {
                        Account = pendingAccount,
                        ErrorMessage = result.ErrorMessage
                    };
                    return View(viewModel);
                }

                // 生成 QR Code 圖片（Base64 格式）
                string? qrCodeImageUrl = null;
                if (!string.IsNullOrEmpty(result.QRCodeUrl))
                {
                    qrCodeImageUrl = GenerateQRCodeImage(result.QRCodeUrl);
                    if (string.IsNullOrEmpty(qrCodeImageUrl))
                    {
                        var urlPreview = result.QRCodeUrl?.Length > 50 
                            ? result.QRCodeUrl.Substring(0, 50) + "..." 
                            : result.QRCodeUrl ?? "null";
                        Console.WriteLine($"警告：POST 方法中 QR Code 圖片生成失敗，但 QRCodeUrl 存在: {urlPreview}");
                    }
                }
                else
                {
                    Console.WriteLine($"警告：result.QRCodeUrl 為空，無法生成 QR Code");
                }

                // 顯示設定結果
                var setupViewModel = new OTPSetupViewModel
                {
                    Account = pendingAccount,
                    SecretKey = result.SecretKey,
                    QRCodeUrl = result.QRCodeUrl,
                    QRCodeImageUrl = qrCodeImageUrl,
                    IsOTPEnabled = false // 尚未啟用，需要驗證成功後才啟用
                };

                return View(setupViewModel);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OTP 設定處理發生錯誤: {ex.Message}");
                // 嘗試取得帳號以便顯示錯誤訊息
                var errorAccount = TempData["PendingLogin_Account"]?.ToString() 
                    ?? Request.Form["account"].ToString() 
                    ?? HttpContext.Session.GetString("UserAccount") 
                    ?? string.Empty;
                
                var viewModel = new OTPSetupViewModel
                {
                    Account = errorAccount,
                    ErrorMessage = "系統發生錯誤，請稍後再試"
                };
                return View(viewModel);
            }
        }

        /// <summary>
        /// 生成 QR Code 圖片（Base64 格式）
        /// </summary>
        private string GenerateQRCodeImage(string qrCodeUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(qrCodeUrl))
                {
                    Console.WriteLine("QRCodeUrl 為空，無法生成 QR Code");
                    return string.Empty;
                }

                Console.WriteLine($"開始生成 QR Code，URL 長度: {qrCodeUrl.Length}");
                
                using (var qrGenerator = new QRCodeGenerator())
                {
                    var qrCodeData = qrGenerator.CreateQrCode(qrCodeUrl, QRCodeGenerator.ECCLevel.Q);
                    using (var pngByteQrCode = new PngByteQRCode(qrCodeData))
                    {
                        var qrCodeBytes = pngByteQrCode.GetGraphic(20);
                        var qrBase64 = Convert.ToBase64String(qrCodeBytes);
                        var result = $"data:image/png;base64,{qrBase64}";
                        Console.WriteLine($"QR Code 生成成功，Base64 長度: {qrBase64.Length}");
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生成 QR Code 圖片時發生錯誤: {ex.Message}");
                Console.WriteLine($"堆疊追蹤: {ex.StackTrace}");
                return string.Empty;
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

