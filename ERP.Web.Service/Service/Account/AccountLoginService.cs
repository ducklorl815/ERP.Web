using ERP.Web.Service.ViewModels.Account;

namespace LifeTech.ERP.Web.Service.Service
{
    public class AccountLoginService
    {
        public async Task<AccountLoginViewModel_result> LoginAsync(string account, string password, bool autoLogin)
        {
            return new AccountLoginViewModel_result();
        }
    }
}