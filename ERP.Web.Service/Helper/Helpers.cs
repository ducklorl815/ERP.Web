namespace ERP.Web.Service.Helper
{
    public class Helpers
    {
        public static async Task<string> GetLetterSequence(int number)
        {
            string result = string.Empty;
            int baseNumber = 26;

            while (number > 0)
            {
                int remainder = (number - 1) % baseNumber;
                char letter = (char)('A' + remainder);
                result = letter + result;
                number = (number - 1) / baseNumber;
            }

            return result;
        }
    }
}
