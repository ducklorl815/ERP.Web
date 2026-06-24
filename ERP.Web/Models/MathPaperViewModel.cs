namespace ERP.Web.Models
{
    public class MathPaperViewModel
    {
        public MathOperationType OperationType { get; set; }

        public string OperationLabel { get; set; } = string.Empty;

        public string Title { get; set; } = "數學考卷";

        public string LeftOperandLabel { get; set; } = "左側數字";

        public string RightOperandLabel { get; set; } = "右側數字";

        public string LeftOperandSpec { get; set; } = string.Empty;

        public string RightOperandSpec { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.Now;

        public int QuestionCount { get; set; }

        public int TotalScore { get; set; } = 100;

        /// <summary>
        /// 每題分數（100 ÷ 題數，四捨五入）
        /// </summary>
        public int ScorePerQuestion =>
            QuestionCount > 0
                ? (int)Math.Round((double)TotalScore / QuestionCount, MidpointRounding.AwayFromZero)
                : 0;

        public List<MathQuestionViewModel> Questions { get; set; } = new();
    }
}
