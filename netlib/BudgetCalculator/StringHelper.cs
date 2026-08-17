using System.Text.RegularExpressions;

namespace BudgetCalculator
{
    internal static class StringHelper
    {
        public static string ReplaceBy(this string text, string matchStr, string replaceBy)
        {
            Match m = Regex.Match(text, matchStr);
            if (m.Success)
            {
                string a = m.Index > 0 ? text.Substring(0, m.Index) : "";
                string b = text.Substring(m.Index + m.Length);

                return $"{a}{replaceBy}{b}";
            }
            else
            {
                return text;
            }
        }
    }
}
