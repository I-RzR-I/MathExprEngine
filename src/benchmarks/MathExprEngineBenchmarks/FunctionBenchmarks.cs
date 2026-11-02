#if !BASELINE_ENGINE

using BenchmarkDotNet.Attributes;
using MathExprEngine;
using MathExprEngineBenchmarks.Enums;
using System;
using System.Collections.Generic;

namespace MathExprEngineBenchmarks
{
    [MemoryDiagnoser]
    public class FunctionBenchmarks
    {
        private const string BuiltInExpression = "max(a, b, c) + sqrt(pow(a, 2) + pow(b, 2)) - sum(a, b, c, 1, 2)";

        private const string CustomExpression = "clamp(a * 3, 0, 10) + avg(a, b, c) - avg(clamp(b, 1, 2), c)";

        private static readonly IDictionary<string, Func<double[], double>> CustomFunctions =
            new Dictionary<string, Func<double[], double>>
            {
                ["clamp"] = args => Math.Min(Math.Max(args[0], args[1]), args[2]),
                ["avg"] = Average
            };

        private string _expression;
        private IDictionary<string, object> _parameters;
        private MathRuleEngine _interpreter;
        private MathRuleEngine _tiered;
        private CompiledExpression _compiled;

        [Params(FunctionKind.BuiltIn, FunctionKind.Custom)]
        public FunctionKind Kind { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _expression = Kind == FunctionKind.Custom ? CustomExpression : BuiltInExpression;
            _parameters = new Dictionary<string, object> { ["a"] = 3.0, ["b"] = 4.0, ["c"] = 5.0 };

            _interpreter = EngineFactory.CreateInterpreter();
            _interpreter.RegisterFunctions(CustomFunctions);

            _tiered = EngineFactory.CreateDefault();
            _tiered.RegisterFunctions(CustomFunctions);
            EngineFactory.WarmToCompiledTier(_tiered, _expression, _parameters);

            var compiling = EngineFactory.CreateDefault();
            compiling.RegisterFunctions(CustomFunctions);
            _compiled = compiling.Compile(_expression);
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

        private static double Average(double[] args)
        {
            var total = 0.0;
            foreach (var value in args)
                total += value;

            return total / args.Length;
        }
    }
}

#endif
