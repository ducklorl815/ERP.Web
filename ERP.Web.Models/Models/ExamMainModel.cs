namespace ERP.Web.Models.Models
{
    public class Vocabulary
    {
        public Guid WordID { get; set; }
        public string CategoryType { get; set; } // 類別 片語_Phrase 單字_Word
        public string Class { get; set; }
        public int ClassNum { get; set; } // 課程編號
        public string Category { get; set; } // 類別
        public string ClassName { get; set; } // 課程標題
        public string Question { get; set; } // 
        public string Answer { get; set; } // 意思
        public Guid KidID { get; set; }
        public int Correct { get; set; }
        public int Focus { get; set; }
        public Guid LessionID { get; set; }
        public string TestType { get; set; } 
        
    }

    public class KidTestIndex
    {
        public string Class { get; set; }
        public string TestType { get; set; }
    }
    public class ExamMainKeyword
    {
        public List<string> ClassNameList { get; set; }
        public string CorrectType { get; set; }
        public string KidID { get; set; }
        public DateTime TestDate { get; set; }
    }
    public class ExamMainModel
    {
        public Guid WordID { get; set; }
        public string ClassName { get; set; }
        /// <summary>考卷類別 ex:English,Math</summary>
        public string TestType { get; set; }
        /// <summary>文字內容規格 ex:Word,Phrase</summary>
        public string CategoryType { get; set; }
        public string Question { get; set; }
        public string Answer { get; set; }
        public string Cname { get; set; }
        /// <summary>考試 對錯</summary>
        public int Correct { get; set; }
        /// <summary>學校 專注</summary>
        public int Focus { get; set; }
        public DateTime TestDate { get; set; }
    }

    public class LessionModel
    {
        public string ClassName { get; set; }
        public int ClassNum { get; set; }
        public string TestType { get; set; }
        public string Category { get; set; }
        public string CategoryType { get; set; }
        public int LessionSort { get; set; }
    }
}
