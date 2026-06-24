using ERP.Web.Models;

namespace ERP.Web.Helpers
{
    /// <summary>
    /// 加減乘除考卷出題輔助方法
    /// </summary>
    public static class MathPaperHelper
    {
        public static MathPaperViewModel BuildPaper(MathPaperInputViewModel input)
        {
            var questions = BuildQuestions(input);

            return new MathPaperViewModel
            {
                OperationType = input.OperationType,
                OperationLabel = GetOperationLabel(input.OperationType),
                Title = string.IsNullOrWhiteSpace(input.Title)
                    ? GetDefaultTitle(input.OperationType)
                    : input.Title.Trim(),
                LeftOperandLabel = GetLeftOperandLabel(input.OperationType),
                RightOperandLabel = GetRightOperandLabel(input.OperationType),
                LeftOperandSpec = input.LeftOperand,
                RightOperandSpec = input.RightOperand,
                CreatedAt = DateTimeOffset.Now,
                QuestionCount = questions.Count,
                Questions = questions
            };
        }

        public static List<MathQuestionViewModel> BuildQuestions(MathPaperInputViewModel input)
        {
            var leftNumbers = MathNumberSpecHelper.ParseNumberSpec(input.LeftOperand);
            var rightNumbers = MathNumberSpecHelper.ParseNumberSpec(input.RightOperand);

            var questions = input.OperationType switch
            {
                MathOperationType.Addition => BuildAddition(leftNumbers, rightNumbers),
                MathOperationType.Subtraction => BuildSubtraction(leftNumbers, rightNumbers),
                MathOperationType.Multiplication => BuildMultiplication(leftNumbers, rightNumbers),
                MathOperationType.Division => BuildDivision(leftNumbers, rightNumbers),
                MathOperationType.Mixed => BuildMixed(input, leftNumbers, rightNumbers),
                _ => new List<MathQuestionViewModel>()
            };

            return ApplyShuffleAndTake(questions, input);
        }

        private static List<MathQuestionViewModel> BuildAddition(IEnumerable<int> leftNumbers, IEnumerable<int> rightNumbers)
        {
            var questions = new List<MathQuestionViewModel>();
            foreach (var left in leftNumbers)
            {
                foreach (var right in rightNumbers)
                {
                    questions.Add(CreateQuestion(MathOperationType.Addition, left, right, left + right));
                }
            }

            return questions;
        }

        private static List<MathQuestionViewModel> BuildSubtraction(IEnumerable<int> leftNumbers, IEnumerable<int> rightNumbers)
        {
            var questions = new List<MathQuestionViewModel>();
            foreach (var left in leftNumbers)
            {
                foreach (var right in rightNumbers)
                {
                    // 減法只出現非負答案
                    if (left < right)
                    {
                        continue;
                    }

                    questions.Add(CreateQuestion(MathOperationType.Subtraction, left, right, left - right));
                }
            }

            return questions;
        }

        private static List<MathQuestionViewModel> BuildMultiplication(IEnumerable<int> leftNumbers, IEnumerable<int> rightNumbers)
        {
            var questions = new List<MathQuestionViewModel>();
            foreach (var left in leftNumbers)
            {
                foreach (var right in rightNumbers)
                {
                    questions.Add(CreateQuestion(MathOperationType.Multiplication, left, right, left * right));
                }
            }

            return questions;
        }

        private static List<MathQuestionViewModel> BuildDivision(IEnumerable<int> leftNumbers, IEnumerable<int> rightNumbers)
        {
            var questions = new List<MathQuestionViewModel>();
            foreach (var dividend in leftNumbers)
            {
                foreach (var divisor in rightNumbers)
                {
                    if (divisor == 0 || dividend % divisor != 0)
                    {
                        continue;
                    }

                    questions.Add(CreateQuestion(MathOperationType.Division, dividend, divisor, dividend / divisor));
                }
            }

            return questions;
        }

