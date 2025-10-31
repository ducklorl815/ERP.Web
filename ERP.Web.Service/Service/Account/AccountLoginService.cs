using ERP.Web.Models.Respository.Account;
using ERP.Web.Service.ViewModels.Account;
using Microsoft.Extensions.Configuration;

namespace LifeTech.ERP.Web.Service.Service
{
    /// <summary>
    /// 帳號登入服務
    /// 負責登入相關的業務邏輯
    /// </summary>
    public class AccountLoginService
    {
        private readonly AccountLoginRepo _accountLoginRepo;

        public AccountLoginService(IConfiguration configuration)
        {
            _accountLoginRepo = new AccountLoginRepo(configuration);
        }

        /// <summary>
        /// 執行登入驗證
        /// </summary>
        /// <param name="account">帳號</param>
        /// <param name="password">密碼</param>
        /// <param name="autoLogin">是否自動登入（此參數不影響驗證，只是傳遞給前端使用）</param>
        /// <returns>登入結果</returns>
        public async Task<AccountLoginViewModel_result> LoginAsync(string account, string password, bool autoLogin)
        {
            try
            {
                // 1. 基本驗證：檢查帳號密碼是否為空
                if (string.IsNullOrWhiteSpace(account) || string.IsNullOrWhiteSpace(password))
                {
                    return new AccountLoginViewModel_result
                    {
                        IsSuccess = false,
                        Msg = "帳號或密碼不能為空"
                    };
                }

                // 2. 呼叫 Repository 驗證帳號密碼
                bool isValid = await _accountLoginRepo.ValidateUserAsync(account, password);

                if (!isValid)
                {
                    // 記錄登入失敗
                    await _accountLoginRepo.LogFailedLoginAsync(account, "帳號或密碼錯誤");

                    return new AccountLoginViewModel_result
                    {
                        IsSuccess = false,
                        Msg = "帳號或密碼錯誤，請重新輸入"
                    };
                }

                // 3. 驗證成功，取得使用者完整資料
                var userData = await _accountLoginRepo.GetUserDataAsync(account);

                if (userData == null)
                {
                    return new AccountLoginViewModel_result
                    {
                        IsSuccess = false,
                        Msg = "無法取得使用者資料"
                    };
                }

                // 4. 檢查帳號是否啟用
                if (!userData.IsActive)
                {
                    return new AccountLoginViewModel_result
                    {
                        IsSuccess = false,
                        Msg = "此帳號已被停用，請聯絡系統管理員"
                    };
                }

                // 5. 更新最後登入時間
                await _accountLoginRepo.UpdateLastLoginTimeAsync(account);

                // 6. 組裝回傳資料
                return new AccountLoginViewModel_result
                {
                    IsSuccess = true,
                    Msg = "登入成功",
                    Account = userData.Account,
                    EmpID = userData.EmpID,
                    EmpName = userData.EmpName,
                    AutoLogin = autoLogin // 原樣回傳，給前端使用
                };
            }
            catch (Exception ex)
            {
                // 記錄錯誤（實際應使用 Logger）
                Console.WriteLine($"登入服務發生錯誤: {ex.Message}");

                return new AccountLoginViewModel_result
                {
                    IsSuccess = false,
                    Msg = "系統發生錯誤，請稍後再試"
                };
            }
        }

        /// <summary>
        /// 根據帳號取得使用者資料（用於自動登入）
        /// </summary>
        /// <param name="account">帳號</param>
        /// <returns>使用者資料</returns>
        public async Task<AccountLoginViewModel_result?> GetUserDataByAccountAsync(string account)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(account))
                    return null;

                // 取得使用者資料
                var userData = await _accountLoginRepo.GetUserDataAsync(account);

                if (userData == null || !userData.IsActive)
                    return null;

                // 組裝回傳資料
                return new AccountLoginViewModel_result
                {
                    IsSuccess = true,
                    Msg = "自動登入成功",
                    Account = userData.Account,
                    EmpID = userData.EmpID,
                    EmpName = userData.EmpName,
                    AutoLogin = false
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"取得使用者資料時發生錯誤: {ex.Message}");
                return null;
            }
        }
    }
}
