namespace ERP.Web.Helpers
{
    /// <summary>
    /// 數字範圍解析輔助方法
    /// </summary>
    public static class MathNumberSpecHelper
    {
        /// <summary>
        /// 解析數字範圍或清單，例：1-9、2,3,5
        /// </summary>
        public static List<int> ParseNumberSpec(string? spec, int defaultMin = 1, int defaultMax = 9)
        {
            if (string.IsNullOrWhiteSpace(spec))
            {
                return Enumerable.Range(defaultMin, defaultMax - defaultMin + 1).ToList();
            }

            spec = spec.Trim();

            if (spec.Contains('-', StringComparison.Ordinal) && !spec.Contains(',', StringComparison.Ordinal))
            {
                var parts = spec.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (parts.Length == 2
                    && int.TryParse(parts[0], out var min)
                    && int.TryParse(parts[1], out var max))
                {
                    var start = Math.Min(min, max);
                    var end = Math.Max(min, max);
                    return Enumerable.Range(start, end - start + 1).ToList();
                }
            }

            var numbers = spec
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => int.TryParse(part, out var value) ? value : (int?)null)
                .Where(value => value.HasValue)
                .Select(value => value!.Value)
                .Distinct()
                .OrderBy(value => value)
                .ToList();

            return numbers.Count > 0
                ? numbers
                : Enumerable.Range(defaultMin, defaultMax - defaultMin + 1).ToList();
        }
    }
}
