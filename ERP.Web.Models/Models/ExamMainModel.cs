﻿namespace ERP.Web.Models.Models
{
    public class Vocabulary
    {
        public Guid ID { get; set; }
        public string Type { get; set; } // 類別 片語_Phrase 單字_Word
        public int ClassNum { get; set; } // 課程編號
        public string Category { get; set; } // 類別
        public string ClassName { get; set; } // 課程標題
        public string Question { get; set; } // 
        public string Answer { get; set; } // 意思
    }
}
