using ERP.Web.Utility.Models;
using Microsoft.Extensions.Options;

namespace ERP.Web.Models.Respository.Tools
{
    public partial class ToolsRespo
    {
        private readonly DBList _dBList;
        public ToolsRespo
            (
             IOptions<DBList> dbList)
        {
            _dBList = dbList.Value;
        }
    }
}
