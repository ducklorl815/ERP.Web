using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.Account
{
    public partial class AccountController : Controller
    {
        public async Task<IActionResult> Logout()
        {
            var userName = User?.Identity?.Name;
            _permissionService.ClearUserPermissionsCache(userName);

            //await HttpContext.SignOutAsync();

            return RedirectToAction("Index", "Home");
        }
    }
}
