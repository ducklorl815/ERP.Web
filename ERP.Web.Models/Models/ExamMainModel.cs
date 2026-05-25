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
        public int ReTest { get; set; }
        public Guid LessionID { get; set; }
        public string TestType { get; set; }

        /// <summary>
        /// 該學生此單字「最近一次」考試紀錄的 Correct（0=答錯；null=尚無任何考試紀錄）。
        /// </summary>
        public int? LastExamCorrect { get; set; }

        /// <summary>
        /// 該學生在相同 TestType 下，此單字累計出現在 KidExamWordIndex 的次數（被考次數）。
        /// </summary>
        public int ExamTimes { get; set; }

    }
    public class ExamListModel
    {
        public string ClassName { get; set; } // 課程標題
        public int LessionSort { get; set; }
        public string TestType { get; set; }
        public DateTime CreateDate { get; set; }
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
        /// <summary>考試類型：English, Math</summary>
        public string TestType { get; set; }
        /// <summary>關鍵字：課程名稱、問題、答案一併搜尋</summary>
        public string SearchText { get; set; }
    }

    /// <summary>複習頁課程（辭庫）統計：該生在此課程已考題目的對錯數</summary>
    public class ClassNameStatModel
    {
        public string ClassName { get; set; }
        public int TestedCount { get; set; }
        public int CorrectCount { get; set; }
        public int WrongCount { get; set; }
    }
    public class ExamMainModel
    {
        public int RowNum { get; set; }
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
        /// <summary>最新一筆 KidExamWordIndex.ReTest</summary>
        public int ReTest { get; set; }
        /// <summary>此題累計出現在考卷中的次數</summary>
        public int ExamTimes { get; set; }
    }
    public class ExamRcdModel
    {
        public Guid WordID { get; set; }
        public Guid NewKidTestID { get; set; }
        /// <summary>考試 對錯</summary>
        public int Correct { get; set; } = 1;
        /// <summary>重考 次數</summary>
        public int ReTest { get; set; } = 0;
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

    public class MentalQuestion
    {
        public Guid ID { get; set; }

        public int MentalLevel { get; set; } // 9 or 10

        public int Number1 { get; set; }
        public int Number2 { get; set; }
        public int Number3 { get; set; }
        public int Number4 { get; set; }
        public int? Number5 { get; set; } // 只有 9級才會用到

        public int Answer { get; set; }

    }
}
