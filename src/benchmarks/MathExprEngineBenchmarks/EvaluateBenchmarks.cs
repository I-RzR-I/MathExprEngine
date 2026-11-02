using BenchmarkDotNet.Attributes;
using MathExprEngine;
using MathExprEngineBenchmarks.Enums;
using System.Collections.Generic;

namespace MathExprEngineBenchmarks
{
    [MemoryDiagnoser]
    [ProcessCount(3)]
    public class EvaluateBenchmarks
    {
        private string _expression;
        private IDictionary<string, object> _parameters;
        private MathRuleEngine _interpreter;
#if !BASELINE_ENGINE
        private MathRuleEngine _tiered;
        private MathRuleEngine _cachedAst;
        private CompiledExpression _compiled;
#endif

        [Params(ExpressionSize.Small, ExpressionSize.Medium, ExpressionSize.Large)]
        public ExpressionSize Size { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _expression = Corpus.GetExpression(Size);
            _parameters = Corpus.CreateParameters(Size);
            _interpreter = EngineFactory.CreateInterpreter();
#if !BASELINE_ENGINE
            _tiered = EngineFactory.CreateDefault();
            EngineFactory.WarmToCompiledTier(_tiered, _expression, _parameters);

            _cachedAst = EngineFactory.CreateCachedAst();
            _cachedAst.Evaluate(_expression, _parameters);

            _compiled = EngineFactory.CreateDefault().Compile(_expression);
#endif
        }

        [Benchmark(Baseline = true)]
        public double Interpreter_Evaluate()
        {
            return _interpreter.Evaluate(_expression, _parameters);
        }

#if !BASELINE_ENGINE
        [Benchmark]
        public double Cached_Evaluate()
        {
            return _tiered.Evaluate(_expression, _parameters);
        }

        [Benchmark]
        public double Compiled_Evaluate()
        {
            return _compiled.Evaluate(_parameters);
        }

        [Benchmark]
        public double CachedAst_Evaluate()
        {
            return _cachedAst.Evaluate(_expression, _parameters);
        }
#endif
    }
}
