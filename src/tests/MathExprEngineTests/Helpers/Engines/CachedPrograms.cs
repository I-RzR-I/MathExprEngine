using MathExprEngine;
using MathExprEngine.Compilation;

namespace MathExprEngineTests.Helpers.Engines
{
    internal static class CachedPrograms
    {
        public static ExpressionProgram Peek(MathRuleEngine engine, string expression)
        {
            ExpressionProgram program;
            Assert.IsTrue(engine.TryPeekCachedProgram(expression, out program), "Expected a cached program for '" + expression + "'.");
            Assert.IsNotNull(program);

            return program;
        }

        public static bool IsCached(MathRuleEngine engine, string expression)
        {
            ExpressionProgram program;
            return engine.TryPeekCachedProgram(expression, out program);
        }
    }
}