        private static List<MathQuestionViewModel> BuildMixed(
            MathPaperInputViewModel input,
            List<int> leftNumbers,
            List<int> rightNumbers)
        {
            // 你要的「綜合」：一題一個多步算式（先乘除後加減），且結果必須為整數
            var enabledOperations = GetEnabledMixedOperations(input);
            if (enabledOperations.Count == 0)
            {
                return new List<MathQuestionViewModel>();
            }

            var termCount = Math.Clamp(input.MixedTermCount, 3, 6);
            var operatorCount = termCount - 1;

            // 取用數字來源：左右範圍合併（去重），避免綜合題太偏某一側
            var numberPool = leftNumbers
                .Concat(rightNumbers)
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (numberPool.Count == 0)
            {
                return new List<MathQuestionViewModel>();
            }

            var rng = input.Seed.HasValue ? new Random(input.Seed.Value) : new Random();
            var targetCount = input.QuestionCount > 0 ? input.QuestionCount : 50;

            var questions = new List<MathQuestionViewModel>(targetCount);
            const int maxAttemptsPerQuestion = 5000;

            for (var qIndex = 0; qIndex < targetCount; qIndex++)
            {
                var created = false;
                for (var attempt = 0; attempt < maxAttemptsPerQuestion; attempt++)
                {
                    var operands = new int[termCount];
                    for (var i = 0; i < termCount; i++)
                    {
                        operands[i] = numberPool[rng.Next(numberPool.Count)];
                    }

                    var ops = new MathOperationType[operatorCount];
                    for (var i = 0; i < operatorCount; i++)
                    {
                        ops[i] = enabledOperations[rng.Next(enabledOperations.Count)];
                    }

                    if (!TryEvaluateMixedExpression(operands, ops, out var answer, out var expression))
                    {
                        continue;
                    }

                    questions.Add(new MathQuestionViewModel
                    {
                        Operation = MathOperationType.Mixed,
                        Expression = expression,
                        Answer = answer
                    });

                    created = true;
                    break;
                }

                if (!created)
                {
                    // 代表在目前條件下太難產生整數題，直接停止（讓上層顯示「無法產生」）
                    break;
                }
            }

            return questions;
        }

        /// <summary>
        /// 綜合題計算：遵守先乘除後加減（標準優先序），且所有除法須整除，最後結果為整數。
        /// </summary>
        private static bool TryEvaluateMixedExpression(
            IReadOnlyList<int> operands,
            IReadOnlyList<MathOperationType> ops,
            out int answer,
            out string expression)
        {
            answer = 0;
            expression = string.Empty;

            if (operands.Count < 3 || ops.Count != operands.Count - 1)
            {
                return false;
            }

            // 先做乘除縮約（左到右）
            var reducedOperands = new List<int>();
            var reducedOps = new List<MathOperationType>();

            var current = operands[0];
            for (var i = 0; i < ops.Count; i++)
            {
                var op = ops[i];
                var next = operands[i + 1];

                if (op == MathOperationType.Multiplication || op == MathOperationType.Division)
                {
                    if (op == MathOperationType.Multiplication)
                    {
                        current = current * next;
                    }
                    else
                    {
                        if (next == 0 || current % next != 0)
                        {
                            return false;
                        }

                        current = current / next;
                    }
                }
                else if (op == MathOperationType.Addition || op == MathOperationType.Subtraction)
                {
                    reducedOperands.Add(current);
                    reducedOps.Add(op);
                    current = next;
                }
                else
                {
                    return false;
                }
            }
            reducedOperands.Add(current);

            // 再做加減（左到右）
            var result = reducedOperands[0];
            for (var i = 0; i < reducedOps.Count; i++)
            {
                var op = reducedOps[i];
                var next = reducedOperands[i + 1];
                result = op == MathOperationType.Addition ? result + next : result - next;
            }

            // 目前需求：不允許出現負數答案
            if (result < 0)
            {
                return false;
            }

            answer = result;

            // 組字串：例 7 + 5 × 3 ÷ 6
            var parts = new List<string>(operands.Count + ops.Count);
            parts.Add(operands[0].ToString());
            for (var i = 0; i < ops.Count; i++)
            {
                parts.Add(ops[i] switch
                {
                    MathOperationType.Addition => "+",
                    MathOperationType.Subtraction => "-",
                    MathOperationType.Multiplication => "×",
                    MathOperationType.Division => "÷",
                    _ => "?"
                });
                parts.Add(operands[i + 1].ToString());
            }
            expression = string.Join(" ", parts);

            return true;
        }

