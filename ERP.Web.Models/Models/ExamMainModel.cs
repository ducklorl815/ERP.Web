using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP.Web.Models.Models
{
    public class Vocabulary
    {
        public int Id { get; set; }
        public int Class { get; set; } // 課程編號
        public string Word { get; set; } // 單字
        public string Meaning { get; set; } // 意思
    }
}
