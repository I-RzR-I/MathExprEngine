#if !BASELINE_ENGINE

using BenchmarkDotNet.Attributes;
using MathExprEngine;
using MathExprEngineBenchmarks.Enums;
using System.Collections.Generic;

namespace MathExprEngineBenchmarks
{
    [MemoryDiagnoser]
    public class ParameterBenchmarks
    {
        private string _expression;
        private IDictionary<string, object> _parameters;
        private MathRuleEngine _interpreter;
        private MathRuleEngine _tiered;
        private CompiledExpression _compiled;

        [Params(1, 100)]
        public int Count { get; set; }

        [Params(ParameterKind.Double, ParameterKind.String)]
        public ParameterKind Kind { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _expression = Corpus.CreateParameterExpression(Count, Kind, out _parameters);
            _interpreter = EngineFactory.CreateInterpreter();

            _tiered = EngineFactory.CreateDefault();
            EngineFactory.WarmToCompiledTier(_tiered, _expression, _parameters);

            _compiled = EngineFactory.CreateDefault().Compile(_expression);
        }

        [Benchmark(Baseline = true)]
        public double Interpreter_Evaluate()
        {
            return _interpreter.Evaluate(_expression, _parameters);
        }

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
    }
}

#endif
