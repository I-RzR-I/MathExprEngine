using System.Collections.Generic;
using System.Text.RegularExpressions;
using MathExprEngineTests.Models;

namespace MathExprEngineTests.Helpers
{
    internal static class ParseXData
    {
        private static readonly Regex _regex = new Regex(@"\{\[(\d+)\];\[(\d+)\]\}");

        public static IEnumerable<ParserXDataResult> Parse(string rasExpression)
        {
            var result = new List<ParserXDataResult>();

            var matches = _regex.Matches(rasExpression);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var res = new ParserXDataResult
                    {
                        RawVariable = match.Value,
                        LineColVariable = match.Value.Replace("{", "").Replace("}", ""),
                        Line = match.Groups[1].Value,
                        Col = match.Groups[2].Value
                    };

                    result.Add(res);
                }
            }

            return result;
        }
    }
}