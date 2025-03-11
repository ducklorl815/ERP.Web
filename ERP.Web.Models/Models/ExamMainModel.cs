namespace ERP.Web.Models.Models
{
    public class Vocabulary
    {
        public Guid ID { get; set; }
        public int Class { get; set; } // 課程編號
        public string Word { get; set; } // 單字
        public string Meaning { get; set; } // 意思
    }
}
