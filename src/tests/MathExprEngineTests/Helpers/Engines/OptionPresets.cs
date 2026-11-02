using MathExprEngine;

namespace MathExprEngineTests.Helpers.Engines
{
    internal static class OptionPresets
    {
        public static void InterpreterOnly(MathRuleEngineOptions options)
        {
            options.EnableCompilation = false;
            options.ExpressionCacheCapacity = 0;
        }

        public static void AllowNonFinite(MathRuleEngineOptions options)
        {
            options.AllowNonFiniteResults = true;
        }
    }
}
