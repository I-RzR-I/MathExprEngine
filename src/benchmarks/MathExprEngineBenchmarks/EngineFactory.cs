using MathExprEngine;
using System;
using System.Collections.Generic;

namespace MathExprEngineBenchmarks
{
    public static class EngineFactory
    {
#if BASELINE_ENGINE
        private const string CompiledExpressionTypeName = "MathExprEngine.CompiledExpression";

#endif
        public static MathRuleEngine CreateInterpreter()
        {
#if BASELINE_ENGINE
            if (typeof(MathRuleEngine).Assembly.GetType(CompiledExpressionTypeName) != null)
                throw new InvalidOperationException(
                    "BASELINE_ENGINE build loaded an engine that contains " + CompiledExpressionTypeName
                    + "; the benchmark would not measure the pre-compiler baseline.");

            return new MathRuleEngine();
#else
            return new MathRuleEngine(new MathRuleEngineOptions { ExpressionCacheCapacity = 0 });
#endif
        }

#if !BASELINE_ENGINE
        public static MathRuleEngine CreateDefault()
        {
            return new MathRuleEngine(new MathRuleEngineOptions());
        }

        public static void WarmToCompiledTier(MathRuleEngine engine, string expression, IDictionary<string, object> parameters)
        {
            var threshold = new MathRuleEngineOptions().CompilationThreshold;
            for (var i = 0; i < threshold; i++)
                engine.Evaluate(expression, parameters);
        }

        public static MathRuleEngine CreateEager()
        {
            return new MathRuleEngine(new MathRuleEngineOptions { CompilationThreshold = 1 });
        }

        public static MathRuleEngine CreateCachedAst()
        {
            return new MathRuleEngine(new MathRuleEngineOptions { EnableCompilation = false });
        }
#endif
    }
}
