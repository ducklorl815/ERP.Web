using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.Account
{
    public partial class AccountController : Controller
    {
        public async Task<IActionResult> Logout()
        {
            var userName = User?.Identity?.Name;
            userName = "4DC64990-C818-4A28-AAEC-4C726F5E6CEB";
            _permissionService.ClearUserPermissionsCache(userName);

            //await HttpContext.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }
    }
}
