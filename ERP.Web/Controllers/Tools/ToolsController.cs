using ERP.Web.Service.Service.ControllerSetting;
using Microsoft.AspNetCore.Mvc;

namespace ERP.Web.Controllers.Tools
{
    public partial class ToolsController : Controller
    {
        private readonly ToolsService _toolsService;
        public ToolsController
            (
            ToolsService toolsService
            )
        {
            _toolsService = toolsService;
        }
    }
}
