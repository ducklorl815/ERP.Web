using ERP.Web.Models.Respository.Tools;
using Microsoft.AspNetCore.Http;

namespace ERP.Web.Service.Service.ControllerSetting
{
    public partial class ToolsService
    {
        private readonly ToolsRespo _toolsRepo;
        public ToolsService
            (
            ToolsRespo toolsRepo
            )
        {
            _toolsRepo = toolsRepo;
        }
    }
}
