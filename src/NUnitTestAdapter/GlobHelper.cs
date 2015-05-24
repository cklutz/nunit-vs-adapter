using System;
using System.Text.RegularExpressions;

namespace NUnit.VisualStudio.TestAdapter
{
    internal static class GlobHelper
    {
        internal static bool IsMatch(string str, string pattern)
        {
            pattern = Regex.Escape(pattern);
            pattern = pattern.Replace(@"\*", ".*");
            pattern = pattern.Replace(@"\?", ".");
            pattern = "^" + pattern + "$";

            var regex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            return regex.IsMatch(str);
        }
    }
}