using System.ComponentModel.DataAnnotations;

namespace ERP.Web.Models
{
    /// <summary>
    /// 數學考卷出題輸入參數（加減乘除通用）
    /// </summary>
    public class MathPaperInputViewModel
    {
        public MathOperationType OperationType { get; set; } = MathOperationType.Multiplication;

        /// <summary>
        /// 算式左側數字範圍，例：1-9 或 2,3,5
        /// </summary>
        [Display(Name = "左側數字")]
        public string LeftOperand { get; set; } = "1-9";

        /// <summary>
        /// 算式右側數字範圍，例：1-9 或 2,3,5
        /// </summary>
        [Display(Name = "右側數字")]
        public string RightOperand { get; set; } = "1-9";

        /// <summary>
        /// 出題數量；0 代表使用全部有效組合
        /// </summary>
        [Display(Name = "題數")]
        [Range(0, 200, ErrorMessage = "題數需介於 0 到 200")]
        public int QuestionCount { get; set; }

        /// <summary>
        /// 是否隨機排序題目
        /// </summary>
        [Display(Name = "隨機排序")]
        public bool Shuffle { get; set; } = true;

        /// <summary>
        /// 考卷標題
        /// </summary>
        [Display(Name = "考卷標題")]
        public string? Title { get; set; }

        /// <summary>
        /// 固定亂數種子（可選，用於重現同一份考卷）
        /// </summary>
        public int? Seed { get; set; }

        /// <summary>
        /// 綜合題：運算式項數（幾個數字）。例如 4 代表 3 個運算子：a + b × c ÷ d
        /// </summary>
        [Display(Name = "項數")]
        [Range(3, 6, ErrorMessage = "項數需介於 3 到 6")]
        public int MixedTermCount { get; set; } = 4;

        /// <summary>綜合題：包含加法</summary>
        [Display(Name = "加法")]
        public bool IncludeAddition { get; set; } = true;

        /// <summary>綜合題：包含減法</summary>
        [Display(Name = "減法")]
        public bool IncludeSubtraction { get; set; } = true;

        /// <summary>綜合題：包含乘法</summary>
        [Display(Name = "乘法")]
        public bool IncludeMultiplication { get; set; } = true;

        /// <summary>綜合題：包含除法</summary>
        [Display(Name = "除法")]
        public bool IncludeDivision { get; set; } = true;
    }
}