        private static List<MathQuestionViewModel> ApplyShuffleAndTake(
            List<MathQuestionViewModel> questions,
            MathPaperInputViewModel input)
        {
            if (input.Shuffle)
            {
                var rng = input.Seed.HasValue ? new Random(input.Seed.Value) : new Random();
                Shuffle(questions, rng);
            }
            else
            {
                questions = SortByDescendingOperands(questions);
            }

            if (input.QuestionCount > 0 && input.QuestionCount < questions.Count)
            {
                questions = questions.Take(input.QuestionCount).ToList();
            }

            return questions;
        }

        /// <summary>
        /// 未勾選隨機排序時，依左側、右側數字降冪排列（綜合題則依答案、算式文字）。
        /// </summary>
        private static List<MathQuestionViewModel> SortByDescendingOperands(List<MathQuestionViewModel> questions)
        {
            return questions
                .OrderByDescending(q => q.Left)
                .ThenByDescending(q => q.Right)
                .ThenByDescending(q => q.Answer)
                .ThenByDescending(q => q.Expression)
                .ToList();
        }

        private static List<MathOperationType> GetEnabledMixedOperations(MathPaperInputViewModel input)
        {
            var operations = new List<MathOperationType>();
            if (input.IncludeAddition) operations.Add(MathOperationType.Addition);
            if (input.IncludeSubtraction) operations.Add(MathOperationType.Subtraction);
            if (input.IncludeMultiplication) operations.Add(MathOperationType.Multiplication);
            if (input.IncludeDivision) operations.Add(MathOperationType.Division);
            return operations;
        }

        private static MathQuestionViewModel CreateQuestion(
            MathOperationType operation,
            int left,
            int right,
            int answer)
        {
            return new MathQuestionViewModel
            {
                Operation = operation,
                Left = left,
                Right = right,
                Answer = answer
            };
        }

        private static void Shuffle<T>(IList<T> list, Random rng)
        {
            for (var i = list.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        public static string GetOperationLabel(MathOperationType type) => type switch
        {
            MathOperationType.Addition => "加法",
            MathOperationType.Subtraction => "減法",
            MathOperationType.Multiplication => "乘法",
            MathOperationType.Division => "除法",
            MathOperationType.Mixed => "加減乘除綜合",
            _ => "數學"
        };

        public static string GetDefaultTitle(MathOperationType type) => type switch
        {
            MathOperationType.Addition => "加法考卷",
            MathOperationType.Subtraction => "減法考卷",
            MathOperationType.Multiplication => "99 乘法考卷",
            MathOperationType.Division => "除法考卷",
            MathOperationType.Mixed => "四則運算綜合考卷",
            _ => "數學考卷"
        };

        public static string GetLeftOperandLabel(MathOperationType type) => type switch
        {
            MathOperationType.Addition => "加數（左）",
            MathOperationType.Subtraction => "被減數",
            MathOperationType.Multiplication => "被乘數",
            MathOperationType.Division => "被除數",
            MathOperationType.Mixed => "運算數（左）",
            _ => "左側數字"
        };

        public static string GetRightOperandLabel(MathOperationType type) => type switch
        {
            MathOperationType.Addition => "加數（右）",
            MathOperationType.Subtraction => "減數",
            MathOperationType.Multiplication => "乘數",
            MathOperationType.Division => "除數",
            MathOperationType.Mixed => "運算數（右）",
            _ => "右側數字"
        };
    }
}
