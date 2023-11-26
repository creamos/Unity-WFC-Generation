using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class AlphaStringComparer : IComparer<string>
{
    public int Compare (string x, string y)
    {
        /*
        var regex = new Regex(@"-?\d+");

        // retrieve first index
        var xRegexResult = regex.Match(x);
        var yRegexResult = regex.Match(y);


        // if both has an index, compare it
        if (xRegexResult.Success && yRegexResult.Success) {

            int res = int.Parse(xRegexResult.Groups[0].Value).CompareTo(int.Parse(yRegexResult.Groups[0].Value));

            if (res == 0) return x.CompareTo(y);
            else return res;
        }

        // otherwise return as string comparison
        return x.CompareTo(y);
        */

        return x == y ? 0 : OrderByAlphaNumeric(x, y).First() == x ? -1 : 1;
    }

    public static string[] OrderByAlphaNumeric (string a, string b)
    {
        string[] entries = {a,b};

        int max = entries
                .SelectMany(i => Regex.Matches(i, @"\d+")
                .Cast<Match>()
                .Select(m => (int?)m.Value.Length))
                .Max() ?? 0;

        return entries.OrderBy(i =>
            Regex.Replace(i, @"-?\d+", m => (m.Value[0] == '-' ? '0' : "") + m.Value.Trim('-').PadLeft(max, '0'))).ToArray();
    }

}