namespace ERP.Web.Models
{
    public class MathQuestionViewModel
    {
        /// <summary>
        /// 綜合題：運算式文字（不含等號與答案），例如：7 + 5 × 3 ÷ 6
        /// </summary>
        public string? Expression { get; set; }

        public int Left { get; set; }

        public int Right { get; set; }

        public MathOperationType Operation { get; set; }

        public int Answer { get; set; }

        public string OperatorSymbol => Operation switch
        {
            MathOperationType.Addition => "+",
            MathOperationType.Subtraction => "-",
            MathOperationType.Multiplication => "×",
            MathOperationType.Division => "÷",
            _ => "?"
        };

        /// <summary>
        /// 題目顯示文字（含等號，不含答案）
        /// </summary>
        public string QuestionText =>
            !string.IsNullOrWhiteSpace(Expression)
                ? $"{Expression} ="
                : $"{Left} {OperatorSymbol} {Right} =";
    }
}
