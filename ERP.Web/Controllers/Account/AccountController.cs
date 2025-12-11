using ERP.Web.Utility.Services;
using ERP.Web.Service.Service.Account;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace ERP.Web.Controllers.Account
{
    public partial class AccountController : Controller
    {
        private readonly AccountLoginService _accountLoginService;
        private readonly OTPService _otpService;
        private readonly PermissionService _permissionService;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AccountController(
            IConfiguration configuration, 
            IMemoryCache cache, 
            IHttpContextAccessor httpContextAccessor, 
            AccountLoginService accountLoginService,
            OTPService otpService)
        {
            _permissionService = new PermissionService(configuration, cache, httpContextAccessor);
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _accountLoginService = accountLoginService;
            _otpService = otpService;
        }

    }
}
