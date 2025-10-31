using ERP.Web.Utility.Services;
using LifeTech.ERP.Web.Service.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace ERP.Web.Controllers.Account
{
    public partial class AccountController : Controller
    {
        private readonly AccountLoginService _accountLoginService;
        private readonly PermissionService _permissionService;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountController(IConfiguration configuration, IMemoryCache cache, IHttpContextAccessor httpContextAccessor, AccountLoginService accountLoginService)
        {
            _permissionService = new PermissionService(configuration, cache, httpContextAccessor);
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _accountLoginService = accountLoginService;
        }

    }
}
