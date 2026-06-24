namespace ERP.Web.Models
{
    /// <summary>
    /// 數學出題平台首頁（五種運算）
    /// </summary>
    public class MathPlatformIndexViewModel
    {
        public MathPaperInputViewModel Addition { get; set; } = CreateDefault(MathOperationType.Addition);

        public MathPaperInputViewModel Subtraction { get; set; } = CreateDefault(MathOperationType.Subtraction);

        public MathPaperInputViewModel Multiplication { get; set; } = CreateDefault(MathOperationType.Multiplication);

        public MathPaperInputViewModel Division { get; set; } = CreateDefault(MathOperationType.Division);

        public MathPaperInputViewModel Mixed { get; set; } = CreateDefault(MathOperationType.Mixed);

        /// <summary>
        /// 驗證失敗時要回到的分頁
        /// </summary>
        public MathOperationType ActiveTab { get; set; } = MathOperationType.Multiplication;

        public MathPaperInputViewModel GetByType(MathOperationType type) => type switch
        {
            MathOperationType.Addition => Addition,
            MathOperationType.Subtraction => Subtraction,
            MathOperationType.Multiplication => Multiplication,
            MathOperationType.Division => Division,
            MathOperationType.Mixed => Mixed,
            _ => Multiplication
        };

        public void SetByType(MathOperationType type, MathPaperInputViewModel input)
        {
            switch (type)
            {
                case MathOperationType.Addition:
                    Addition = input;
                    break;
                case MathOperationType.Subtraction:
                    Subtraction = input;
                    break;
                case MathOperationType.Multiplication:
                    Multiplication = input;
                    break;
                case MathOperationType.Division:
                    Division = input;
                    break;
                case MathOperationType.Mixed:
                    Mixed = input;
                    break;
            }
        }

        public static MathPlatformIndexViewModel FromInput(MathPaperInputViewModel input)
        {
            var page = new MathPlatformIndexViewModel
            {
                ActiveTab = input.OperationType
            };
            page.SetByType(input.OperationType, input);
            return page;
        }

        private static MathPaperInputViewModel CreateDefault(MathOperationType type)
        {
            var input = new MathPaperInputViewModel
            {
                OperationType = type,
                Shuffle = true
            };

            switch (type)
            {
                case MathOperationType.Addition:
                case MathOperationType.Subtraction:
                    input.LeftOperand = "1-99";
                    input.RightOperand = "1-99";
                    break;
                case MathOperationType.Multiplication:
                    input.LeftOperand = "1-9";
                    input.RightOperand = "1-9";
                    break;
                case MathOperationType.Division:
                    input.LeftOperand = "1-81";
                    input.RightOperand = "1-9";
                    break;
                case MathOperationType.Mixed:
                    input.LeftOperand = "1-9";
                    input.RightOperand = "1-9";
                    break;
            }

            return input;
        }
    }
}
