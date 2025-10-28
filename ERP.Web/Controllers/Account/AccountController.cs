using ERP.Web.Utility.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace ERP.Web.Controllers.Account
{
    public partial class AccountController : Controller
    {
        private readonly PermissionService _permissionService;

        public AccountController(IConfiguration config, IMemoryCache cache, IHttpContextAccessor accessor)
        {
            _permissionService = new PermissionService(config, cache, accessor);
        }
    }
}
