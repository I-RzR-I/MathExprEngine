using System.Linq;

namespace MathExprEngineTests.Helpers.Inputs
{
    internal static class ExpressionText
    {
        public static string FlatChain(int terms)
        {
            return string.Join("+", Enumerable.Repeat("1", terms));
        }

        public static string NestedNeg(int levels)
        {
            return string.Concat(Enumerable.Repeat("neg(", levels)) + "1" + new string(')', levels);
        }

        public static string NestedParentheses(int depth)
        {
            return new string('(', depth) + "1" + new string(')', depth);
        }

        public static string ZeroPadded(string head, int links)
        {
            return head + string.Concat(Enumerable.Repeat("+0", links));
        }

        public static string SumOfOnes(int count)
        {
            return "sum(" + string.Join(",", Enumerable.Repeat("1", count)) + ")";
        }
    }
}
